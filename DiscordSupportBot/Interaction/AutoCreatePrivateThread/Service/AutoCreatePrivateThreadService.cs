using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;

namespace DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service
{
    public enum ConfigMutationResult
    {
        Success,
        NotFound,
        Duplicate,
        RoleLimitReached
    }

    public sealed record AutoCreatePrivateThreadConfigSnapshot(
        ulong GuildId,
        ulong ChannelId,
        ulong MessageId,
        ImmutableArray<ulong> MentionUserIds,
        ImmutableArray<ulong> MentionRoleIds);

    public class AutoCreatePrivateThreadService : IInteractionService
    {
        private readonly DiscordSocketClient _client;
        private readonly SemaphoreSlim _configLock = new(1, 1);
        private readonly SemaphoreSlim _setupLock = new(1, 1);
        private readonly object _buttonLocksGate = new();
        private readonly Dictionary<(ulong ChannelId, ulong UserId), ButtonLockState> _buttonLocks = [];
        private ImmutableDictionary<ulong, AutoCreatePrivateThreadConfigSnapshot> _configs;

        public AutoCreatePrivateThreadService(DiscordSocketClient client)
        {
            _client = client;

            using var db = new SupportContext();
            _configs = db.AutoCreatePrivateThreadConfig
                .AsNoTracking()
                .ToList()
                .Select(ToSnapshot)
                .ToImmutableDictionary(x => x.MessageId);

            _client.ButtonExecuted += HandleButtonExecuted;
        }

        public bool TryGetConfig(ulong messageId, out AutoCreatePrivateThreadConfigSnapshot config)
            => Volatile.Read(ref _configs).TryGetValue(messageId, out config);

        public ImmutableArray<AutoCreatePrivateThreadConfigSnapshot> GetConfigs(ulong guildId, ulong channelId)
            => Volatile.Read(ref _configs).Values
                .Where(x => x.GuildId == guildId && x.ChannelId == channelId)
                .OrderByDescending(x => x.MessageId)
                .ToImmutableArray();

