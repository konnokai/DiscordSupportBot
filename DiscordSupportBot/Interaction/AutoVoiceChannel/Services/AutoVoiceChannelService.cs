using System.Collections.Concurrent;

namespace DiscordSupportBot.Interaction.AutoVoiceChannel.Services
{
    public class AutoVoiceChannelService : IInteractionService
    {
        private readonly ConcurrentDictionary<ulong, HashSet<ulong>> _voiceChannelCache = new();
        private readonly DiscordSocketClient _client;
        private enum ChannelEvent { Create, MoveOnly, Error, None };

        public AutoVoiceChannelService(DiscordSocketClient client)
        {
            _client = client;
            _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;

            Task.Run(async () =>
            {
                await RefreshVoiceChannelCacheAsync();
            });

            _ = new Timer((obj) =>
            {
                _ = Task.Run(async () =>
                {
                    await RemoveEmptyVoiceChannel();
                    await RefreshVoiceChannelCacheAsync();
                });
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        }

        private async Task RemoveEmptyVoiceChannel()
        {
            using var db = SupportContext.GetDbContext();
            foreach (var item in db.GuildConfig)
            {
                try
                {
                    if (item.AutoVoiceChannel == 0)
                        continue;

                    var guild = _client.GetGuild(item.GuildId);
                    if (guild == null)
                        continue;

                    await foreach (var redisValue in Program.RedisDb.SetScanAsync($"discordVoiceChannelCache:{item.GuildId}"))
                    {
                        ulong voiceChannelId = ulong.Parse(redisValue);
                        var voiceChannel = guild.GetVoiceChannel(voiceChannelId);

                        if (voiceChannel == null)
                        {
                            await Program.RedisDb.SetRemoveAsync($"discordVoiceChannelCache:{item.GuildId}", redisValue);
                            continue;
                        }

                        if (!voiceChannel.ConnectedUsers.Any())
                            await voiceChannel.DeleteAndClearFromCacheAsync(_voiceChannelCache);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"RemoveEmptyVoiceChannel-{item.GuildId} ({item.AutoVoiceChannel}): {ex}");
                }
            }
        }

        private async Task RefreshVoiceChannelCacheAsync()
        {
            _voiceChannelCache.Clear();

            using var db = SupportContext.GetDbContext();
            foreach (var item in db.GuildConfig)
            {
                HashSet<ulong> cache = new();

                try
                {
                    await foreach (var redisValue in Program.RedisDb.SetScanAsync($"discordVoiceChannelCache:{item.GuildId}"))
                    {
                        ulong channelId = ulong.Parse(redisValue);
                        cache.Add(channelId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"RefreshVoiceChannelCacheAsync-Redis: {item.GuildId}");
                }

                _voiceChannelCache.TryAdd(item.GuildId, cache);
            }
        }

        private Task _client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            _ = Task.Run(async () =>
            {
                if (user is not IGuildUser usr)
                    return;

                var beforeVch = before.VoiceChannel;
                var afterVch = after.VoiceChannel;

                if (beforeVch == afterVch)
                    return;

                using var db = SupportContext.GetDbContext();
                var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == usr.GuildId);
                if (guildConfig == null)
                    return;

                if (beforeVch?.Guild == afterVch?.Guild) // User Move
                {
                    ChannelEvent @event = ChannelEvent.Error;
                    if (afterVch.Id == guildConfig.AutoVoiceChannel && !usr.IsBot)
                        @event = await CreateVoiceChannelAndMoveUser(usr, afterVch);
                    if (@event != ChannelEvent.MoveOnly &&
                        _voiceChannelCache.TryGetValue(beforeVch.Guild.Id, out var result) &&
                        result.Contains(beforeVch.Id) &&
                        !beforeVch.ConnectedUsers.Any())
                        await beforeVch.DeleteAndClearFromCacheAsync(_voiceChannelCache);
                }
                else if (beforeVch is null && !usr.IsBot) // User Join
                {
                    if (afterVch.Id == guildConfig.AutoVoiceChannel)
                        await CreateVoiceChannelAndMoveUser(usr, afterVch);
                }
                else if (afterVch is null) // User Leave
                {
                    if (_voiceChannelCache.TryGetValue(beforeVch.Guild.Id, out var result) && result.Contains(beforeVch.Id) && !beforeVch.ConnectedUsers.Any())
                        await beforeVch.DeleteAndClearFromCacheAsync(_voiceChannelCache);
                }
            });
            return Task.CompletedTask;
        }

        private async Task<ChannelEvent> CreateVoiceChannelAndMoveUser(IGuildUser user, SocketVoiceChannel voiceChannel)
        {
            var result = ChannelEvent.None;
            try
            {
                string roomName = $"{user.Username}";
                IVoiceChannel newChannel = voiceChannel.Guild.VoiceChannels.FirstOrDefault((x) => x.Name == roomName);

                if (newChannel == null)
                {
                    newChannel = await voiceChannel.Guild.CreateVoiceChannelAsync(roomName, (act) =>
                    {
                        act.Bitrate = voiceChannel.Bitrate;
                        act.CategoryId = voiceChannel.CategoryId;
                        act.UserLimit = voiceChannel.UserLimit;
                    });

                    Log.Info($"建立語音頻道: {newChannel.Name}");

                    try
                    {
                        await Program.RedisDb.SetAddAsync($"discordVoiceChannelCache:{voiceChannel.Guild.Id}", newChannel.Id);
                    }
                    catch (Exception) { }

                    _voiceChannelCache.AddOrUpdate(voiceChannel.Guild.Id,
                        (guildId) => new() { newChannel.Id },
                        (guildId, hashSet) =>
                        {
                            hashSet.Add(newChannel.Id);
                            return hashSet;
                        });

                    result = ChannelEvent.Create;
                }

                await voiceChannel.Guild.MoveAsync(user, newChannel);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                result = ChannelEvent.Error;
            }

            return result;
        }
    }

    public static class Ext
    {
        public static async Task DeleteAndClearFromCacheAsync(this IVoiceChannel voiceChannel, ConcurrentDictionary<ulong, HashSet<ulong>> cache)
        {
            try
            {
                await voiceChannel.DeleteAsync();
            }
            catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.MissingPermissions)
            {
                Log.Warn($"因缺少權限，無法刪除語音頻道: {voiceChannel}");
                return;
            }

            try
            {
                await Program.RedisDb.SetRemoveAsync($"discordVoiceChannelCache:{voiceChannel.GuildId}", voiceChannel.Id);
            }
            catch (Exception) { }

            if (cache.TryGetValue(voiceChannel.GuildId, out var result))
                result.Remove(voiceChannel.Id);

            Log.Info($"刪除語音頻道: {voiceChannel.Name}");
        }
    }
}
