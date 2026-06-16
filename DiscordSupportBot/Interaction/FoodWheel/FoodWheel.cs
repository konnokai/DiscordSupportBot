using Discord.Interactions;
using DiscordSupportBot.Interaction.FoodWheel.Service;

namespace DiscordSupportBot.Interaction.FoodWheel
{
    public class FoodWheel : TopLevelModule<FoodWheelService>
    {
        readonly Random _random = new();

        [SlashCommand("food-wheel", "不知道吃啥就轉一下")]
        public async Task FoodWheelAsync()
            => await SpinAsync(WheelType.Food, "吃");

        [SlashCommand("drink-wheel", "不知道喝啥就轉一下")]
        public async Task DrinkWheelAsync()
            => await SpinAsync(WheelType.Drink, "喝");

        private async Task SpinAsync(WheelType type, string verb)
        {
            var items = _service.GetEffectiveItems(Context.User.Id, type);
            if (items.Count == 0)
            {
                await Context.Interaction.SendErrorAsync($"你已將全部{TypeName(type)}選項加入黑名單，沒有可抽的項目了");
                return;
            }

            var selected = items[_random.Next(items.Count)];
            await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                .WithOkColor()
                .WithDescription($"{verb} {Format.Underline(selected)} 吧!")
                .WithFooter($"{TypeName(type)}清單數量: {items.Count}")
                .Build());
        }

        #region 黑名單

        [SlashCommand("wheel-blacklist-add", "將項目加入轉盤黑名單")]
        public async Task BlacklistAddAsync(WheelType type, [Autocomplete(typeof(BlacklistAddAutocompleteHandler))] string item)
        {
            item = item.Trim();
            if (string.IsNullOrWhiteSpace(item))
            {
                await Context.Interaction.SendErrorAsync("項目不可為空");
                return;
            }

            if (!_service.GetEffectiveItems(Context.User.Id, type).Contains(item))
            {
                await Context.Interaction.SendErrorAsync($"{Format.Bold(item)} 不在可抽清單中，無法加入黑名單");
                return;
            }

            var success = await _service.AddEntryAsync(Context.User.Id, type, WheelEntryKind.Blacklist, item);
            if (success)
                await Context.Interaction.SendConfirmAsync($"已將 {Format.Bold(item)} 加入{TypeName(type)}黑名單", ephemeral: true);
            else
                await Context.Interaction.SendErrorAsync($"{Format.Bold(item)} 已在黑名單中");
        }

        [SlashCommand("wheel-blacklist-remove", "將項目移出轉盤黑名單")]
        public async Task BlacklistRemoveAsync(WheelType type, [Autocomplete(typeof(BlacklistRemoveAutocompleteHandler))] string item)
        {
            item = item.Trim();
            var success = await _service.RemoveEntryAsync(Context.User.Id, type, WheelEntryKind.Blacklist, item);
            if (success)
                await Context.Interaction.SendConfirmAsync($"已將 {Format.Bold(item)} 移出{TypeName(type)}黑名單", ephemeral: true);
            else
                await Context.Interaction.SendErrorAsync($"{Format.Bold(item)} 不在黑名單中");
        }

        [SlashCommand("wheel-blacklist-list", "查看你的轉盤黑名單")]
        public async Task BlacklistListAsync(WheelType type)
        {
            var items = _service.GetEntries(Context.User.Id, type, WheelEntryKind.Blacklist);
            if (items.Count == 0)
            {
                await Context.Interaction.SendErrorAsync($"你尚未設定任何{TypeName(type)}黑名單");
                return;
            }

            await Context.Interaction.SendConfirmAsync($"{TypeName(type)}黑名單 ({items.Count})", string.Join('\n', items.Select((x) => Format.Bold(x))), ephemeral: true);
        }

        #endregion

        #region 自訂項目

        [SlashCommand("wheel-custom-add", "新增自訂項目到你的轉盤")]
        public async Task CustomAddAsync(WheelType type, string item)
        {
            item = item.Trim();
            if (string.IsNullOrWhiteSpace(item))
            {
                await Context.Interaction.SendErrorAsync("項目不可為空");
                return;
            }

            if (item.Length > 50)
            {
                await Context.Interaction.SendErrorAsync("項目長度過長 (上限 50 字)");
                return;
            }

            if (_service.GetMasterList(type).Contains(item))
            {
                await Context.Interaction.SendErrorAsync($"{Format.Bold(item)} 已是預設{TypeName(type)}項目，無需新增");
                return;
            }

            var success = await _service.AddEntryAsync(Context.User.Id, type, WheelEntryKind.Custom, item);
            if (success)
                await Context.Interaction.SendConfirmAsync($"已新增自訂{TypeName(type)}項目 {Format.Bold(item)}", ephemeral: true);
            else
                await Context.Interaction.SendErrorAsync($"{Format.Bold(item)} 已在你的自訂清單中");
        }

        [SlashCommand("wheel-custom-remove", "移除你新增的自訂項目")]
        public async Task CustomRemoveAsync(WheelType type, [Autocomplete(typeof(CustomRemoveAutocompleteHandler))] string item)
        {
            item = item.Trim();
            var success = await _service.RemoveEntryAsync(Context.User.Id, type, WheelEntryKind.Custom, item);
            if (success)
                await Context.Interaction.SendConfirmAsync($"已移除自訂{TypeName(type)}項目 {Format.Bold(item)}", ephemeral: true);
            else
                await Context.Interaction.SendErrorAsync($"{Format.Bold(item)} 不在你的自訂清單中");
        }

        [SlashCommand("wheel-custom-list", "查看你新增的自訂項目")]
        public async Task CustomListAsync(WheelType type)
        {
            var items = _service.GetEntries(Context.User.Id, type, WheelEntryKind.Custom);
            if (items.Count == 0)
            {
                await Context.Interaction.SendErrorAsync($"你尚未新增任何自訂{TypeName(type)}項目");
                return;
            }

            await Context.Interaction.SendConfirmAsync($"自訂{TypeName(type)}項目 ({items.Count})", string.Join('\n', items.Select((x) => Format.Bold(x))), ephemeral: true);
        }

        #endregion

        private static string TypeName(WheelType type)
            => type == WheelType.Drink ? "飲料" : "食物";
    }
}
