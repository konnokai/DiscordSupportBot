using Discord.Interactions;

namespace Discord_Support_Bot.Interaction.AutoVoiceChannel
{
    public class AutoVoiceChannel : TopLevelModule<Services.AutoVoiceChannelService>
    {
        private readonly DiscordSocketClient _client;
        public AutoVoiceChannel(DiscordSocketClient client)
        {
            _client = client;
        }

        [SlashCommand("set-auto-voice-channel", "設定某個頻道加入後可自動建立專用語音頻道")]
        [EnabledInDm(false)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageChannels | GuildPermission.MoveMembers)]
        public async Task SetAutoVoiceChannelAsync(IVoiceChannel voiceChannel)
        {
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

        [SlashCommand("remove-auto-voice-channel", "移除自動建立專用語音頻道")]
        [EnabledInDm(false)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageChannels | GuildPermission.MoveMembers)]
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
