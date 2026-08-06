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
                if (arg.GuildId == null || !TryParseAddOneCustomId(arg.Data.CustomId, out var fundType, out var targetUserId))
                {
                    await arg.SendErrorAsync("基金按鈕資料無效", true);
                    return;
                }

                var cooldownKey = $"{AddOneCooldownKeyPrefix}:{arg.Message.Id}:{arg.User.Id}";
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

                await AddFundAndRespondAsync(
                    arg,
                    fundType,
                    arg.GuildId.Value,
                    arg.Channel.Id,
                    arg.User.Id,
                    targetUserId,
                    includeAddOneButton: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"FundService-AddOneButton: {arg.GuildId} | {arg.User.Id} | {arg.Data.CustomId}");
                await arg.SendErrorAsync($"處理過程發生錯誤: {ex.Message}", true);
            }
        }

        internal static async Task AddFundAndRespondAsync(
            IDiscordInteraction interaction,
            FundType fundType,
            ulong guildId,
            ulong channelId,
            ulong executeUserId,
            ulong targetUserId,
            string messagePrefix = "",
            bool includeAddOneButton = true)
        {
            var message = messagePrefix;
            message += CheckIsAddOwner(fundType, guildId, executeUserId, targetUserId, out var needAddUserId);
            message += await AddFundAsync(fundType, guildId, channelId, executeUserId, needAddUserId);

            MessageComponent components = null;
            if (includeAddOneButton)
            {
                components = new ComponentBuilder()
                    .WithButton("讓他飛", $"{AddOneCustomIdPrefix}{(int)fundType}:{targetUserId}", ButtonStyle.Success)
                    .Build();
            }

            await interaction.SendConfirmAsync(message, true, components: components);
        }

        private static bool TryParseAddOneCustomId(string customId, out FundType fundType, out ulong targetUserId)
        {
            fundType = default;
            targetUserId = default;

            var values = customId[AddOneCustomIdPrefix.Length..].Split(':');
            if (values.Length != 2 ||
                !int.TryParse(values[0], out var fundTypeValue) ||
                !Enum.IsDefined(typeof(FundType), fundTypeValue) ||
                !ulong.TryParse(values[1], out targetUserId))
            {
                return false;
            }

            fundType = (FundType)fundTypeValue;
            return true;
        }

        internal static string CheckIsAddOwner(FundType fundType, ulong guildId, ulong executeUserId, ulong targetUserId, out ulong resultUserId)
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

                return "無法對 Owner 添加基金，亂彈!\n";
            }

            return string.Empty;
        }

        const long MinIncrementAmount = 200;
        const long MaxIncrementAmount = 1000;
        const string NotifyChannelsKey = "SupportBot:Fund:NotifyChannels";

        internal static async Task<string> AddFundAsync(FundType fundType, ulong guildId, ulong channelId, ulong executeUserId, ulong userId)
        {
            await RedisConnection.RedisDb.SetAddAsync(NotifyChannelsKey, channelId.ToString());
            var key = GetFundLeaderboardRedisKey(fundType, guildId);
            var incrementAmount = Random.Shared.NextInt64(MinIncrementAmount, MaxIncrementAmount + 1);

            // 獲取增加前的排名 (SortedSetRankAsync 回傳 0-based index)
            var oldRank = await RedisConnection.RedisDb.SortedSetRankAsync(key, userId.ToString(), Order.Descending);

            // 單純使用 ZSET 作為唯一來源（score = 總額）
            var newAmount = await RedisConnection.RedisDb.SortedSetIncrementAsync(key, userId.ToString(), incrementAmount);

            // 獲取增加後的排名
            var newRank = await RedisConnection.RedisDb.SortedSetRankAsync(key, userId.ToString(), Order.Descending);

            var message = $"<@{executeUserId}> 已對 <@{userId}> 增加 {incrementAmount} {GetFundTypeName(fundType)}基金，現在金額: {newAmount}";

            // 檢測排名是否變更
            if (oldRank.HasValue && newRank.HasValue && newRank < oldRank) // new 只會比 old 小 (1 < 2)
            {
                var beatenMemberId = string.Empty;
                var beatenEntries = await RedisConnection.RedisDb.SortedSetRangeByRankWithScoresAsync(key, oldRank.Value, oldRank.Value, Order.Descending);
                if (beatenEntries.Length > 0) // 原則上不會是空的
                {
                    beatenMemberId = beatenEntries[0].Element.ToString();
                }

                var newRankDisplay = newRank.Value + 1;
                if (string.IsNullOrEmpty(beatenMemberId))
                {
                    message += $"\n\n喜報！已成為{GetFundTypeName(fundType)}基金的榜 {newRankDisplay}";
                }
                else
                {
                    message += $"\n\n喜報！已超越 <@{beatenMemberId}> 成為{GetFundTypeName(fundType)}基金的榜 {newRankDisplay}";
                }
            }

            return message;
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
