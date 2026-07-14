namespace DiscordSupportBot.DataBase.Table
{
    public class AutoCreatePrivateThreadConfig : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public string MentionUserIds { get; set; } = "[]";
        public string MentionRoleIds { get; set; } = "[]";
    }
}
