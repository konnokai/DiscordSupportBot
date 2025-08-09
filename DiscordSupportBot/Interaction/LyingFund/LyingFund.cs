using Discord.Interactions;

namespace DiscordSupportBot.Interaction.LyingFund
{
    public class LyingFund : TopLevelModule
    {
        [SlashCommand("lying-fund", "說謊基金")]
        public async Task LyingFundAsync(IUser user)
        {
            var fund = await RedisConnection.RedisDb.HashIncrementAsync($"support:LyinhFund:{Context.Guild.Id}", user.Id, 500);
            await Context.Interaction.SendConfirmAsync($"已對 `{user}` 增加 500 說謊基金，現在金額: {fund}");
        }

        [SlashCommand("lying-fund-leaderboard", "說謊基金排行榜")]
        public async Task LyingFundLeaderBoardAsync()
        {
            var hashEntries = await RedisConnection.RedisDb.HashGetAllAsync($"support:LyinhFund:{Context.Guild.Id}");

            if (hashEntries.Length == 0)
            {
                await Context.Interaction.SendErrorAsync("目前沒有任何人有說謊基金。");
                return;
            }

            await Context.Interaction.SendConfirmAsync($"`{Context.Guild.Name}` 說謊基金排行榜\n\n" +
                $"{string.Join('\n', hashEntries.OrderByDescending((x) => x.Value).Select((x) => $"<@{x.Name}>: {x.Value}"))}");
        }
    }
}
