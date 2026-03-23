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
            if (message is not SocketUserMessage userMessage)
                return;

            if (userMessage.Channel is not SocketTextChannel textChannel)
                return;

            var guildId = textChannel.Guild.Id;
            if (!_guildLinkFixes.TryGetValue(guildId, out var guildDict))
                return;

            var content = message.Content;
            if (string.IsNullOrWhiteSpace(content))
                return;

            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
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
                    // Enhance: Preserve spoiler formatting if present
                    var hasSpoiler = word.StartsWith("||") && word.EndsWith("||");
                    var newUrl = match.Groups["prefix"].Value + newDomain + match.Groups["suffix"].Value.Replace("||", "");
                    await userMessage.ReplyAsync(hasSpoiler ? "||" + newUrl + "||" : newUrl, allowedMentions: AllowedMentions.None);

                    try
                    {
                        await userMessage.ModifyAsync((act) => act.Flags = MessageFlags.SuppressEmbeds); // Suppress embeds to avoid confusion with the original link
                    }
                    catch { /* Ignore */ }

                    Log.Info($"[{textChannel.Guild}/{textChannel}/{userMessage.Author.Username}] (LinkFix): [{word}] => [{newUrl}]");
                }
                catch (Exception) { /* Ignore */ }
            }
        }

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
