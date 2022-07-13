using Dapper;
using Microsoft.Data.Sqlite;

namespace Discord_Support_Bot.SQLite.Activity
{
    class UserActivity
    {
        public static bool IsInited { get; private set; } = true;
        static string ConnectString { get; } = "Data Source=" + Program.GetDataFilePath("UserActivity.db");

        //public static async Task InitActivityAsync()
        //{
        //    IsInited = false;

        //    if (File.Exists(Program.GetDataFilePath("UserActivity.db")))
        //    {
        //        foreach (var item in Select<Table.Guild>("sqlite_master", "name", "WHERE type = 'table' AND name NOT LIKE 'sqlite_%';"))
        //        {
        //            try
        //            {
        //                foreach (var item2 in Select<UserTable>(item.name.ToString()))
        //                {
        //                    await RedisConnection.RedisDb.StringSetAsync($"SupportBot:Activity:UserMessage:{item.name}:{item2.UserID}", item2.ActivityNum).ConfigureAwait(false);
        //                }
        //            }
        //            catch { }
        //        }
        //    }

        //    IsInited = true;
        //}

        public static async Task AddActivity(ulong gid, ulong uid)
        {
            try
            {
                await RedisConnection.RedisDb.StringIncrementAsync($"SupportBot:Activity:UserMessage:{gid}:{uid}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public static async Task<List<UserTable>> GetActivityAsync(ulong gid)
        {
            try
            {
                var userTables = Select<UserTable>(gid.ToString());
                var redisKeyList = RedisConnection.RedisServer.Keys(2, pattern: $"SupportBot:Activity:UserMessage:{gid}:*", cursor: 0, pageSize: 10000);
                var resultList = new List<UserTable>();

                foreach (var item in redisKeyList)
                {
                    var uid = ulong.Parse(item.ToString().Split(new char[] { ':' })[4]);
                    IUser user = Program.Client.GetUser(uid);
                    if (user == null)
                    {
                        try { user = await Program.Client.Rest.GetUserAsync(uid); }
                        catch { }
                        if (user == null) 
                            continue;
                    }

                    var activityNum = int.Parse((await RedisConnection.RedisDb.StringGetAsync(item).ConfigureAwait(false)).ToString());

                    var newUserTable = new UserTable() { UserID = uid, UserName = user.Username, ActivityNum = activityNum };
                    var userTable = userTables.FirstOrDefault((x) => x.UserID == uid);
                    if (userTable != null)
                        newUserTable.ActivityNum += userTable.ActivityNum;
                }

                foreach (var item in userTables)
                {
                    IUser user = Program.Client.GetUser(item.UserID);
                    if (user == null)
                    {
                        try { user = await Program.Client.Rest.GetUserAsync(item.UserID); }
                        catch { }
                        if (user == null)
                            continue;
                    }

                    item.UserName = user.Username;
                    resultList.Add(item);
                }

                return resultList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }

        public static async Task SaveDatebaseAsync()
        {
            var userNum = 0;
            var guilds = Select<Guild>("sqlite_master", "name", "WHERE type = 'table' AND name NOT LIKE 'sqlite_%';");

            foreach (var item in Program.Client.Guilds)
            {
                if (!guilds.Any((x) => x.name == item.Id))
                {
                    await ExecuteSQLCommandAsync($"CREATE TABLE IF NOT EXISTS \"{item.Id}\" (" +
                       "\"UserID\" BIGINT, " +
                       "\"ActivityNum\" INT, " +
                       "PRIMARY KEY(\"UserID\"));");
                }

                var redisKeyList = RedisConnection.RedisServer.Keys(2, pattern: $"SupportBot:Activity:UserMessage:{item.Id}:*", cursor: 0, pageSize: 100000);
                if (!redisKeyList.Any()) continue;
                userNum += redisKeyList.Count();

                var userTables = Select<UserTable>(item.Id.ToString());

                using (var cn = new SqliteConnection(ConnectString))
                {
                    foreach (var item2 in redisKeyList)
                    {
                        int activityNumInt = 0;
                        var uid = ulong.Parse(item2.ToString().Split(new char[] { ':' })[4]);
                        try
                        {
                            var activityNum = await RedisConnection.RedisDb.StringGetDeleteAsync(item2).ConfigureAwait(false);
                            if (!activityNum.HasValue)
                                continue;

                            activityNumInt = int.Parse(activityNum);
                        }
                        catch (Exception ex) 
                        {
                            Log.Error($"UserActivity-SaveDatebaseAsync: {ex}");
                            continue;
                        }

                        var userTable = userTables.FirstOrDefault((x) => x.UserID == uid);
                        if (userTable == null)
                            userTable = new UserTable() { UserID = uid, ActivityNum = activityNumInt };
                        else
                            userTable.ActivityNum += activityNumInt;

                        await ExecuteSQLCommandAsync($@"INSERT OR REPLACE INTO `{item.Id}` VALUES (@UserID, @ActivityNum)", userTable);
                    }
                }
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
                    Log.Error($"SELECT {tableName} 失敗");
                    Log.Error(ex.ToString());
                    return new List<T>();
                }
            }
        }
    }
}
