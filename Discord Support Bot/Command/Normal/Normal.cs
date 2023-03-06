using Discord.Commands;

namespace Discord_Support_Bot.Command.Normal
{
    public class Normal : TopLevelModule<NormalService>
    {
        private readonly DiscordSocketClient _client;

        public Normal(DiscordSocketClient client)
        {
            _client = client;
        }

        [Command("Ping")]
        [Summary("延遲檢測")]
        public async Task PingAsync()
        {
            await Context.Channel.SendConfirmAsync($":ping_pong: {_client.Latency}ms").ConfigureAwait(false);
        }

        [Command("Invite")]
        [Summary("取得邀請連結")]
        public async Task InviteAsync()
        {
            try
            {
                await (await Context.Message.Author.CreateDMChannelAsync().ConfigureAwait(false))
                     .SendConfirmAsync("<https://discordapp.com/api/oauth2/authorize?client_id=" + Program.Client.CurrentUser.Id + "&permissions=268774467&scope=bot%20applications.commands>").ConfigureAwait(false);
            }
            catch (Exception) { await Context.Channel.SendErrorAsync("無法私訊，請確認已開啟伺服器內成員私訊許可").ConfigureAwait(false); }
        }

        [Command("Status")]
        [Summary("顯示機器人目前的狀態")]
        [Alias("Stats")]
        public async Task StatusAsync()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor();
            embedBuilder.WithTitle("輔助小幫手");
#if DEBUG
            embedBuilder.Title += " (測試版)";
#endif

