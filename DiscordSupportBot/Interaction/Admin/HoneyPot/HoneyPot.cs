using Discord.Interactions;
using System.Diagnostics;

namespace DiscordSupportBot.Interaction.Admin.HoneyPot
{
    public class HoneyPot : TopLevelModule<HoneyPotService>
    {
        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        [SlashCommand("set-honeypot", "設定蜜罐頻道，在此頻道發言的用戶將會被踢出")]
        public async Task SetHoneyPotAsync(ITextChannel channel)
        {
            await DeferAsync(true);

            var currentUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            if (!currentUser.GuildPermissions.KickMembers)
            {
                await Context.Interaction.SendErrorAsync($"我在此伺服器無 `踢出成員` 權限，請給予權限後重新設定", true);
                return;
            }

            try
            {
                using var db = SupportContext.GetDbContext();
                var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == Context.Interaction.GuildId);
                if (guildConfig == null)
                {
                    guildConfig = new GuildConfig() { GuildId = Context.Interaction.GuildId.Value, HoneyPotChannelId = channel.Id };
                    db.GuildConfig.Add(guildConfig);
                }
                else
                {
                    guildConfig.HoneyPotChannelId = channel.Id;
                    db.GuildConfig.Update(guildConfig);
                }

                db.SaveChanges();

                // 更新服務中的快取
                _service.AddHoneyPotChannel(channel.Id);

                await Context.Interaction.SendConfirmAsync($"已設定 `{channel.Name}` 為蜜罐頻道", 
                    $"當非機器人用戶在此頻道發送訊息時，該用戶將會被踢出伺服器", true, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "SetHoneyPotAsync");
                await Context.Interaction.SendErrorAsync($"設定失敗，請向 {Program.ApplicatonOwner} 確認原因\n{ex.Message}", true);
            }
        }

        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("remove-honeypot", "移除蜜罐頻道設定")]
        public async Task RemoveHoneyPotAsync()
        {
            await DeferAsync(true);

            try
            {
                using var db = SupportContext.GetDbContext();
                var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == Context.Interaction.GuildId);
                if (guildConfig == null || guildConfig.HoneyPotChannelId == 0)
                {
                    await Context.Interaction.SendErrorAsync("無蜜罐頻道設定可供移除", true);
                    return;
                }

                var channelId = guildConfig.HoneyPotChannelId;
                guildConfig.HoneyPotChannelId = 0;
                db.GuildConfig.Update(guildConfig);
                db.SaveChanges();

                // 從服務中移除快取
                _service.RemoveHoneyPotChannel(channelId);

                await Context.Interaction.SendConfirmAsync($"已移除蜜罐頻道設定", true, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"RemoveHoneyPotAsync");
                await Context.Interaction.SendErrorAsync($"移除失敗，請向 {Program.ApplicatonOwner} 確認原因\n{ex.Message}", true);
            }
        }
    }
}
