using Discord.Interactions;

namespace DiscordSupportBot.Interaction.Activity
{
    public class Activity : TopLevelModule
    {
        [SlashCommand("message-activity", "幹話排行榜")]
        [RequireContext(ContextType.Guild)]
        public async Task MessageActivityAsync([Summary("頁數", "預設為第一頁")] int page = 1)
        {
            await DeferAsync();

            var components = await ActivityLeaderboard.CreateMessageLeaderboardAsync(Context.Client, Context.Guild, Context.User.Id, page - 1).ConfigureAwait(false);
            if (components == null)
            {
                await Context.Interaction.SendErrorAsync("此伺服器無訊息紀錄", true).ConfigureAwait(false);
                return;
            }

            await FollowupAsync(components: components, allowedMentions: AllowedMentions.None).ConfigureAwait(false);
        }

        [SlashCommand("emote-activity", "表情使用排行榜")]
        [RequireContext(ContextType.Guild)]
        public async Task EmoteActivityAsync([Summary("頁數", "預設為第一頁")] int page = 1)
        {
            await DeferAsync();

            var components = await ActivityLeaderboard.CreateEmoteLeaderboardAsync(Context.Guild, Context.User.Id, page - 1).ConfigureAwait(false);
            if (components == null)
            {
                await Context.Interaction.SendErrorAsync("此伺服器無表情紀錄", true).ConfigureAwait(false);
                return;
            }

            await FollowupAsync(components: components, allowedMentions: AllowedMentions.None).ConfigureAwait(false);
        }

        [SlashCommand("emote-unused", "未使用表情排行榜")]
        [RequireContext(ContextType.Guild)]
        public async Task EmoteUnusedAsync(
            [Summary("至多次數", "只列出使用次數不超過此值的表情，設定 5 會列出 0～5 次的表情，預設 0 只列出未使用過的表情")][MinValue(0)] int maxUsage = 0,
            [Summary("頁數", "預設為第一頁")] int page = 1)
        {
            await DeferAsync();

            var components = await ActivityLeaderboard.CreateUnusedEmoteLeaderboardAsync(Context.Guild, Context.User.Id, maxUsage, page - 1).ConfigureAwait(false);
            if (components == null)
            {
                await Context.Interaction.SendErrorAsync($"沒有使用次數不超過 {maxUsage:N0} 次的表情", true).ConfigureAwait(false);
                return;
            }

            await FollowupAsync(components: components, allowedMentions: AllowedMentions.None).ConfigureAwait(false);
        }

        [ComponentInteraction($"{ActivityLeaderboard.PageButtonPrefix}:*:*:*")]
        [RequireContext(ContextType.Guild)]
        public Task ChangePageAsync(string kind, ulong ownerId, int page)
            => UpdateLeaderboardAsync(kind, ownerId, page);

        [ComponentInteraction($"{ActivityLeaderboard.UnusedPageButtonPrefix}:*:*:*")]
        [RequireContext(ContextType.Guild)]
        public Task ChangeUnusedPageAsync(int maxUsage, ulong ownerId, int page)
            => UpdateLeaderboardAsync(ActivityLeaderboard.KindUnusedEmote, ownerId, page, maxUsage);

        [ComponentInteraction($"{ActivityLeaderboard.MyRankButtonPrefix}:*")]
        [RequireContext(ContextType.Guild)]
        public Task JumpToMyRankAsync(ulong ownerId)
            => UpdateLeaderboardAsync(ActivityLeaderboard.KindMessage, ownerId, null);

        private async Task UpdateLeaderboardAsync(string kind, ulong ownerId, int? page, int maxUsage = 0)
        {
            if (Context.User.Id != ownerId)
            {
                await RespondAsync("僅指令執行者可以翻頁", ephemeral: true).ConfigureAwait(false);
                return;
            }

            await DeferAsync();

            var components = kind switch
            {
                ActivityLeaderboard.KindUnusedEmote => await ActivityLeaderboard.CreateUnusedEmoteLeaderboardAsync(Context.Guild, ownerId, maxUsage, page ?? 0).ConfigureAwait(false),
                ActivityLeaderboard.KindEmote => await ActivityLeaderboard.CreateEmoteLeaderboardAsync(Context.Guild, ownerId, page ?? 0).ConfigureAwait(false),
                _ => await ActivityLeaderboard.CreateMessageLeaderboardAsync(Context.Client, Context.Guild, ownerId, page).ConfigureAwait(false),
            };
            if (components == null)
            {
                await FollowupAsync("排行榜已經沒有資料了", ephemeral: true).ConfigureAwait(false);
                return;
            }

            // 更新訊息時 Discord.Net 不會自動補 V2 flag，要自己帶
            await ModifyOriginalResponseAsync((x) =>
            {
                x.Components = components;
                x.AllowedMentions = AllowedMentions.None;
                x.Flags = MessageFlags.ComponentsV2;
            }).ConfigureAwait(false);
        }

        [SlashCommand("emote-use-count", "表情使用量")]
        [RequireContext(ContextType.Guild)]
        public async Task EmoteUseCountAsync([Summary("表情")] string emote)
        {
            ulong emoteId;
            try
            {
                emoteId = ulong.Parse(emote.Split([':'])[2].TrimEnd('>'));
            }
            catch (Exception)
            {
                await Context.Interaction.SendErrorAsync("輸入的參數非表情").ConfigureAwait(false);
                return;
            }

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

            var emoteActivityList = (await EmoteActivity.GetActivityAsync(Context.Guild.Id).ConfigureAwait(false)).ToList();
            if (emoteActivityList.Count == 0)
            {
                await Context.Interaction.SendErrorAsync("此伺服器無表情紀錄").ConfigureAwait(false);
                return;
            }

            var emoteTable = emoteActivityList.FirstOrDefault((x) => x.EmoteID == emoteData.Id);
            if (emoteTable == null)
            {
                await Context.Interaction.SendErrorAsync("該表情不存在於伺服器內").ConfigureAwait(false);
                return;
            }

            await Context.Interaction.SendConfirmAsync($"{emoteData} {emoteTable.ActivityNum} 次").ConfigureAwait(false);
        }
    }
}
