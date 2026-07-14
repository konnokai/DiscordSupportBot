using Discord.Interactions;
using DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service;

namespace DiscordSupportBot.Interaction.AutoCreatePrivateThread
{
    [Group("auto-create-private-thread", "自動建立私密討論串")]
    [RequireContext(ContextType.Guild)]
    [DefaultMemberPermissions(GuildPermission.ManageThreads | GuildPermission.CreatePrivateThreads)]
    [RequireUserPermission(GuildPermission.ManageThreads | GuildPermission.CreatePrivateThreads)]
    public class AutoCreatePrivateThread : TopLevelModule<AutoCreatePrivateThreadService>
    {
        private readonly DiscordSocketClient _client;

        public AutoCreatePrivateThread(DiscordSocketClient client)
        {
            _client = client;
        }

        [SlashCommand("setup", "在 Bot 訊息上建立私密討論串按鈕")]
        public async Task SetupAsync(
            [Summary("message-id", "要顯示按鈕的 Bot 訊息 ID"), Autocomplete(typeof(AutoCreatePrivateThreadSetupAutocompleteHandler))] string messageIdText,
            [Summary("button-title", "按鈕標題")] string buttonTitle)
        {
            await DeferAsync(true).ConfigureAwait(false);

            var parsedMessageId = await ParseMessageIdAsync(messageIdText).ConfigureAwait(false);
            if (!parsedMessageId.Success)
                return;
            var messageId = parsedMessageId.MessageId;
            if (Context.Channel is not SocketTextChannel channel)
            {
                await Context.Interaction.SendErrorAsync("此指令只能在伺服器文字頻道中使用", true).ConfigureAwait(false);
                return;
            }
            if (string.IsNullOrWhiteSpace(buttonTitle) || buttonTitle.Length > 80)
            {
                await Context.Interaction.SendErrorAsync("按鈕標題不可為空，且最多 80 個字元", true).ConfigureAwait(false);
                return;
            }

            var botPermissions = Context.Guild.CurrentUser.GetPermissions(channel);
            if (!botPermissions.ViewChannel ||
                !botPermissions.SendMessages ||
                !botPermissions.ReadMessageHistory ||
                !botPermissions.CreatePrivateThreads ||
                !botPermissions.ManageThreads ||
                !botPermissions.SendMessagesInThreads)
            {
                await Context.Interaction.SendErrorAsync(
                    "Bot 缺少查看頻道、讀取歷史訊息、發送訊息、建立／管理私密討論串或在討論串發言的權限",
                    true).ConfigureAwait(false);
                return;
            }

            var message = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
            if (message is not IUserMessage)
            {
                await Context.Interaction.SendErrorAsync("找不到可修改的訊息", true).ConfigureAwait(false);
                return;
            }
            if (message.Author.Id != _client.CurrentUser.Id)
            {
                await Context.Interaction.SendErrorAsync("該訊息不是由我發送的，無法編輯", true).ConfigureAwait(false);
                return;
            }

            var components = new ComponentBuilder()
                .WithButton(buttonTitle, "create-private-thread", ButtonStyle.Primary)
                .Build();
            await _service.SetupAsync(
                Context.Guild.Id,
                channel.Id,
                messageId,
                async () =>
                {
                    var latestMessage = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
                    if (latestMessage is not IUserMessage latestUserMessage || latestMessage.Author.Id != _client.CurrentUser.Id)
                        throw new InvalidOperationException("按鈕訊息已不存在或不再是 Bot 訊息");

                    var previousComponents = ComponentBuilder.FromComponents(latestUserMessage.Components).Build();
                    await latestUserMessage.ModifyAsync(x => x.Components = components).ConfigureAwait(false);
                    return () => latestUserMessage.ModifyAsync(x => x.Components = previousComponents);
                }).ConfigureAwait(false);

            await Context.Interaction.SendConfirmAsync(
                "已建立按鈕與設定。角色 mention 會將具有父頻道存取權的合資格成員加入 private thread 並通知，而不只是發送通知。",
                true,
                true).ConfigureAwait(false);
        }

