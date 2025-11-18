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
            Masochism
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

            var newAmount = await AddFundAsync(fundType, guildId, userId);
            await arg.RespondAsync(embed: new EmbedBuilder().WithDescription($"已對 <@{userId}> 增加 500 {GetFundTypeName(fundType)}基金，現在金額: {newAmount}").WithOkColor().Build());
        }

        internal async Task<long> AddFundAsync(FundType fundType, ulong guildId, ulong userId)
        {
            return await RedisConnection.RedisDb.HashIncrementAsync($"support:{fundType}Fund:{guildId}", userId, 500);
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
                _ => fundType.ToString(),
            };
        }
    }
}
