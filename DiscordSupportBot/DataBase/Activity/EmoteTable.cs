namespace DiscordSupportBot.DataBase.Activity
{
    class EmoteTable
    {
        public ulong EmoteID { get; set; }
        /// <summary>
        /// 此屬性僅會由 <see cref="EmoteActivity.GetActivityAsync(ulong)"/> 設定
        /// </summary>
        public string EmoteName { get; set; }
        public int ActivityNum { get; set; }
    }
}
