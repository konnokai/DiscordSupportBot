namespace DiscordSupportBot.DataBase.Table
{
    public class AutoCreatePrivateThreadBinding : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong UserId { get; set; }
        public ulong ThreadId { get; set; }
    }
}