        [SlashCommand("mention-add", "加入一個會被加入私密討論串並通知的使用者或身分組")]
        public async Task MentionAddAsync(
            [Summary("message-id", "按鈕訊息 ID"), Autocomplete(typeof(AutoCreatePrivateThreadConfigAutocompleteHandler))] string messageIdText,
            [Summary("target", "要加入的使用者或身分組")] IMentionable target)
        {
            await DeferAsync(true).ConfigureAwait(false);

            var configured = await GetConfiguredChannelAsync(messageIdText).ConfigureAwait(false);
            if (!configured.Success)
                return;
            var (messageId, channel, config) = (configured.MessageId, configured.Channel, configured.Config);

            switch (target)
            {
                case IUser selectedUser:
                    var user = Context.Guild.GetUser(selectedUser.Id);
                    if (user == null)
                    {
                        await Context.Interaction.SendErrorAsync("該使用者不是目前伺服器的成員", true).ConfigureAwait(false);
                        return;
                    }
                    if (!user.GetPermissions(channel).ViewChannel)
                    {
                        await Context.Interaction.SendErrorAsync("該使用者無法查看按鈕所在的父頻道", true).ConfigureAwait(false);
                        return;
                    }

                    var userResult = await _service.AddUserAsync(messageId, user.Id).ConfigureAwait(false);
                    await SendMutationResultAsync(userResult.Result, $"使用者 {Format.Bold(user.DisplayName)}").ConfigureAwait(false);
                    return;

                case IRole selectedRole:
                    var role = Context.Guild.GetRole(selectedRole.Id);
                    if (role == null)
                    {
                        await Context.Interaction.SendErrorAsync("找不到該身分組", true).ConfigureAwait(false);
                        return;
                    }
                    if (role.IsEveryone)
                    {
                        await Context.Interaction.SendErrorAsync("不允許加入 @everyone", true).ConfigureAwait(false);
                        return;
                    }

                    if (!Context.Guild.HasAllMembers)
                        await Context.Guild.DownloadUsersAsync().ConfigureAwait(false);

                    var members = role.Members.ToArray();
                    if (members.Length >= 100)
                    {
                        await Context.Interaction.SendErrorAsync("身分組必須少於 100 位成員", true).ConfigureAwait(false);
                        return;
                    }
                    if (!role.IsMentionable && !Context.Guild.CurrentUser.GetPermissions(channel).MentionEveryone)
                    {
                        await Context.Interaction.SendErrorAsync("該身分組不可 mention，且 Bot 沒有 Mention Everyone 權限", true).ConfigureAwait(false);
                        return;
                    }

                    var roleResult = await _service.AddRoleAsync(messageId, role.Id).ConfigureAwait(false);
                    if (roleResult.Result != ConfigMutationResult.Success)
                    {
                        await SendMutationResultAsync(roleResult.Result, $"身分組 {Format.Bold(role.Name)}").ConfigureAwait(false);
                        return;
                    }

                    var inaccessibleCount = members.Count(x => !x.GetPermissions(channel).ViewChannel);
                    var accessNote = inaccessibleCount == 0
                        ? ""
                        : $"；其中 {inaccessibleCount} 位成員無法查看父頻道，Discord 不會將他們加入 thread";
                    await Context.Interaction.SendConfirmAsync(
                        $"已加入身分組 {Format.Bold(role.Name)}{accessNote}。Mention 會將合資格成員加入 private thread 並通知。",
                        true,
                        true).ConfigureAwait(false);
                    return;

                default:
                    await Context.Interaction.SendErrorAsync("請選擇使用者或身分組", true).ConfigureAwait(false);
                    return;
            }
        }

        [SlashCommand("mention-remove", "移除一個私密討論串通知使用者或身分組")]
        public async Task MentionRemoveAsync(
            [Summary("message-id", "按鈕訊息 ID"), Autocomplete(typeof(AutoCreatePrivateThreadConfigAutocompleteHandler))] string messageIdText,
            [Summary("target", "要移除的使用者或身分組")] IMentionable target)
        {
            await DeferAsync(true).ConfigureAwait(false);

            var configured = await GetConfiguredChannelAsync(messageIdText).ConfigureAwait(false);
            if (!configured.Success)
                return;
            var messageId = configured.MessageId;

            var result = target switch
            {
                IUser user => await _service.RemoveUserAsync(messageId, user.Id).ConfigureAwait(false),
                IRole role => await _service.RemoveRoleAsync(messageId, role.Id).ConfigureAwait(false),
                _ => (ConfigMutationResult.NotFound, null)
            };

            var targetName = target switch
            {
                IUser user => $"使用者 {Format.Bold(user.Username)}",
                IRole role => $"身分組 {Format.Bold(role.Name)}",
                _ => "選取對象"
            };
            await SendMutationResultAsync(result.Item1, targetName, removing: true).ConfigureAwait(false);
        }

