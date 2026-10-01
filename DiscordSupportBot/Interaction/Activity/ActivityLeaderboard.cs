namespace DiscordSupportBot.Interaction.Activity
{
    /// <summary>
    /// 發言與表情排行榜的 Components V2 畫面，slash 指令、前綴指令和翻頁按鈕共用
    /// </summary>
    static class ActivityLeaderboard
    {
        public const int PageSize = 25;
        public const string KindMessage = "msg";
        public const string KindEmote = "emote";
        public const string PageButtonPrefix = "act";
        public const string MyRankButtonPrefix = "act-me";
        const string PageIndicatorId = "act-page";

        static readonly Color AccentColor = new(0, 229, 132);
        static readonly string[] Medals = ["🥇", "🥈", "🥉"];

        public record UserDisplay(string Name, string AvatarUrl);

        record TopEntry(string Text, string ThumbnailUrl);

        public static int GetLastPage(int count) => count <= 0 ? 0 : (count - 1) / PageSize;

        // 兩次點擊之間資料可能變少，頁數要夾回有效範圍
        public static int ClampPage(int page, int count) => Math.Clamp(page, 0, GetLastPage(count));

        public static List<UserTable> SortUsers(IEnumerable<UserTable> users)
            => users.OrderByDescending((x) => x.ActivityNum).ThenBy((x) => x.UserID).ToList();

        public static List<EmoteTable> SortEmotes(IEnumerable<EmoteTable> emotes)
            => emotes.OrderByDescending((x) => x.ActivityNum).ThenBy((x) => x.EmoteID).ToList();

        // 點號要跳脫，不然行首的「4. 」會被 Discord 當成有序清單而縮排
        public static string FormatRank(int rank)
            => rank <= Medals.Length ? Medals[rank - 1] : $"{rank}\\.";

        public static IEnumerable<T> GetPageItems<T>(IReadOnlyList<T> sorted, int page)
            => sorted.Skip(page * PageSize).Take(PageSize);

        /// <returns>沒有任何發言紀錄時回傳 null</returns>
        public static async Task<MessageComponent> CreateMessageLeaderboardAsync(DiscordSocketClient client, SocketGuild guild, ulong ownerId, int? page)
        {
            var sorted = SortUsers(await UserActivity.GetActivityAsync(guild.Id).ConfigureAwait(false));
            if (sorted.Count == 0)
                return null;

            // page 是 null 代表要跳到指令執行者自己的名次
            var ownerIndex = sorted.FindIndex((x) => x.UserID == ownerId);
            var targetPage = ClampPage(page ?? (ownerIndex >= 0 ? ownerIndex / PageSize : 0), sorted.Count);

            var users = await ResolveUsersAsync(client, guild, GetPageItems(sorted, targetPage).Select((x) => x.UserID)).ConfigureAwait(false);
            return BuildMessagePage(guild.Name, sorted, targetPage, ownerId, users);
        }

        /// <returns>沒有任何表情使用紀錄時回傳 null</returns>
        public static async Task<MessageComponent> CreateEmoteLeaderboardAsync(SocketGuild guild, ulong ownerId, int page)
        {
            var emotes = await EmoteActivity.GetActivityAsync(guild.Id).ConfigureAwait(false);
            var used = SortEmotes(emotes.Where((x) => x.ActivityNum > 0));
            if (used.Count == 0)
                return null;

            return BuildEmotePage(guild.Name, used, emotes.Count - used.Count, ClampPage(page, used.Count), ownerId);
        }

        /// <summary>
        /// 只查這一頁的人：先看 bot 快取的伺服器成員，沒有才打 REST，REST 也失敗就不放進結果
        /// </summary>
        public static async Task<Dictionary<ulong, UserDisplay>> ResolveUsersAsync(DiscordSocketClient client, SocketGuild guild, IEnumerable<ulong> userIds)
        {
            var result = new Dictionary<ulong, UserDisplay>();
            var missingIds = new List<ulong>();

            foreach (var userId in userIds)
            {
                if (guild.GetUser(userId) is SocketGuildUser guildUser)
                    result[userId] = new UserDisplay(guildUser.DisplayName, guildUser.GetDisplayAvatarUrl());
                else
                    missingIds.Add(userId);
            }

            var fetchedUsers = await Task.WhenAll(missingIds.Select(async (userId) =>
            {
                try
                {
                    return (UserId: userId, User: (IUser)await client.Rest.GetUserAsync(userId).ConfigureAwait(false));
                }
                catch (Exception)
                {
                    // 帳號刪除之類的情況會抓不到，畫面改顯示 mention 就好
                    return (UserId: userId, User: (IUser)null);
                }
            })).ConfigureAwait(false);

            foreach (var (userId, user) in fetchedUsers)
            {
                if (user != null)
                    result[userId] = new UserDisplay(user.GlobalName ?? user.Username, user.GetDisplayAvatarUrl());
            }

            return result;
        }

        public static MessageComponent BuildMessagePage(string guildName, IReadOnlyList<UserTable> sorted, int page, ulong ownerId, IReadOnlyDictionary<ulong, UserDisplay> users)
        {
            var lastPage = GetLastPage(sorted.Count);
            var items = GetPageItems(sorted, page).ToList();
            var startRank = page * PageSize + 1;

            string GetName(ulong userId)
                => users.TryGetValue(userId, out var user) ? Format.Sanitize(user.Name) : $"<@{userId}>";

            TopEntry top = null;
            var lines = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var rank = startRank + i;

                if (rank == 1)
                {
                    users.TryGetValue(item.UserID, out var user);
                    top = new TopEntry($"{Medals[0]} **{GetName(item.UserID)}**\n{item.ActivityNum:N0} 則訊息", user?.AvatarUrl);
                    continue;
                }

                var line = $"{FormatRank(rank)} {GetName(item.UserID)} · {item.ActivityNum:N0} 則";
                lines.Add(item.UserID == ownerId ? $"**{line}**" : line);
            }

            var ownerIndex = sorted.ToList().FindIndex((x) => x.UserID == ownerId);
            var footer = ownerIndex >= 0
                ? $"-# 你的排名：第 {ownerIndex + 1:N0} 名 · {sorted[ownerIndex].ActivityNum:N0} 則"
                : "-# 你還沒有發言紀錄";

            ActionRowBuilder actionRow = null;
            if (lastPage > 0)
            {
                actionRow = BuildPageButtons(KindMessage, ownerId, page, lastPage);
                if (ownerIndex >= 0)
                {
                    actionRow.WithButton(new ButtonBuilder()
                        .WithLabel("跳到我的名次")
                        .WithCustomId($"{MyRankButtonPrefix}:{ownerId}")
                        .WithStyle(ButtonStyle.Primary)
                        .WithDisabled(ownerIndex / PageSize == page));
                }
            }

            return BuildPage($"{Format.Sanitize(guildName)} 發言排行榜", $"共 {sorted.Count:N0} 位", page, lastPage, top, lines, footer, actionRow);
        }

        public static MessageComponent BuildEmotePage(string guildName, IReadOnlyList<EmoteTable> sortedUsed, int unusedCount, int page, ulong ownerId)
        {
            var lastPage = GetLastPage(sortedUsed.Count);
            var items = GetPageItems(sortedUsed, page).ToList();
            var startRank = page * PageSize + 1;

            TopEntry top = null;
            var lines = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var rank = startRank + i;

                if (rank == 1)
                {
                    // 有縮圖就已經看得到表情了，文字裡不用再放一次
                    var emoteText = string.IsNullOrEmpty(item.EmoteUrl) ? $" {item.EmoteName}" : "";
                    top = new TopEntry($"{Medals[0]}{emoteText}\n{item.ActivityNum:N0} 次", item.EmoteUrl);
                    continue;
                }

                lines.Add($"{FormatRank(rank)} {item.EmoteName} · {item.ActivityNum:N0} 次");
            }

            var footer = unusedCount > 0 ? $"-# 另有 {unusedCount:N0} 個表情未使用" : null;
            var actionRow = lastPage > 0 ? BuildPageButtons(KindEmote, ownerId, page, lastPage) : null;

            return BuildPage($"{Format.Sanitize(guildName)} 表情使用排行榜", $"共 {sortedUsed.Count:N0} 個表情", page, lastPage, top, lines, footer, actionRow);
        }

        static ActionRowBuilder BuildPageButtons(string kind, ulong ownerId, int page, int lastPage)
        {
            // 停用的按鈕也要有不重複的 custom_id，所以上一頁、下一頁直接帶 page ± 1
            return new ActionRowBuilder()
                .WithButton(new ButtonBuilder().WithLabel("◀").WithCustomId($"{PageButtonPrefix}:{kind}:{ownerId}:{page - 1}").WithStyle(ButtonStyle.Secondary).WithDisabled(page <= 0))
                .WithButton(new ButtonBuilder().WithLabel($"{page + 1} / {lastPage + 1}").WithCustomId(PageIndicatorId).WithStyle(ButtonStyle.Secondary).WithDisabled(true))
                .WithButton(new ButtonBuilder().WithLabel("▶").WithCustomId($"{PageButtonPrefix}:{kind}:{ownerId}:{page + 1}").WithStyle(ButtonStyle.Secondary).WithDisabled(page >= lastPage));
        }

        static MessageComponent BuildPage(string title, string summary, int page, int lastPage, TopEntry top, List<string> lines, string footer, ActionRowBuilder actionRow)
        {
            var container = new ContainerBuilder().WithAccentColor(AccentColor);
            container.AddComponent(new TextDisplayBuilder($"## {title}\n-# {summary} · 第 {page + 1} / {lastPage + 1} 頁"));

            if (top != null)
            {
                // Section 一定要有附件，沒有圖就改用一般文字
                if (string.IsNullOrEmpty(top.ThumbnailUrl))
                    container.AddComponent(new TextDisplayBuilder(top.Text));
                else
                    container.AddComponent(new SectionBuilder(new ThumbnailBuilder(new UnfurledMediaItemProperties(top.ThumbnailUrl)), [new TextDisplayBuilder(top.Text)]));
            }

            if (lines.Count > 0)
            {
                container.AddComponent(new SeparatorBuilder());
                container.AddComponent(new TextDisplayBuilder(string.Join('\n', lines)));
            }

            if (footer != null)
            {
                container.AddComponent(new SeparatorBuilder());
                container.AddComponent(new TextDisplayBuilder(footer));
            }

            if (actionRow != null)
                container.AddComponent(actionRow);

            return new ComponentBuilderV2().AddComponent(container).Build();
        }
    }
}
