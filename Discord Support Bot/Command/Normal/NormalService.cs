using Tweetinvi;
using Tweetinvi.Models;

namespace Discord_Support_Bot.Command.Normal
{
    public class NormalService : ICommandService
    {
        //public TwitterClient TwitterAppClient { get; set; }

        private ConsumerOnlyCredentials appCredentials;
        private DiscordSocketClient _client;
        private Timer timerChangeGuildAvatar, timerAutoWheel;
        private SocketTextChannel socketText = null;

        public NormalService(DiscordSocketClient client, BotConfig botConfig)
        {
            _client = client;

            appCredentials = new ConsumerOnlyCredentials(botConfig.TwitterClientKey, botConfig.TwitterClientSecret)
            {
                BearerToken = botConfig.TwitterClientBearerToken
            };
            //TwitterAppClient = new TwitterClient(appCredentials);

#if RELEASE
            //timerChangeGuildAvatar = new Timer(async (obj) =>
            //{
            //    using (var db = new SupportContext())
            //    {
            //        var list = Queryable.Where(db.GuildConfig, (x) => x.TwitterId != 0);
            //        foreach (var item in list)
            //        {
            //            try
            //            {
            //                var user = await TwitterAppClient.Users.GetUserAsync(item.TwitterId);
            //                var imgUrl = user.ProfileImageUrlFullSize;

            //                if (imgUrl != "" && item.LastTwitterProfileURL != imgUrl)
            //                {
            //                    Log.Info($"ChangeGuildAvatar: {user.ScreenName}({user.Id}) - {imgUrl}");

            //                    MemoryStream memoryStream;
            //                    using (WebClient webClient = new WebClient())
            //                    {
            //                        memoryStream = new MemoryStream(webClient.DownloadData(imgUrl));
            //                    }

            //                    var guild = _client.GetGuild(item.GuildId);
            //                    await guild.ModifyAsync((func) => func.Icon = new Image(memoryStream));

            //                    if (item.NoticeChangeAvatarChannelId != 0)
            //                    {
            //                        try
            //                        {
            //                            memoryStream.Position = 0;
            //                            var channel = guild.GetTextChannel(item.NoticeChangeAvatarChannelId);
            //                            await channel.SendFileAsync(memoryStream, Path.GetFileName(imgUrl), "頭像已變更");
            //                        }
            //                        catch (Exception ex)
            //                        {
            //                            Log.Error($"NoticeChangeGuildAvatar - {item.GuildId}\r\n{ex.Message}\r\n{ex.StackTrace}");
            //                        }
            //                    }

            //                    item.LastTwitterProfileURL = imgUrl;
            //                    db.GuildConfig.Update(item);
            //                    await db.SaveChangesAsync();
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                Log.Error($"ChangeGuildAvatar - {item.GuildId}\r\n{ex.Message}\r\n{ex.StackTrace}");
            //            }
            //        }
            //    }
            //}, null, TimeSpan.FromSeconds(10), TimeSpan.FromHours(1));
#endif

            timerAutoWheel = new Timer(async (obj) =>
            {
                if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday) return;
                if (DateTime.Now.Minute != 0) return;
                if (socketText == null) socketText = _client.GetGuild(756532032275873923).GetTextChannel(756762145961541713);

                switch (DateTime.Now.Hour)
                {
                    case 10:
                        await socketText.SendMessageAsync(
                           embed: new EmbedBuilder()
                           .WithOkColor()
                           .WithTitle("提醒: 今天有眷屬快樂俄羅斯輪盤")
                           .WithDescription("正式報名是 19:00，20:00 開抽\n" +
                            "獎品: `客製化身份組一天x1`\n" +
                            "或者: `勞改一天x1`\n" +
                            "客製化身份組可加圖片\n" +
                            "勞改者將會由輪盤決定去哪裡")
                           .Build()
                        ).ConfigureAwait(false);
                        break;
                    case 19:
                        await socketText.SendMessageAsync(
                          embed: new EmbedBuilder()
                          .WithOkColor()
                          .WithTitle("提醒: 快樂輪盤報名")
                          .WithDescription("請點擊下面的派對橘貓報名\n" +
                           "20:00 開始抽")
                          .Build()
                        ).ContinueWith(async (msg) =>
                           await (await msg).AddReactionAsync(await _client.GetGuild(756532032275873923).GetEmoteAsync(856398828486524929)));
                        break;
                }
            });

            timerAutoWheel.Change((long)Math.Round(Convert.ToDateTime($"{DateTime.Now.AddMinutes(1):yyyy/MM/dd HH:mm:00}").Subtract(DateTime.Now).TotalSeconds) * 1000, 60 * 1000);
        }
    }
}
