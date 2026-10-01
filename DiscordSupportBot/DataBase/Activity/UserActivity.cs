namespace DiscordSupportBot.DataBase.Activity
{
    class UserActivity
    {
        static readonly ActivityStore Store = new(Program.GetDataFilePath("UserActivity.db"), "SupportBot:Activity:UserMessage", "UserID");

        public static Task AddActivityAsync(ulong gid, ulong uid)
            => Store.IncrementAsync(gid, uid);

        public static async Task<List<UserTable>> GetActivityAsync(ulong gid)
        {
            try
            {
                var counts = Store.GetSqliteCounts(gid);

                // 還沒存進 SQLite 的新使用者只在 Redis 裡，也要算進去
                foreach (var (uid, redisNum) in await Store.GetRedisCountsAsync(gid).ConfigureAwait(false))
                    counts[uid] = counts.GetValueOrDefault(uid) + redisNum;

                return counts.Select((x) => new UserTable() { UserID = x.Key, ActivityNum = Math.Max(0, x.Value) }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UserActivity-GetActivityAsync");
                return new List<UserTable>();
            }
        }

        public static async Task SaveDatabaseAsync()
        {
            var userNum = await Store.SaveAsync(Program.Client.Guilds.Select((x) => x.Id)).ConfigureAwait(false);
            Log.Info($"使用者發言保存完成: {userNum}位使用者");
        }

        public static Task<int> ExecuteSQLCommandAsync(string command, object data = null)
            => Store.ExecuteSQLCommandAsync(command, data);
    }
}
