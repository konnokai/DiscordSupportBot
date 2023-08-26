using Discord.Interactions;

namespace Discord_Support_Bot.Interaction.LyingFund
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
    }
}
