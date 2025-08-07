using Discord.Interactions;

namespace DiscordSupportBot.Interaction.AutoVoiceChannel
{
    public class AutoVoiceChannel : TopLevelModule<Services.AutoVoiceChannelService>
    {
        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageChannels | GuildPermission.MoveMembers)]
        [SlashCommand("set-auto-voice-channel", "設定某個頻道加入後可自動建立專用語音頻道")]
        public async Task SetAutoVoiceChannelAsync(IVoiceChannel voiceChannel)
        {
            var currentUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            if (!currentUser.GuildPermissions.ManageChannels)
            {
                await Context.Interaction.SendErrorAsync($"我在此伺服器無`管理頻道`權限，請給予權限後重新設定");
                return;
            }

            if (!currentUser.GuildPermissions.MoveMembers)
            {
                await Context.Interaction.SendErrorAsync($"我在此伺服器無`移動成員`權限，請給予權限後重新設定");
                return;
            }

            await DeferAsync(true);

            try
            {
                using var db = new SupportContext();
                var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == Context.Interaction.GuildId);
                if (guildConfig == null)
                {
                    guildConfig = new GuildConfig() { GuildId = Context.Interaction.GuildId.Value, AutoVoiceChannel = voiceChannel.Id };
                    db.GuildConfig.Add(guildConfig);
                }
                else
                {
                    guildConfig.AutoVoiceChannel = voiceChannel.Id;
                    db.GuildConfig.Update(guildConfig);
                }

                db.SaveChanges();
                await Context.Interaction.SendConfirmAsync($"已設定 `{voiceChannel.Name}` 為自動語音建立頻道\n" +
                    $"當使用者加入此頻道時，小幫手會在此頻道的分類下建立以該使用者為名的語音頻道\n" +
                    $"反之當該語音頻道已經無人時會自動刪除\n\n" +
                    $"新頻道的以下設定將會繼承至 `{voiceChannel.Name}`\n" +
                    $"位元率: `{voiceChannel.Bitrate / 1000}Kbps`\n" +
                    $"人數限制: `" + (voiceChannel.UserLimit.HasValue ? voiceChannel.UserLimit.Value.ToString() + "人" : "無限制") + "`", true, true);
            }
            catch (Exception ex)
            {
                Log.Error($"SetAutoVoiceChannelAsync: {ex}");
                await Context.Interaction.SendErrorAsync($"設定失敗，請向 {Program.ApplicatonOwner} 確認原因\n{ex.Message}", true);
            }
        }

        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("remove-auto-voice-channel", "移除自動建立專用語音頻道")]
        public async Task RemoveAutoVoiceChannelAsync()
        {
            await DeferAsync(true);

            try
            {
                using var db = new SupportContext();
                var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == Context.Interaction.GuildId);
                if (guildConfig == null)
                {
                    await Context.Interaction.SendErrorAsync("無自動語音頻道可供移除");
                    return;
                }
                else
                {
                    guildConfig.AutoVoiceChannel = 0;
                    db.GuildConfig.Update(guildConfig);
                }

                db.SaveChanges();
                await Context.Interaction.SendConfirmAsync($"已移除自動語音頻道", true, true);
            }
            catch (Exception ex)
            {
                Log.Error($"RemoveAutoVoiceChannelAsync: {ex}");
                await Context.Interaction.SendErrorAsync($"設定失敗，請向 {Program.ApplicatonOwner} 確認原因\n{ex.Message}", true);
            }
        }
    }
}
