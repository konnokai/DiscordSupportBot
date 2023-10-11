namespace DiscordSupportBot.SQLite.Table
{
    public class Lottery : DbEntity
    {
        public DateTime CreateTime { get; private set; } = DateTime.Now;
        /// <summary>
        /// Guid
        /// </summary>
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        /// <summary>
        /// Guild Id
        /// </summary>
        public ulong GuildId { get; set; }
        /// <summary>
        /// 抽獎內容
        /// </summary>
        public string Context { get; set; } = "";
        /// <summary>
        /// 抽獎獎品 (僅供管理員可看)
        /// </summary>
        public string AwardContext { get; set; } = "";
        /// <summary>
        /// 參與抽獎的結束時間
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 最大抽出人數
        /// </summary>
        public int MaxAward { get; set; } = 1;
        /// <summary>
        /// 參與抽獎清單 (使用Json保存並轉換成List<ulong>)
        /// </summary>
        public string ParticipantList { get; set; } = "[]";
    }
}