        public async Task<AutoCreatePrivateThreadConfigSnapshot> SetupAsync(
            ulong guildId,
            ulong channelId,
            ulong messageId,
            Func<Task<Func<Task>>> modifyMessage)
        {
            await _setupLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var restoreMessage = await modifyMessage().ConfigureAwait(false);
                try
                {
                    return await SaveSetupAsync(guildId, channelId, messageId).ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        await restoreMessage().ConfigureAwait(false);
                    }
                    catch (Exception rollbackException)
                    {
                        Log.Error(rollbackException, $"AutoCreatePrivateThread-SetupRollback: message={messageId}");
                    }
                    throw;
                }
            }
            finally
            {
                _setupLock.Release();
            }
        }

        private async Task<AutoCreatePrivateThreadConfigSnapshot> SaveSetupAsync(ulong guildId, ulong channelId, ulong messageId)
        {
            await _configLock.WaitAsync().ConfigureAwait(false);
            try
            {
                using var db = new SupportContext();
                var entity = await db.AutoCreatePrivateThreadConfig
                    .SingleOrDefaultAsync(x => x.MessageId == messageId)
                    .ConfigureAwait(false);

                if (entity == null)
                {
                    entity = new AutoCreatePrivateThreadConfig
                    {
                        GuildId = guildId,
                        ChannelId = channelId,
                        MessageId = messageId
                    };
                    db.AutoCreatePrivateThreadConfig.Add(entity);
                }
                else
                {
                    entity.GuildId = guildId;
                    entity.ChannelId = channelId;
                }

                await db.SaveChangesAsync().ConfigureAwait(false);
                return ReplaceCachedConfig(ToSnapshot(entity));
            }
            finally
            {
                _configLock.Release();
            }
        }

        public Task<(ConfigMutationResult Result, AutoCreatePrivateThreadConfigSnapshot Config)> AddUserAsync(ulong messageId, ulong userId)
            => MutateMentionsAsync(messageId, entity =>
            {
                var ids = DeserializeIds(entity.MentionUserIds).ToHashSet();
                if (!ids.Add(userId))
                    return ConfigMutationResult.Duplicate;

                entity.MentionUserIds = JsonConvert.SerializeObject(ids.Order());
                return ConfigMutationResult.Success;
            });

        public Task<(ConfigMutationResult Result, AutoCreatePrivateThreadConfigSnapshot Config)> AddRoleAsync(ulong messageId, ulong roleId)
            => MutateMentionsAsync(messageId, entity =>
            {
                var ids = DeserializeIds(entity.MentionRoleIds).ToHashSet();
                if (ids.Contains(roleId))
                    return ConfigMutationResult.Duplicate;
                if (ids.Count >= 10)
                    return ConfigMutationResult.RoleLimitReached;

                ids.Add(roleId);
                entity.MentionRoleIds = JsonConvert.SerializeObject(ids.Order());
                return ConfigMutationResult.Success;
            });

        public Task<(ConfigMutationResult Result, AutoCreatePrivateThreadConfigSnapshot Config)> RemoveUserAsync(ulong messageId, ulong userId)
            => MutateMentionsAsync(messageId, entity =>
            {
                var ids = DeserializeIds(entity.MentionUserIds).ToHashSet();
                if (!ids.Remove(userId))
                    return ConfigMutationResult.NotFound;

                entity.MentionUserIds = JsonConvert.SerializeObject(ids.Order());
                return ConfigMutationResult.Success;
            });

        public Task<(ConfigMutationResult Result, AutoCreatePrivateThreadConfigSnapshot Config)> RemoveRoleAsync(ulong messageId, ulong roleId)
            => MutateMentionsAsync(messageId, entity =>
            {
                var ids = DeserializeIds(entity.MentionRoleIds).ToHashSet();
                if (!ids.Remove(roleId))
                    return ConfigMutationResult.NotFound;

                entity.MentionRoleIds = JsonConvert.SerializeObject(ids.Order());
                return ConfigMutationResult.Success;
            });

        private async Task<(ConfigMutationResult Result, AutoCreatePrivateThreadConfigSnapshot Config)> MutateMentionsAsync(
            ulong messageId,
            Func<AutoCreatePrivateThreadConfig, ConfigMutationResult> mutation)
        {
            await _configLock.WaitAsync().ConfigureAwait(false);
            try
            {
                using var db = new SupportContext();
                var entity = await db.AutoCreatePrivateThreadConfig
                    .SingleOrDefaultAsync(x => x.MessageId == messageId)
                    .ConfigureAwait(false);

                if (entity == null)
                    return (ConfigMutationResult.NotFound, null);

                var result = mutation(entity);
                if (result != ConfigMutationResult.Success)
                    return (result, ToSnapshot(entity));

                await db.SaveChangesAsync().ConfigureAwait(false);
                return (result, ReplaceCachedConfig(ToSnapshot(entity)));
            }
            finally
            {
                _configLock.Release();
            }
        }

        private AutoCreatePrivateThreadConfigSnapshot ReplaceCachedConfig(AutoCreatePrivateThreadConfigSnapshot config)
        {
            ImmutableInterlocked.AddOrUpdate(ref _configs, config.MessageId, config, (_, _) => config);
            return config;
        }

        private async Task HandleButtonExecuted(SocketMessageComponent component)
        {
            if (component.Data.CustomId != "create-private-thread")
                return;

            try
            {
                await component.DeferAsync(ephemeral: true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"AutoCreatePrivateThread-Defer: message={component.Message.Id}, user={component.User.Id}");
                return;
            }

            _ = Task.Run(() => ButtonExecuted(component));
        }

        private async Task ButtonExecuted(SocketMessageComponent component)
        {
            if (component.Channel is not SocketTextChannel channel)
            {
                try
                {
                    await component.FollowupAsync("此按鈕只能在伺服器文字頻道中使用。", ephemeral: true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"AutoCreatePrivateThread-InvalidChannelFollowup: message={component.Message.Id}, user={component.User.Id}");
                }
                return;
            }

            SocketThreadChannel thread = null;
            var notificationStarted = false;
            var lockKey = (channel.Id, component.User.Id);
            var buttonLock = await AcquireButtonLockAsync(lockKey).ConfigureAwait(false);

            try
            {
                if (component.GuildId == null)
                {
                    await component.FollowupAsync("此按鈕只能在伺服器文字頻道中使用。", ephemeral: true).ConfigureAwait(false);
                    return;
                }

                var guild = _client.GetGuild(component.GuildId.Value);
                if (guild == null)
                {
                    await component.FollowupAsync("找不到此伺服器，請稍後再試。", ephemeral: true).ConfigureAwait(false);
                    return;
                }

                var oldThread = channel.Threads.FirstOrDefault(x =>
                    x.IsPrivateThread &&
                    !x.IsArchived &&
                    x.Name == component.User.Id.ToString());
                if (oldThread != null)
                {
                    await component.FollowupAsync(
                        $"已經存在你的私密討論串 [點我跳轉](https://discord.com/channels/{guild.Id}/{oldThread.Id})",
                        ephemeral: true).ConfigureAwait(false);
                    return;
                }

                var clicker = component.User as SocketGuildUser ?? guild.GetUser(component.User.Id);
                if (clicker == null)
                {
                    await component.FollowupAsync("無法取得你的伺服器成員資訊，請稍後再試。", ephemeral: true).ConfigureAwait(false);
                    return;
                }

                var botPermissions = guild.CurrentUser.GetPermissions(channel);
                if (!botPermissions.ViewChannel ||
                    !botPermissions.SendMessages ||
                    !botPermissions.CreatePrivateThreads ||
                    !botPermissions.ManageThreads ||
                    !botPermissions.SendMessagesInThreads)
                {
                    await component.FollowupAsync(
                        "Bot 缺少查看頻道、發送訊息、建立／管理私密討論串或在討論串發言的權限。",
                        ephemeral: true).ConfigureAwait(false);
                    return;
                }

                TryGetConfig(component.Message.Id, out var config);
                var configuredUserIds = config?.MentionUserIds ?? ImmutableArray.Create(guild.OwnerId);
                var configuredRoleIds = config?.MentionRoleIds ?? ImmutableArray<ulong>.Empty;

                if ((!configuredUserIds.IsEmpty || !configuredRoleIds.IsEmpty) && !guild.HasAllMembers)
                    await guild.DownloadUsersAsync().ConfigureAwait(false);

                thread = await channel.CreateThreadAsync(
                    component.User.Id.ToString(),
                    ThreadType.PrivateThread,
                    autoArchiveDuration: ThreadArchiveDuration.OneDay,
                    invitable: false).ConfigureAwait(false);

                try
                {
                    await thread.AddUserAsync(clicker).ConfigureAwait(false);
                }
                catch
                {
                    await DeleteOrphanThreadAsync(thread, component.Message.Id, component.User.Id).ConfigureAwait(false);
                    thread = null;
                    throw;
                }

                var userIds = new HashSet<ulong> { clicker.Id };
                var skippedUserCount = 0;
                foreach (var userId in configuredUserIds)
                {
                    var user = guild.GetUser(userId);
                    if (user == null || !user.GetPermissions(channel).ViewChannel)
                    {
                        skippedUserCount++;
                        Log.Warn($"AutoCreatePrivateThread: skip inaccessible user {userId}, message={component.Message.Id}");
                        continue;
                    }

                    userIds.Add(user.Id);
                    if (user.Id == clicker.Id)
                        continue;

                    try
                    {
                        await thread.AddUserAsync(user).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        skippedUserCount++;
                        userIds.Remove(user.Id);
                        Log.Error(ex, $"AutoCreatePrivateThread-AddUser: user={user.Id}, thread={thread.Id}");
                    }
                }

                var canMentionEveryone = guild.CurrentUser.GetPermissions(channel).MentionEveryone;
                var roleIds = new HashSet<ulong>();
                var skippedRoleCount = 0;
                foreach (var roleId in configuredRoleIds)
                {
                    var role = guild.GetRole(roleId);
                    if (role == null || role.IsEveryone || role.Members.Count() >= 100 || (!role.IsMentionable && !canMentionEveryone))
                    {
                        skippedRoleCount++;
                        Log.Warn($"AutoCreatePrivateThread: skip invalid role {roleId}, message={component.Message.Id}");
                        continue;
                    }

                    roleIds.Add(role.Id);
                }

                var threadId = thread.Id;
                var failedRoleMention = await SendMentionMessagesAsync(
                    thread,
                    component.Message.Id,
                    userIds,
                    roleIds,
                    () => notificationStarted = true).ConfigureAwait(false);
                thread = null;

                var warningParts = new List<string>();
                if (skippedUserCount > 0)
                    warningParts.Add($"{skippedUserCount} 位設定使用者未能加入");
                if (skippedRoleCount > 0)
                    warningParts.Add($"{skippedRoleCount} 個身分組已失效或不符合限制");
                if (failedRoleMention)
                    warningParts.Add("Discord 回報部分身分組成員未能加入");
                var warning = warningParts.Count == 0 ? "" : $"\n注意：{string.Join("；", warningParts)}。";

                await component.FollowupAsync(
                    $"已建立私密討論串 [點我跳轉](https://discord.com/channels/{guild.Id}/{threadId}){warning}",
                    ephemeral: true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (thread != null && !notificationStarted)
                    await DeleteOrphanThreadAsync(thread, component.Message.Id, component.User.Id).ConfigureAwait(false);

                Log.Error(ex, $"AutoCreatePrivateThread-Button: message={component.Message.Id}, user={component.User.Id}");
                try
                {
                    await component.FollowupAsync(
                        $"建立私密討論串時發生錯誤，請確認 Bot 具有管理及建立私密討論串的權限。\n{ex.Message}",
                        ephemeral: true).ConfigureAwait(false);
                }
                catch (Exception followupException)
                {
                    Log.Error(followupException, $"AutoCreatePrivateThread-ErrorFollowup: message={component.Message.Id}, user={component.User.Id}");
                }
            }
            finally
            {
                ReleaseButtonLock(lockKey, buttonLock);
            }
        }

        private static async Task<bool> SendMentionMessagesAsync(
            SocketThreadChannel thread,
            ulong sourceMessageId,
            IEnumerable<ulong> userIds,
            IEnumerable<ulong> roleIds,
            Action onMessageSent)
        {
            var failedRoleMention = false;
            var mentions = roleIds.Order()
                .Select(x => (Id: x, IsRole: true, Text: $"<@&{x}>"))
                .Concat(userIds.Order().Select(x => (Id: x, IsRole: false, Text: $"<@{x}>")));
            var batch = new List<(ulong Id, bool IsRole, string Text)>();
            var length = 0;

            foreach (var mention in mentions)
            {
                var addedLength = mention.Text.Length + (batch.Count == 0 ? 0 : 1);
                if (batch.Count >= 80 || length + addedLength > 1900)
                {
                    await SendBatchAsync(batch).ConfigureAwait(false);
                    batch.Clear();
                    length = 0;
                    addedLength = mention.Text.Length;
                }

                batch.Add(mention);
                length += addedLength;
            }

            if (batch.Count > 0)
                await SendBatchAsync(batch).ConfigureAwait(false);

            return failedRoleMention;

            async Task SendBatchAsync(List<(ulong Id, bool IsRole, string Text)> items)
            {
                var batchRoleIds = items.Where(x => x.IsRole).Select(x => x.Id).ToList();
                var message = await thread.SendMessageAsync(
                    string.Join(' ', items.Select(x => x.Text)),
                    allowedMentions: new AllowedMentions
                    {
                        UserIds = items.Where(x => !x.IsRole).Select(x => x.Id).ToList(),
                        RoleIds = batchRoleIds
                    }).ConfigureAwait(false);
                onMessageSent();

                if (message.Flags.HasValue && message.Flags.Value.HasFlag(MessageFlags.FailedToMentionRolesInThread))
                {
                    failedRoleMention = true;
                    Log.Error($"AutoCreatePrivateThread: Discord failed to add one or more mentioned roles to thread {thread.Id}, message={sourceMessageId}, roles={string.Join(',', batchRoleIds)}");
                }
            }
        }

        private Task<ButtonLockState> AcquireButtonLockAsync((ulong ChannelId, ulong UserId) key)
        {
            ButtonLockState state;
            lock (_buttonLocksGate)
            {
                if (!_buttonLocks.TryGetValue(key, out state))
                {
                    state = new ButtonLockState();
                    _buttonLocks.Add(key, state);
                }
                state.ReferenceCount++;
            }

            return WaitAsync(state);

            static async Task<ButtonLockState> WaitAsync(ButtonLockState state)
            {
                await state.Semaphore.WaitAsync().ConfigureAwait(false);
                return state;
            }
        }

        private void ReleaseButtonLock((ulong ChannelId, ulong UserId) key, ButtonLockState state)
        {
            state.Semaphore.Release();
            lock (_buttonLocksGate)
            {
                state.ReferenceCount--;
                if (state.ReferenceCount == 0 &&
                    _buttonLocks.TryGetValue(key, out var current) &&
                    ReferenceEquals(current, state))
                {
                    _buttonLocks.Remove(key);
                    state.Semaphore.Dispose();
                }
            }
        }

        private static async Task DeleteOrphanThreadAsync(SocketThreadChannel thread, ulong messageId, ulong userId)
        {
            try
            {
                await thread.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"AutoCreatePrivateThread-DeleteOrphan: thread={thread.Id}, message={messageId}, user={userId}");
            }
        }

        private static AutoCreatePrivateThreadConfigSnapshot ToSnapshot(AutoCreatePrivateThreadConfig entity)
            => new(
                entity.GuildId,
                entity.ChannelId,
                entity.MessageId,
                DeserializeIds(entity.MentionUserIds),
                DeserializeIds(entity.MentionRoleIds));

        private static ImmutableArray<ulong> DeserializeIds(string json)
        {
            try
            {
                return (JsonConvert.DeserializeObject<IEnumerable<ulong>>(json) ?? [])
                    .Distinct()
                    .Order()
                    .ToImmutableArray();
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "AutoCreatePrivateThread: invalid mention ID JSON in database");
                return ImmutableArray<ulong>.Empty;
            }
        }

        private sealed class ButtonLockState
        {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);
            public int ReferenceCount { get; set; }
        }
    }
}
