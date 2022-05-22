using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Exporting;
using DiscordChatExporter.Core.Exporting.Filtering;
using DiscordChatExporter.Core.Exporting.Partitioning;
using DiscordChatExporter.Core.Utils.Extensions;
using System.IO.Compression;

namespace Discord_Support_Bot.Command.Administration
{
    public class Administration : TopLevelModule<AdministraionService>
    {
        private readonly DiscordSocketClient _client;
        public Administration(DiscordSocketClient discordSocketClient)
        {
            _client = discordSocketClient;
        }

        [RequireContext(ContextType.Guild)]
        [RequireOwner]
        [Command("Clear")]
        [Summary("清除機器人的發言")]
        public async Task Clear()
        {
            await _service.ClearUser((ITextChannel)Context.Channel).ConfigureAwait(false);
        }

        [Command("UpdateStatus")]
        [Summary("更新機器人的狀態\n參數: Guild, Member, Info")]
        [Alias("UpStats")]
        [RequireOwner]
        public async Task UpdateStatusAsync([Summary("狀態")] string stats)
        {
            switch (stats.ToLowerInvariant())
            {
                case "guild":
                    Program.UpdateStatus = Program.UpdateStatusFlags.Guild;
                    break;
                case "member":
                    Program.UpdateStatus = Program.UpdateStatusFlags.Member;
                    break;
                case "info":
                    Program.UpdateStatus = Program.UpdateStatusFlags.Info;
                    break;
                default:
                    await Context.Channel.SendErrorAsync(string.Format("找不到 {0} 狀態", stats)).ConfigureAwait(false);
                    return;
            }
            Program.ChangeStatus();
            return;
        }

        [Command("Say")]
        [Summary("說話")]
        [RequireOwner]
        public async Task SayAsync([Summary("內容")][Remainder] string text)
        {
            await ReplyAsync(text).ConfigureAwait(false);
        }

        [Command("ListServer")]
        [Summary("顯示所有的伺服器")]
        [Alias("LS")]
        [RequireOwner]
        public async Task ListServerAsync([Summary("頁數")] int page = 0)
        {
            await Context.SendPaginatedConfirmAsync(page, (cur) => { EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor().WithTitle("目前所在的伺服器有"); foreach (var item in _client.Guilds.Skip(cur * 7).Take(7)) { int totalMember = item.Users.Count((x) => !x.IsBot); bool isBotOwnerInGuild = item.GetUser(Program.ApplicatonOwner.Id) != null; embedBuilder.AddField(item.Name, "ID: " + item.Id + "\nOwner ID: " + item.OwnerId + "\n人數: " + totalMember.ToString() + "\nBot擁有者是否在該伺服器: " + (isBotOwnerInGuild ? "是" : "否") + "\n是否已信任該伺服器: " + (isBotOwnerInGuild || Program.TrustedGuildList.Any((x) => x.GuildId == item.Id) ? "是" : "否")); } return embedBuilder; }, _client.Guilds.Count, 7).ConfigureAwait(false);
        }

        [Command("ListBot")]
        [Summary("顯示目前伺服器的Bot")]
        [Alias("LB")]
        [RequireOwner]
        public async Task ListBotAsync([Summary("頁數")] int page = 0)
        {
            await Context.Guild.DownloadUsersAsync().ConfigureAwait(false);
            var users = await Context.Guild.GetUsersAsync().FirstOrDefaultAsync();
            var roleUsers = users.Where(x => x.IsBot).OrderBy((x) => x.Username);

            await Context.SendPaginatedConfirmAsync(page, (cur) =>
            {
                return new EmbedBuilder()
                .WithOkColor()
                .WithTitle(string.Format("伺服器內共有 {0} 個Bot", roleUsers.Count()))
                .WithDescription(string.Join('\n', roleUsers.Skip(cur * 20).Take(20)));
            }, roleUsers.Count(), 20).ConfigureAwait(false);
        }

        [Command("Die")]
        [Summary("關閉機器人")]
        [Alias("Bye")]
        [RequireOwner]
        public async Task DieAsync()
        {
            Program.isDisconnect = true;
            await Context.Channel.SendErrorAsync("關閉中").ConfigureAwait(false);
        }

