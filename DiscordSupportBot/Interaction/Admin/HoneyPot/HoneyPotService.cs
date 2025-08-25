using System.Diagnostics;

namespace DiscordSupportBot.Interaction.Admin.HoneyPot
{
    public class HoneyPotService : IInteractionService
    {
        private readonly HashSet<ulong> _honeyPotChannelIds = [];
        private readonly DiscordSocketClient _client;

        public HoneyPotService(DiscordSocketClient client)
        {
            RefreshHoneyPotChannels();

            _client = client;
            _client.MessageReceived += _client_MessageReceived;
        }

        private void RefreshHoneyPotChannels()
        {
            _honeyPotChannelIds.Clear();

            using var db = SupportContext.GetDbContext();
            foreach (var id in db.GuildConfig.Where((x) => x.HoneyPotChannelId != 0).Select((x) => x.HoneyPotChannelId))
            {
                _honeyPotChannelIds.Add(id);
            }
        }

        internal void AddHoneyPotChannel(ulong channelId)
        {
            _honeyPotChannelIds.Add(channelId);
        }

        internal void RemoveHoneyPotChannel(ulong channelId)
        {
            _honeyPotChannelIds.Remove(channelId);
        }

        private bool IsHoneyPotChannel(ulong channelId)
        {
            return _honeyPotChannelIds.Contains(channelId);
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            // 忽略機器人訊息
            if (arg.Author.IsBot)
                return;

            // 如果是私人訊息頻道，無法踢出用戶
            if (arg.Channel is not SocketGuildChannel guildChannel)
                return;

            // 檢查是否為蜜罐頻道
            if (!IsHoneyPotChannel(arg.Channel.Id))
                return;

            try
            {
                var guild = guildChannel.Guild;
                var guildUser = guild.GetUser(arg.Author.Id);

                if (guildUser == null)
                    return;

                // 添加蜜罐反應表情
                try
                {
                    await arg.AddReactionAsync(Emoji.Parse(":honey_pot:"));
                }
                catch (Exception ex)
                {
                    Log.Warn($"無法添加蜜罐表情反應: {ex.Demystify()}");
                }

                // 踢出用戶
                await guildUser.KickAsync("在蜜罐頻道發言");

                Log.Info($"用戶 {guildUser.Username} ({guildUser.Id}) 在蜜罐頻道 {guildChannel.Name} ({guildChannel.Id}) 發言，已被踢出");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"HoneyPotService 處理訊息時發生錯誤 | Channel: {arg.Channel.Name} ({arg.Channel.Id}) | User: {arg.Author.Username} ({arg.Author.Id})");
            }
        }
    }
}