            embedBuilder.WithDescription($"建置版本 {Program.VERSION}");
            embedBuilder.AddField("作者", "孤之界#1121", true);
            embedBuilder.AddField("擁有者", $"{Program.ApplicatonOwner.Username}#{Program.ApplicatonOwner.Discriminator}", true);
            embedBuilder.AddField("狀態", $"伺服器 {_client.Guilds.Count}\n服務成員數 {_client.Guilds.Sum((x) => x.MemberCount)}", false);
            embedBuilder.AddField("上線時間", $"{Program.stopWatch.Elapsed:d\\天\\ hh\\:mm\\:ss}", false);

            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("Activity")]
        [Summary("幹話排行榜榜榜榜...")]
        [Alias("Act")]
        [RequireContext(ContextType.Guild)]
        public async Task Activity([Summary("頁數，預設為第一頁")] int page = 0)
        {
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var userActivity = (await UserActivity.GetActivityAsync(Context.Guild.Id).ConfigureAwait(false)).OrderByDescending((x) => x.ActivityNum).ToList();
            if (userActivity == null) return;
            var user = userActivity.FirstOrDefault((x) => x.UserID == Context.User.Id);

            await Context.SendPaginatedConfirmAsync(page, async (row) =>
            {
                EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor().WithTitle($"{Context.Guild.Name} 發言排行榜");
                var items = userActivity.Skip(row * 20).Take(20).ToList(); string temp = "";

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];

                    IUser user = Program.Client.GetUser(item.UserID);
                    if (user == null)
                    {
                        try { user = await Program.Client.Rest.GetUserAsync(item.UserID); }
                        catch { }
                        if (user == null)
                            continue;
                    }

                    temp += $"{row * 25 + i + 1}. {user.Username}[<@{item.UserID}>] `{item.ActivityNum}` 則訊息\n";
                }

                embedBuilder.WithDescription(temp);
                embedBuilder.WithFooter($"{row + 1} / {userActivity.Count / 25 + 1}" + (user != null ? $" | {Context.User.Username}的排名為: {userActivity.IndexOf(user) + 1}" : ""));
                return embedBuilder;
            }, userActivity.Count, 25, false).ConfigureAwait(false);
        }

        [Command("EmoteActivity")]
        [Summary("表情使用排行榜")]
        [Alias("EAct")]
        [RequireContext(ContextType.Guild)]
        public async Task EmoteActivity([Summary("頁數，預設為第一頁")] int page = 0)
        {
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var emoteActivity = (await SQLite.Activity.EmoteActivity.GetActivityAsync(Context.Guild.Id).ConfigureAwait(false)).OrderByDescending((x) => x.ActivityNum).ToList();
            if (emoteActivity == null) return;

            await Context.SendPaginatedConfirmAsync(page, (row) =>
            {
                EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor().WithTitle($"{Context.Guild.Name} 表情使用排行榜");
                var items = emoteActivity.Skip(row * 50).Take(50).ToList();
                var resultList = new List<string>();

                for (int i = 0; i < Math.Min(items.Count, 25); i++)
                {
                    var item = items[i];
                    resultList.Add($"`{row * 50 + i + 1}.` {item.EmoteName} `{item.ActivityNum} 次`");
                }

                if (items.Count >= 25)
                {
                    for (int i = 25; i < items.Count; i++)
                    {
                        var item = items[i];
                        resultList[i - 25] += ($"  |  `{row * 50 + i + 1}.` {item.EmoteName} `{item.ActivityNum} 次`");
                    }
                }

                embedBuilder.WithDescription(string.Join('\n', resultList));
                return embedBuilder;
            }, emoteActivity.Count, 50).ConfigureAwait(false);
        }

        [Command("EmoteUseCount")]
        [Summary("表情使用量")]
        [Alias("EUC")]
        [RequireContext(ContextType.Guild)]
        public async Task EmoteUseCount([Summary("表情")] string emote)
        {
            ulong emoteId;
            try
            {
                emoteId = ulong.Parse(emote.Split(new char[] { ':' })[2].TrimEnd('>'));
            }
            catch (Exception) { await Context.Channel.SendErrorAsync("輸入的參數非表情").ConfigureAwait(false); return; }

            GuildEmote emoteData;
            try
            {
                emoteData = await Context.Guild.GetEmoteAsync(emoteId).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await Context.Channel.SendErrorAsync("該表情不存在於伺服器內").ConfigureAwait(false);
                return;
            }

            var emoteActivityNum = await RedisConnection.RedisDb.StringGetAsync($"SupportBot:Activity:Emote:{Context.Guild.Id}:{emoteId}").ConfigureAwait(false);  //Todo: Fix
            if (emoteActivityNum.IsNull)
            {
                await Context.Channel.SendErrorAsync("該表情無使用紀錄").ConfigureAwait(false);
                return;
            }

            await Context.Channel.SendConfirmAsync($"{emoteData} {emoteActivityNum} 次").ConfigureAwait(false);
        }

        [Command("LinkTwitter")]
        [Summary("將伺服器連結到指定的Twitter帳號\r\n" +
            "\r\n" +
            "目前可以將伺服器頭像與該Twitter帳號同步\r\n" +
            "解除連結請使用 `!!!ut` 指令" +
            "(緩慢新增功能中...)\r\n\r\n" +
            "Bot需要 `管理伺服器` 權限\r\n" +
            "例:\r\n" +
            "!!!lt inui_toko")]
        [Alias("LT")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.ManageGuild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task LinkTwitter(string twitterName = "") //Todo: 讓管理員自訂推特變更紀錄頻道
        {
            if (string.IsNullOrWhiteSpace(twitterName))
            {
                await Context.Channel.SendErrorAsync("Twitter名稱不可為空").ConfigureAwait(false);
                return;
            }

            Tweetinvi.Models.IUser user;
            try
            {
                user = await _service.TwitterAppClient.Users.GetUserAsync(twitterName).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await Context.Channel.SendErrorAsync("該帳號已被封鎖或是不存在，請確認名稱是否正確").ConfigureAwait(false);
                return;
            }

            if (await PromptUserConfirmAsync(new EmbedBuilder().WithOkColor().WithDescription($"連結 `{user.Name}` - ({user.Id}) 嗎?")).ConfigureAwait(false))
            {
                using (var db = new SupportContext())
                {
                    if (db.GuildConfig.Any(x => x.GuildId == Context.Guild.Id))
                    {
                        var guild = db.GuildConfig.First((x) => x.GuildId == Context.Guild.Id);
                        guild.TwitterId = user.Id;
                        db.GuildConfig.Update(guild);
                    }
                    else
                    {
                        db.GuildConfig.Add(new GuildConfig() { GuildId = Context.Guild.Id, TwitterId = user.Id });
                    }

                    await db.SaveChangesAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync($"已連結 `{user.Name}`").ConfigureAwait(false);
            }
        }

        [Command("UnlinkTwitter")]
        [Summary("解除連結Twitter帳號\r\n\r\n" +
            "例:\r\n" +
            "!!!ut")]
        [Alias("UT")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task UnlinkTwitter()
        {
            using (var db = new SupportContext())
            {
                if (db.GuildConfig.Any(x => x.GuildId == Context.Guild.Id))
                {
                    var guild = db.GuildConfig.First((x) => x.GuildId == Context.Guild.Id);

                    if (guild.TwitterId != 0)
                    {
                        var twitterId = guild.TwitterId;
                        guild.TwitterId = 0;

                        db.GuildConfig.Update(guild);
                        await db.SaveChangesAsync().ConfigureAwait(false);

                        await Context.Channel.SendConfirmAsync($"已解除連結 `{twitterId}`").ConfigureAwait(false);
                        return;
                    }
                }
            }

            await Context.Channel.SendErrorAsync("伺服器未連結推特帳號，請使用 `!!!h lt` 查看說明").ConfigureAwait(false);
        }
    }
}
