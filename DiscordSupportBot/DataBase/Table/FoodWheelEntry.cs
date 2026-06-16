namespace DiscordSupportBot.DataBase.Table
{
    public class FoodWheelEntry : DbEntity
    {
        /// <summary>
        /// 使用者 Id
        /// </summary>
        public ulong UserId { get; set; }
        /// <summary>
        /// 轉盤類型 (0 = 食物, 1 = 飲料)
        /// </summary>
        public int WheelType { get; set; }
        /// <summary>
        /// 項目種類 (0 = 黑名單排除, 1 = 自訂新增)
        /// </summary>
        public int Kind { get; set; }
        /// <summary>
        /// 項目內容
        /// </summary>
        public string Item { get; set; }
    }
}
