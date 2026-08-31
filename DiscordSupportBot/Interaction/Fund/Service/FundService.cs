using Discord.Interactions;
using StackExchange.Redis;
using System.Reflection;

namespace DiscordSupportBot.Interaction.Fund.Service
{
    public class FundService : IInteractionService
    {
        private const string AddOneCustomIdPrefix = "fund-add-one:";
        private const string AddOneCooldownKeyPrefix = "SupportBot:Fund:AddOneCooldown";

        public enum FundType
        {
            [ChoiceDisplay("說謊")]
            Lying,
            [ChoiceDisplay("暈船")]
            Dizzy,
            [ChoiceDisplay("色狗")]
            HentaiDog,
            [ChoiceDisplay("渣男")]
            FuckBoy,
            [ChoiceDisplay("抖M")]
            Masochism,
            [ChoiceDisplay("小丑")]
            Clown,
            [ChoiceDisplay("爛笑話")]
            BadJoke,
            [ChoiceDisplay("炸寢")]
            SleepBomb,
            [ChoiceDisplay("怪人")]
            Freak,
            [ChoiceDisplay("錯字")]
            Typo,
            [ChoiceDisplay("作夢")]
            Dreaming,
            [ChoiceDisplay("藏圖")]
            HiddenImage,
        }

        private readonly DiscordSocketClient _client;
        // ponytail: one lock keeps message edits ordered; split per message only if this becomes a bottleneck.
        private readonly SemaphoreSlim _addOneMessageLock = new(1, 1);

        internal sealed record FundAddResult(
            FundType FundType,
            ulong ExecuteUserId,
            ulong RecipientUserId,
            long IncrementAmount,
            long PreviousAmount,
            long NewAmount,
            long? PreviousRank,
            long? NewRank,
            bool IsSunday,
            bool WasOwnerRedirected)
        {
            public bool RankImproved => PreviousRank.HasValue && NewRank.HasValue && NewRank < PreviousRank;
        }

        public FundService(DiscordSocketClient client)
        {
            _client = client;

            _client.ModalSubmitted += _client_ModalSubmitted;
            _client.ButtonExecuted += HandleAddOneButtonAsync;
        }

        private async Task _client_ModalSubmitted(SocketModal arg)
        {
            if (arg.HasResponded)
                return;

            if (!arg.Data.CustomId.StartsWith("add_lying_fund"))
                return;

            await arg.DeferAsync(false);

            var guildId = ulong.Parse(arg.Data.CustomId.Split(':')[1]);
            var targetUserId = ulong.Parse(arg.Data.CustomId.Split(':')[2]);
            var fundType = Enum.Parse<FundType>(arg.Data.Components
                .First(x => x.CustomId == "select_fund_type").Values.First());
            var jumpUrl = arg.Data.Components.First((x) => x.CustomId == "jump_url").Value;

            try
            {
                await AddFundAndRespondAsync(
                    arg,
                    fundType,
                    guildId,
                    arg.Channel.Id,
                    arg.User.Id,
                    targetUserId,
                    $"[點我回原訊息]({jumpUrl})\n\n");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"FundService-ModalSubmitted: {guildId} | {targetUserId} | {fundType}");
                await arg.SendErrorAsync($"處理過程發生錯誤: {ex.Message}", true);
            }
        }

        private async Task HandleAddOneButtonAsync(SocketMessageComponent arg)
        {
            if (arg.HasResponded || !arg.Data.CustomId.StartsWith(AddOneCustomIdPrefix, StringComparison.Ordinal))
                return;

            await arg.DeferAsync(false);

            try
            {
                if (arg.GuildId == null || !TryParseAddOneCustomId(arg.Data.CustomId, out var fundType, out var targetUserId, out var cooldownScopeId))
                {
                    await arg.SendErrorAsync("基金按鈕資料無效", true);
                    return;
                }

                var cooldownKey = GetAddOneCooldownKey(cooldownScopeId == 0 ? arg.Message.Id : cooldownScopeId, arg.User.Id);
                var canUseButton = await RedisConnection.RedisDb.StringSetAsync(
                    cooldownKey,
                    1,
                    expiry: TimeSpan.FromHours(1),
                    when: When.NotExists);
                if (!canUseButton)
                {
                    await arg.SendErrorAsync("無法連續使用此按鈕", true);
                    return;
                }

                await _addOneMessageLock.WaitAsync();
                try
                {
                    var wasOwnerRedirected = CheckIsAddOwner(fundType, arg.GuildId.Value, arg.User.Id, targetUserId, out var needAddUserId);
                    var result = await AddFundAsync(fundType, arg.GuildId.Value, arg.Channel.Id, arg.User.Id, needAddUserId, wasOwnerRedirected);
                    await AppendFundResultAsync(arg, result);
                }
                finally
                {
                    _addOneMessageLock.Release();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"FundService-AddOneButton: {arg.GuildId} | {arg.User.Id} | {arg.Data.CustomId}");
                await arg.SendErrorAsync($"處理過程發生錯誤: {ex.Message}", true);
            }
        }

