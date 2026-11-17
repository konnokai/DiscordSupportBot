using Discord.Interactions;
using Discord.Net;
using DiscordSupportBot.Interaction.LinkFix.Service;

namespace DiscordSupportBot.Interaction.LinkFix
{
    [RequireContext(ContextType.Guild)]
    public class LinkFixMessageCommand : TopLevelModule
    {
        [MessageCommand("刪除機器人訊息")]
        public async Task RemoveLinkFixMessageAsync(IMessage message)
        {
            if (message.Author.Id != Context.Client.CurrentUser.Id)
            {
                await Context.Interaction.SendErrorAsync("只能移除我發送的訊息");
                return;
            }

            // 連結修正一律是回覆訊息，內容只有修正後的網址；按鈕用戶組等系統訊息因此不會被誤改
            if (message.Type != MessageType.Reply || !LinkFixService.IsLinkFixContent(message.Content) || message is not IUserMessage userMessage)
            {
                await Context.Interaction.SendErrorAsync("只能移除連結修正的訊息");
                return;
            }

            // 編輯可能超過互動回應的 3 秒期限，先延後回應
            await DeferAsync(true);

            try
            {
                // 不真的刪除，改寫內容並壓掉預覽，讓原本的 embed 消失
                await userMessage.ModifyAsync((act) =>
                {
                    act.Content = "已移除原訊息";
                    act.Flags = MessageFlags.SuppressEmbeds;
                });
                Log.Info($"[{Context.Guild}/{Context.Channel}] {Context.User.Username} 移除連結修正訊息 {message.Id} 的預覽");
                await Context.Interaction.SendConfirmAsync("已移除連結預覽", true, true);
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
            {
                await Context.Interaction.SendConfirmAsync("訊息已經被移除了", true, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LinkFixMessageCommand.RemoveLinkFixMessageAsync");
                await Context.Interaction.SendErrorAsync("移除失敗，請稍後再試", true);
            }
        }
    }
}
