using Discord.Interactions;
using Discord.Net;

namespace DiscordSupportBot.Interaction.Admin
{
    public class AutoGrantRole : TopLevelModule
    {
        [SlashCommand("auto-grant-role", "自動根據清單給予特定用戶組")]
        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task AutoGrantRoleAsync([Summary("用戶組", "要給予的用戶組")] IRole role, [Summary("清單", "文字檔，內含使用者完整名稱(包含後面#四位數)或使用者Id")] Attachment attachment)
        {
            if (role == Context.Guild.EveryoneRole)
            {
                await Context.Interaction.SendErrorAsync("不可給予 Everyone 用戶組");
                return;
            }

            if (Context.Guild.CurrentUser.Roles.Max((x) => x.Position) < role.Position)
            {
                await Context.Interaction.SendErrorAsync($"我的用戶組比 {role} 還低，故沒有權限可增加用戶組");
                return;
            }

            await DeferAsync(true);

            using HttpClient httpClient = new();
            string userIdContext;
            try
            {
                userIdContext = await httpClient.GetStringAsync(attachment.Url);
            }
            catch (Exception ex)
            {
                Log.Error($"AutoGrantRoleAsync-DownloadAttachment: {ex}");
                await Context.Interaction.SendErrorAsync($"下載使用者清單失敗: {ex.Message}", true);
                return;
            }

            string[] userList;
            try
            {
                userList = userIdContext.Split(["\r", "\n", "\r\n", ",", "|"], StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception ex)
            {
                Log.Error($"AutoGrantRoleAsync-SplitUserList: {ex}");
                await Context.Interaction.SendErrorAsync($"分離使用者清單失敗: {ex.Message}", true);
                return;
            }

            int ignoreNum = 0, addNum = 0, notInListNum = 0, errorNum = 0;
            foreach (var user in Context.Guild.Users)
            {
                try
                {
                    if (userList.Any((x) => x.Trim() == user.Id.ToString() || x.Trim() == $"{user.Username}#{user.Discriminator}"))
                    {
                        if (!user.Roles.Contains(role))
                        {
                            await user.AddRoleAsync(role);
                            addNum++;
                        }
                        else
                        {
                            ignoreNum++;
                        }
                    }
                    else
                    {
                        notInListNum++;
                    }
                }
                catch (HttpException discordEx) when (discordEx.DiscordCode == DiscordErrorCode.MissingPermissions || discordEx.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                {
                    Log.Error($"AutoGrantRoleAsync-AddRole-MissingPermissions");
                    await Context.Interaction.SendErrorAsync($"我沒有權限可增加用戶組，請確認我的用戶組比 {role} 還高", true);
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error($"AutoGrantRoleAsync-AddRole: {ex}");
                    await Context.Interaction.SendErrorAsync($"無法新增 {user} 的用戶組: {ex.Message}", true);
                    errorNum++;
                }
            }

            await Context.Interaction.FollowupAsync(embed: new EmbedBuilder()
                .WithOkColor()
                .WithTitle("新增完成")
                .WithDescription($"清單人數: {userList.Length}\n" +
                                 $"新增人數: {addNum}\n" +
                                 $"已持有用戶組而忽略人數: {ignoreNum}\n" +
                                 $"未在清單內而忽略人數: {notInListNum}\n" +
                                 $"遇到錯誤人數: {errorNum}")
                .Build());
        }
    }
}