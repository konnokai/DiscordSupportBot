using Dapper;
using Microsoft.Data.Sqlite;

namespace Discord_Support_Bot.SQLite.Activity
{
    class EmoteActivity
    {
        public static bool IsInited { get; private set; } = true;
        static string ConnectString { get; } = "Data Source=" + Program.GetDataFilePath("EmoteActivity.db");

        //public static async Task InitActivityAsync()
        //{
        //    IsInited = false;

        //    if (File.Exists(Program.GetDataFilePath("EmoteActivity.db")))
        //    {
        //        foreach (var item in Select<Guild>("sqlite_master", "name", "WHERE type = 'table' AND name NOT LIKE 'sqlite_%';"))
        //        {
        //            try
        //            {
        //                var guild = Program.Client.Guilds.FirstOrDefault((x) => x.Id == item.name);
        //                if (guild == null) continue;

        //                var temp = Select<EmoteTable>(item.name.ToString());

        //                foreach (var item2 in temp)
        //                {
        //                    try
        //                    {
        //                        var tempEmote = guild.Emotes.FirstOrDefault((x) => x.Id == item2.EmoteID);
        //                        if (tempEmote == null) continue;

        //                        await RedisConnection.RedisDb.StringSetAsync($"SupportBot:Activity:Emote:{item.name}:{item2.EmoteID}", item2.ActivityNum).ConfigureAwait(false);
        //                    }
        //                    catch { }
        //                }
        //            }
        //            catch { }
        //        }
        //    }

        //    IsInited = true;
        //}

        public static async Task AddActivityAsync(ulong gid, ulong eid)
        {
            try
            {
                await RedisConnection.RedisDb.StringIncrementAsync($"SupportBot:Activity:Emote:{gid}:{eid}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public static async Task<List<EmoteTable>> GetActivityAsync(ulong gid)
        {
            try
            {
                var emoteTables = Select<EmoteTable>(gid.ToString());
                var redisKeyList = RedisConnection.RedisServer.Keys(2, pattern: $"SupportBot:Activity:Emote:{gid}:*", cursor: 0, pageSize: 2500);
                var guildEmotes = await Program.Client.GetGuild(gid).GetEmotesAsync().ConfigureAwait(false);
                var resultList = new List<EmoteTable>();

                foreach (var item in redisKeyList)
                {
                    var eid = ulong.Parse(item.ToString().Split(new char[] { ':' })[4]);
                    var emote = guildEmotes.FirstOrDefault((x) => x.Id == eid);
                    if (emote == null) continue;

                    var activityNum = int.Parse((await RedisConnection.RedisDb.StringGetAsync(item).ConfigureAwait(false)).ToString());

                    var newEmoteTable = new EmoteTable() { EmoteID = eid, EmoteName = emote.ToString(), ActivityNum = activityNum };
                    var emoteTable = emoteTables.FirstOrDefault((x) => x.EmoteID == eid);
                    if (emoteTable != null)
                        newEmoteTable.ActivityNum += emoteTable.ActivityNum;

                    resultList.Add(newEmoteTable);
                }

                foreach (var item in emoteTables)
                {
                    var emote = guildEmotes.FirstOrDefault((x) => x.Id == item.EmoteID);
                    if (emote == null)
                        continue;

                    item.EmoteName = emote.ToString();
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
            var emoteNum = 0;
            var guilds = Select<Guild>("sqlite_master", "name", "WHERE type = 'table' AND name NOT LIKE 'sqlite_%';");

            foreach (var item in Program.Client.Guilds)
            {
                if (!guilds.Any((x) => x.name == item.Id))
                {
                    await ExecuteSQLCommandAsync($"CREATE TABLE IF NOT EXISTS \"{item.Id}\" (" +
                      "\"EmoteID\" BIGINT, " +
                      "\"ActivityNum\" INT, " +
                      "PRIMARY KEY(\"EmoteID\"));");
                }

                var redisKeyList = RedisConnection.RedisServer.Keys(2, pattern: $"SupportBot:Activity:Emote:{item.Id}:*", cursor: 0, pageSize: 1000);
                if (!redisKeyList.Any()) continue;
                emoteNum += redisKeyList.Count();

                var emoteTables = Select<EmoteTable>(item.Id.ToString());

                using (var cn = new SqliteConnection(ConnectString))
                {
                    foreach (var item2 in redisKeyList)
                    {
                        int activityNumInt = 0;
                        var eid = ulong.Parse(item2.ToString().Split(new char[] { ':' })[4]);
                        try
                        {
                            var activityNum = await RedisConnection.RedisDb.StringGetDeleteAsync(item2).ConfigureAwait(false);
                            if (!activityNum.HasValue)
                                continue;

                            activityNumInt = int.Parse(activityNum);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"EmoteActivity-SaveDatebaseAsync: {ex}");
                            continue;
                        }

                        var emoteTable = emoteTables.FirstOrDefault((x) => x.EmoteID == eid);
                        if (emoteTable == null)
                            emoteTable = new EmoteTable() { EmoteID = eid, ActivityNum = activityNumInt };
                        else
                            emoteTable.ActivityNum += activityNumInt;

                        await ExecuteSQLCommandAsync($@"INSERT OR REPLACE INTO `{item.Id}` VALUES (@EmoteID, @ActivityNum)", emoteTable);
                    }
                }
            }

            Log.Info($"表情保存完成: {emoteNum}個表情");
        }

        public static async Task<int> ExecuteSQLCommandAsync(string command, object data = null)
        {
            using (var cn = new SqliteConnection(ConnectString))
            {
                try
                {
                    return await cn.ExecuteAsync(command, data).ConfigureAwait(false);
                }
                catch //(Exception ex)
                {
                    //Log.FormatColorWrite($"執行 \"{command}\" 指令失敗\r\n{ex.Message}", ConsoleColor.DarkRed);
                    return -1;
                }
            }
        }

        public static List<T> Select<T>(string tableName, string column = "*", string other = null)
        {
            if (!File.Exists(Program.GetDataFilePath("EmoteActivity.db"))) return new List<T>();

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
