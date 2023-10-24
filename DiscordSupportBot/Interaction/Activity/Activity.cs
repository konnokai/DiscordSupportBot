using Discord.Interactions;

namespace DiscordSupportBot.Interaction.Activity
{
    public class Activity : TopLevelModule
    {
        [SlashCommand("message-activity", "幹話排行榜")]
        [RequireContext(ContextType.Guild)]
        public async Task MessageActivityAsync([Summary("頁數", "預設為第一頁")] int page = 0)
        {
            await DeferAsync();

            var userActivity = (await UserActivity.GetActivityAsync(Context.Guild.Id).ConfigureAwait(false)).OrderByDescending((x) => x.ActivityNum).ToList();
            if (!userActivity.Any())
            {
                await Context.Interaction.SendErrorAsync("此伺服器無訊息紀錄", true).ConfigureAwait(false);
                return;
            }

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
            }, userActivity.Count, 25, false, false, true).ConfigureAwait(false);
        }

        [SlashCommand("emote-activity", "表情使用排行榜")]
        [RequireContext(ContextType.Guild)]
        public async Task EmoteActivityAsync([Summary("頁數", "預設為第一頁")] int page = 0)
        {
            await DeferAsync();

            var emoteActivity = (await EmoteActivity.GetActivityAsync(Context.Guild.Id).ConfigureAwait(false)).OrderByDescending((x) => x.ActivityNum).ToList();
            if (!emoteActivity.Any())
            {
                await Context.Interaction.SendErrorAsync("此伺服器無表情紀錄", true).ConfigureAwait(false);
                return;
            }

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
            }, emoteActivity.Count, 50, true, false, true).ConfigureAwait(false);
        }

        [SlashCommand("emote-use-count", "表情使用量")]
        [RequireContext(ContextType.Guild)]
        public async Task EmoteUseCountAsync([Summary("表情")] string emote)
        {
            ulong emoteId;
            try
            {
                emoteId = ulong.Parse(emote.Split(new char[] { ':' })[2].TrimEnd('>'));
            }
            catch (Exception) { await Context.Interaction.SendErrorAsync("輸入的參數非表情").ConfigureAwait(false); return; }

            GuildEmote emoteData;
            try
            {
                emoteData = await Context.Guild.GetEmoteAsync(emoteId).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await Context.Interaction.SendErrorAsync("該表情不存在於伺服器內").ConfigureAwait(false);
                return;
            }

            var emoteActivityList = (await EmoteActivity.GetActivityAsync(Context.Guild.Id).ConfigureAwait(false)).OrderByDescending((x) => x.ActivityNum).ToList();
            if (!emoteActivityList.Any())
            {
                await Context.Interaction.SendErrorAsync("此伺服器無表情紀錄").ConfigureAwait(false);
                return;
            }

            var emoteTable = emoteActivityList.FirstOrDefault((x) => x.EmoteID == emoteData.Id);
            if (emoteTable == null)
            {
                await Context.Interaction.SendErrorAsync("該表情在資料庫內無資料").ConfigureAwait(false);
                return;
            }

            await Context.Interaction.SendConfirmAsync($"{emoteData} {emoteTable.ActivityNum} 次").ConfigureAwait(false);
        }
    }
}
