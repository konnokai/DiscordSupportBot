namespace DiscordSupportBot.Interaction.Utility
{
    public class UtilityService : IInteractionService
    {
        public UtilityService(DiscordSocketClient discordSocketClient)
        {
            discordSocketClient.ButtonExecuted += async (btn) =>
            {
                if (btn.Data.CustomId == "sub")
                {
                    await btn.RespondAsync("然而並沒有甚麼鳥用", ephemeral: true);
                }
            };
        }
    }
}