        [Command("Leave")]
        [Summary("讓機器人離開指定的伺服器")]
        [RequireOwner]
        public async Task LeaveAsync([Summary("伺服器ID")] ulong gid = 0)
        {
            if (gid == 0) { await Context.Channel.SendErrorAsync("伺服器ID為空").ConfigureAwait(false); return; }

            try { await _client.GetGuild(gid).LeaveAsync().ConfigureAwait(false); }
            catch (Exception) { await Context.Channel.SendErrorAsync("失敗，請確認ID是否正確").ConfigureAwait(false); return; }

            await Context.Channel.SendConfirmAsync("✅").ConfigureAwait(false);
        }

        [Command("BigLeave")]
        [Alias("BLeave")]
        [RequireOwner]
        public async Task BigLeave()
        {
            List<SocketGuild> guilds = new List<SocketGuild>(_client.Guilds.Where((x) => x.MemberCount <= 10));

            foreach (var item in guilds)
            {
                await item.LeaveAsync();
                Log.FormatColorWrite("已退出 " + item.Name + " 人數 " + item.MemberCount.ToString());
            }

            await ReplyAsync("Done").ConfigureAwait(false);
        }

        public enum ResetType { user, emote, all };

        [Command("ResetACT")]
        [Summary("重製排行榜\n" +
            "可指定要重製發言(user)，表情(emote)或是全部(all)\n" +
            "(指令縮寫絕對不是故意的)")]
        [Alias("ReACT")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetACT(ResetType resetType)
        {
            var redisEmoteKeyList = RedisConnection.RedisServer.Keys(pattern: $"SupportBot:Activity:Emote:{Context.Guild.Id}:*", cursor: 0, pageSize: 2500).ToArray();
            var redisUserKeyList = RedisConnection.RedisServer.Keys(pattern: $"SupportBot:Activity:User:{Context.Guild.Id}:*", cursor: 0, pageSize: 100000).ToArray();

            switch (resetType)
            {
                case ResetType.user:
                    await UserActivity.ExecuteSQLCommandAsync($"DROP TABLE IF EXISTS `{Context.Guild.Id}`").ConfigureAwait(false);
                    await RedisConnection.RedisDb.KeyDeleteAsync(redisUserKeyList).ConfigureAwait(false);
                    break;
                case ResetType.emote:
                    await EmoteActivity.ExecuteSQLCommandAsync($"DROP TABLE IF EXISTS `{Context.Guild.Id}`").ConfigureAwait(false);
                    await RedisConnection.RedisDb.KeyDeleteAsync(redisEmoteKeyList).ConfigureAwait(false);
                    break;
                case ResetType.all:
                default:
                    await UserActivity.ExecuteSQLCommandAsync($"DROP TABLE IF EXISTS `{Context.Guild.Id}`").ConfigureAwait(false);
                    await EmoteActivity.ExecuteSQLCommandAsync($"DROP TABLE IF EXISTS `{Context.Guild.Id}`").ConfigureAwait(false);
                    await RedisConnection.RedisDb.KeyDeleteAsync(redisUserKeyList).ConfigureAwait(false);
                    await RedisConnection.RedisDb.KeyDeleteAsync(redisEmoteKeyList).ConfigureAwait(false);
                    break;
            }

            await ReplyAsync("完成").ConfigureAwait(false);
        }

        [Command("GetInviteURL")]
        [Summary("取得伺服器的邀請連結")]
        [RequireBotPermission(GuildPermission.CreateInstantInvite)]
        [RequireOwner]
        [Alias("GetURL")]
        public async Task GetInviteURLAsync([Summary("伺服器ID")] ulong gid = 0, [Summary("頻道ID")] ulong cid = 0)
        {
            if (gid == 0) gid = Context.Guild.Id;
            SocketGuild guild = _client.GetGuild(cid);

            try
            {
                if (cid == 0)
                {
                    IReadOnlyCollection<SocketTextChannel> socketTextChannels = guild.TextChannels;

                    await Context.SendPaginatedConfirmAsync(0, (cur) =>
                    {
                        EmbedBuilder embedBuilder = new EmbedBuilder()
                           .WithOkColor()
                           .WithTitle("以下為 " + guild.Name + " 所有的文字頻道")
                           .WithDescription(string.Join('\n', socketTextChannels.Skip(cur * 10).Take(10).Select((x) => x.Id + " / " + x.Name)));

                        return embedBuilder;
                    }, socketTextChannels.Count, 10).ConfigureAwait(false);
                }
                else
                {
                    IInviteMetadata invite = await guild.GetTextChannel(cid).CreateInviteAsync(300, 1, false);
                    await ReplyAsync(invite.Url).ConfigureAwait(false);
                }
            }
            catch (Exception ex) { Log.Error(ex.Message); }
        }

        [Command("AddTrustedGuild")]
        [Summary("新增信任的Guild")]
        [Alias("ATG")]
        [RequireOwner]
        public async Task AddTrustedGuild([Summary("伺服器ID")] ulong gid)
        {
            if (_client.Guilds.Any((x) => x.Id == gid))
            {
                SocketGuild guild = _client.Guilds.First((x) => x.Id == gid);

                if (!Program.TrustedGuildList.Any((x) => x.GuildId == gid))
                {
                    using (var db = new SQLite.SupportContext())
                    {
                        db.TrustedGuild.Add(new SQLite.Table.TrustedGuild() { GuildId = gid });
                        db.SaveChanges();

                        Program.TrustedGuildList.Add(new SQLite.Table.TrustedGuild() { GuildId = gid });
                        await Context.Channel.SendConfirmAsync($"已加入 {guild.Name}({guild.Id}) 到信任的清單").ConfigureAwait(false);
                    }
                }
                else await Context.Channel.SendErrorAsync($"錯誤，{guild.Name}({guild.Id}) 已存在於信任的清單").ConfigureAwait(false);
            }
            else await Context.Channel.SendErrorAsync("找不到該伺服器").ConfigureAwait(false);
        }

        [Command("NGE")]
        [Summary("給予新成員的身分組，僅限 <@&635185609027354645> 使用")]
        [RequireContext(ContextType.Guild)]
        [RequireGuild(308120017201922048)]
        public async Task NGE(IUser user)
        {
            await NGE(user.Id).ConfigureAwait(false);
        }

        [Command("NGE")]
        [Summary("給予新成員的身分組，僅限 <@&635185609027354645> 使用")]
        [RequireContext(ContextType.Guild)]
        [RequireGuild(308120017201922048)]
        public async Task NGE(ulong uid) //僅限特定伺服器使用
        {
            IGuild guild = Context.Guild;
            if ((await guild.GetUserAsync(Context.Message.Author.Id).ConfigureAwait(false)).RoleIds.Any((x) => x == 635185609027354645)) //保全
            {
                IGuildUser guildUser = await guild.GetUserAsync(uid).ConfigureAwait(false);
                IRole role = guild.GetRole(430974984102477825); //郊區
                IRole role2 = guild.GetRole(789223343025946714); //等待

                IRole role3 = guild.GetRole(408967202948120578); //菜鳥
                IRole role4 = guild.GetRole(643051873955217408); //伺服階級
                IRole role5 = guild.GetRole(643050412513165312); //特殊徽章
                IRole role6 = guild.GetRole(643050134627811339); //閒聊頻道
                IRole role7 = guild.GetRole(534764005625954334); //社員證明

                if (guildUser != null && guildUser.RoleIds.Contains(role.Id))
                {
                    try
                    {
                        await _client.Rest.AddRoleAsync(308120017201922048, uid, role3.Id).ConfigureAwait(false);
                        await _client.Rest.AddRoleAsync(308120017201922048, uid, role4.Id).ConfigureAwait(false);
                        await _client.Rest.AddRoleAsync(308120017201922048, uid, role5.Id).ConfigureAwait(false);
                        await _client.Rest.AddRoleAsync(308120017201922048, uid, role6.Id).ConfigureAwait(false);
                        await _client.Rest.AddRoleAsync(308120017201922048, uid, role7.Id).ConfigureAwait(false);

                        await _client.Rest.RemoveRoleAsync(308120017201922048, uid, role.Id).ConfigureAwait(false);
                        await _client.Rest.RemoveRoleAsync(308120017201922048, uid, role2.Id).ConfigureAwait(false);

                        IUserMessage deletable = await Context.Channel.SendConfirmAsync($"已將 {guildUser.Mention} 的用戶組更改").ConfigureAwait(false);
                        Thread.Sleep(10000);
                        await deletable.DeleteAsync();
                    }
                    catch (Exception ex) { Log.Error(ex.Message); }
                }
                else await Context.Channel.EmbedAsync("不存在該用戶或者該用戶沒有 <@&430974984102477825> 用戶組").ConfigureAwait(false);
            }
            else await Context.Channel.EmbedAsync("沒有 <@&635185609027354645> 用戶組").ConfigureAwait(false);
        }

        [Command("Report")]
        [Summary("幫你匿名投訴的指令\r\n" +
            "\r\n" +
            "例:\r\n" +
            "!!!report 投訴內容\r\n" +
            "(換行請使用`Shift + Enter`)")]
        public async Task Report([Remainder] string text)
        {
            await ReplyAsync($"已收到投訴，當管理員看見時會處理\r\n\r\n" +
                $"{text}").ConfigureAwait(false);
            await Context.Message.DeleteAsync().ConfigureAwait(false);
        }

        [Command("Out")]
        [Summary("給予新成員的身分組，僅限 <@&635185609027354645> 使用")]
        [RequireContext(ContextType.Guild)]
        [RequireGuild(308120017201922048)]
        public async Task Out(ulong uid) //僅限特定伺服器使用
        {
            if (Context.Guild.GetUser(Context.Message.Author.Id).Roles.Any((x) => x.Id == 635185609027354645)) //保全
            {
                IGuildUser guildUser = Context.Guild.GetUser(uid);
                IRole role = Context.Guild.GetRole(430974984102477825); //郊區
                IRole role2 = Context.Guild.GetRole(789223343025946714); //等待

                if (guildUser != null && guildUser.RoleIds.Contains(role.Id))
                {
                    try
                    {
                        await _client.Rest.RemoveRoleAsync(308120017201922048, uid, role2.Id).ConfigureAwait(false);

                        IUserMessage deletable = await Context.Channel.SendConfirmAsync($"已將 <@{uid}> 的用戶組更改").ConfigureAwait(false);
                        Thread.Sleep(10000);
                        await deletable.DeleteAsync();
                    }
                    catch (Exception ex) { Log.FormatColorWrite(ex.Message, ConsoleColor.Red, true); }
                }
                else await Context.Channel.SendErrorAsync("不存在該用戶或者該用戶沒有 <@&430974984102477825> 用戶組").ConfigureAwait(false);
            }
            else await Context.Channel.SendErrorAsync("沒有 <@&635185609027354645> 用戶組").ConfigureAwait(false);
        }

        [Command("Kick")]
        [Summary("剔除用戶組內的成員\n注意，Bot的用戶組順序必須高於剃除的用戶組")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task Kick([Summary("用戶組ID")] ulong rid,
            [Summary("為true時剔除`包含`用戶組內的成員\n若為false時則剔除`不包含`的成員\n預設為true")] bool kickSwitch = true,
            [Summary("剔除訊息(可不輸入)")][Remainder] string text = "")
        {
            await _service.Kick(Context, rid, kickSwitch, text).ConfigureAwait(false);
        }

        [Command("CheckRole")]
        [Summary("檢查無用戶組的成員並給予指定用戶組\n注意，Bot的用戶組順序必須高於給予的用戶組")]
        [Alias("CR")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task CheckRole([Summary("要給予的用戶組")] ulong rid)
        {
            await _service.CheckRole(Context, rid).ConfigureAwait(false);
        }

        [Command("CheckRole2")]
        [Summary("檢查用戶組，僅限迪斯可使用")]
        [Alias("CR2")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [RequireGuild(463657254105645056)]
        public async Task CheckRole2() //僅限特定伺服器使用
        {
            await _service.CheckRole(Context).ConfigureAwait(false);
        }

        [Command("ReloadDB")]
        [Summary("重整活動資料庫")]
        [Alias("REDB")]
        [RequireOwner]
        public async Task ReloadDB()
        {
            await EmoteActivity.InitActivityAsync().ConfigureAwait(false);
            await UserActivity.InitActivityAsync().ConfigureAwait(false);
            await ReplyAsync(":white_check_mark:").ConfigureAwait(false);
        }

        [Command("SetMemberNumberChannel")]
        [Summary("設定成員數量顯示的頻道\r\n" +
            "可以設定在文字或是語音頻道\r\n" +
            "Bot需要該頻道的 `管理頻道` 權限\r\n\r\n" +
            "例:\r\n" +
            "!!!smnc 756880128671481856")]
        [Alias("SMNC")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetMemberNumberChannel([Summary("頻道Id")] ulong cId = 0)
        {
            string result = "";
            using (var db = new SQLite.SupportContext())
            {
                var guild = db.UpdateGuildInfo.FirstOrDefault(x => x.GuildId == Context.Guild.Id);

                if (guild != null && guild.ChannelMemberId != 0)
                {
                    if (await Context.PromptUserConfirmAsync(new EmbedBuilder()
                        .WithDescription($"已設定 `{Context.Guild.GetChannel(guild.ChannelMemberId).Name}` 為伺服器人數顯示的頻道\r\n要移除嗎?")).ConfigureAwait(false))
                    {
                        guild.ChannelMemberId = 0;
                        db.UpdateGuildInfo.Update(guild);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        result = "已移除伺服器人數顯示功能";
                    }
                    else
                    {
                        result = "無事可做...";
                    }
                }
                else
                {
                    if (cId == 0) cId = Context.Channel.Id;

                    var channel = Context.Guild.GetChannel(cId);
                    if (channel == null) { result = "指定的頻道不存在，使用目前的頻道\r\n"; channel = (SocketGuildChannel)Context.Channel; cId = channel.Id; }

                    if (!Context.Guild.GetUser(_client.CurrentUser.Id).GetPermissions(channel).ManageChannel)
                    {
                        result += $"Bot無 `{channel.Name}` 的 `管理頻道` 權限\r\n" +
                            $"請給予後再執行一次指令";
                    }
                    else
                    {
                        if (guild == null)
                            await db.UpdateGuildInfo.AddAsync(new SQLite.Table.UpdateGuildInfo() { GuildId = Context.Guild.Id, ChannelMemberId = cId }).ConfigureAwait(false);
                        else
                        {
                            guild.ChannelMemberId = channel.Id;
                            db.UpdateGuildInfo.Update(guild);
                        }

                        try
                        {
                            await db.SaveChangesAsync().ConfigureAwait(false);

                            result += $"已指定 `{channel.Name}` 為伺服器人數顯示用的頻道\r\n" +
                                   "請注意，該頻道的名稱會被更改為`伺服器人數-{人數}`";
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                            Log.Error(ex.InnerException.Message);
                        }
                    }
                }
            }

            await Context.Channel.SendConfirmAsync(result).ConfigureAwait(false);
        }

        [Command("SetNitroNumberChannel")]
        [Summary("設定Nitro數量顯示的頻道\r\n" +
           "可以設定在文字或是語音頻道\r\n" +
           "Bot需要該頻道的 `管理頻道` 權限\r\n\r\n" +
           "例:\r\n" +
           "!!!snnc 756880128671481856")]
        [Alias("SNNC")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetNitroNumberChannel([Summary("頻道Id")] ulong cId = 0)
        {
            string result = "";
            using (var db = new SQLite.SupportContext())
            {
                var guild = db.UpdateGuildInfo.FirstOrDefault(x => x.GuildId == Context.Guild.Id);

                if (guild != null && guild.ChannelNitroId != 0)
                {
                    if (await Context.PromptUserConfirmAsync(new EmbedBuilder()
                        .WithDescription($"已設定 `{Context.Guild.GetChannel(guild.ChannelNitroId).Name}` 為伺服器Nitro數量顯示的頻道\r\n要移除嗎?")).ConfigureAwait(false))
                    {
                        guild.ChannelNitroId = 0;
                        db.UpdateGuildInfo.Update(guild);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        result = "已移除伺服器Nitro數量顯示功能";
                    }
                    else
                    {
                        result = "無事可做...";
                    }
                }
                else
                {
                    if (cId == 0) cId = Context.Channel.Id;

                    var channel = Context.Guild.GetChannel(cId);
                    if (channel == null) { result = "指定的頻道不存在，使用目前的頻道\r\n"; channel = (SocketGuildChannel)Context.Channel; cId = channel.Id; }

                    if (!Context.Guild.GetUser(_client.CurrentUser.Id).GetPermissions(channel).ManageChannel)
                    {
                        result += $"Bot無 `{channel.Name}` 的 `管理頻道` 權限\r\n" +
                            $"請給予後再執行一次指令";
                    }
                    else
                    {
                        if (guild == null)
                            await db.UpdateGuildInfo.AddAsync(new SQLite.Table.UpdateGuildInfo() { GuildId = Context.Guild.Id, ChannelNitroId = cId }).ConfigureAwait(false);
                        else
                        {
                            guild.ChannelNitroId = channel.Id;
                            db.UpdateGuildInfo.Update(guild);
                        }

                        try
                        {
                            await db.SaveChangesAsync().ConfigureAwait(false);

                            result += $"已指定 `{channel.Name}` 為Nitro數量顯示用的頻道\r\n" +
                                   "請注意，該頻道的名稱會被更改為`Nitro數-{人數}`";
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                            Log.Error(ex.InnerException.Message);
                        }
                    }
                }
            }

            await Context.Channel.SendConfirmAsync(result).ConfigureAwait(false);
        }


        [Command("ExportEmoji")]
        [Summary("匯出伺服器表情")]
        [Alias("ExE")]
        [RequireBotPermission(GuildPermission.ManageEmojisAndStickers)]
        [RequireUserPermission(GuildPermission.ManageEmojisAndStickers)]
        public async Task ExportEmoji()
        {
            await Context.Channel.SendConfirmAsync("Working...");

            var list = await Context.Guild.GetEmotesAsync().ConfigureAwait(false);
            var exportNum = 0;

            using FileStream fs = new FileStream(Program.GetDataFilePath($"{Context.Guild.Id}_Emoji.zip"), FileMode.OpenOrCreate);           
            using (ZipArchive zipArchive = new ZipArchive(fs, ZipArchiveMode.Update))
            {
                foreach (var item in list)
                {
                    byte[] bytes = await _service.httpClient.GetByteArrayAsync(item.Url).ConfigureAwait(false);
                    var zipArchiveEntry = zipArchive.CreateEntry(item.Name + Path.GetExtension(item.Url), CompressionLevel.Optimal);

                    using (var zipStream = zipArchiveEntry.Open())
                    {
                        await zipStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    }
                }

                exportNum = zipArchive.Entries.Count;
            }


            if (File.Exists(Program.GetDataFilePath($"{Context.Guild.Id}_Emoji.zip")))
            {
                await Context.Channel.SendFileAsync(Program.GetDataFilePath($"{Context.Guild.Id}_Emoji.zip"), $"總共匯出 {exportNum} 個表情").ConfigureAwait(false);
                File.Delete(Program.GetDataFilePath($"{Context.Guild.Id}_Emoji.zip"));
            }
        }

        [Command("ImportEmoji")]
        [Summary("匯入伺服器表情")]
        [Alias("ImE")]
        [RequireBotPermission(GuildPermission.ManageEmojisAndStickers)]
        [RequireUserPermission(GuildPermission.ManageEmojisAndStickers)]
        public async Task ImportEmoji(string url = "")
        {
            if (string.IsNullOrEmpty(url))
            {
                if (Context.Message.Attachments.Count != 1)
                {
                    await Context.Channel.SendErrorAsync("附件數量不為1");
                    return;
                }

                url = Context.Message.Attachments.First().Url;
            }

            if (!url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                await Context.Channel.SendErrorAsync("副檔名非.zip");
                return;
            }

            await Context.Channel.SendConfirmAsync("Working...");

            var inportNum = 0;
            byte[] bytes = await _service.httpClient.GetByteArrayAsync(url).ConfigureAwait(false);

            try
            {
                using MemoryStream ms = new MemoryStream(bytes);
                using (ZipArchive zipArchive = new ZipArchive(ms, ZipArchiveMode.Read))
                {
                    foreach (var item in zipArchive.Entries)
                    {
                        try
                        {
                            GuildEmote guildEmote;
                            if ((guildEmote = Context.Guild.Emotes.FirstOrDefault((x) => x.Name == Path.GetFileNameWithoutExtension(item.Name))) != null)
                                await Context.Guild.DeleteEmoteAsync(guildEmote).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await Context.Channel.SendErrorAsync(ex.Message);
                            Log.Error($"{ex.Message}\r\n{ex.StackTrace}");
                        }

                        await Context.Guild.CreateEmoteAsync(Path.GetFileNameWithoutExtension(item.Name), new Image(item.Open())).ConfigureAwait(false);
                    }

                    inportNum = zipArchive.Entries.Count;
                }

                await Context.Channel.SendConfirmAsync($"總共匯入 {inportNum} 個表情").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.Message);
                Log.Error($"{ex.Message}\r\n{ex.StackTrace}");
            }

            try { await Context.Message.DeleteAsync().ConfigureAwait(false); }
            catch { }
        }

        // https://github.com/Tyrrrz/DiscordChatExporter
        [Command("ExportChannelMessage")]
        [Summary("匯出類別下的所有頻道聊天紀錄")]
        [Alias("ExpChannelMsg")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.ReadMessageHistory)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireOwner]
        public async Task ExportChannel(ulong categoryId = 0)
        {
            var category = Context.Guild.GetCategoryChannel(categoryId);
            if (category == null)
            {
                await Context.Channel.SendErrorAsync("分類不存在!");
                return;
            }

            var needExportChannel = category.Channels.Where((x) => x is SocketTextChannel);
            int i = 0;
            foreach (var item in needExportChannel)
            {
                i++;
                if (_service._exportedChannelId.Contains(item.Id))
                {
                    await Context.Channel.SendErrorAsync($"{item.Name} 已經匯出過了，無法重複使用");
                    continue;
                }

                _service._exportedChannelId.Add(Context.Channel.Id);
                await Context.Channel.SendConfirmAsync($"({i}/{needExportChannel}) {item.Name} Working...");

                var guild = await _service._discordClient.GetGuildAsync(new Snowflake(Context.Guild.Id));
                var channels = await _service._discordClient.GetGuildChannelsAsync(guild.Id);
                var textChannel = channels.First((x) => x.Id == new Snowflake(item.Id));
                //DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss");
                string exportFileName = ExportRequest.GetDefaultOutputFileName(guild, textChannel, ExportFormat.HtmlDark, null, null);
                string exportFilePath = Path.GetDirectoryName(exportFileName);
                var request = new ExportRequest(
                    guild,
                    textChannel,
                    Program.GetDataFilePath($"Export_temp"),
                    ExportFormat.HtmlDark,
                    null,
                    null,
                    PartitionLimit.Null,
                    MessageFilter.Null,
                    ShouldDownloadMedia: true,
                    ShouldReuseMedia: true,
                    DateFormat: "yyyy-MM-dd hh:mm tt"
                );
                
                await _service._channelExporter.ExportChannelAsync(
                    request
                );

                if (!Directory.Exists(Program.GetDataFilePath("Export"))) Directory.CreateDirectory(Program.GetDataFilePath("Export"));
                ZipFile.CreateFromDirectory(
                    Program.GetDataFilePath($"Export_temp"),
                    Program.GetDataFilePath($"Export\\{Path.GetFileNameWithoutExtension(exportFileName)}.zip"));
                Directory.Delete(Program.GetDataFilePath($"Export_temp"), true);
            }

            await Context.Channel.SendConfirmAsync("Done");
        }

        //[Command("EN")]
        //[Summary("EN")]
        //[RequireBotPermission(GuildPermission.ManageChannels)]
        //[RequireOwner]
        //public async Task EN()
        //{
        //    var guild = _client.GetGuild(877230478082600992);
        //    foreach (var item in guild.CategoryChannels)
        //    {
        //        if (item.Id == 877230478820778074) continue;
        //        await item.AddPermissionOverwriteAsync(guild.GetRole(877457819429924864), new OverwritePermissions(viewChannel: PermValue.Allow));
        //        Console.WriteLine($"Done: {item.Name} ({item.Id})");
        //        //foreach (var item2 in item.PermissionOverwrites.Where((x) => x.TargetType == PermissionTarget.Role))
        //        //{

        //        //}
        //    }
        //    Console.WriteLine("All Done");
        //}
    }
}