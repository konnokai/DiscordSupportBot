using Dapper;
using Microsoft.Data.Sqlite;

namespace DiscordSupportBot.DataBase.Activity
{
    class EmoteActivity
    {
        public static bool IsInited { get; private set; } = true;
        static string ConnectString { get; } = "Data Source=" + Program.GetDataFilePath("EmoteActivity.db");

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
                var redisEmoteList = RedisConnection.RedisServer.Keys(2, pattern: $"SupportBot:Activity:Emote:{gid}:*", cursor: 0, pageSize: 2500)
                    .Select((x) => ulong.Parse(x.ToString().Split(':')[4])).ToList();
                var guildEmotes = await Program.Client.GetGuild(gid).GetEmotesAsync().ConfigureAwait(false);
                var resultList = new List<EmoteTable>();

                foreach (var guildEmote in guildEmotes)
                {
                    int redisActivityNum = 0;
                    if (await RedisConnection.RedisDb.KeyExistsAsync($"SupportBot:Activity:Emote:{gid}:{guildEmote.Id}"))
                        redisActivityNum = int.Parse((await RedisConnection.RedisDb.StringGetAsync($"SupportBot:Activity:Emote:{gid}:{guildEmote.Id}")).ToString());

                    var emoteTable = emoteTables.SingleOrDefault((x) => x.EmoteID == guildEmote.Id);
                    if (emoteTable == null)
                    {
                        if (redisActivityNum > 0)
                            resultList.Add(new EmoteTable() { EmoteID = guildEmote.Id, EmoteName = guildEmote.ToString(), ActivityNum = redisActivityNum });

                        continue;
                    }

                    emoteTable.EmoteName = guildEmote.ToString();
                    emoteTable.ActivityNum += redisActivityNum;
                    resultList.Add(emoteTable);
                }

                return resultList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new List<EmoteTable>();
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
