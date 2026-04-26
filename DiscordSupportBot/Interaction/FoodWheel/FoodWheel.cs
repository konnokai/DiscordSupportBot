using Discord.Interactions;

namespace DiscordSupportBot.Interaction.FoodWheel
{
    public class FoodWheel : TopLevelModule
    {
        readonly Random _random = new();
        readonly List<string> _foodList = new List<string>()
            {
                "牛肉麵", "滷肉飯", "便當", "麥當勞", "肯德基", "摩斯漢堡", "Subway", "Pizza Hut", "義大利麵", "拉麵", "壽司", "火鍋", "燒烤", "炸雞", "沙拉", "水果", "冰淇淋", "雞排飯", "炒麵",
                "水餃", "鍋貼", "蚵仔煎", "肉絲炒飯", "蔥油餅夾蛋", "滷味拼盤", "肉圓", "蛋餅", "羊肉炒飯", "麻醬麵", "鴨肉飯", "排骨飯", "魚丸湯麵", "肉羹飯", "蝦仁炒蛋飯", "炸醬麵", "雞腿便當",
                "三明治套餐", "鹽酥雞飯", "蔥抓餅夾肉", "豬排飯", "燒雞腿飯", "沙茶牛肉飯", "蝦仁煨麵", "海鮮炒麵", "客家小炒", "麻油雞飯", "蔥燒牛肉麵", "三杯雞飯", "蒜泥白肉飯", "什錦炒麵",
                "左宗棠雞飯", "紅燒獅子頭", "蒜蓉蝦仁飯", "菜脯蛋炒飯", "蔥爆牛肉飯", "香酥排骨麵", "塔香鱔魚意麵", "五更腸旺", "蚵仔煎飯", "梅干扣肉飯", "沙茶羊肉飯", "蜜汁叉燒飯", "蒸餃", "陽春麵加蛋"
            }.Distinct().ToList();

        [SlashCommand("food-wheel", "不知道吃啥就轉一下")]
        public async Task FoodWheelAsync()
        {
            var selectedFood = _foodList[_random.Next(_foodList.Count)];
            await Context.Interaction.SendConfirmAsync($"今天吃 {Format.Underline(selectedFood)} 吧!");
        }
    }
}
