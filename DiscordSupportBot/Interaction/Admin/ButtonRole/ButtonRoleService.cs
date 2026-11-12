using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace DiscordSupportBot.Interaction.Admin.Service
{
    public class ButtonRoleService : IInteractionService
    {
        internal enum ButtonRoleAction { Add, Remove, None }

        public enum ButtonRoleSaveResult { Success, RefreshFailed, Failed }

        private readonly DiscordSocketClient _client;
        private readonly ConcurrentDictionary<ulong, List<ButtonRole>> _cache = new();
        private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _userLocks = new();
        private readonly object _cacheLock = new();

        public ButtonRoleService(DiscordSocketClient client)
        {
            _client = client;

            using (var db = SupportContext.GetDbContext())
            {
                foreach (var group in db.ButtonRole.AsNoTracking().ToList().GroupBy((x) => x.MessageId))
                    _cache.TryAdd(group.Key, group.ToList());
            }

            _client.ButtonExecuted += _client_ButtonExecuted;
        }

        // Normal 可切換，Verify 只負責給予，已經有就不動
        internal static ButtonRoleAction GetAction(IReadOnlyCollection<ulong> roleIds, ulong roleId, ButtonRole.ReactionType type)
        {
            if (!roleIds.Contains(roleId))
                return ButtonRoleAction.Add;

            return type == ButtonRole.ReactionType.Normal ? ButtonRoleAction.Remove : ButtonRoleAction.None;
        }

        private Task _client_ButtonExecuted(SocketMessageComponent msg)
        {
            if (msg.HasResponded || msg.User.IsBot)
                return Task.CompletedTask;

            ButtonRole? buttonRole;
            lock (_cacheLock)
            {
                if (!_cache.TryGetValue(msg.Message.Id, out var cached))
                    return Task.CompletedTask;

                buttonRole = cached.FirstOrDefault((x) => x.ButtonGuid == msg.Data.CustomId);
            }

            // 只處理自己的按鈕，其他功能的 custom id 留給後面註冊的 handler
            if (buttonRole is null)
                return Task.CompletedTask;

            // Discord.Net 的事件是依序執行的，長時間的工作要丟到背景，避免卡住 gateway
            _ = Task.Run(() => HandleButtonAsync(msg, buttonRole));
            return Task.CompletedTask;
        }

        private async Task HandleButtonAsync(SocketMessageComponent msg, ButtonRole buttonRole)
        {
            try
            {
                await msg.DeferAsync(true);

                if (msg.GuildId is not ulong guildId || msg.ChannelId is not ulong channelId)
                {
                    await msg.SendErrorAsync("只能在伺服器內使用", true);
                    return;
                }

                // shortcut: 每個使用者一個鎖且不回收，長期累積大量使用者後要補上清理
                var userLock = _userLocks.GetOrAdd(msg.User.Id, _ => new SemaphoreSlim(1, 1));
                await userLock.WaitAsync();
                try
                {
                    var (user, role) = await GetUserAndRoleAsync(msg.User.Id, buttonRole);

                    if (role is null)
                    {
                        await msg.SendErrorAsync("找不到用戶組，請向管理員確認", true);
                        return;
                    }

                    if (user is null)
                    {
                        await msg.SendErrorAsync("找不到使用者資料，請稍後再試", true);
                        return;
                    }

                    switch (GetAction(user.RoleIds, role.Id, buttonRole.Type))
                    {
                        case ButtonRoleAction.Add:
                            try
                            {
                                await user.AddRoleAsync(role.Id);
                            }
                            catch (Exception ex)
                            {
                                await msg.SendErrorAsync("無法新增用戶組，請向管理員確認問題", true);
                                Log.Error(ex.Demystify(), $"{guildId} - {channelId} - {msg.Message.Id} - {msg.User.Id} 新增用戶組失敗");
                                break;
                            }
                            await msg.SendConfirmAsync($"已給予 `{role}` 用戶組", true, true);
                            break;
                        case ButtonRoleAction.Remove:
                            try
                            {
                                await user.RemoveRoleAsync(role.Id);
                            }
                            catch (Exception ex)
                            {
                                await msg.SendErrorAsync("無法移除用戶組，請向管理員確認問題", true);
                                Log.Error(ex.Demystify(), $"{guildId} - {channelId} - {msg.Message.Id} - {msg.User.Id} 移除用戶組失敗");
                                break;
                            }
                            await msg.SendConfirmAsync($"已移除 `{role}` 用戶組", true, true);
                            break;
                        default:
                            await msg.SendConfirmAsync($"你已經有 `{role}` 用戶組", true, true);
                            break;
                    }
                }
                finally
                {
                    userLock.Release();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"ButtonRole-ButtonExecuted: message={msg.Message.Id}, user={msg.User.Id}");
                try
                {
                    await msg.SendErrorAsync("處理按鈕時發生錯誤，請向管理員確認", msg.HasResponded);
                }
                catch (Exception sendEx)
                {
                    Log.Error(sendEx.Demystify(), $"ButtonRole-ButtonExecuted-ErrorReply: message={msg.Message.Id}, user={msg.User.Id}");
                }
            }
        }

        internal bool IsManagedMessage(ulong messageId)
        {
            lock (_cacheLock)
            {
                return _cache.ContainsKey(messageId);
            }
        }

        public async Task<ButtonRoleSaveResult> ButtonRoleAddOrUpdateAsync(ulong guildId, ulong channelId, ulong messageId, string buttonName, string emojiName, ulong roleId, ButtonRole.ReactionType type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(buttonName) || buttonName.Length > 80 || string.IsNullOrWhiteSpace(emojiName))
                    return ButtonRoleSaveResult.Failed;

                List<ButtonRole> existing;
                lock (_cacheLock)
                {
                    _cache.TryGetValue(messageId, out var cached);
                    existing = cached is null ? [] : [.. cached];
                }

                // Discord 一則訊息最多 25 個按鈕，超過會讓之後每次刷新都失敗
                if (existing.All((x) => x.EmojiName != emojiName) && existing.Count >= 25)
                    return ButtonRoleSaveResult.Failed;

                ButtonRole entity;
                using (var db = SupportContext.GetDbContext())
                {
                    entity = db.ButtonRole.FirstOrDefault((x) => x.MessageId == messageId && x.EmojiName == emojiName);
                    if (entity is null)
                    {
                        entity = new ButtonRole()
                        {
                            GuildId = guildId,
                            ChannelId = channelId,
                            MessageId = messageId,
                            ButtonName = buttonName,
                            ButtonGuid = Guid.NewGuid().ToString(),
                            EmojiName = emojiName,
                            RoleId = roleId,
                            Type = type
                        };
                        db.ButtonRole.Add(entity);
                    }
                    else
                    {
                        entity.ButtonName = buttonName;
                        entity.RoleId = roleId;
                        entity.Type = type;
                    }

                    await db.SaveChangesAsync();
                }

                lock (_cacheLock)
                {
                    _cache.AddOrUpdate(messageId,
                        _ => [entity],
                        (_, list) =>
                        {
                            list.RemoveAll((x) => x.EmojiName == emojiName);
                            list.Add(entity);
                            return list;
                        });
                }

                return await RefreshMessageButtonAsync(guildId, channelId, messageId)
                    ? ButtonRoleSaveResult.Success
                    : ButtonRoleSaveResult.RefreshFailed;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "ButtonRoleAddOrUpdate");
                return ButtonRoleSaveResult.Failed;
            }
        }

        public async Task<ButtonRoleSaveResult> ButtonRoleRemove(ulong guildId, ulong messageId, string emojiName)
        {
            try
            {
                ButtonRole entity;
                using (var db = SupportContext.GetDbContext())
                {
                    entity = db.ButtonRole.FirstOrDefault((x) => x.GuildId == guildId && x.MessageId == messageId && x.EmojiName == emojiName);
                    if (entity is null)
                        return ButtonRoleSaveResult.Failed;

                    db.ButtonRole.Remove(entity);
                    await db.SaveChangesAsync();
                }

                lock (_cacheLock)
                {
                    if (_cache.TryGetValue(messageId, out var list))
                    {
                        list.RemoveAll((x) => x.EmojiName == emojiName);
                        if (list.Count == 0)
                            _cache.TryRemove(messageId, out _);
                    }
                }

                return await RefreshMessageButtonAsync(entity.GuildId, entity.ChannelId, messageId)
                    ? ButtonRoleSaveResult.Success
                    : ButtonRoleSaveResult.RefreshFailed;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "ButtonRoleRemove");
                return ButtonRoleSaveResult.Failed;
            }
        }

        private async Task<(IGuildUser? User, IRole? Role)> GetUserAndRoleAsync(ulong userId, ButtonRole buttonRole)
        {
            var guild = _client.GetGuild(buttonRole.GuildId);
            var role = guild?.GetRole(buttonRole.RoleId);

            if (role is null)
                return (null, null);

            IGuildUser? user = null;
            try
            {
                // 點擊後快取的身分組可能還沒更新，直接跟 API 拿最新狀態
                user = await _client.Rest.GetGuildUserAsync(guild.Id, userId);
            }
            catch (Exception ex)
            {
                Log.Warn($"{buttonRole.GuildId} - {userId} 取得成員資料失敗，改用快取: {ex.Demystify()}");
            }

            user ??= guild.GetUser(userId) as IGuildUser;

            if (user is null)
                return (null, null);

            return (user, role);
        }

        private async Task<bool> RefreshMessageButtonAsync(ulong guildId, ulong channelId, ulong messageId)
        {
            try
            {
                var channel = _client.GetGuild(guildId)?.GetTextChannel(channelId);
                if (channel is null)
                {
                    Log.Warn($"{guildId} - {channelId} - {messageId} 找不到頻道");
                    return false;
                }

                var msg = await channel.GetMessageAsync(messageId);
                if (msg is null)
                {
                    Log.Warn($"{guildId} - {channelId} - {messageId} 找不到訊息");
                    return false;
                }

                if (msg.Author.Id != _client.CurrentUser.Id)
                {
                    Log.Warn($"{guildId} - {channelId} - {messageId} 訊息非 Bot 發送");
                    return false;
                }

                if (msg is not IUserMessage userMessage)
                {
                    Log.Warn($"{guildId} - {channelId} - {messageId} 訊息類型非 IUserMessage");
                    return false;
                }

                var componentBuilder = new ComponentBuilder();

                List<ButtonRole>? buttonRoles;
                lock (_cacheLock)
                {
                    _cache.TryGetValue(messageId, out var cached);
                    buttonRoles = cached is null ? null : [.. cached];
                }

                if (buttonRoles is null)
                {
                    Log.Warn($"{messageId} 無資料，清除按鈕");
                    await userMessage.ModifyAsync((act) => act.Components = componentBuilder.Build());
                    return true;
                }

                foreach (var item in buttonRoles)
                {
                    IEmote emote = Emote.TryParse(item.EmojiName, out var maybeEmote) ? maybeEmote : new Emoji(item.EmojiName);
                    componentBuilder.WithButton(item.ButtonName, item.ButtonGuid, ButtonStyle.Secondary, emote);
                }

                await userMessage.ModifyAsync((act) => act.Components = componentBuilder.Build());

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"RefreshMessageButton: {guildId} - {channelId} - {messageId}");
                return false;
            }
        }
    }
}
