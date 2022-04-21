using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Discord_Support_Bot.SQLite.Activity
{
    class EmoteActivity
    {
        public static bool IsInited { get; private set; } = false;
        static string ConnectString { get; } = "Data Source=" + Program.GetDataFilePath("EmoteActivity.db");

        public static async Task InitActivityAsync()
        {
            IsInited = false;

            if (File.Exists(Program.GetDataFilePath("EmoteActivity.db")))
            {
                foreach (var item in Select<Table.Guild>("sqlite_master", "name", "WHERE type = 'table' AND name NOT LIKE 'sqlite_%';"))
                {
                    try
                    {
                        var guild = Program.Client.Guilds.FirstOrDefault((x) => x.Id == item.name);
                        if (guild == null) continue;

                        var temp = Select<EmoteTable>(item.name.ToString());

                        foreach (var item2 in temp)
                        {
                            try
                            {
                                var tempEmote = guild.Emotes.FirstOrDefault((x) => x.Id == item2.EmoteID);
                                if (tempEmote == null) continue; 

                                await RedisConnection.RedisDb.StringSetAsync($"SupportBot:Activity:Emote:{item.name}:{item2.EmoteID}", item2.ActivityNum).ConfigureAwait(false);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }

            IsInited = true;
        }

        public static async Task AddActivityAsync(ulong gid, ulong eid)
        {
            try
            {
                await RedisConnection.RedisDb.StringIncrementAsync($"SupportBot:Activity:Emote:{gid}:{eid}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.FormatColorWrite(ex.Message, ConsoleColor.DarkRed);
            }
        }

        public static async Task<List<EmoteTable>> GetActivityAsync(ulong gid)
        {
            try
            {
                List<EmoteTable> emoteList = new List<EmoteTable>();

                var redisKeyList = RedisConnection.RedisServer.Keys(pattern: $"SupportBot:Activity:Emote:{gid}:*", cursor: 0, pageSize: 2500);
                var emotes = await Program.Client.GetGuild(gid).GetEmotesAsync().ConfigureAwait(false);

                foreach (var item in redisKeyList)
                {
                    var eid = ulong.Parse(item.ToString().Split(new char[] { ':' })[4]);
                    var emote = emotes.FirstOrDefault((x) => x.Id == eid);
                    if (emote == null) continue;

                    var activityNum = int.Parse((await RedisConnection.RedisDb.StringGetAsync(item).ConfigureAwait(false)).ToString());
                    emoteList.Add(new EmoteTable() { EmoteID = eid, EmoteName = emote.ToString(), ActivityNum = activityNum });
                }

                return emoteList;
            }
            catch (Exception ex)
            {
                Log.FormatColorWrite(ex.Message, ConsoleColor.DarkRed);
                return null;
            }
        }

        public static async Task SaveDatebaseAsync()
        {
            var emoteNum = 0;
            var guilds = Select<Table.Guild>("sqlite_master", "name", "WHERE type = 'table' AND name NOT LIKE 'sqlite_%';");

            foreach (var item in Program.Client.Guilds)
            {
                if (!guilds.Any((x) => x.name == item.Id))
                {
                    await ExecuteSQLCommandAsync($"CREATE TABLE IF NOT EXISTS \"{item.Id}\" (" +
                      "\"EmoteID\" BIGINT, " +
                      "\"ActivityNum\" INT, " +
                      "PRIMARY KEY(\"EmoteID\"));");
                }                

                var redisKeyList = RedisConnection.RedisServer.Keys(pattern: $"SupportBot:Activity:Emote:{item.Id}:*", cursor: 0, pageSize: 1000);
                if (!redisKeyList.Any()) continue;
                var emoteTables = Select<EmoteTable>(item.Id.ToString());

                using (var cn = new SqliteConnection(ConnectString))
                {
                    foreach (var item2 in redisKeyList)
                    {
                        var eid = ulong.Parse(item2.ToString().Split(new char[] { ':' })[4]);
                        var activityNum = int.Parse((await RedisConnection.RedisDb.StringGetAsync(item2).ConfigureAwait(false)).ToString());

                        if (emoteTables.Any((x) => x.EmoteID == eid) && emoteTables.First((x) => x.EmoteID == eid).ActivityNum == activityNum)
                            continue;

                        EmoteTable emoteTable = new EmoteTable() { EmoteID = eid, ActivityNum = activityNum };
                        await ExecuteSQLCommandAsync($@"INSERT OR REPLACE INTO `{item.Id}` VALUES (@EmoteID, @ActivityNum)", emoteTable);
                    }
                }

                emoteNum += redisKeyList.Count();
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
                    Log.FormatColorWrite($"SELECT {tableName} 失敗\r\n{ex.Message}", ConsoleColor.DarkRed);
                    return new List<T>();
                }
            }
        }
    }
}
