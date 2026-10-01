namespace DiscordSupportBot.DataBase.Activity
{
    class EmoteActivity
    {
        static readonly ActivityStore Store = new(Program.GetDataFilePath("EmoteActivity.db"), "SupportBot:Activity:Emote", "EmoteID");

        public static Task AddActivityAsync(ulong gid, ulong eid)
            => Store.IncrementAsync(gid, eid);

        // 計數可能已經存進 SQLite，Redis 這邊會變負數，存檔時再一起扣回去
        public static Task RemoveActivityAsync(ulong gid, ulong eid)
            => Store.DecrementAsync(gid, eid);

        public static Task OnReactionAddedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (TryGetCountableEmote(channel.Id, reaction, out var gid, out var eid))
                return AddActivityAsync(gid, eid);

            return Task.CompletedTask;
        }

        public static Task OnReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (TryGetCountableEmote(channel.Id, reaction, out var gid, out var eid))
                return RemoveActivityAsync(gid, eid);

            return Task.CompletedTask;
        }

        private static bool TryGetCountableEmote(ulong channelId, SocketReaction reaction, out ulong gid, out ulong eid)
        {
            gid = 0;
            eid = 0;

            if (reaction.Emote is not Emote emote)
                return false;

            if (Program.Client.GetChannel(channelId) is not SocketGuildChannel guildChannel)
                return false;

            var guild = guildChannel.Guild;
            if (reaction.UserId == Program.Client.CurrentUser?.Id)
                return false;

            // 移除事件通常不會帶 User，改從伺服器快取查
            var isBot = reaction.User.IsSpecified ? reaction.User.Value.IsBot : guild.GetUser(reaction.UserId)?.IsBot ?? false;
            if (isBot)
                return false;

            // 跟訊息統計一樣，只算這個伺服器自己的表情
            if (!guild.Emotes.Any((x) => x.Id == emote.Id))
                return false;

            gid = guild.Id;
            eid = emote.Id;
            return true;
        }

        public static async Task<List<EmoteTable>> GetActivityAsync(ulong gid)
        {
            try
            {
                var sqliteCounts = Store.GetSqliteCounts(gid);
                var redisCounts = await Store.GetRedisCountsAsync(gid).ConfigureAwait(false);
                var guildEmotes = await Program.Client.GetGuild(gid).GetEmotesAsync().ConfigureAwait(false);
                var resultList = new List<EmoteTable>();

                foreach (var guildEmote in guildEmotes)
                {
                    var hasSqliteRecord = sqliteCounts.TryGetValue(guildEmote.Id, out var sqliteNum);
                    redisCounts.TryGetValue(guildEmote.Id, out var redisNum);

                    if (!hasSqliteRecord && redisNum <= 0)
                        continue;

                    resultList.Add(new EmoteTable() { EmoteID = guildEmote.Id, EmoteName = guildEmote.ToString(), EmoteUrl = guildEmote.Url, ActivityNum = Math.Max(0, sqliteNum + redisNum) });
                }

                return resultList;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "EmoteActivity-GetActivityAsync");
                return new List<EmoteTable>();
            }
        }

        public static async Task SaveDatabaseAsync()
        {
            var emoteNum = await Store.SaveAsync(Program.Client.Guilds.Select((x) => x.Id)).ConfigureAwait(false);
            Log.Info($"表情保存完成: {emoteNum}個表情");
        }

        public static Task<int> ExecuteSQLCommandAsync(string command, object data = null)
            => Store.ExecuteSQLCommandAsync(command, data);
    }
}
