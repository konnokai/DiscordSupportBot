using Discord.Interactions;
using DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordSupportBot.Interaction.AutoCreatePrivateThread
{
    public class AutoCreatePrivateThreadSetupAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            if (context.Channel is not SocketTextChannel channel)
                return AutocompletionResult.FromSuccess();

            var client = services.GetRequiredService<DiscordSocketClient>();
            var current = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";
            var messages = await channel.GetMessagesAsync(100).FlattenAsync().ConfigureAwait(false);
            var results = messages
                .Where(x => x.Author.Id == client.CurrentUser.Id)
                .Select(x => new { Message = x, Label = FormatMessage(x) })
                .Where(x => Matches(x.Message, x.Label, current))
                .Take(25)
                .Select(x => new AutocompleteResult(x.Label, x.Message.Id.ToString()));

            return AutocompletionResult.FromSuccess(results);
        }

        internal static string FormatMessage(IMessage message)
        {
            var content = message.Content
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
            if (string.IsNullOrEmpty(content))
                content = "無文字內容";
            if (content.Length > 72)
                content = content[..69] + "...";

            return $"{message.Id} | {content}";
        }

        private static bool Matches(IMessage message, string label, string current)
            => string.IsNullOrWhiteSpace(current) ||
               message.Id.ToString().Contains(current, StringComparison.OrdinalIgnoreCase) ||
               label.Contains(current, StringComparison.OrdinalIgnoreCase);
    }

    public class AutoCreatePrivateThreadConfigAutocompleteHandler : AutocompleteHandler
    {
        public override Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            if (context.Guild == null || context.Channel is not SocketTextChannel channel)
                return Task.FromResult(AutocompletionResult.FromSuccess());

            var service = services.GetRequiredService<AutoCreatePrivateThreadService>();
            var current = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";
            var cachedMessages = channel.CachedMessages.ToDictionary(x => x.Id);
            var results = service.GetConfigs(context.Guild.Id, channel.Id)
                .Select(x =>
                {
                    var label = cachedMessages.TryGetValue(x.MessageId, out var message)
                        ? AutoCreatePrivateThreadSetupAutocompleteHandler.FormatMessage(message)
                        : $"訊息 {x.MessageId}";
                    return new { x.MessageId, Label = label };
                })
                .Where(x => string.IsNullOrWhiteSpace(current) ||
                            x.MessageId.ToString().Contains(current, StringComparison.OrdinalIgnoreCase) ||
                            x.Label.Contains(current, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .Select(x => new AutocompleteResult(x.Label, x.MessageId.ToString()));

            return Task.FromResult(AutocompletionResult.FromSuccess(results));
        }
    }
}
