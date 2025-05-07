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
            ModalComponentBuilder modalComponentBuilder = new();
            modalComponentBuilder.WithTextInput("訊息 Id", "messageId", TextInputStyle.Short, "", 18, 20, 0, true, messageId);
            modalComponentBuilder.WithTextInput("內容", "message", TextInputStyle.Paragraph, "訊息，可到 https://eb.nadeko.bot/ 設定詳細樣式", 1, 4000, 0, true);

            await Context.Interaction.RespondWithModalAsync(new ModalBuilder("編輯訊息", "editMessage", modalComponentBuilder).Build());
        }
    }
}