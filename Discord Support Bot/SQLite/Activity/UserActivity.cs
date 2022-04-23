using Dapper;
using Microsoft.Data.Sqlite;

namespace Discord_Support_Bot.SQLite.Activity
{
    class UserActivity
    {
        public static bool IsInited { get; private set; } = false;
        static string ConnectString { get; } = "Data Source=" + Program.GetDataFilePath("UserActivity.db");

        public static async Task InitActivityAsync()
        {
            IsInited = false;

            if (File.Exists(Program.GetDataFilePath("UserActivity.db")))
            {
                foreach (var item in Select<Table.Guild>("sqlite_master", "name", "WHERE type = 'table' AND name NOT LIKE 'sqlite_%';"))
                {
                    try
                    {
                        foreach (var item2 in Select<UserTable>(item.name.ToString()))
                        {
                            await RedisConnection.RedisDb.StringSetAsync($"SupportBot:Activity:UserMessage:{item.name}:{item2.UserID}", item2.ActivityNum).ConfigureAwait(false);
                        }
                    }
                    catch { }
                }
            }

            IsInited = true;
        }

        public static async Task AddActivity(ulong gid, ulong uid)
        {
            try
            {
                await RedisConnection.RedisDb.StringIncrementAsync($"SupportBot:Activity:UserMessage:{gid}:{uid}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.FormatColorWrite(ex.Message, ConsoleColor.DarkRed);
            }
        }

        public static async Task<List<UserTable>> GetActivityAsync(ulong gid)
        {
            try
            {
                List<UserTable> userList = new List<UserTable>();

                var redisKeyList = RedisConnection.RedisServer.Keys(pattern: $"SupportBot:Activity:UserMessage:{gid}:*", cursor: 0, pageSize: 2500);
                var users = Program.Client.GetGuild(gid).Users;

                foreach (var item in redisKeyList)
                {
                    var uid = ulong.Parse(item.ToString().Split(new char[] { ':' })[4]);
                    var user = users.FirstOrDefault((x) => x.Id == uid);
                    if (user == null)
                    {
                        user = Program.Client.GetGuild(gid).GetUser(uid);
                        if (user == null) continue;
                    }

                    var activityNum = int.Parse((await RedisConnection.RedisDb.StringGetAsync(item).ConfigureAwait(false)).ToString());
                    userList.Add(new UserTable() { UserID = uid, UserName = user.Username, ActivityNum = activityNum });
                }

                return userList;
            }
            catch (Exception ex)
            {
                Log.FormatColorWrite(ex.Message, ConsoleColor.DarkRed);
                return null;
            }
        }

        public static async Task SaveDatebaseAsync()
        {
            var userNum = 0;
            var guilds = Select<Table.Guild>("sqlite_master", "name", "WHERE type = 'table' AND name NOT LIKE 'sqlite_%';");

            foreach (var item in Program.Client.Guilds)
            {
                if (!guilds.Any((x) => x.name == item.Id))
                {
                    await ExecuteSQLCommandAsync($"CREATE TABLE IF NOT EXISTS \"{item.Id}\" (" +
                       "\"UserID\" BIGINT, " +
                       "\"ActivityNum\" INT, " +
                       "PRIMARY KEY(\"UserID\"));");
                }

                var redisKeyList = RedisConnection.RedisServer.Keys(pattern: $"SupportBot:Activity:UserMessage:{item.Id}:*", cursor: 0, pageSize: 100000);
                if (!redisKeyList.Any()) continue;
                var userTables = Select<UserTable>(item.Id.ToString());

                using (var cn = new SqliteConnection(ConnectString))
                {
                    foreach (var item2 in redisKeyList)
                    {
                        var uid = ulong.Parse(item2.ToString().Split(new char[] { ':' })[4]);
                        var activityNum = int.Parse((await RedisConnection.RedisDb.StringGetAsync(item2).ConfigureAwait(false)).ToString());

                        if (userTables.Any((x) => x.UserID == uid) && userTables.First((x) => x.UserID == uid).ActivityNum == activityNum)
                            continue;

                        UserTable userTable = new UserTable() { UserID = uid, ActivityNum = activityNum };
                        await ExecuteSQLCommandAsync($@"INSERT OR REPLACE INTO `{item.Id}` VALUES (@UserID, @ActivityNum)", userTable);
                    }
                }

                userNum += redisKeyList.Count();
            }

            Log.Info($"使用者發言保存完成: {userNum}位使用者");
        }

        public static async Task<int> ExecuteSQLCommandAsync(string command, object data = null)
        {
            using (var cn = new SqliteConnection(ConnectString))
            {
                try
                {
                    return await cn.ExecuteAsync(command, data);
                }
                catch
                {
                    //Log.FormatColorWrite($"執行 \"{command}\" 指令失敗\r\n{ex.Message}", ConsoleColor.DarkRed);
                    return -1;
                }
            }
        }

        public static List<T> Select<T>(string tableName, string column = "*", string other = null)
        {
            if (!File.Exists(Program.GetDataFilePath("UserActivity.db"))) return new List<T>();

            using (var cn = new SqliteConnection(ConnectString))
            {
                try
                {
                    var list = cn.Query($"SELECT {column} FROM `{tableName}` {(other != null ? other : "")}");
                    return JsonConvert.DeserializeObject<List<T>>(JsonConvert.SerializeObject(list, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Log.FormatColorWrite($"SELECT {tableName} 失敗\r\n{ex.Message}", ConsoleColor.DarkRed);
                    return new List<T>();
                }
            }
        }
    }
}
