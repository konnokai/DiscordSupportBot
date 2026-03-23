namespace DiscordSupportBot.DataBase.Table
{
    public class LinkFixConfig : DbEntity
    {
        public ulong GuildId { get; set; }
        public string OldDomain { get; set; }
        public string NewDomain { get; set; }
    }
}
