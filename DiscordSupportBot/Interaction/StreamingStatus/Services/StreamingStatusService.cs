namespace DiscordSupportBot.Interaction.StreamingStatus.Services
{
    public class StreamingStatusService : IInteractionService
    {
        // Redis hash: field = 語音頻道Id, value = 觸發直播狀態的使用者Id，用來在停播/離開時清除殘留狀態（bot 重開也不遺失）
        private const string RedisKey = "discordStreamingStatusCache";

        private readonly DiscordSocketClient _client;
        private readonly HttpClient _httpClient;

        // 已啟用本功能的伺服器；整包替換參考以避免併發問題（presence 事件量大）
        private HashSet<ulong> _enabledGuilds = new();

        public StreamingStatusService(DiscordSocketClient client, BotConfig botConfig)
        {
            _client = client;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {botConfig.DiscordToken}");

            Task.Run(RefreshEnabledGuilds);
            _ = new Timer((_) => RefreshEnabledGuilds(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

            _client.PresenceUpdated += OnPresenceUpdated;
            _client.UserVoiceStateUpdated += OnVoiceStateUpdated;
        }

        public void SetEnabled(ulong guildId, bool enabled)
        {
            var set = new HashSet<ulong>(_enabledGuilds);
            if (enabled) set.Add(guildId); else set.Remove(guildId);
            _enabledGuilds = set;
        }

        private void RefreshEnabledGuilds()
        {
            try
            {
                using var db = SupportContext.GetDbContext();
                _enabledGuilds = db.GuildConfig.Where((x) => x.EnableStreamingStatus).Select((x) => x.GuildId).ToHashSet();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RefreshEnabledGuilds");
            }
        }

        private Task OnPresenceUpdated(SocketUser user, SocketPresence before, SocketPresence after)
        {
            _ = Task.Run(async () =>
            {
                var enabled = _enabledGuilds; // 快照
                foreach (var guildId in enabled)
                {
                    var guildUser = _client.GetGuild(guildId)?.GetUser(user.Id);
                    if (guildUser != null)
                        await EvaluateAsync(guildUser, null);
                }
            });
            return Task.CompletedTask;
        }

        private Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            _ = Task.Run(async () =>
            {
                if (user is not SocketGuildUser guildUser)
                    return;

                // 使用者剛離開的頻道（移動或離開）
                var leftChannel = (before.VoiceChannel != null && before.VoiceChannel.Id != after.VoiceChannel?.Id)
                    ? before.VoiceChannel : null;

                await EvaluateAsync(guildUser, leftChannel);
            });
            return Task.CompletedTask;
        }

        private async Task EvaluateAsync(SocketGuildUser user, SocketVoiceChannel leftChannel)
        {
            if (!_enabledGuilds.Contains(user.Guild.Id))
                return;

            // 先清除使用者剛離開的頻道（若狀態是由本人設定）
            if (leftChannel != null)
                await ClearIfOwnerAsync(leftChannel, user.Id);

            var vch = user.VoiceChannel;
            if (vch == null)
                return;

            if (TryGetStreamingPlatform(user, out string platform))
            {
                // ponytail: 單一直播者；同頻道多人直播時以最後觸發者為準，要支援多人接力再回掃 vch.ConnectedUsers
                string template = GetTemplate(user.Guild.Id);
                await SetVoiceStatusAsync(vch.Id, template.Replace("{platform}", platform));
                await RedisConnection.RedisDb.HashSetAsync(RedisKey, vch.Id.ToString(), user.Id.ToString());
            }
            else
            {
                // 人在語音頻道但已停止直播 → 若狀態是本人設定則清除
                await ClearIfOwnerAsync(vch, user.Id);
            }
        }

        private async Task ClearIfOwnerAsync(SocketVoiceChannel channel, ulong userId)
        {
            var owner = await RedisConnection.RedisDb.HashGetAsync(RedisKey, channel.Id.ToString());
            if (owner.HasValue && (ulong)owner == userId)
            {
                await SetVoiceStatusAsync(channel.Id, "");
                await RedisConnection.RedisDb.HashDeleteAsync(RedisKey, channel.Id.ToString());
            }
        }

        private static bool TryGetStreamingPlatform(SocketGuildUser user, out string platform)
        {
            var streaming = user.Activities?.FirstOrDefault((a) => a.Type == ActivityType.Streaming);
            if (streaming == null)
            {
                platform = null;
                return false;
            }

            string url = (streaming as StreamingGame)?.Url ?? "";
            if (url.Contains("twitch.tv", StringComparison.OrdinalIgnoreCase))
                platform = "Twitch";
            else if (url.Contains("youtube", StringComparison.OrdinalIgnoreCase) || url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                platform = "YouTube";
            else
                platform = string.IsNullOrWhiteSpace(streaming.Name) ? "直播" : streaming.Name;

            return true;
        }

        private static string GetTemplate(ulong guildId)
        {
            try
            {
                using var db = SupportContext.GetDbContext();
                var template = db.GuildConfig.FirstOrDefault((x) => x.GuildId == guildId)?.StreamingStatusTemplate;
                if (!string.IsNullOrWhiteSpace(template))
                    return template;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"GetTemplate-{guildId}");
            }
            return "正在 {platform} 直播中";
        }

        private async Task SetVoiceStatusAsync(ulong channelId, string status)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(new { status }), Encoding.UTF8, "application/json");
                var resp = await _httpClient.PutAsync($"https://discord.com/api/v10/channels/{channelId}/voice-status", content);
                if (!resp.IsSuccessStatusCode)
                    Log.Error($"SetVoiceStatus-{channelId} 失敗: {resp.StatusCode} {await resp.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"SetVoiceStatusAsync-{channelId}");
            }
        }
    }
}
