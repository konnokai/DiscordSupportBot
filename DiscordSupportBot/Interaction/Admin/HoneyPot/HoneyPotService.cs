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
                // 成員快取不一定有這個人，訊息本身帶的作者資料比較可靠
                var guildUser = arg.Author as SocketGuildUser ?? guild.GetUser(arg.Author.Id);

                if (guildUser == null)
                {
                    Log.Warn($"蜜罐頻道 {guildChannel.Name} ({guildChannel.Id}) 收到 {arg.Author.Username} ({arg.Author.Id}) 的訊息，但取不到伺服器成員資料，略過");
                    return;
                }

                // 管理員免疫
                if (guildUser.Roles.Any((x) => x.Permissions.Administrator))
                {
                    Log.Info($"蜜罐頻道 {guildChannel.Name} ({guildChannel.Id}) 收到管理員 {guildUser.Username} ({guildUser.Id}) 的訊息，略過");
                    return;
                }

                // 免疫比機器人身分組高的用戶
                var botTopPosition = guild.CurrentUser.Roles.Max((x) => x.Position);
                var userTopPosition = guildUser.Roles.Max((x) => x.Position);
                if (botTopPosition <= userTopPosition)
                {
                    Log.Warn($"蜜罐頻道 {guildChannel.Name} ({guildChannel.Id}) 收到 {guildUser.Username} ({guildUser.Id}) 的訊息，但對方最高身分組位置 {userTopPosition} 不低於機器人的 {botTopPosition}，略過");
                    return;
                }

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
