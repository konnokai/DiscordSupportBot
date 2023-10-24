namespace DiscordSupportBot.Command.Normal
{
    public class NormalService : ICommandService
    {
        private DiscordSocketClient _client;
        private Timer _timerAutoWheel;
        private SocketTextChannel socketText = null;

        public NormalService(DiscordSocketClient client)
        {
            _client = client;

            _timerAutoWheel = new Timer(async (obj) =>
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

            _timerAutoWheel.Change((long)Math.Round(Convert.ToDateTime($"{DateTime.Now.AddMinutes(1):yyyy/MM/dd HH:mm:00}").Subtract(DateTime.Now).TotalSeconds) * 1000, 60 * 1000);
        }
    }
}
