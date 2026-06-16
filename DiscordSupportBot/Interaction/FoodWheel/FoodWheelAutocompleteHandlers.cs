using Discord.Interactions;
using DiscordSupportBot.Interaction.FoodWheel.Service;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordSupportBot.Interaction.FoodWheel
{
    /// <summary>
    /// 轉盤項目 Autocomplete 基底。類型 (食物/飲料) 由各指令隱含決定，
    /// 不從同層的選項讀取，避免使用者切換類型後 item 清單未更新的問題。
    /// </summary>
    public abstract class WheelAutocompleteHandlerBase : AutocompleteHandler
    {
        protected abstract IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId);

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var service = services.GetRequiredService<FoodWheelService>();
            var current = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";

            var results = GetCandidates(service, autocompleteInteraction.User.Id)
                .Where(x => string.IsNullOrEmpty(current) || x.Contains(current, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .Select(x => new AutocompleteResult(x, x));

            return Task.FromResult(AutocompletionResult.FromSuccess(results));
        }
    }

    // 黑名單新增：建議「可抽清單」中尚未被黑名單的項目
    public class FoodBlacklistAddAutocompleteHandler : WheelAutocompleteHandlerBase
    {
        protected override IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId)
            => service.GetEffectiveItems(userId, WheelType.Food);
    }

    public class DrinkBlacklistAddAutocompleteHandler : WheelAutocompleteHandlerBase
    {
        protected override IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId)
            => service.GetEffectiveItems(userId, WheelType.Drink);
    }

    // 黑名單移除：建議使用者已加入黑名單的項目
    public class FoodBlacklistRemoveAutocompleteHandler : WheelAutocompleteHandlerBase
    {
        protected override IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId)
            => service.GetEntries(userId, WheelType.Food, WheelEntryKind.Blacklist);
    }

    public class DrinkBlacklistRemoveAutocompleteHandler : WheelAutocompleteHandlerBase
    {
        protected override IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId)
            => service.GetEntries(userId, WheelType.Drink, WheelEntryKind.Blacklist);
    }

    // 自訂移除：建議使用者已新增的自訂項目
    public class FoodCustomRemoveAutocompleteHandler : WheelAutocompleteHandlerBase
    {
        protected override IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId)
            => service.GetEntries(userId, WheelType.Food, WheelEntryKind.Custom);
    }

    public class DrinkCustomRemoveAutocompleteHandler : WheelAutocompleteHandlerBase
    {
        protected override IEnumerable<string> GetCandidates(FoodWheelService service, ulong userId)
            => service.GetEntries(userId, WheelType.Drink, WheelEntryKind.Custom);
    }
}
