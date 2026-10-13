using Discord.Interactions;
using DiscordSupportBot.Interaction.FoodWheel.Service;

namespace DiscordSupportBot.Interaction.FoodWheel
{
    // /food-wheel blacklist ...、/food-wheel custom ...
    [Group("food-wheel", "食物轉盤")]
    public class FoodWheelModule : TopLevelModule<FoodWheelService>
    {
        [SlashCommand("spin", "不知道吃啥就轉一下")]
        public Task FoodWheelAsync()
            => WheelCommandHelper.SpinAsync(Context, _service, WheelType.Food, "吃");

        [Group("blacklist", "食物黑名單 (僅影響你自己)")]
        public class BlacklistModule : TopLevelModule<FoodWheelService>
        {
            [SlashCommand("add", "將食物加入轉盤黑名單")]
            public Task AddAsync([Autocomplete(typeof(FoodBlacklistAddAutocompleteHandler))] string item)
                => WheelCommandHelper.BlacklistAddAsync(Context, _service, WheelType.Food, item);

            [SlashCommand("remove", "將食物移出轉盤黑名單")]
            public Task RemoveAsync([Autocomplete(typeof(FoodBlacklistRemoveAutocompleteHandler))] string item)
                => WheelCommandHelper.BlacklistRemoveAsync(Context, _service, WheelType.Food, item);

            [SlashCommand("list", "查看你的食物黑名單")]
            public Task ListAsync()
                => WheelCommandHelper.BlacklistListAsync(Context, _service, WheelType.Food);
        }

        [Group("custom", "自訂食物 (僅影響你自己)")]
        public class CustomModule : TopLevelModule<FoodWheelService>
        {
            [SlashCommand("add", "新增自訂食物到你的轉盤")]
            public Task AddAsync(string item)
                => WheelCommandHelper.CustomAddAsync(Context, _service, WheelType.Food, item);

            [SlashCommand("remove", "移除你新增的自訂食物")]
            public Task RemoveAsync([Autocomplete(typeof(FoodCustomRemoveAutocompleteHandler))] string item)
                => WheelCommandHelper.CustomRemoveAsync(Context, _service, WheelType.Food, item);

            [SlashCommand("list", "查看你新增的自訂食物")]
            public Task ListAsync()
                => WheelCommandHelper.CustomListAsync(Context, _service, WheelType.Food);
        }
    }

    // /drink-wheel blacklist ...、/drink-wheel custom ...
    [Group("drink-wheel", "飲料轉盤")]
    public class DrinkWheelModule : TopLevelModule<FoodWheelService>
    {
        [SlashCommand("spin", "不知道喝啥就轉一下")]
        public Task DrinkWheelAsync()
            => WheelCommandHelper.SpinAsync(Context, _service, WheelType.Drink, "喝");

        [Group("blacklist", "飲料黑名單 (僅影響你自己)")]
        public class BlacklistModule : TopLevelModule<FoodWheelService>
        {
            [SlashCommand("add", "將飲料加入轉盤黑名單")]
            public Task AddAsync([Autocomplete(typeof(DrinkBlacklistAddAutocompleteHandler))] string item)
                => WheelCommandHelper.BlacklistAddAsync(Context, _service, WheelType.Drink, item);

            [SlashCommand("remove", "將飲料移出轉盤黑名單")]
            public Task RemoveAsync([Autocomplete(typeof(DrinkBlacklistRemoveAutocompleteHandler))] string item)
                => WheelCommandHelper.BlacklistRemoveAsync(Context, _service, WheelType.Drink, item);

            [SlashCommand("list", "查看你的飲料黑名單")]
            public Task ListAsync()
                => WheelCommandHelper.BlacklistListAsync(Context, _service, WheelType.Drink);
        }

        [Group("custom", "自訂飲料 (僅影響你自己)")]
        public class CustomModule : TopLevelModule<FoodWheelService>
        {
            [SlashCommand("add", "新增自訂飲料到你的轉盤")]
            public Task AddAsync(string item)
                => WheelCommandHelper.CustomAddAsync(Context, _service, WheelType.Drink, item);

            [SlashCommand("remove", "移除你新增的自訂飲料")]
            public Task RemoveAsync([Autocomplete(typeof(DrinkCustomRemoveAutocompleteHandler))] string item)
                => WheelCommandHelper.CustomRemoveAsync(Context, _service, WheelType.Drink, item);

            [SlashCommand("list", "查看你新增的自訂飲料")]
            public Task ListAsync()
                => WheelCommandHelper.CustomListAsync(Context, _service, WheelType.Drink);
        }
    }

