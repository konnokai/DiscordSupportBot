using Discord.Interactions;
using DiscordSupportBot.Interaction.FoodWheel.Service;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordSupportBot.Interaction.FoodWheel
{
    /// <summary>
    /// 轉盤項目 Autocomplete 基底，依 type 選項與使用者提供候選清單
    /// </summary>
    public abstract class WheelAutocompleteHandlerBase : AutocompleteHandler
    {
        protected abstract IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId, WheelType type);

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var service = services.GetRequiredService<FoodWheelService>();

            var type = ParseWheelType(autocompleteInteraction);
            var current = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";

            var results = GetCandidates(service, autocompleteInteraction.User.Id, type)
                .Where(x => string.IsNullOrEmpty(current) || x.Contains(current, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .Select(x => new AutocompleteResult(x, x));

            return Task.FromResult(AutocompletionResult.FromSuccess(results));
        }

        private static WheelType ParseWheelType(IAutocompleteInteraction autocompleteInteraction)
        {
            var typeOption = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == "type");
            if (typeOption?.Value is null)
                return WheelType.Food;

            if (typeOption.Value is string s && Enum.TryParse<WheelType>(s, true, out var parsed))
                return parsed;

            if (int.TryParse(typeOption.Value.ToString(), out var i) && Enum.IsDefined(typeof(WheelType), i))
                return (WheelType)i;

            return WheelType.Food;
        }
    }

    /// <summary>
    /// 黑名單新增：建議「可抽清單」中尚未被黑名單的項目
    /// </summary>
    public class BlacklistAddAutocompleteHandler : WheelAutocompleteHandlerBase
    {
        protected override IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId, WheelType type)
            => service.GetEffectiveItems(userId, type);
    }

    /// <summary>
    /// 黑名單移除：建議使用者已加入黑名單的項目
    /// </summary>
    public class BlacklistRemoveAutocompleteHandler : WheelAutocompleteHandlerBase
    {
        protected override IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId, WheelType type)
            => service.GetEntries(userId, type, WheelEntryKind.Blacklist);
    }

    /// <summary>
    /// 自訂移除：建議使用者已新增的自訂項目
    /// </summary>
    public class CustomRemoveAutocompleteHandler : WheelAutocompleteHandlerBase
    {
        protected override IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId, WheelType type)
            => service.GetEntries(userId, type, WheelEntryKind.Custom);
    }
}
