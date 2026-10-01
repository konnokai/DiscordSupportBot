using Discord.Commands;
using DiscordSupportBot.Interaction.Activity;

namespace DiscordSupportBot.Command.Normal
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
            embedBuilder.AddField("上線時間", $"{Program.StopWatch.Elapsed:d\\天\\ hh\\:mm\\:ss}", false);

            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("Activity")]
        [Summary("幹話排行榜榜榜榜...")]
        [Alias("Act")]
        [RequireContext(ContextType.Guild)]
        public async Task Activity([Summary("頁數，預設為第一頁")] int page = 1)
        {
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var components = await ActivityLeaderboard.CreateMessageLeaderboardAsync(_client, Context.Guild, Context.User.Id, page - 1).ConfigureAwait(false);
            if (components == null) return;

            await Context.Channel.SendMessageAsync(components: components, allowedMentions: AllowedMentions.None).ConfigureAwait(false);
        }

        [Command("EmoteActivity")]
        [Summary("表情使用排行榜")]
        [Alias("EAct")]
        [RequireContext(ContextType.Guild)]
        public async Task EmoteActivity([Summary("頁數，預設為第一頁")] int page = 1)
        {
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var components = await ActivityLeaderboard.CreateEmoteLeaderboardAsync(Context.Guild, Context.User.Id, page - 1).ConfigureAwait(false);
            if (components == null) return;

            await Context.Channel.SendMessageAsync(components: components, allowedMentions: AllowedMentions.None).ConfigureAwait(false);
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
    }
}
