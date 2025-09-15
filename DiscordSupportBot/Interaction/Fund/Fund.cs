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
            FuckBoy,
            [ChoiceDisplay("抖M")]
            Masochism
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

        [SlashCommand("all-fund-leaderboard", "所有基金的前三名排行榜")]
        public async Task AllFundLeaderBoardAsync()
        {
            await Context.Interaction.DeferAsync(false);

            var fundTypes = Enum.GetValues(typeof(FundType)).Cast<FundType>();
            var embed = new EmbedBuilder()
                .WithTitle($"`{Context.Guild.Name}` 所有基金前三名排行榜")
                .WithOkColor();

            bool hasAny = false;
            foreach (var fundType in fundTypes)
            {
                var hashEntries = await RedisConnection.RedisDb.HashGetAllAsync($"support:{fundType}Fund:{Context.Guild.Id}");
                if (hashEntries.Length > 0)
                {
                    hasAny = true;
                    embed.AddField(GetFundTypeName(fundType), string.Join('\n', hashEntries.OrderByDescending((x) => x.Value).Take(3).Select((x) => $"<@{x.Name}>: {x.Value}")), true);
                }
            }

            if (!hasAny)
            {
                await Context.Interaction.SendErrorAsync("目前沒有任何人有基金");
            }
            else
            {
                await Context.Interaction.FollowupAsync(embed: embed.Build());
            }
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

        [MessageCommand("對該訊息的作者添加抖M基金")]
        public async Task AddMasochismFundMessageCommandAsync(IMessage message)
        {
            await AddFundAsync(FundType.Masochism, message.Author);
        }

        private string GetFundTypeName(FundType fundType)
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
