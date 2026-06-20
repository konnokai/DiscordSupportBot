namespace DiscordSupportBot.DataBase.Table
{
    class GuildConfig : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong AutoVoiceChannel { get; set; } = 0;
        public ulong ChannelMemberId { get; set; } = 0;
        public ulong ChannelNitroId { get; set; } = 0;
        public ulong HoneyPotChannelId { get; set; } = 0;
        public bool EnableStreamingStatus { get; set; } = false;
        public string StreamingStatusTemplate { get; set; } = "正在 {platform} 直播中";
    }
}
