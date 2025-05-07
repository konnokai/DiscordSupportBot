using Discord.Interactions;
using DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service;

namespace DiscordSupportBot.Interaction.AutoCreatePrivateThread
{
    public class AutoCreatePrivateThread : TopLevelModule<AutoCreatePrivateThreadService>
    {
        private readonly DiscordSocketClient _client;

        public AutoCreatePrivateThread(DiscordSocketClient client)
        {
            _client = client;
        }


        [SlashCommand("auto-create-private-thread", "自動創建私密討論串")]
        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.ManageThreads | GuildPermission.CreatePrivateThreads)]
        [RequireBotPermission(GuildPermission.ManageThreads | GuildPermission.CreatePrivateThreads)]
        public async Task AutoCreatePrivateThreadAsync([Summary("messageId", "要顯示按鈕的訊息")] string messageIdStr,
            [Summary("buttonTitle", "按鈕標題")] string buttonTitle)
        {
            await DeferAsync(true);

            if (!ulong.TryParse(messageIdStr.Trim(), out ulong messageId))
            {
                await Context.Interaction.SendErrorAsync("請輸入正確的訊息 Id", true);
                return;
            }

            var channel = Context.Channel as ITextChannel;
            var message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                await Context.Interaction.SendErrorAsync("找不到該訊息", true);
                return;
            }

            if (message.Author.Id != _client.CurrentUser.Id)
            {
                await Context.Interaction.SendErrorAsync("該訊息不是由我發送的，無法編輯", true);
                return;
            }

            if (message is not IUserMessage userMessage)
            {
                await Context.Interaction.SendErrorAsync("此訊息的類型錯誤，無法修改訊息", true);
                return;
            }

            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton(buttonTitle, "create-private-thread", ButtonStyle.Primary);
            await userMessage.ModifyAsync((act) => act.Components = componentBuilder.Build());

            await Context.Interaction.SendConfirmAsync("已成功添加按鈕", true);
        }
    }
}
