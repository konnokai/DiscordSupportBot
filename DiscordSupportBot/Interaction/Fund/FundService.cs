using Discord.Interactions;
using StackExchange.Redis;

namespace DiscordSupportBot.Interaction.Fund
{
    public class FundService : IInteractionService
    {
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
        }

        private readonly DiscordSocketClient _client;

        public FundService(DiscordSocketClient client)
        {
            _client = client;

            _client.ModalSubmitted += _client_ModalSubmitted;
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
                .First(x => x.CustomId == "select_fund_type")
                .Values.First().ToString());

            var message = CheckIsAddOwner(fundType, guildId, arg.User.Id, targetUserId, out ulong needAddUserId);
            message += await AddFundAsync(fundType, guildId, needAddUserId);
            await arg.SendConfirmAsync(message, true);
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
                    var randomMember = zEntries[new Random().Next(0, zEntries.Length)].ToString();
                    if (!ulong.TryParse(randomMember, out resultUserId)) // 原則上不會失敗，直接忽略
                    {
                    }
                }

                return "無法對 Owner 添加基金，亂彈!\n";
            }

            return string.Empty;
        }

        const long IncrementAmount = 500;
        internal static async Task<string> AddFundAsync(FundType fundType, ulong guildId, ulong userId)
        {
            var key = GetFundLeaderboardRedisKey(fundType, guildId);

            // 獲取增加前的排名 (SortedSetRankAsync 回傳 0-based index)
            var oldRank = await RedisConnection.RedisDb.SortedSetRankAsync(key, userId.ToString(), Order.Descending);

            // 單純使用 ZSET 作為唯一來源（score = 總額）
            var newAmount = await RedisConnection.RedisDb.SortedSetIncrementAsync(key, userId.ToString(), IncrementAmount);

            // 獲取增加後的排名
            var newRank = await RedisConnection.RedisDb.SortedSetRankAsync(key, userId.ToString(), Order.Descending);

            var message = $"已對 <@{userId}> 增加 {IncrementAmount} {GetFundTypeName(fundType)}基金，現在金額: {newAmount}";

            // 檢測排名是否變更
            if (oldRank.HasValue && newRank.HasValue && newRank < oldRank) // new 只會比 old 小 (1 < 2)
            {
                var beatenMemberId = string.Empty;
                var beatenEntries = await RedisConnection.RedisDb.SortedSetRangeByRankWithScoresAsync(key, newRank.Value - 1, newRank.Value - 1, Order.Descending);
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
            return fundType switch
            {
                FundType.Lying => "說謊",
                FundType.Dizzy => "暈船",
                FundType.HentaiDog => "色狗",
                FundType.FuckBoy => "渣男",
                FundType.Masochism => "抖M",
                FundType.Clown => "小丑",
                FundType.BadJoke => "爛笑話",
                FundType.SleepBomb => "炸寢",
                FundType.Freak => "怪人",
                FundType.Typo => "錯字",
                _ => fundType.ToString(),
            };
        }

        // 取得排行榜 ZSET 的 key
        internal static string GetFundLeaderboardRedisKey(FundType fundType, ulong guildId)
        {
            return $"SupportBot:Fund:Leaderboard:{fundType}:{guildId}";
        }
    }
}
