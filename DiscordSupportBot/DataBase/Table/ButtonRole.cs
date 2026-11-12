namespace DiscordSupportBot.DataBase.Table
{
    public class ButtonRole : DbEntity
    {
        public enum ReactionType { Normal, Verify }

        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public string ButtonGuid { get; set; } = string.Empty;
        public string ButtonName { get; set; } = string.Empty;
        public string EmojiName { get; set; } = string.Empty;
        public ulong RoleId { get; set; }
        public ReactionType Type { get; set; } = ReactionType.Normal;
    }
}
