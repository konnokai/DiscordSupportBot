using StackExchange.Redis;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DiscordSupportBot.Interaction.Utility
{
    public class UtilityService : IInteractionService
    {
        private const string SpotRatePageUrl = "https://www.tcb-bank.com.tw/personal-banking/deposit-exchange/exchange-rate/spot";
        private const string SpotRateApiUrl = "https://www.tcb-bank.com.tw/api/client/ForeignExchange/GetSpotForeignExchange";
        private const string SpotRateRedisKey = "SupportBot:Utility:CnySellSpotRate";

        private static readonly HttpClient SpotRateHttpClient = new(new HttpClientHandler
        {
            UseCookies = false
        });

        private static readonly Regex FormTokenRegex = new(
            """<input(?=[^>]*\bname=["']__RequestVerificationToken["'])[^>]*\bvalue=["']([^"']+)["'][^>]*>""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly Timer _spotRateRefreshTimer;
        private volatile SpotRateSnapshot _spotRateSnapshot;
        private int _initialized;

        public UtilityService(DiscordSocketClient discordSocketClient)
        {
            discordSocketClient.ButtonExecuted += async (btn) =>
            {
                if (btn.Data.CustomId == "sub")
                {
                    await btn.RespondAsync("然而並沒有甚麼鳥用", ephemeral: true);
                }
            };

            _spotRateRefreshTimer = new Timer(
                (_state) => _ = RefreshCnySellSpotRateAsync(),
                null,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// 先載入 Redis 內最後成功的匯率，再立即刷新一次並於之後每小時刷新。
        /// </summary>
        public async Task InitializeAsync()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            await LoadCachedCnySellSpotRateAsync();
            _spotRateRefreshTimer.Change(TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        internal bool TryGetCnySellSpotRate(out SpotRateSnapshot spotRate)
        {
            spotRate = _spotRateSnapshot;
            return spotRate != null;
        }

        private async Task RefreshCnySellSpotRateAsync()
        {
            try
            {
                var spotRate = await FetchCnySellSpotRateAsync();
                _spotRateSnapshot = spotRate;
                await CacheCnySellSpotRateAsync(spotRate);
                Log.Info($"人民幣賣出即期匯率已更新: {spotRate.ExchangeRate} ({spotRate.UpdateTime})");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RefreshCnySellSpotRateAsync");
            }
        }

        private async Task LoadCachedCnySellSpotRateAsync()
        {
            try
            {
                var value = await RedisConnection.RedisDb.StringGetAsync(SpotRateRedisKey);
                if (!value.HasValue)
                    return;

                var spotRate = System.Text.Json.JsonSerializer.Deserialize<SpotRateSnapshot>(value.ToString());
                if (spotRate == null || spotRate.ExchangeRate <= 0 || string.IsNullOrWhiteSpace(spotRate.UpdateTime))
                    return;

                _spotRateSnapshot = spotRate;
                Log.Info($"已從 Redis 載入人民幣賣出即期匯率: {spotRate.ExchangeRate} ({spotRate.UpdateTime})");
            }
            catch (System.Text.Json.JsonException ex)
            {
                Log.Error(ex, "LoadCachedCnySellSpotRateAsync-Json");
            }
            catch (RedisException ex)
            {
                Log.Error(ex, "LoadCachedCnySellSpotRateAsync-Redis");
            }
        }

        private static async Task CacheCnySellSpotRateAsync(SpotRateSnapshot spotRate)
        {
            try
            {
                await RedisConnection.RedisDb.StringSetAsync(
                    SpotRateRedisKey,
                    System.Text.Json.JsonSerializer.Serialize(spotRate));
            }
            catch (RedisException ex)
            {
                Log.Error(ex, "CacheCnySellSpotRateAsync");
            }
        }

        /// <summary>
        /// 取得合作金庫人民幣銀行賣出即期匯率。防偽 Cookie Token 與 HTML Form Token 必須取自同一次頁面請求。
        /// </summary>
        private static async Task<SpotRateSnapshot> FetchCnySellSpotRateAsync(CancellationToken cancellationToken = default)
        {
            using var pageResponse = await SpotRateHttpClient.GetAsync(SpotRatePageUrl, cancellationToken);
            pageResponse.EnsureSuccessStatusCode();

            var html = await pageResponse.Content.ReadAsStringAsync(cancellationToken);
            var tokenMatch = FormTokenRegex.Match(html);
            if (!tokenMatch.Success)
                throw new InvalidOperationException("找不到合作金庫匯率頁面的防偽表單 Token");

            var formToken = WebUtility.HtmlDecode(tokenMatch.Groups[1].Value);

            if (!pageResponse.Headers.TryGetValues("Set-Cookie", out var setCookies))
                throw new InvalidOperationException("合作金庫匯率頁面未回傳 Cookie");

            var setCookie = setCookies.FirstOrDefault((value) =>
                value.StartsWith("__RequestVerificationToken=", StringComparison.Ordinal));
            if (setCookie == null)
                throw new InvalidOperationException("找不到合作金庫匯率頁面的防偽 Cookie");

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = formToken,
                ["date"] = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["time"] = "1"
            });
            content.Headers.ContentType.CharSet = "UTF-8";

            using var request = new HttpRequestMessage(HttpMethod.Post, SpotRateApiUrl)
            {
                Content = content
            };
            request.Headers.Add("Cookie", setCookie.Split(';', 2)[0]);

            using var response = await SpotRateHttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseCnySellSpotRate(json);
        }

        internal static SpotRateSnapshot ParseCnySellSpotRate(string json)
        {
            using var document = JsonDocument.Parse(json);
            var updateTime = document.RootElement.GetProperty("updateTime").GetString();
            if (string.IsNullOrWhiteSpace(updateTime))
                throw new InvalidOperationException("合作金庫回傳的匯率資料時間格式錯誤");

            var promptExchange = document.RootElement
                .GetProperty("result")
                .EnumerateArray()
                .Single((rate) =>
                    rate.GetProperty("Currency").GetString() == "CNY" &&
                    rate.GetProperty("Type").GetString() == "賣出")
                .GetProperty("PromptExchange")
                .GetString();

            if (!decimal.TryParse(promptExchange, NumberStyles.Number, CultureInfo.InvariantCulture, out var exchangeRate))
                throw new InvalidOperationException("合作金庫回傳的人民幣賣出即期匯率格式錯誤");

            return new SpotRateSnapshot(exchangeRate, updateTime);
        }

        /// <summary>
        /// 依支付寶海外支付、人民幣匯率與信用卡手續費規則計算最終台幣支付金額。
        /// </summary>
        internal static AlipayPaymentCalculation CalculateAlipayPayment(decimal cnyAmount, decimal exchangeRate)
        {
            if (cnyAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(cnyAmount));
            if (exchangeRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(exchangeRate));

            var hasOverseasFee = cnyAmount >= 200m;
            var overseasFee = hasOverseasFee ? cnyAmount * 0.03m : 0m;
            var cnyAmountAfterOverseasFee = cnyAmount + overseasFee;
            var roundedExchangeRate = Math.Ceiling(exchangeRate * 10m) / 10m;
            var twdAmountBeforeCreditCardFee = cnyAmountAfterOverseasFee * roundedExchangeRate;
            var creditCardFee = twdAmountBeforeCreditCardFee * 0.015m;
            var twdAmountAfterCreditCardFee = twdAmountBeforeCreditCardFee + creditCardFee;
            var finalTwdAmount = Math.Ceiling(twdAmountAfterCreditCardFee / 100m) * 100m;

            return new AlipayPaymentCalculation(
                cnyAmount,
                hasOverseasFee,
                overseasFee,
                cnyAmountAfterOverseasFee,
                exchangeRate,
                roundedExchangeRate,
                twdAmountBeforeCreditCardFee,
                creditCardFee,
                twdAmountAfterCreditCardFee,
                finalTwdAmount);
        }

        internal sealed record AlipayPaymentCalculation(
            decimal CnyAmount,
            bool HasOverseasFee,
            decimal OverseasFee,
            decimal CnyAmountAfterOverseasFee,
            decimal ExchangeRate,
            decimal RoundedExchangeRate,
            decimal TwdAmountBeforeCreditCardFee,
            decimal CreditCardFee,
            decimal TwdAmountAfterCreditCardFee,
            decimal FinalTwdAmount);

        internal sealed record SpotRateSnapshot(decimal ExchangeRate, string UpdateTime);
    }
}
