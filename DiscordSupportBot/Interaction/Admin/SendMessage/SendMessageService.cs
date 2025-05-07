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
                            var message = arg.Data.Components.Single((x) => x.CustomId == "message").Value;

                            var rep = new ReplacementBuilder().Build();
                            var smartText = rep.Replace(SmartText.CreateFrom(message));

                            await arg.Channel.SendAsync(smartText, false);

                            await arg.SendConfirmAsync("Done", false, true);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "ModalSubmitted: sendMessage");
                        }
                    }
                    break;
                case "editMessage":
                    {
                        try
                        {
                            var messageIdStr = arg.Data.Components.Single((x) => x.CustomId == "messageId").Value;
                            if (!ulong.TryParse(messageIdStr, out var messageId))
                            {
                                await arg.SendErrorAsync("訊息 Id 格式錯誤，需為純數字");
                                return;
                            }

                            var guildId = arg.GuildId.Value;
                            var channelId = arg.ChannelId.Value;

                            var msg = await _client.GetGuild(guildId).GetTextChannel(channelId).GetMessageAsync(messageId);
                            if (msg is null)
                            {
                                Log.Warn($"{guildId} - {channelId} - {messageId} 找不到訊息");
                                return;
                            }

                            if (msg.Author.Id != _client.CurrentUser.Id)
                            {
                                Log.Warn($"{guildId} - {channelId} - {messageId} 訊息非 Bot 發送");
                                return;
                            }

                            if (msg is not IUserMessage userMessage)
                            {
                                Log.Warn($"{guildId} - {channelId} - {messageId} 訊息類型非 IUserMessage: {msg.GetType().FullName}");
                                return;
                            }

                            var message = arg.Data.Components.Single((x) => x.CustomId == "message").Value;

                            var rep = new ReplacementBuilder().Build();
                            var smartText = rep.Replace(SmartText.CreateFrom(message));

                            await userMessage.EditAsync(smartText, false);

                            await arg.SendConfirmAsync("Done", false, true);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "ModalSubmitted: editMessage");
                        }
                    }
                    break;
            }
        }
    }
}
