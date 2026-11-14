using Discord.Interactions;
using DiscordSupportBot.Interaction.Admin.Service;

namespace DiscordSupportBot.Interaction.Admin
{
    public class SendMessage : TopLevelModule<SendMessageService>
    {
        [SlashCommand("send-message-to-this-channel", "透過 Bot 發送訊息到此頻道")]
        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SendMessageAsync()
        {
            if (Context.Channel is not SocketTextChannel channel)
            {
                await Context.Interaction.SendErrorAsync("只能在文字頻道使用此指令");
                return;
            }

            var botPermissions = Context.Guild.CurrentUser.GetPermissions(channel);
            // 討論串不繼承發送訊息權限，改看討論串專用權限；父頻道沒有發送權限時 Discord.Net 會把 EmbedLinks 一起清掉，故討論串不檢查
            if (!botPermissions.ViewChannel ||
                (channel is IThreadChannel
                    ? !botPermissions.SendMessagesInThreads
                    : !botPermissions.SendMessages || !botPermissions.EmbedLinks))
            {
                await Context.Interaction.SendErrorAsync("我在這個頻道缺少查看頻道、發送訊息或嵌入連結的權限，請修正後再試");
                return;
            }

            ModalComponentBuilder modalComponentBuilder = new();
            modalComponentBuilder.WithTextInput("內容", "message", TextInputStyle.Paragraph, "訊息，可到 https://eb.nadeko.bot/ 設定詳細樣式", 1, 4000, 0, true);

            await Context.Interaction.RespondWithModalAsync(new ModalBuilder("發送訊息", "sendMessage", modalComponentBuilder).Build());
        }

        [SlashCommand("edit-message", "編輯 Bot 發送的訊息")]
        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task EditMessageAsync(string messageId)
        {
            if (Context.Channel is not SocketTextChannel channel)
            {
                await Context.Interaction.SendErrorAsync("只能在文字頻道使用此指令");
                return;
            }

            var botPermissions = Context.Guild.CurrentUser.GetPermissions(channel);
            if (!botPermissions.ViewChannel || !botPermissions.ReadMessageHistory)
            {
                await Context.Interaction.SendErrorAsync("我在這個頻道缺少查看頻道或讀取歷史訊息的權限，請修正後再試");
                return;
            }

            ModalComponentBuilder modalComponentBuilder = new();
            modalComponentBuilder.WithTextInput("訊息 Id", "messageId", TextInputStyle.Short, "", 18, 20, 0, true, messageId);
            modalComponentBuilder.WithTextInput("內容", "message", TextInputStyle.Paragraph, "訊息，可到 https://eb.nadeko.bot/ 設定詳細樣式", 1, 4000, 0, true);

            await Context.Interaction.RespondWithModalAsync(new ModalBuilder("編輯訊息", "editMessage", modalComponentBuilder).Build());
        }
    }
}