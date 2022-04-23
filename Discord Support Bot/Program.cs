using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Discord_Support_Bot
{
    class Program
    {
        public static string VERSION => GetLinkerTime(Assembly.GetEntryAssembly());

        public static IUser ApplicatonOwner { get; private set; } = null;
        public static Stopwatch stopWatch { get; private set; } = new Stopwatch();
        public static DiscordSocketClient Client { get; set; }
        public static UpdateStatusFlags UpdateStatus { get; set; } = UpdateStatusFlags.Guild;
        public static List<TrustedGuild> TrustedGuildList { get; set; } = new List<TrustedGuild>();
        public static ConnectionMultiplexer Redis { get; set; }

        public static bool isDisconnect = false, isConnect = false;
        static Timer timerAddBookMark, timerUpdateStatus, timerUpdateGuildInfo, timerSaveDatebase;
        static List<ulong> pinChannelList = new List<ulong>();
        static readonly BotConfig botConfig = new BotConfig();

        public enum UpdateStatusFlags { Guild, Member, Info }

        static void Main(string[] args)
        {
            stopWatch.Start();

            Log.Info(VERSION + " 初始化中");
            Console.OutputEncoding = Encoding.UTF8;
            Console.CancelKeyPress += Console_CancelKeyPress;

            botConfig.InitBotConfig();

            timerAddBookMark = new Timer(TimerHandler);
            timerUpdateStatus = new Timer(TimerHandler2);
            timerUpdateGuildInfo = new Timer(TimerHandler3);
            timerSaveDatebase = new Timer(TimerHandler4);

            MakePinChannelList();

            if (!Directory.Exists(Path.GetDirectoryName(GetDataFilePath(""))))
                Directory.CreateDirectory(Path.GetDirectoryName(GetDataFilePath("")));

            using (var db = new SQLite.SupportContext())
            {
                if (!File.Exists(GetDataFilePath("DataBase.db")))
                {
                    db.Database.EnsureCreated();
                }

                TrustedGuildList = db.TrustedGuild.ToList();
            }

            try
            {
                RedisConnection.Init(botConfig.RedisOption);
                Redis = RedisConnection.Instance.ConnectionMultiplexer;
                Log.Info("Redis已連線");
            }
            catch (Exception ex)
            {
                Log.Error("Redis連線錯誤，請確認伺服器是否已開啟");
                Log.Error(ex.Message);
                return;
            }

            MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static void TimerHandler(object state) //僅限指定伺服器使用
        {
            if (isDisconnect) return;

            string text = DateTime.Now.ToString("yyyy/MM/dd") + " 書籤";
            foreach (ITextChannel item in Client.GetGuild(463657254105645056).TextChannels.Where((x) => x.IsNsfw))
            {
                if (pinChannelList.Contains(item.Id))
                {
                    item.SendMessageAsync(text);
                    Log.FormatColorWrite("已發送文字 \"" + text + "\" 到 " + item.Guild.Name + "/" + item.Name, ConsoleColor.DarkCyan);
                }
            }
        }

        private static void TimerHandler2(object state)
        {
            if (isDisconnect) return;

            ChangeStatus();
        }
		
        private static async void TimerHandler3(object state)
        {
            if (isDisconnect) return;

            using (var db = new SQLite.SupportContext())
            {
                foreach (var item in db.UpdateGuildInfo.ToList().Where((x) => x.ChannelMemberId != 0 || x.ChannelNitroId != 0))
                {
                    SocketGuild guild = Client.GetGuild(item.GuildId);
                    if (guild == null) 
                    {
                        Log.Error("找不到 " + item.GuildId.ToString()); 
                        db.UpdateGuildInfo.Remove(item);
                        await db.SaveChangesAsync();
                        continue;
                    }

                    if (item.ChannelMemberId != 0)
                    {
                        try
                        {
                            SocketGuildChannel channel1 = guild.GetChannel(item.ChannelMemberId);

                            if (channel1 == null) { Log.Error("找不到 " + item.ChannelMemberId.ToString()); item.ChannelMemberId = 0; }
                            else await channel1.ModifyAsync((act) => { act.Name = "伺服器人數-" + guild.MemberCount.ToString(); });

                            Log.Info($"UpdateGuildMemberInfo: {guild.Name}({guild.Id}) - {guild.MemberCount}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"UpdateGuildMemberInfo: {guild.Name} ({guild.Id}): {item.ChannelMemberId}");
                            Log.Error(ex.ToString());

                            if (ex.Message.Contains("50001") || ex.Message.Contains("50013"))
                                item.ChannelMemberId = 0;
                        }
                    }

                    if (item.ChannelNitroId != 0)
                    {
                        try
                        {
                            SocketGuildChannel channel1 = guild.GetChannel(item.ChannelNitroId);

                            if (channel1 == null) { Log.Error("找不到 " + item.ChannelNitroId.ToString()); item.ChannelNitroId = 0; }
                            else await channel1.ModifyAsync((act) => { act.Name = "Nitro數-" + guild.PremiumSubscriptionCount.ToString(); });

                            Log.Info($"UpdateGuildNitroInfo: {guild.Name}({guild.Id}) - {guild.PremiumSubscriptionCount}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"UpdateGuildNitroInfo: {guild.Name} ({guild.Id}): {item.ChannelNitroId}");
                            Log.Error(ex.ToString());

                            if (ex.Message.Contains("50001") || ex.Message.Contains("50013"))
                                item.ChannelNitroId = 0;
                        }
                    }

                    db.UpdateGuildInfo.Update(item);
                    await db.SaveChangesAsync();
                }
            }
        }

        private static async void TimerHandler4(object state)
        {
            if (isDisconnect) return;

            await SQLite.Activity.EmoteActivity.SaveDatebaseAsync();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            isDisconnect = true;
            e.Cancel = true;
        }

        static async Task MainAsync()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Warning,
                ConnectionTimeout = int.MaxValue,
                MessageCacheSize = 50
            });

            Client.GuildMemberUpdated += async (before, after) => //僅限特定伺服器使用
            {
                var beforeUser = before.Value;
                if (beforeUser.Roles == after.Roles) return;

                SocketGuild socketGuild = beforeUser.Guild;
                if (socketGuild.Id == 463657254105645056)
                {
                    SocketRole rolePasserByJiaJia = socketGuild.GetRole(491838110460674058); //路過的甲甲
                    SocketRole roleMuted = socketGuild.GetRole(568223415778148352); //Muted
                    SocketRole roleP = socketGuild.GetRole(534871710474960896); //製作P

                    if (after.Roles.Contains(roleMuted) || after.Roles.Contains(roleP)) return;
                    if (beforeUser.Roles.Contains(roleMuted) || beforeUser.Roles.Contains(roleP)) return;
                    if (!beforeUser.Roles.Contains(rolePasserByJiaJia) && after.Roles.Contains(rolePasserByJiaJia)) return;

                    if (after.Roles.Count >= 3 && after.Roles.Contains(rolePasserByJiaJia))
                    {
                        await socketGuild.GetUser(before.Id).RemoveRoleAsync(rolePasserByJiaJia);
                        Console.WriteLine(string.Format("已移除 {0} 的 {1}", beforeUser.Username, rolePasserByJiaJia.Name));
                    }
                }
            };

            Client.JoinedGuild += (guild) =>
            {
                SendMessageToDiscord($"加入 {guild.Name}({guild.Id})\n擁有者: {guild.Owner.Username}({guild.Owner.Mention})");
                return Task.CompletedTask;
            };

            Client.Ready += async () =>
            {
#if RELEASE
                timerAddBookMark.Change((long)(Math.Round(Convert.ToDateTime($"{DateTime.Now.AddDays(1):yyyy/MM/dd 00:00:00}").Subtract(DateTime.Now).TotalSeconds) + 3) * 1000, 24 * 60 * 60 * 1000);
                timerUpdateGuildInfo.Change((long)Math.Round(Convert.ToDateTime($"{DateTime.Now.AddMinutes(1):yyyy/MM/dd HH:mm:00}").Subtract(DateTime.Now).TotalSeconds) * 1000, 5 * 60 * 1000);

                #region 正常寫法 
                //DateTime end = DateTime.Now.AddDays(1);
                //end = Convert.ToDateTime($"{end.ToShortDateString()} 00:00:00");
                //TimeSpan ts = end.Subtract(DateTime.Now);
                //long dayCount = (long)Math.Round(ts.TotalSeconds) + 3;
                //timerAddBookMark.Change(dayCount * 1000, 24 * 60 * 60 * 1000);
                #endregion
#endif

                timerUpdateStatus.Change(0, 20 * 60 * 1000);
                timerSaveDatebase.Change(20 * 60 * 1000, 20 * 60 * 1000);

                ApplicatonOwner = (await Client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner;

                isConnect = true;
#if RELEASE
                Thread.Sleep(2000);

                await SQLite.Activity.EmoteActivity.InitActivityAsync();
                await SQLite.Activity.UserActivity.InitActivityAsync();
#else
                Log.FormatColorWrite("準備完成", ConsoleColor.Green);
#endif
            };

            #region 初始化指令系統
            var s = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(botConfig)
                .AddSingleton(new CommandService(new CommandServiceConfig()
                {
                    CaseSensitiveCommands = false,
                    DefaultRunMode = RunMode.Async
                }));

            s.LoadFrom(Assembly.GetAssembly(typeof(CommandHandler)));
            IServiceProvider service = s.BuildServiceProvider();
            await service.GetService<CommandHandler>().InitializeAsync();
            #endregion

            #region Login
            await Client.LoginAsync(TokenType.Bot, botConfig.DiscordToken);
            #endregion

            await Client.StartAsync();

            do { await Task.Delay(1000); }
            while (!isDisconnect);

#if RELEASE
            Log.Info("保存資料庫中...");
            await SQLite.Activity.EmoteActivity.SaveDatebaseAsync();
            await SQLite.Activity.UserActivity.SaveDatebaseAsync();            
#endif
            await Client.StopAsync();

            return;
        }

        private static void MakePinChannelList()
        {
            //大眾口味飆車區
            pinChannelList.Add(706441543724171385); //貼圖
            pinChannelList.Add(706441613093634098); //本一
            pinChannelList.Add(706441648015409199); //本二

            //幼跟沒幼
            pinChannelList.Add(706439997825220698); //幼跟沒幼貼圖
            pinChannelList.Add(706440023754408007); //幼跟沒幼本子
            pinChannelList.Add(706440083527565333); //FBI貼圖
            pinChannelList.Add(706440051378094140); //FBI本子

            //隔離專區
            pinChannelList.Add(706439567577841695);
            pinChannelList.Add(706439609965608970);
            pinChannelList.Add(706542107837333554);
            pinChannelList.Add(706531891645513739);
            pinChannelList.Add(706541902618427453);
            pinChannelList.Add(706439433653846025);
            pinChannelList.Add(706532048168288286);
            pinChannelList.Add(706532155064582246);
        }

        public static void ChangeStatus()
        {
            Action<string> setGame = new Action<string>((string text) => { Client.SetGameAsync($"!!!h | {text}"); });

            switch (UpdateStatus)
            {
                case UpdateStatusFlags.Guild:
                    setGame($"在 {Client.Guilds.Count} 個伺服器");
                    UpdateStatus = UpdateStatusFlags.Member;
                    break;
                case UpdateStatusFlags.Member:
                    try
                    {
                        setGame($"服務 {Client.Guilds.Sum((x) => x.MemberCount)} 個成員");
                        UpdateStatus = UpdateStatusFlags.Info;
                    }
                    catch (Exception) { UpdateStatus = UpdateStatusFlags.Info; ChangeStatus(); }
                    break;
                case UpdateStatusFlags.Info:
                    setGame("去打你的程式啦");
                    UpdateStatus = UpdateStatusFlags.Guild;
                    break;
            }
        }

        public static string GetDataFilePath(string fileName)
        {
            return AppDomain.CurrentDomain.BaseDirectory + "Data" +
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/") + fileName;
        }

        public static void SendMessageToDiscord(string content)
        {
            Message message = new Message();

            if (isConnect) message.username = Client.CurrentUser.Username;
            else message.username = "Bot";

            if (isConnect) message.avatar_url = Client.CurrentUser.GetAvatarUrl();
            else message.avatar_url = "";

            message.content = content;

            using (WebClient webClient = new WebClient())
            {
                webClient.Encoding = Encoding.UTF8;
                webClient.Headers["Content-Type"] = "application/json";
                webClient.UploadString(botConfig.WebHookUrl, JsonConvert.SerializeObject(message));
            }
        }
        public static string GetLinkerTime(Assembly assembly)
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value[(index + BuildVersionMetadataPrefix.Length)..];
                    return value;
                }
            }
            return default;
        }
    }
    public class Message
    {
        public string username { get; set; }
        public string content { get; set; }
        public string avatar_url { get; set; }
    }

}
