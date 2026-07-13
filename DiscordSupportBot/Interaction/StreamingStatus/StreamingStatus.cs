using Discord.Interactions;

namespace DiscordSupportBot.Interaction.StreamingStatus
{
    public class StreamingStatus : TopLevelModule<Services.StreamingStatusService>
    {
        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        [SlashCommand("toggle-streaming-status", "切換成員在語音頻道內以 Twitch/YouTube 直播時自動設定該頻道狀態")]
        public async Task ToggleStreamingStatusAsync()
        {
            if (!_service.IsEnable)
            {
                await Context.Interaction.SendErrorAsync($"此功能需要啟用 Presence Intent，請向 {Program.ApplicatonOwner} 確認");
                return;
            }

            await DeferAsync(true);

            try
            {
                ulong guildId = Context.Interaction.GuildId.Value;

                using var db = new SupportContext();
                var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == guildId);
                if (guildConfig == null)
                {
                    guildConfig = new GuildConfig() { GuildId = guildId, EnableStreamingStatus = true };
                    db.GuildConfig.Add(guildConfig);
                }
                else
                {
                    guildConfig.EnableStreamingStatus = !guildConfig.EnableStreamingStatus;
                    db.GuildConfig.Update(guildConfig);
                }

                db.SaveChanges();
                _service.SetEnabled(guildId, guildConfig.EnableStreamingStatus);

                await Context.Interaction.SendConfirmAsync(guildConfig.EnableStreamingStatus
                    ? $"已啟用直播狀態偵測\n當成員在語音頻道內以 Twitch/YouTube 等平台直播時，會自動將該語音頻道狀態設為 `{guildConfig.StreamingStatusTemplate.Replace("{platform}", "Twitch")}`\n" +
                        "停止直播或離開頻道時會自動清除"
                    : "已關閉直播狀態偵測", true, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ToggleStreamingStatusAsync");
                await Context.Interaction.SendErrorAsync($"設定失敗，請向 {Program.ApplicatonOwner} 確認原因\n{ex.Message}", true);
            }
        }

        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("set-streaming-status-template", "設定直播狀態文字模板，需包含 {platform} 作為平台名稱位置")]
        public async Task SetStreamingStatusTemplateAsync([Summary("template", "例如：正在 {platform} 直播中")] string template)
        {
            await DeferAsync(true);

            if (!template.Contains("{platform}"))
            {
                await Context.Interaction.SendErrorAsync("模板必須包含 `{platform}` 作為平台名稱的位置", true);
                return;
            }

            try
            {
                ulong guildId = Context.Interaction.GuildId.Value;

                using var db = new SupportContext();
                var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == guildId);
                if (guildConfig == null)
                {
                    guildConfig = new GuildConfig() { GuildId = guildId, StreamingStatusTemplate = template };
                    db.GuildConfig.Add(guildConfig);
                }
                else
                {
                    guildConfig.StreamingStatusTemplate = template;
                    db.GuildConfig.Update(guildConfig);
                }

                db.SaveChanges();
                await Context.Interaction.SendConfirmAsync($"已設定直播狀態模板\n預覽: `{template.Replace("{platform}", "Twitch")}`", true, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SetStreamingStatusTemplateAsync");
                await Context.Interaction.SendErrorAsync($"設定失敗，請向 {Program.ApplicatonOwner} 確認原因\n{ex.Message}", true);
            }
        }
    }
}
