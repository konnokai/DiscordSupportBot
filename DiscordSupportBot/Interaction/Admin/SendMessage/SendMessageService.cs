using Discord.Net;
using DiscordSupportBot.Common;
using DiscordSupportBot.Extensions;

namespace DiscordSupportBot.Interaction.Admin.Service
{
    public class SendMessageService : IInteractionService
    {
        private readonly DiscordSocketClient _client;

        public SendMessageService(DiscordSocketClient client)
        {
            _client = client;

            _client.ModalSubmitted += _client_ModalSubmitted;
        }

        private async Task _client_ModalSubmitted(SocketModal arg)
        {
            if (arg.HasResponded)
                return;

            switch (arg.Data.CustomId)
            {
                case "sendMessage":
                    {
                        try
                        {
                            // 先 defer，避免送訊息被 rate limit 拖過 3 秒造成回覆失敗
                            await arg.DeferAsync(true);

                            var message = arg.Data.Components.Single((x) => x.CustomId == "message").Value;

                            var rep = new ReplacementBuilder().Build();
                            var smartText = rep.Replace(SmartText.CreateFrom(message));

                            await arg.Channel.SendAsync(smartText, false);

                            await arg.SendConfirmAsync("Done", true, true);
                        }
                        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions || ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                        {
                            Log.Warn($"{arg.GuildId} - {arg.ChannelId} 缺少發送訊息的權限: {ex.DiscordCode}");
                            await TrySendErrorAsync(arg, "我在這個頻道沒有發送訊息的權限，請修正後再試");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "ModalSubmitted: sendMessage");
                            await TrySendErrorAsync(arg, "發送訊息失敗，請確認內容格式後再試");
                        }
                    }
                    break;
                case "editMessage":
                    {
                        try
                        {
                            await arg.DeferAsync(true);

                            var messageIdStr = arg.Data.Components.Single((x) => x.CustomId == "messageId").Value;
                            if (!ulong.TryParse(messageIdStr, out var messageId))
                            {
                                await arg.SendErrorAsync("訊息 Id 格式錯誤，需為純數字", true);
                                return;
                            }

                            var guildId = arg.GuildId.Value;
                            var channelId = arg.ChannelId.Value;

                            var msg = await _client.GetGuild(guildId).GetTextChannel(channelId).GetMessageAsync(messageId);
                            if (msg is null)
                            {
                                Log.Warn($"{guildId} - {channelId} - {messageId} 找不到訊息");
                                await arg.SendErrorAsync("找不到訊息，請確認訊息 Id 與目前頻道是否正確", true);
                                return;
                            }

                            if (msg.Author.Id != _client.CurrentUser.Id)
                            {
                                Log.Warn($"{guildId} - {channelId} - {messageId} 訊息非 Bot 發送");
                                await arg.SendErrorAsync("只能編輯 Bot 發送的訊息", true);
                                return;
                            }

                            if (msg is not IUserMessage userMessage)
                            {
                                Log.Warn($"{guildId} - {channelId} - {messageId} 訊息類型非 IUserMessage: {msg.GetType().FullName}");
                                await arg.SendErrorAsync("此訊息類型無法編輯", true);
                                return;
                            }

                            var message = arg.Data.Components.Single((x) => x.CustomId == "message").Value;

                            var rep = new ReplacementBuilder().Build();
                            var smartText = rep.Replace(SmartText.CreateFrom(message));

                            await userMessage.EditAsync(smartText, false);

                            await arg.SendConfirmAsync("Done", true, true);
                        }
                        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions || ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                        {
                            Log.Warn($"{arg.GuildId} - {arg.ChannelId} 缺少讀取或編輯訊息的權限: {ex.DiscordCode}");
                            await TrySendErrorAsync(arg, "我在這個頻道沒有讀取或編輯訊息的權限，請修正後再試");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "ModalSubmitted: editMessage");
                            await TrySendErrorAsync(arg, "編輯訊息失敗，請確認訊息 Id 與內容後再試");
                        }
                    }
                    break;
            }
        }

        private static async Task TrySendErrorAsync(SocketModal arg, string message)
        {
            try
            {
                await arg.SendErrorAsync(message, true);
            }
            catch (Exception ex)
            {
                Log.Warn($"ModalSubmitted 回覆錯誤訊息失敗: {ex.Message}");
            }
        }
    }
}
