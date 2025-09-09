using Discord.Interactions;

namespace DiscordSupportBot.Interaction.Fund
{
    public class Fund : TopLevelModule
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
            FuckBoy
        }

        [SlashCommand("add-fund", "對某人添加基金")]
        public async Task AddFundAsync([Summary("基金類型")] FundType fundType, [Summary("目標使用者")] IUser user)
        {
            if (Context.Guild.GetUser(user.Id) == null)
            {
                await Context.Interaction.SendErrorAsync("指定的使用者不在此伺服器中");
                return;
            }

            var fund = await RedisConnection.RedisDb.HashIncrementAsync($"support:{fundType}Fund:{Context.Guild.Id}", user.Id, 500);
            await Context.Interaction.SendConfirmAsync($"已對 `{user}` 增加 500 {GetFundTypeName(fundType)}基金，現在金額: {fund}");
        }

        [SlashCommand("fund-leaderboard", "基金排行榜")]
        public async Task FundLeaderBoardAsync([Summary("基金類型")] FundType fundType)
        {
            var hashEntries = await RedisConnection.RedisDb.HashGetAllAsync($"support:{fundType}Fund:{Context.Guild.Id}");

            if (hashEntries.Length == 0)
            {
                await Context.Interaction.SendErrorAsync($"目前沒有任何人有{GetFundTypeName(fundType)}基金");
                return;
            }

            await Context.Interaction.SendConfirmAsync($"`{Context.Guild.Name}` {GetFundTypeName(fundType)}基金排行榜\n\n" +
                $"{string.Join('\n', hashEntries.OrderByDescending((x) => x.Value).Select((x) => $"<@{x.Name}>: {x.Value}"))}");
        }

        [MessageCommand("對該訊息的作者添加說謊基金")]
        public async Task AddLyingFundMessageCommandAsync(IMessage message)
        {
            await AddFundAsync(FundType.Lying, message.Author);
        }

        [MessageCommand("對該訊息的作者添加暈船基金")]
        public async Task AddDizzyFundMessageCommandAsync(IMessage message)
        {
            await AddFundAsync(FundType.Dizzy, message.Author);
        }

        [MessageCommand("對該訊息的作者添加色狗基金")]
        public async Task AddHentaiDogFundMessageCommandAsync(IMessage message)
        {
            await AddFundAsync(FundType.HentaiDog, message.Author);
        }

        [MessageCommand("對該訊息的作者添加渣男基金")]
        public async Task AddFuckBoyFundMessageCommandAsync(IMessage message)
        {
            await AddFundAsync(FundType.FuckBoy, message.Author);
        }

        private string GetFundTypeName(FundType fundType)
        {
            return fundType switch
            {
                FundType.Lying => "說謊",
                FundType.Dizzy => "暈船",
                FundType.HentaiDog => "色狗",
                FundType.FuckBoy => "渣男",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
