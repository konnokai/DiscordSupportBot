using Discord.Interactions;

namespace DiscordSupportBot.Interaction.Fund.Service
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

            var guildId = ulong.Parse(arg.Data.CustomId.Split(':')[1]);
            var userId = ulong.Parse(arg.Data.CustomId.Split(':')[2]);
            var fundType = Enum.Parse<FundType>(arg.Data.Components
                .First(x => x.CustomId == "select_fund_type")
                .Values.First().ToString());

            var message = string.Empty;
            if (userId == Program.ApplicatonOwner.Id)
            {
                message = "無法對 Owner 添加基金，反擊!\n";
                userId = arg.User.Id;
            }

            message += await AddFundAsync(fundType, guildId, userId);
            await arg.SendConfirmAsync(message);
        }

        internal async Task<string> AddFundAsync(FundType fundType, ulong guildId, ulong userId)
        {
            var newAmount = await RedisConnection.RedisDb.HashIncrementAsync(GetFundRedisKey(fundType, guildId), userId, 500);
            return $"已對 <@{userId}> 增加 500 {GetFundTypeName(fundType)}基金，現在金額: {newAmount}";
        }

        internal string GetFundTypeName(FundType fundType)
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
                _ => fundType.ToString(),
            };
        }

        internal string GetFundRedisKey(FundType fundType, ulong guildId)
        {
            return $"SupportBot:Fund:{fundType}:{guildId}";
        }
    }
}
