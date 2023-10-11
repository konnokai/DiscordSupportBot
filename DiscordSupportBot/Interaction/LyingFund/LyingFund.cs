using Discord.Interactions;

namespace DiscordSupportBot.Interaction.LyingFund
{
    [DontAutoRegister]
    [Attribute.RequireGuild(910364272473829396)]
    public class LyingFund : TopLevelModule
    {
        [SlashCommand("lying-fund", "說謊基金")]
        public async Task LyingFundAsync(IUser user)
        {
            var fund = await Program.Redis.GetDatabase(0).HashIncrementAsync("support:LyinhFund", user.Id, 500);
            await Context.Interaction.SendConfirmAsync($"已對 `{user}` 增加 500 說謊基金，現在金額: {fund}");
        }

        [SlashCommand("lying-fund-leaderboard", "說謊基金排行榜")]
        public async Task LyingFundLeaderBoardAsync()
        {
            var hashEntries = await Program.Redis.GetDatabase(0).HashGetAllAsync("support:LyinhFund");
            
            await Context.Interaction.SendConfirmAsync($"說謊基金排行榜\n\n" +
                $"{string.Join('\n', hashEntries.OrderByDescending((x) => x.Value).Select((x) => $"<@{x.Name}>: {x.Value}"))}");
        }
    }
}
