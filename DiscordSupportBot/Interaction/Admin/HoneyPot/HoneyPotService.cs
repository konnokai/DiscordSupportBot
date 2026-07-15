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
            try
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

                var guild = guildChannel.Guild;
                var guildUser = guild.GetUser(arg.Author.Id);

                if (guildUser == null)
                    return;

                // 管理員免疫
                if (guildUser.Roles.Any((x) => x.Permissions.Administrator))
                    return;

                // 免疫比機器人身分組高的用戶
                if (guild.GetUser(_client.CurrentUser.Id) is SocketGuildUser botUser && botUser.Roles.Max((x) => x.Position) <= guildUser.Roles.Max((x) => x.Position))
                    return;

                // 添加蜜罐反應表情
                //try
                //{
                //    await arg.AddReactionAsync(Emoji.Parse(":honey_pot:"));
                //}
                //catch (Exception ex)
                //{
                //    Log.Warn($"無法添加蜜罐表情反應: {ex.Demystify()}");
                //}

                // 封鎖用戶 + 刪除全部訊息
                await guildUser.BanAsync(1, "在蜜罐頻道發言");

                Log.Info($"用戶 {guildUser.Username} ({guildUser.Id}) 在蜜罐頻道 {guildChannel.Name} ({guildChannel.Id}) 發言，已被停權並自動刪除一天內的訊息");

                try
                {
                    await arg.Channel.SendErrorAsync($"{arg.Author} ({arg.Author.Id}) 在蜜罐頻道發言，已被伺服器停權");
                }
                catch (Exception ex)
                {
                    Log.Warn($"無法發送用戶被停權訊息: {ex.Demystify()}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"HoneyPotService 處理訊息時發生錯誤 | Channel: {arg.Channel.Name} ({arg.Channel.Id}) | User: {arg.Author.Username} ({arg.Author.Id})");
            }
        }
    }
}
