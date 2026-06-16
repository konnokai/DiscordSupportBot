using Discord.Interactions;

namespace DiscordSupportBot.Interaction.FoodWheel
{
    public class FoodWheel : TopLevelModule
    {
        readonly Random _random = new();
        readonly List<string> _foodList = [
                "牛肉麵", "滷肉飯", "便當", "麥當勞", "肯德基", "摩斯漢堡", "Subway", "Pizza Hut", "義大利麵", "拉麵", "壽司", "火鍋", "燒烤", "炸雞", "沙拉", "水果", "冰淇淋", "雞排飯", "炒麵",
                "水餃", "鍋貼", "蚵仔煎", "肉絲炒飯", "蔥油餅夾蛋", "滷味拼盤", "肉圓", "蛋餅", "羊肉炒飯", "麻醬麵", "鴨肉飯", "排骨飯", "魚丸湯麵", "肉羹飯", "蝦仁炒蛋飯", "炸醬麵", "雞腿便當", "拉麵",
                "三明治套餐", "鹽酥雞飯", "蔥抓餅夾肉", "豬排飯", "燒雞腿飯", "沙茶牛肉飯", "蝦仁煨麵", "海鮮炒麵", "客家小炒", "麻油雞飯", "蔥燒牛肉麵", "三杯雞飯", "蒜泥白肉飯", "什錦炒麵", "烤鴨",
                "左宗棠雞飯", "紅燒獅子頭", "蒜蓉蝦仁飯", "菜脯蛋炒飯", "蔥爆牛肉飯", "香酥排骨麵", "塔香鱔魚意麵", "五更腸旺", "蚵仔煎飯", "梅干扣肉飯", "沙茶羊肉飯", "蜜汁叉燒飯", "蒸餃", "陽春麵加蛋",
                "牛肉炒飯", "麻辣香鍋", "花雕雞飯", "紅燒牛肉麵", "炸醬米粉", "鹽水雞飯", "蔥油餅夾肉鬆", "滷肉飯加蛋", "牛肉湯麵", "麻辣燙", "炸雞排飯", "炒米粉", "牛肉燴飯", "蒜香雞腿飯", "洨"
            ];
        readonly List<string> _drinkList =
            [
                "可樂", "雪碧", "芬達", "紅茶", "綠茶", "奶茶", "咖啡", "果汁", "水", "運動飲料", "能量飲料", "酒", "魔爪", "紅牛", "茶裏王", "布丁奶茶", "康師傅冰紅茶", "維他露P", "養樂多", "蘋果西打",
                "葡萄汁", "柳橙汁", "檸檬水", "椰子水", "蜂蜜水", "薑母茶", "花茶", "氣泡水", "冰沙", "奶昔", "優酪乳", "豆漿", "杏仁茶", "紅豆牛奶", "綠豆湯", "薏仁水", "冬瓜茶", "洛神花茶", "金桔檸檬汁",
                "百香果汁", "芒果汁", "西瓜汁"
            ];

        [SlashCommand("food-wheel", "不知道吃啥就轉一下")]
        public async Task FoodWheelAsync()
        {
            var selectedFood = _foodList[_random.Next(_foodList.Count)];
            await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                .WithOkColor()
                .WithDescription($"吃 {Format.Underline(selectedFood)} 吧!")
                .WithFooter($"食物清單數量: {_foodList.Count}")
                .Build());
        }

        [SlashCommand("drink-wheel", "不知道喝啥就轉一下")]
        public async Task DrinkWheelAsync()
        {
            var selectedDrink = _drinkList[_random.Next(_drinkList.Count)];
            await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                .WithOkColor()
                .WithDescription($"喝 {Format.Underline(selectedDrink)} 吧!")
                .WithFooter($"飲料清單數量: {_drinkList.Count}")
                .Build());
        }
    }
}
