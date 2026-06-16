using Discord.Interactions;

namespace DiscordSupportBot.Interaction.FoodWheel
{
    public enum WheelType
    {
        [ChoiceDisplay("食物")]
        Food = 0,
        [ChoiceDisplay("飲料")]
        Drink = 1
    }

    public enum WheelEntryKind
    {
        /// <summary>
        /// 黑名單排除
        /// </summary>
        Blacklist = 0,
        /// <summary>
        /// 自訂新增
        /// </summary>
        Custom = 1
    }
}
