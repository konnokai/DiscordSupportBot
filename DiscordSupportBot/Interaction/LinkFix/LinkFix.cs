using Discord.Interactions;
using DiscordSupportBot.Interaction.LinkFix.Service;

namespace DiscordSupportBot.Interaction.LinkFix
{
    public class LinkFix : TopLevelModule<LinkFixService>
    {
        [SlashCommand("link-fix", "連結修正")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task LinkFixAsync(string oldDomain, string? newDomain = null)
        {
            if (string.IsNullOrWhiteSpace(newDomain))
            {
                var rmSuccess = await _service.RemoveLinkFixAsync(Context.Guild.Id, oldDomain);

                if (rmSuccess)
                    await Context.Interaction.SendConfirmAsync($"已移除 {Format.Bold(oldDomain)} 的連結修正");
                else
                    await Context.Interaction.SendErrorAsync($"找不到 {Format.Bold(oldDomain)} 的連結修正");

                return;
            }

            oldDomain = CleanDomain(oldDomain);
            newDomain = newDomain.Trim();

            if (string.IsNullOrWhiteSpace(oldDomain) || string.IsNullOrWhiteSpace(newDomain))
            {
                await Context.Interaction.SendErrorAsync($"新舊網域都必須有效");
                return;
            }

            if (oldDomain == newDomain)
            {
                await Context.Interaction.SendErrorAsync($"新舊網域不能相同");
                return;
            }

            var success = await _service.AddLinkFixAsync(Context.Guild.Id, oldDomain, newDomain);
            if (success)
                await Context.Interaction.SendConfirmAsync($"{Format.Bold(oldDomain)} 現在會被修正為 {Format.Bold(newDomain)}");
            else
                await Context.Interaction.SendErrorAsync($"{Format.Bold(oldDomain)} 已存在"); // 原則上不會觸發
        }

        [RequireContext(ContextType.Guild)]
        public async Task LinkFixList()
        {
            var linkFixes = _service.GetLinkFixes(Context.Guild.Id);
            if (linkFixes.Count == 0)
            {
                await Context.Interaction.SendErrorAsync("此伺服器尚未設定連結修正");
                return;
            }

            var items = linkFixes.Select(x => $"{Format.Bold(x.Key)} -> {Format.Bold(x.Value)}").ToList();
            await Context.SendPaginatedConfirmAsync(0, (page) =>
            {
                return new EmbedBuilder()
                    .WithTitle("連結修正")
                    .WithDescription(string.Join('\n', items.Skip(page * 10).Take(10)))
                    .WithOkColor();
            }, linkFixes.Count, 10);
        }


        private string CleanDomain(string domain)
        {
            var match = _service.PartialUrlRegex().Match(domain);
            if (!match.Success)
                return string.Empty;

            return match.Groups["domain"].ToString().ToLowerInvariant();
        }
    }
}