        [SlashCommand("show", "顯示按鈕目前設定的通知對象")]
        public async Task ShowAsync(
            [Summary("message-id", "按鈕訊息 ID"), Autocomplete(typeof(AutoCreatePrivateThreadConfigAutocompleteHandler))] string messageIdText)
        {
            await DeferAsync(true).ConfigureAwait(false);

            var configured = await GetConfiguredChannelAsync(messageIdText).ConfigureAwait(false);
            if (!configured.Success)
                return;
            var config = configured.Config;

            const int displayUserLimit = 50;
            var users = config.MentionUserIds
                .Take(displayUserLimit)
                .Select(x => Context.Guild.GetUser(x))
                .Select(x => x == null ? "未知使用者" : x.DisplayName)
                .Select(Format.Bold)
                .ToList();
            if (config.MentionUserIds.Length > displayUserLimit)
                users.Add($"另有 {config.MentionUserIds.Length - displayUserLimit} 位使用者未顯示");
            if (users.Count == 0)
                users.Add("無");
            var roles = config.MentionRoleIds
                .Select(x => Context.Guild.GetRole(x))
                .Select(x => x == null ? "未知身分組" : x.Name)
                .Select(Format.Bold)
                .DefaultIfEmpty("無");

            var description = $"使用者 ({config.MentionUserIds.Length})\n{string.Join('\n', users)}\n\n" +
                $"身分組 ({config.MentionRoleIds.Length}/10)\n{string.Join('\n', roles)}\n\n" +
                "身分組 mention 會將具有父頻道存取權的合資格成員加入 private thread 並通知。";
            await Context.Interaction.SendConfirmAsync("私密討論串通知設定", description, true, true).ConfigureAwait(false);
        }

        private async Task<(bool Success, ulong MessageId)> ParseMessageIdAsync(string text)
        {
            if (ulong.TryParse(text.Trim(), out var messageId))
                return (true, messageId);

            await Context.Interaction.SendErrorAsync("請輸入正確的訊息 ID", true).ConfigureAwait(false);
            return (false, 0);
        }

        private async Task<(bool Success, ulong MessageId, SocketTextChannel Channel, AutoCreatePrivateThreadConfigSnapshot Config)>
            GetConfiguredChannelAsync(string messageIdText)
        {
            var parsedMessageId = await ParseMessageIdAsync(messageIdText).ConfigureAwait(false);
            if (!parsedMessageId.Success)
                return (false, 0, null, null);
            if (Context.Channel is not SocketTextChannel channel)
            {
                await Context.Interaction.SendErrorAsync("此指令只能在伺服器文字頻道中使用", true).ConfigureAwait(false);
                return (false, 0, null, null);
            }
            if (!_service.TryGetConfig(parsedMessageId.MessageId, out var config) ||
                config.GuildId != Context.Guild.Id ||
                config.ChannelId != channel.Id)
            {
                await Context.Interaction.SendErrorAsync("找不到此頻道內該按鈕訊息的設定，請先執行 setup", true).ConfigureAwait(false);
                return (false, 0, null, null);
            }

            return (true, parsedMessageId.MessageId, channel, config);
        }

        private Task SendMutationResultAsync(ConfigMutationResult result, string targetName, bool removing = false)
        {
            return result switch
            {
                ConfigMutationResult.Success => Context.Interaction.SendConfirmAsync(
                    removing ? $"已移除{targetName}" : $"已加入{targetName}", true, true),
                ConfigMutationResult.Duplicate => Context.Interaction.SendErrorAsync($"{targetName}已在設定中", true),
                ConfigMutationResult.RoleLimitReached => Context.Interaction.SendErrorAsync("每個設定最多只能加入 10 個身分組", true),
                _ => Context.Interaction.SendErrorAsync(removing ? $"{targetName}不在設定中" : "找不到設定，請先執行 setup", true)
            };
        }
    }
}
