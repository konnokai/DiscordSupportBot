
namespace DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service
{
    public class AutoCreatePrivateThreadService : IInteractionService
    {
        private readonly DiscordSocketClient _client;

        public AutoCreatePrivateThreadService(DiscordSocketClient client)
        {
            _client = client;
            _client.ButtonExecuted += ButtonExecuted;
        }

        private async Task ButtonExecuted(SocketMessageComponent component)
        {
            if (component.Data.CustomId != "create-private-thread")
                return;

            try
            {
                var channel = component.Channel as SocketTextChannel;
                var guild = _client.GetGuild(component.GuildId.Value);

                var oldThread = channel.Threads.FirstOrDefault((x) => x.Name.EndsWith(component.User.Id.ToString()));
                if (oldThread != null)
                {
                    await component.SendErrorAsync($"已經存在同名的私密討論串 [點我跳轉](https://discord.com/channels/{guild.Id}/{oldThread.Id})");
                    return;
                }

                var thread = await channel.CreateThreadAsync(component.User.Id.ToString(), ThreadType.PrivateThread, autoArchiveDuration: ThreadArchiveDuration.OneDay, invitable: false);
                await thread.SendMessageAsync($"<@{component.User.Id}> <@{guild.OwnerId}>");
                await component.DeferAsync(true);
            }
            catch (Exception ex)
            {
                await component.SendErrorAsync($"出現錯誤，請重試或是直接跟 Bot 擁有者回報此問題\n{ex.Message}");
            }
        }
    }
}
