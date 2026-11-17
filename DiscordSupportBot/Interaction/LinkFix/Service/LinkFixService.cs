using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DiscordSupportBot.Interaction.LinkFix.Service
{
    public partial class LinkFixService : IInteractionService
    {
        private readonly DiscordSocketClient _client;
        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>> _guildLinkFixes = new();

        public LinkFixService(DiscordSocketClient client)
        {
            _client = client;
            _client.MessageReceived += HandleMessageReceived;

            using var db = new SupportContext();

            foreach (var fix in db.LinkFixConfig.AsNoTracking())
            {
                var guildDict = _guildLinkFixes.GetOrAdd(fix.GuildId, _ => new(StringComparer.InvariantCultureIgnoreCase));
                guildDict.TryAdd(fix.OldDomain.ToLowerInvariant(), fix.NewDomain);
            }
        }

        private async Task HandleMessageReceived(SocketMessage message)
        {
            // 忽略機器人，否則會對其他機器人（含 webhook）的訊息回覆，可能互相觸發
            if (message is not SocketUserMessage userMessage || userMessage.Author.IsBot)
                return;

            if (userMessage.Channel is not SocketTextChannel textChannel)
                return;

            var guildId = textChannel.Guild.Id;
            if (!_guildLinkFixes.TryGetValue(guildId, out var guildDict))
                return;

            var content = message.Content;
            if (string.IsNullOrWhiteSpace(content))
                return;

            var replied = false;

            foreach (var wordMatch in WordRegex().Matches(content).Cast<Match>())
            {
                var word = wordMatch.Value;

                // Enhance: Skip words that are likely to be markdown links or code blocks
                if (word.StartsWith('<') && word.EndsWith('>') || word.StartsWith("~~") && word.EndsWith("~~"))
                    continue;

                var match = FullUrlRegex().Match(word);
                if (!match.Success)
                    continue;

                var domain = match.Groups["domain"].Value;
                if (string.IsNullOrWhiteSpace(domain))
                    continue;

                if (!guildDict.TryGetValue(domain, out var newDomain))
                    continue;

                try
                {
                    // 暴雷標記可能包住整段文字，不能只看單一詞，所以用網址在全文中的位置判斷
                    var hasSpoiler = IsInSpoiler(content, wordMatch.Index + match.Index);
                    var newUrl = match.Groups["prefix"].Value + newDomain + match.Groups["suffix"].Value.Replace("||", "");
                    await userMessage.ReplyAsync(hasSpoiler ? "||" + newUrl + "||" : newUrl, allowedMentions: AllowedMentions.None);
                    replied = true;

                    Log.Info($"[{textChannel.Guild}/{textChannel}/{userMessage.Author.Username}] (LinkFix): [{word}] => [{newUrl}]");
                }
                catch (Exception) { /* Ignore */ }
            }

            if (replied)
                _ = SuppressEmbedsAsync(userMessage);
        }

        /// <summary>
        /// 原始訊息的預覽是 Discord 非同步產生的，馬上壓制會來不及蓋掉；看到預覽就壓，沒預覽的網址等 10 秒後照樣壓一次
        /// </summary>
        private static async Task SuppressEmbedsAsync(SocketUserMessage userMessage)
        {
            try
            {
                for (var i = 0; i < 10 && userMessage.Embeds.Count == 0; i++)
                    await Task.Delay(TimeSpan.FromSeconds(1));

                await userMessage.ModifyAsync((act) => act.Flags = MessageFlags.SuppressEmbeds);
            }
            catch { /* Ignore */ }
        }

        public static bool IsInSpoiler(string content, int index)
        {
            var count = 0;
            for (var i = content.IndexOf("||", StringComparison.Ordinal); i >= 0 && i < index; i = content.IndexOf("||", i + 2, StringComparison.Ordinal))
                count++;

            return count % 2 == 1;
        }

        /// <summary>
        /// 連結修正的回覆內容只會有修正後的網址（可能被暴雷標記包住），用來辨識可刪除的訊息
        /// </summary>
        public static bool IsLinkFixContent(string? content)
            => content is not null && LinkFixContentRegex().IsMatch(content);

        [GeneratedRegex(@"^(?:\|\|)?https?://\S+$")]
        private static partial Regex LinkFixContentRegex();

        [GeneratedRegex(@"\S+")]
        private static partial Regex WordRegex();

        [GeneratedRegex(@"(?<prefix>https?://)(?:www.)?(?<domain>[a-zA-Z0-9\-\.]+)(?<suffix>/.*)?")]
        private partial Regex FullUrlRegex();

        [GeneratedRegex(@"(?<prefix>https?://)?(?:ww[w\d].)?(?<domain>[a-zA-Z0-9\-\.]+)(?<suffix>/.*)?")]
        public partial Regex PartialUrlRegex();

        public async Task<bool> AddLinkFixAsync(ulong guildId, string oldDomain, string newDomain)
        {
            oldDomain = oldDomain.ToLowerInvariant();

            var guildDict = _guildLinkFixes.GetOrAdd(guildId, _ => new ConcurrentDictionary<string, string>());
            guildDict[oldDomain] = newDomain;

            using var db = new SupportContext();
            var linkFixConfig = db.LinkFixConfig.FirstOrDefault((x) => x.GuildId == guildId && x.OldDomain == oldDomain)
                ?? new LinkFixConfig()
                {
                    GuildId = guildId,
                    OldDomain = oldDomain
                };
            linkFixConfig.NewDomain = newDomain;
            db.LinkFixConfig.Update(linkFixConfig);

            await db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveLinkFixAsync(ulong guildId, string oldDomain)
        {
            oldDomain = oldDomain.ToLowerInvariant();

            if (!_guildLinkFixes.TryGetValue(guildId, out var guildDict) || !guildDict.TryRemove(oldDomain, out _))
                return false;

            using var db = new SupportContext();
            var linkFixConfig = db.LinkFixConfig.FirstOrDefault((x) => x.GuildId == guildId && x.OldDomain == oldDomain);
            if (linkFixConfig != null)
                db.LinkFixConfig.Remove(linkFixConfig);

            await db.SaveChangesAsync();

            return true;
        }

        public IReadOnlyDictionary<string, string> GetLinkFixes(ulong guildId)
        {
            if (_guildLinkFixes.TryGetValue(guildId, out var guildDict))
                return guildDict;

            return new Dictionary<string, string>();
        }
    }
}
