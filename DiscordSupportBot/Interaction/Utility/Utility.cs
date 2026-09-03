using Discord.Interactions;
using System.Globalization;

namespace DiscordSupportBot.Interaction.Utility
{
    [Group("utility", "工具")]
    public class Utility : TopLevelModule<UtilityService>
    {
        private readonly DiscordSocketClient _client;

        public Utility(DiscordSocketClient client)
        {
            _client = client;
        }

        [SlashCommand("ping", "延遲檢測")]
        public async Task PingAsync()
        {
            await Context.Interaction.SendConfirmAsync(":ping_pong: " + _client.Latency.ToString() + "ms");
        }

        [SlashCommand("invite", "取得邀請連結")]
        public async Task InviteAsync()
        {
            await Context.Interaction.SendConfirmAsync("<https://discordapp.com/api/oauth2/authorize?client_id=" + _client.CurrentUser.Id + "&permissions=362136726551&scope=bot%20applications.commands>", ephemeral: true);
        }

        [SlashCommand("status", "顯示機器人目前的狀態")]
        public async Task StatusAsync()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor();
            embedBuilder.WithTitle("輔助小幫手");
#if DEBUG
            embedBuilder.Title += " (測試版)";
#endif

            embedBuilder.WithDescription($"建置版本 {Program.VERSION}");
            embedBuilder.AddField("作者", "孤之界#1121", true);
            embedBuilder.AddField("擁有者", $"{Program.ApplicatonOwner.Username}#{Program.ApplicatonOwner.Discriminator}", true);
            embedBuilder.AddField("狀態", $"伺服器 {_client.Guilds.Count}\n服務成員數 {_client.Guilds.Sum((x) => x.MemberCount)}", false);
            embedBuilder.AddField("上線時間", $"{Program.StopWatch.Elapsed:d\\天\\ hh\\:mm\\:ss}", false);

            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("sub", "訂閱按鈕")]
        public async Task SubAsync()
        {
            await RespondAsync("點我訂閱", components: new ComponentBuilder().WithButton("訂閱", "sub", ButtonStyle.Danger).Build());
        }

        [SlashCommand("alipay-payment", "以合作金庫匯率來計算支付寶付款換算後的台幣信用卡支付金額")]
        public async Task AlipayPaymentAsync([Summary("amount", "支付寶顯示的人民幣付款金額")] double amount)
        {
            if (!double.IsFinite(amount) || amount <= 0)
            {
                await Context.Interaction.SendErrorAsync("付款金額必須大於 0");
                return;
            }

            decimal cnyAmount;
            try
            {
                cnyAmount = Convert.ToDecimal(amount);
            }
            catch (OverflowException)
            {
                await Context.Interaction.SendErrorAsync("付款金額過大");
                return;
            }

            if (!_service.TryGetCnySellSpotRate(out var spotRate))
            {
                await Context.Interaction.SendErrorAsync("人民幣匯率尚未成功更新，請稍後再試");
                return;
            }

            var calculation = UtilityService.CalculateAlipayPayment(cnyAmount, spotRate.ExchangeRate);
            var overseasFeeProcess = calculation.HasOverseasFee
                ? $"`¥{FormatAmount(calculation.CnyAmount)} × 3% = ¥{FormatAmount(calculation.OverseasFee)}`\n" +
                  $"`¥{FormatAmount(calculation.CnyAmount)} + ¥{FormatAmount(calculation.OverseasFee)} = ¥{FormatAmount(calculation.CnyAmountAfterOverseasFee)}`"
                : $"未滿人民幣 200 元，不收取 3% 海外支付手續費\n" +
                  $"`¥{FormatAmount(calculation.CnyAmount)} × 1 = ¥{FormatAmount(calculation.CnyAmountAfterOverseasFee)}`";

            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithTitle("支付寶付款換算")
                .AddField("1. 支付寶付款金額", $"`¥{FormatAmount(calculation.CnyAmount)}`")
                .AddField("2. 海外支付手續費", overseasFeeProcess)
                .AddField("3. 人民幣匯率",
                    $"合作金庫銀行賣出即期：`{FormatAmount(calculation.ExchangeRate)}`\n" +
                    $"{spotRate.UpdateTime}\n" +
                    $"`{FormatAmount(calculation.ExchangeRate)} → 無條件進位至小數第 1 位 → {FormatAmount(calculation.RoundedExchangeRate)}`")
                .AddField("4. 換算台幣",
                    $"`¥{FormatAmount(calculation.CnyAmountAfterOverseasFee)} × {FormatAmount(calculation.RoundedExchangeRate)} = NT${FormatAmount(calculation.TwdAmountBeforeCreditCardFee)}`")
                .AddField("5. 信用卡支付手續費",
                    $"`NT${FormatAmount(calculation.TwdAmountBeforeCreditCardFee)} × 1.5% = NT${FormatAmount(calculation.CreditCardFee)}`\n" +
                    $"`NT${FormatAmount(calculation.TwdAmountBeforeCreditCardFee)} + NT${FormatAmount(calculation.CreditCardFee)} = NT${FormatAmount(calculation.TwdAmountAfterCreditCardFee)}`")
                .AddField("6. 最終支付金額",
                    $"`NT${FormatAmount(calculation.TwdAmountAfterCreditCardFee)} → 無條件進位至百位數 → NT${FormatAmount(calculation.FinalTwdAmount)}`\n" +
                    $"**NT${FormatAmount(calculation.FinalTwdAmount)}**");

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        private static string FormatAmount(decimal value)
        {
            return value.ToString("#,0.############################", CultureInfo.InvariantCulture);
        }
    }
}