        private async Task AppendFundResultAsync(SocketMessageComponent interaction, FundAddResult result)
        {
            var channel = interaction.InteractionChannel ?? throw new InvalidOperationException("無法取得基金訊息頻道");
            var originalMessage = await channel.GetMessageAsync(interaction.Message.Id) as IUserMessage
                ?? throw new InvalidOperationException("無法取得基金原始訊息");
            var originalEmbed = originalMessage.Embeds.FirstOrDefault();
            var title = originalEmbed?.Title ?? FormatFundTitle(result.FundType, result.RecipientUserId);
            var description = AppendFundDescription(originalEmbed?.Description ?? string.Empty, title, result);
            description = description[^Math.Min(description.Length, EmbedBuilder.MaxDescriptionLength)..];

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description);

            if (description.Contains("\n\n🏆 排名變動：", StringComparison.Ordinal))
                embed.WithColor(Color.Gold);
            else
                embed.WithOkColor();

            await originalMessage.ModifyAsync(properties => properties.Embed = embed.Build());
        }

        internal static async Task AddFundAndRespondAsync(
            IDiscordInteraction interaction,
            FundType fundType,
            ulong guildId,
            ulong channelId,
            ulong executeUserId,
            ulong targetUserId,
            string messagePrefix = "")
        {
            var cooldownScopeId = interaction.Id;
            await RedisConnection.RedisDb.StringSetAsync(
                GetAddOneCooldownKey(cooldownScopeId, executeUserId),
                1,
                expiry: TimeSpan.FromHours(1),
                when: When.NotExists);

            var wasOwnerRedirected = CheckIsAddOwner(fundType, guildId, executeUserId, targetUserId, out var needAddUserId);
            var result = await AddFundAsync(fundType, guildId, channelId, executeUserId, needAddUserId, wasOwnerRedirected);
            var description = BuildFundDescription(
                string.IsNullOrWhiteSpace(messagePrefix)
                    ? FormatTransaction(result, false)
                    : $"{messagePrefix.TrimEnd()}\n\n{FormatTransaction(result, false)}",
                1,
                result.IncrementAmount,
                result.PreviousAmount,
                result.NewAmount,
                result.RankImproved ? result.PreviousRank + 1 : null,
                result.RankImproved ? result.NewRank + 1 : null);

            var components = new ComponentBuilder()
                .WithButton("讓他飛", $"{AddOneCustomIdPrefix}{(int)fundType}:{targetUserId}:{cooldownScopeId}", ButtonStyle.Success)
                .Build();

            var embed = new EmbedBuilder()
                .WithTitle(FormatFundTitle(fundType, needAddUserId))
                .WithDescription(description);

            if (result.RankImproved)
                embed.WithColor(Color.Gold);
            else
                embed.WithOkColor();

            await interaction.FollowupAsync(embed: embed.Build(), components: components);
        }

        internal static string FormatFundTitle(FundType fundType, ulong recipientUserId)
            => $"💰 {GetFundTypeName(fundType)}基金入帳｜<@{recipientUserId}>";

        internal static string AppendFundDescription(string description, string title, FundAddResult result)
        {
            var summaryIndex = description.IndexOf("\n\n本次入帳：", StringComparison.Ordinal);
            var rankIndex = description.IndexOf("\n\n🏆 排名變動：", StringComparison.Ordinal);
            var detailEnd = rankIndex >= 0 ? rankIndex : summaryIndex;

            // Old live messages have no structured summary. Keep them and start the new format from this click.
            if (summaryIndex < 0)
            {
                var details = string.IsNullOrWhiteSpace(description)
                    ? FormatTransaction(result, !title.Contains($"<@{result.RecipientUserId}>", StringComparison.Ordinal))
                    : $"{description.TrimEnd()}\n\n{FormatTransaction(result, !title.Contains($"<@{result.RecipientUserId}>", StringComparison.Ordinal))}";
                return BuildFundDescription(
                    details,
                    1,
                    result.IncrementAmount,
                    result.PreviousAmount,
                    result.NewAmount,
                    result.RankImproved ? result.PreviousRank + 1 : null,
                    result.RankImproved ? result.NewRank + 1 : null);
            }

            var count = ParseSummaryNumber(description, "本次入帳：", " 筆") + 1;
            var total = ParseSummaryNumber(description, "共 +", "\n") + result.IncrementAmount;
            var initialAmount = ParseSummaryNumber(description, "基金餘額：", " → ");
            var initialRank = rankIndex >= 0
                ? ParseSummaryNumber(description, "🏆 排名變動：第 ", " 名")
                : result.RankImproved ? result.PreviousRank + 1 : null;
            var finalRank = result.RankImproved
                ? result.NewRank + 1
                : rankIndex >= 0 ? ParseSummaryNumber(description, " → 第 ", " 名") : null;
            var transaction = FormatTransaction(result, !title.Contains($"<@{result.RecipientUserId}>", StringComparison.Ordinal));

            return BuildFundDescription(
                $"{description[..detailEnd].TrimEnd()}\n{transaction}",
                count,
                total,
                initialAmount,
                result.NewAmount,
                initialRank,
                finalRank);
        }

        internal static string BuildFundDescription(
            string details,
            long count,
            long total,
            long initialAmount,
            long finalAmount,
            long? initialRank,
            long? finalRank)
        {
            var rankSummary = initialRank.HasValue && finalRank.HasValue && finalRank < initialRank
                ? $"\n\n🏆 排名變動：第 {initialRank:N0} 名 → 第 {finalRank:N0} 名（↑{initialRank - finalRank:N0}）"
                : string.Empty;

            return $"{details}{rankSummary}\n\n本次入帳：{count:N0} 筆，共 +{total:N0}\n基金餘額：{initialAmount:N0} → {finalAmount:N0}";
        }

        private static string FormatTransaction(FundAddResult result, bool showRecipient)
        {
            var recipient = showRecipient ? $" → <@{result.RecipientUserId}>" : string.Empty;
            var rank = result.RankImproved ? $"  🏆 升至第 {result.NewRank + 1:N0} 名" : string.Empty;
            var sunday = result.IsSunday ? "  ☀️ 星期日加倍" : string.Empty;
            var ownerRedirect = result.WasOwnerRedirected ? "  ↪ Owner 亂彈" : string.Empty;
            return $"<@{result.ExecuteUserId}>{recipient}      +{result.IncrementAmount:N0}{rank}{sunday}{ownerRedirect}";
        }

        private static long ParseSummaryNumber(string text, string prefix, string suffix)
        {
            var start = text.IndexOf(prefix, StringComparison.Ordinal);
            if (start < 0)
                throw new InvalidOperationException("基金訊息摘要格式無效");

            start += prefix.Length;
            var end = text.IndexOf(suffix, start, StringComparison.Ordinal);
            if (end < 0 || !long.TryParse(text[start..end].Replace(",", string.Empty), out var value))
                throw new InvalidOperationException("基金訊息摘要數值無效");

            return value;
        }

        internal static bool TryParseAddOneCustomId(string customId, out FundType fundType, out ulong targetUserId, out ulong cooldownScopeId)
        {
            fundType = default;
            targetUserId = default;
            cooldownScopeId = default;

            var values = customId[AddOneCustomIdPrefix.Length..].Split(':');
            if ((values.Length != 2 && values.Length != 3) ||
                !int.TryParse(values[0], out var fundTypeValue) ||
                !Enum.IsDefined(typeof(FundType), fundTypeValue) ||
                !ulong.TryParse(values[1], out targetUserId) ||
                (values.Length == 3 && !ulong.TryParse(values[2], out cooldownScopeId)))
            {
                return false;
            }

            fundType = (FundType)fundTypeValue;
            return true;
        }

        private static string GetAddOneCooldownKey(ulong scopeId, ulong userId)
            => $"{AddOneCooldownKeyPrefix}:{scopeId}:{userId}";

        internal static bool CheckIsAddOwner(FundType fundType, ulong guildId, ulong executeUserId, ulong targetUserId, out ulong resultUserId)
        {
            resultUserId = targetUserId;

            if (targetUserId == Program.ApplicatonOwner.Id)
            {
                resultUserId = executeUserId;

                // 只用 ZSET -> 取得成員列表（降冪）
                var zEntries = RedisConnection.RedisDb.SortedSetRangeByRank(GetFundLeaderboardRedisKey(fundType, guildId), 0, -1, Order.Descending);
                if (zEntries != null && zEntries.Length != 0)
                {
                    var zList = zEntries.ToList();
                    zList.Add(executeUserId); // 將指令執行者也加入隨機選擇的範圍
                    var randomMember = zList[new Random().Next(0, zList.Count)].ToString();
                    if (!ulong.TryParse(randomMember, out resultUserId)) // 原則上不會失敗，直接忽略
                    {
                    }
                }

                return true;
            }

            return false;
        }

        const long MinIncrementAmount = 200;
        const long MaxIncrementAmount = 1000;
        const string NotifyChannelsKey = "SupportBot:Fund:NotifyChannels";

        private static async Task<FundAddResult> AddFundAsync(
            FundType fundType,
            ulong guildId,
            ulong channelId,
            ulong executeUserId,
            ulong userId,
            bool wasOwnerRedirected)
        {
            await RedisConnection.RedisDb.SetAddAsync(NotifyChannelsKey, channelId.ToString());
            var key = GetFundLeaderboardRedisKey(fundType, guildId);
            var isSunday = DateTime.Now.DayOfWeek == DayOfWeek.Sunday;
            var incrementAmount = Random.Shared.NextInt64(MinIncrementAmount, MaxIncrementAmount + 1) * (isSunday ? 2 : 1);

            // 獲取增加前的排名 (SortedSetRankAsync 回傳 0-based index)
            var oldRank = await RedisConnection.RedisDb.SortedSetRankAsync(key, userId.ToString(), Order.Descending);
            var oldAmount = (long)(await RedisConnection.RedisDb.SortedSetScoreAsync(key, userId.ToString()) ?? 0);

            // 單純使用 ZSET 作為唯一來源（score = 總額）
            var newAmount = (long)await RedisConnection.RedisDb.SortedSetIncrementAsync(key, userId.ToString(), incrementAmount);

            // 獲取增加後的排名
            var newRank = await RedisConnection.RedisDb.SortedSetRankAsync(key, userId.ToString(), Order.Descending);

            return new FundAddResult(
                fundType,
                executeUserId,
                userId,
                incrementAmount,
                oldAmount,
                newAmount,
                oldRank,
                newRank,
                isSunday,
                wasOwnerRedirected);
        }

        // 取得某基金前 N 名 (依 score 降冪)
        internal static async Task<List<(ulong UserId, long Score)>> GetTopFundAsync(FundType fundType, ulong guildId, int top = 0)
        {
            var key = GetFundLeaderboardRedisKey(fundType, guildId);
            var entries = await RedisConnection.RedisDb.SortedSetRangeByRankWithScoresAsync(key, 0, top - 1, Order.Descending);

            var list = new List<(ulong, long)>();
            foreach (var entry in entries)
            {
                if (ulong.TryParse(entry.Element, out var uid))
                {
                    list.Add((uid, (long)entry.Score));
                }
            }
            return list;
        }

        internal static string GetFundTypeName(FundType fundType)
        {
            return fundType.GetType()
                .GetField(fundType.ToString())
                .GetCustomAttribute<ChoiceDisplayAttribute>()
                .Name;
        }

        // 取得排行榜 ZSET 的 key
        internal static string GetFundLeaderboardRedisKey(FundType fundType, ulong guildId)
        {
            return $"SupportBot:Fund:Leaderboard:{fundType}:{guildId}";
        }

        // 重置所有基金排行榜（刪除所有相關 ZSET key），並回傳曾觸發的 channel 清單
        internal static async Task<(long DeletedCount, List<ulong> ChannelIds)> ResetAllLeaderboardsAsync()
        {
            var channelEntries = await RedisConnection.RedisDb.SetMembersAsync(NotifyChannelsKey);
            var channelIds = channelEntries
                .Select(e => ulong.TryParse(e, out var id) ? id : 0UL)
                .Where(id => id != 0)
                .ToList();

            var keys = RedisConnection.RedisServer.Keys(database: 2, pattern: "SupportBot:Fund:Leaderboard:*").ToArray();
            long deleted = 0;
            if (keys.Length > 0)
                deleted = await RedisConnection.RedisDb.KeyDeleteAsync(keys);

            await RedisConnection.RedisDb.KeyDeleteAsync(NotifyChannelsKey);

            return (deleted, channelIds);
        }
    }
}