    /// <summary>
    /// 轉盤指令共用邏輯，供各指令模組重用
    /// </summary>
    internal static class WheelCommandHelper
    {
        public static async Task SpinAsync(IInteractionContext context, FoodWheelService service, WheelType type, string verb)
        {
            var items = service.GetEffectiveItems(context.User.Id, type);
            if (items.Count == 0)
            {
                await context.Interaction.SendErrorAsync($"你已將全部{TypeName(type)}選項加入黑名單，沒有可抽的項目了");
                return;
            }

            var selected = items[Random.Shared.Next(items.Count)];
            await context.Interaction.RespondAsync(embed: new EmbedBuilder()
                .WithOkColor()
                .WithDescription($"{verb} {Format.Underline(selected)} 吧!")
                .WithFooter($"{TypeName(type)}清單數量: {items.Count}")
                .Build());
        }

        public static async Task BlacklistAddAsync(IInteractionContext context, FoodWheelService service, WheelType type, string item)
        {
            item = item.Trim();
            if (string.IsNullOrWhiteSpace(item))
            {
                await context.Interaction.SendErrorAsync("項目不可為空");
                return;
            }

            if (!service.GetEffectiveItems(context.User.Id, type).Contains(item))
            {
                await context.Interaction.SendErrorAsync($"{Format.Bold(item)} 不在可抽清單中，無法加入黑名單");
                return;
            }

            var success = await service.AddEntryAsync(context.User.Id, type, WheelEntryKind.Blacklist, item);
            if (success)
                await context.Interaction.SendConfirmAsync($"已將 {Format.Bold(item)} 加入{TypeName(type)}黑名單", ephemeral: true);
            else
                await context.Interaction.SendErrorAsync($"{Format.Bold(item)} 已在黑名單中");
        }

        public static async Task BlacklistRemoveAsync(IInteractionContext context, FoodWheelService service, WheelType type, string item)
        {
            item = item.Trim();
            var success = await service.RemoveEntryAsync(context.User.Id, type, WheelEntryKind.Blacklist, item);
            if (success)
                await context.Interaction.SendConfirmAsync($"已將 {Format.Bold(item)} 移出{TypeName(type)}黑名單", ephemeral: true);
            else
                await context.Interaction.SendErrorAsync($"{Format.Bold(item)} 不在黑名單中");
        }

        public static async Task BlacklistListAsync(IInteractionContext context, FoodWheelService service, WheelType type)
        {
            var items = service.GetEntries(context.User.Id, type, WheelEntryKind.Blacklist);
            if (items.Count == 0)
            {
                await context.Interaction.SendErrorAsync($"你尚未設定任何{TypeName(type)}黑名單");
                return;
            }

            await context.Interaction.SendConfirmAsync($"{TypeName(type)}黑名單 ({items.Count})", string.Join('\n', items.Select((x) => Format.Bold(x))), ephemeral: true);
        }

        public static async Task CustomAddAsync(IInteractionContext context, FoodWheelService service, WheelType type, string item)
        {
            item = item.Trim();
            if (string.IsNullOrWhiteSpace(item))
            {
                await context.Interaction.SendErrorAsync("項目不可為空");
                return;
            }

            if (item.Length > 50)
            {
                await context.Interaction.SendErrorAsync("項目長度過長 (上限 50 字)");
                return;
            }

            if (service.GetMasterList(type).Contains(item))
            {
                await context.Interaction.SendErrorAsync($"{Format.Bold(item)} 已是預設{TypeName(type)}項目，無需新增");
                return;
            }

            var success = await service.AddEntryAsync(context.User.Id, type, WheelEntryKind.Custom, item);
            if (success)
                await context.Interaction.SendConfirmAsync($"已新增自訂{TypeName(type)}項目 {Format.Bold(item)}", ephemeral: true);
            else
                await context.Interaction.SendErrorAsync($"{Format.Bold(item)} 已在你的自訂清單中");
        }

        public static async Task CustomRemoveAsync(IInteractionContext context, FoodWheelService service, WheelType type, string item)
        {
            item = item.Trim();
            var success = await service.RemoveEntryAsync(context.User.Id, type, WheelEntryKind.Custom, item);
            if (success)
                await context.Interaction.SendConfirmAsync($"已移除自訂{TypeName(type)}項目 {Format.Bold(item)}", ephemeral: true);
            else
                await context.Interaction.SendErrorAsync($"{Format.Bold(item)} 不在你的自訂清單中");
        }

        public static async Task CustomListAsync(IInteractionContext context, FoodWheelService service, WheelType type)
        {
            var items = service.GetEntries(context.User.Id, type, WheelEntryKind.Custom);
            if (items.Count == 0)
            {
                await context.Interaction.SendErrorAsync($"你尚未新增任何自訂{TypeName(type)}項目");
                return;
            }

            await context.Interaction.SendConfirmAsync($"自訂{TypeName(type)}項目 ({items.Count})", string.Join('\n', items.Select((x) => Format.Bold(x))), ephemeral: true);
        }

        private static string TypeName(WheelType type)
            => type == WheelType.Drink ? "飲料" : "食物";
    }
}
