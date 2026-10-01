using Dapper;
using Microsoft.Data.Sqlite;
using StackExchange.Redis;

namespace DiscordSupportBot.DataBase.Activity
{
    /// <summary>
    /// 活動計數先累積在 Redis，定時搬進 SQLite（每個伺服器一張表）
    /// </summary>
    class ActivityStore
    {
        // 只扣掉這次存進 SQLite 的量，扣到 0 才刪 key。存檔途中新進來的計數才不會被一起刪掉
        const string DecrementAndDeleteIfZeroScript = """
            local v = redis.call('DECRBY', KEYS[1], ARGV[1])
            if v == 0 then redis.call('DEL', KEYS[1]) end
            return v
            """;

        readonly string _dbPath;
        readonly string _connectString;
        readonly string _redisKeyPrefix;
        readonly string _idColumn;

        public ActivityStore(string dbPath, string redisKeyPrefix, string idColumn)
        {
            _dbPath = dbPath;
            _connectString = "Data Source=" + dbPath;
            _redisKeyPrefix = redisKeyPrefix;
            _idColumn = idColumn;
        }

        public async Task IncrementAsync(ulong gid, ulong id)
        {
            try
            {
                await RedisConnection.RedisDb.StringIncrementAsync(GetRedisKey(gid, id)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{_redisKeyPrefix} 增加計數失敗");
            }
        }

        public async Task DecrementAsync(ulong gid, ulong id)
        {
            try
            {
                await RedisConnection.RedisDb.StringDecrementAsync(GetRedisKey(gid, id)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{_redisKeyPrefix} 減少計數失敗");
            }
        }

        public async Task<Dictionary<ulong, int>> GetRedisCountsAsync(ulong gid)
        {
            var result = new Dictionary<ulong, int>();
            var keys = ScanKeys(gid);
            if (keys.Length == 0)
                return result;

            var values = await RedisConnection.RedisDb.StringGetAsync(keys).ConfigureAwait(false);
            for (int i = 0; i < keys.Length; i++)
            {
                // 讀取時剛好被存檔刪掉的 key 會拿到空值，略過就好
                if (TryParseId(keys[i], out var id) && values[i].TryParse(out int num))
                    result[id] = num;
            }

            return result;
        }

        public Dictionary<ulong, int> GetSqliteCounts(ulong gid)
        {
            if (!File.Exists(_dbPath))
                return new Dictionary<ulong, int>();

            try
            {
                using var cn = new SqliteConnection(_connectString);
                if (!TableExists(cn, gid))
                    return new Dictionary<ulong, int>();

                // SQLite 的 INTEGER 一律是 long，自己轉型比較不會踩到 Dapper 的型別對應
                return cn.Query<(long Id, long ActivityNum)>($"SELECT \"{_idColumn}\", \"ActivityNum\" FROM \"{gid}\"")
                    .ToDictionary((x) => (ulong)x.Id, (x) => (int)x.ActivityNum);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"讀取 {Path.GetFileName(_dbPath)} 的 {gid} 失敗");
                return new Dictionary<ulong, int>();
            }
        }

        /// <returns>這次處理的 key 數量</returns>
        public async Task<int> SaveAsync(IEnumerable<ulong> guildIds)
        {
            var total = 0;

            foreach (var gid in guildIds)
            {
                try
                {
                    var keys = ScanKeys(gid);
                    if (keys.Length == 0)
                        continue;

                    var values = await RedisConnection.RedisDb.StringGetAsync(keys).ConfigureAwait(false);
                    var pending = new List<(RedisKey Key, ulong Id, int Delta)>();
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (TryParseId(keys[i], out var id) && values[i].TryParse(out int delta))
                            pending.Add((keys[i], id, delta));
                    }

                    // SQLite 寫失敗會直接丟例外，Redis 保持原樣，下次存檔再試
                    WriteToSqlite(gid, pending.Where((x) => x.Delta != 0).Select((x) => (x.Id, x.Delta)).ToList());

                    // 值是 0 的 key（加了反應又移除）也要跑一次，順便清掉
                    await Task.WhenAll(pending.Select((x) =>
                        RedisConnection.RedisDb.ScriptEvaluateAsync(DecrementAndDeleteIfZeroScript, [x.Key], [x.Delta]))).ConfigureAwait(false);

                    total += pending.Count;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{_redisKeyPrefix} 保存 {gid} 失敗");
                }
            }

            return total;
        }

        internal void WriteToSqlite(ulong gid, IReadOnlyCollection<(ulong Id, int Delta)> deltas)
        {
            if (deltas.Count == 0)
                return;

            using var cn = new SqliteConnection(_connectString);
            cn.Open();
            using var tx = cn.BeginTransaction();

            cn.Execute($"CREATE TABLE IF NOT EXISTS \"{gid}\" (\"{_idColumn}\" BIGINT, \"ActivityNum\" INT, PRIMARY KEY(\"{_idColumn}\"));", transaction: tx);
            cn.Execute($"INSERT INTO \"{gid}\" (\"{_idColumn}\", \"ActivityNum\") VALUES (@Id, max(0, @Delta)) " +
                $"ON CONFLICT(\"{_idColumn}\") DO UPDATE SET \"ActivityNum\" = max(0, \"ActivityNum\" + @Delta);",
                deltas.Select((x) => new { Id = (long)x.Id, x.Delta }), tx);

            tx.Commit();
        }

        public async Task<int> ExecuteSQLCommandAsync(string command, object data = null)
        {
            try
            {
                using var cn = new SqliteConnection(_connectString);
                return await cn.ExecuteAsync(command, data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"執行 \"{command}\" 失敗");
                return -1;
            }
        }

        string GetRedisKey(ulong gid, ulong id) => $"{_redisKeyPrefix}:{gid}:{id}";

        RedisKey[] ScanKeys(ulong gid)
            => RedisConnection.RedisServer.Keys(RedisConnection.RedisDb.Database, pattern: $"{_redisKeyPrefix}:{gid}:*", pageSize: 1000).ToArray();

        static bool TryParseId(RedisKey key, out ulong id)
        {
            var text = key.ToString();
            return ulong.TryParse(text.AsSpan(text.LastIndexOf(':') + 1), out id);
        }

        static bool TableExists(SqliteConnection cn, ulong gid)
            => cn.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name", new { name = gid.ToString() }) > 0;
    }
}
