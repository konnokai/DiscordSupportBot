namespace DiscordSupportBot.DataBase.Table
{
    public class NCChannelCOD : DbEntity
    {
        public enum PlayerPlatform { XBox, PS, PC };

        public ulong DiscordUserId { get; set; }

        public string CODId { get; set; }

        public PlayerPlatform Platform { get; set; } = PlayerPlatform.PC;
    }
}
