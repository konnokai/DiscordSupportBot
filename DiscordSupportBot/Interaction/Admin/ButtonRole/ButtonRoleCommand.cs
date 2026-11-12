using Discord.Interactions;
using DiscordSupportBot.Interaction.Admin.Service;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DiscordSupportBot.Interaction.Admin
{
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class ButtonRoleCommand : TopLevelModule<ButtonRoleService>
    {
        public class RemoveButtonRoleAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                if (autocompleteInteraction.GuildId is not ulong guildId)
                    return Task.FromResult(AutocompletionResult.FromSuccess());

                using var db = SupportContext.GetDbContext();
                var query = db.ButtonRole.AsNoTracking().Where((x) => x.GuildId == guildId);

                string value = autocompleteInteraction.Data.Current.Value?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(value))
                    query = query.Where((x) => x.EmojiName.Contains(value));

                var results = query
                    .OrderBy((x) => x.MessageId)
                    .Take(25)
                    .ToList()
                    .Select((x) => new AutocompleteResult($"{x.MessageId}: {x.EmojiName} ({x.RoleId})", $"{x.MessageId}|{x.EmojiName}"))
                    .ToList();

                return Task.FromResult(AutocompletionResult.FromSuccess(results));
            }
        }

        [SlashCommand("add-button-role", "新增按鈕用戶組")]
        public async Task AddButtonRole(
            [Summary("訊息Id", "要設定的訊息 Id")] string messageIdStr,
            [Summary("按鈕名稱", "要設定按鈕的名稱")] string buttonStr,
            [Summary("表情", "僅可輸入此伺服器或預設的表情符號")] string emoteStr,
            [Summary("用戶組")] IRole role,
            [Summary("類型", "Normal 可切換，Verify 只給予不移除")] ButtonRole.ReactionType reactionType)
        {
            await DeferAsync(true);

            if (!await HasManagePermissionAsync())
                return;

            if (!ulong.TryParse(messageIdStr, out ulong messageId))
            {
                await Context.Interaction.SendErrorAsync("格式錯誤，請確認訊息 Id 是否正確", true);
                return;
            }

            var msg = await Context.Channel.GetMessageAsync(messageId);
            if (msg is null)
            {
                await Context.Interaction.SendErrorAsync("找不到訊息，請確認訊息 Id 是否正確", true);
                return;
            }

            if (msg.Author.Id != Context.Client.CurrentUser.Id)
            {
                await Context.Interaction.SendErrorAsync("此訊息並非本 Bot 發送，無法修改訊息", true);
                return;
            }

            if (msg is not IUserMessage socketUserMessage)
            {
                await Context.Interaction.SendErrorAsync("此訊息的類型錯誤，無法修改訊息", true);
                return;
            }

            if (!_service.IsManagedMessage(messageId) && socketUserMessage.Components.Count > 0)
            {
                await Context.Interaction.SendErrorAsync("此訊息已有其他按鈕，請改用沒有按鈕的訊息", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(buttonStr) || buttonStr.Length > 80)
            {
                await Context.Interaction.SendErrorAsync("按鈕名稱需為 1~80 字元", true);
                return;
            }

            if (role.Id == Context.Guild.EveryoneRole.Id)
            {
                await Context.Interaction.SendErrorAsync("不可使用 Everyone 用戶組", true);
                return;
            }

            var guildUser = (IGuildUser)Context.User;
            var userTopPosition = guildUser.RoleIds
                .Select((x) => Context.Guild.GetRole(x))
                .Where((x) => x is not null)
                .Max((x) => x!.Position);
            if (Context.User.Id != Context.Guild.OwnerId && userTopPosition <= role.Position)
            {
                await Context.Interaction.SendErrorAsync($"{role} 位置比你目前持有的用戶組高，請調整用戶組順序後重試", true);
                return;
            }

            if (role.Position >= Context.Guild.CurrentUser.Roles.Max((x) => x.Position))
            {
                await Context.Interaction.SendErrorAsync($"我的用戶組比 {role} 低，請調整用戶組順序後重試", true);
                return;
            }

            emoteStr = emoteStr.Trim();

            IEmote emote;
            try
            {
                emote = Emote.TryParse(emoteStr, out var maybeEmote) ? maybeEmote : new Emoji(emoteStr);
            }
            catch (ArgumentException)
            {
                await Context.Interaction.SendErrorAsync("表情格式錯誤，請確認僅只有輸入一個表情且無輸入其他字元", true);
                return;
            }

            try
            {
                await msg.AddReactionAsync(emote);
                await Task.Delay(1000);
                await msg.RemoveReactionAsync(emote, Context.Client.CurrentUser);
            }
            catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.UnknownEmoji)
            {
                await Context.Interaction.SendErrorAsync("未知的表情，僅可輸入此伺服器或預設的表情符號", true);
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "AddButtonRole-AddReactionAsync");
                await Context.Interaction.SendErrorAsync("新增表情失敗，可能是網路問題，請稍後再試", true);
                return;
            }

            var result = await _service.ButtonRoleAddOrUpdateAsync(Context.Guild.Id, Context.Channel.Id, messageId, buttonStr, emoteStr, role.Id, reactionType);
            switch (result)
            {
                case ButtonRoleService.ButtonRoleSaveResult.Success:
                    await Context.Interaction.SendConfirmAsync("已新增", true, true);
                    break;
                case ButtonRoleService.ButtonRoleSaveResult.RefreshFailed:
                    await Context.Interaction.SendErrorAsync("已新增設定，但無法更新訊息按鈕，請確認我在該頻道的權限", true);
                    break;
                default:
                    await Context.Interaction.SendErrorAsync("新增失敗", true);
                    break;
            }
        }

        [SlashCommand("remove-button-role", "移除按鈕用戶組")]
        public async Task RemoveButtonRole(
            [Summary("按鈕", "選擇要移除的按鈕"), Autocomplete(typeof(RemoveButtonRoleAutocompleteHandler))] string emoteData)
        {
            await DeferAsync(true);

            string[] temp = emoteData.Split('|', 2);
            if (temp.Length != 2 || !ulong.TryParse(temp[0], out ulong messageId))
            {
                await Context.Interaction.SendErrorAsync("參數錯誤，請重新選擇按鈕", true);
                return;
            }

            var result = await _service.ButtonRoleRemove(Context.Guild.Id, messageId, temp[1]);
            switch (result)
            {
                case ButtonRoleService.ButtonRoleSaveResult.Success:
                    await Context.Interaction.SendConfirmAsync("已移除", true, true);
                    break;
                case ButtonRoleService.ButtonRoleSaveResult.RefreshFailed:
                    await Context.Interaction.SendErrorAsync("已移除設定，但無法更新訊息按鈕，請確認我在該頻道的權限", true);
                    break;
                default:
                    await Context.Interaction.SendErrorAsync("移除失敗，請確認按鈕是否還存在", true);
                    break;
            }
        }

        private async Task<bool> HasManagePermissionAsync()
        {
            if (Context.Channel is not SocketTextChannel textChannel)
            {
                await Context.Interaction.SendErrorAsync("只能在一般文字頻道使用", true);
                return false;
            }

            var permissions = Context.Guild.CurrentUser.GetPermissions(textChannel);
            if (!permissions.ViewChannel || !permissions.ReadMessageHistory || !permissions.AddReactions)
            {
                await Context.Interaction.SendErrorAsync("我在這個頻道缺少讀取訊息或新增表情的權限，請修正後再試", true);
                return false;
            }

            return true;
        }
    }
}
