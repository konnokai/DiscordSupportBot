using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace DiscordSupportBot.Interaction.FoodWheel.Service
{
    public class FoodWheelService : IInteractionService
    {
        public IReadOnlyList<string> FoodList { get; } =
        [
            "牛肉麵", "滷肉飯", "便當", "麥當勞", "肯德基", "摩斯漢堡", "Subway", "Pizza Hut", "義大利麵", "拉麵", "壽司", "火鍋", "燒烤", "炸雞", "沙拉", "水果", "冰淇淋", "雞排飯", "炒麵",
            "水餃", "鍋貼", "蚵仔煎", "肉絲炒飯", "蔥油餅夾蛋", "滷味拼盤", "肉圓", "蛋餅", "羊肉炒飯", "麻醬麵", "鴨肉飯", "排骨飯", "魚丸湯麵", "肉羹飯", "蝦仁炒蛋飯", "炸醬麵", "雞腿便當", "拉麵",
            "三明治套餐", "鹽酥雞飯", "蔥抓餅夾肉", "豬排飯", "燒雞腿飯", "沙茶牛肉飯", "蝦仁煨麵", "海鮮炒麵", "客家小炒", "麻油雞飯", "蔥燒牛肉麵", "三杯雞飯", "蒜泥白肉飯", "什錦炒麵", "烤鴨",
            "左宗棠雞飯", "紅燒獅子頭", "蒜蓉蝦仁飯", "菜脯蛋炒飯", "蔥爆牛肉飯", "香酥排骨麵", "塔香鱔魚意麵", "五更腸旺", "蚵仔煎飯", "梅干扣肉飯", "沙茶羊肉飯", "蜜汁叉燒飯", "蒸餃", "陽春麵加蛋",
            "牛肉炒飯", "麻辣香鍋", "花雕雞飯", "紅燒牛肉麵", "炸醬米粉", "鹽水雞飯", "蔥油餅夾肉鬆", "滷肉飯加蛋", "牛肉湯麵", "麻辣燙", "炸雞排飯", "炒米粉", "牛肉燴飯", "蒜香雞腿飯", "洨",
            // 台灣小吃與在地料理
            "蚵仔麵線", "大腸麵線", "擔仔麵", "切仔麵", "乾麵", "鍋燒意麵", "米粉湯", "碗粿", "蘿蔔糕", "筒仔米糕", "油飯", "割包", "飯糰", "潤餅", "臭豆腐", "大腸包小腸",
            "雞肉飯", "火雞肉飯", "控肉飯", "豬腳飯", "豬血糕", "鹹粥", "虱目魚粥", "虱目魚肚湯", "當歸鴨", "薑母鴨", "羊肉爐", "四神湯", "貢丸湯", "土魠魚羹", "麻油雞麵線", "酸菜白肉鍋",
            "麻辣鴨血", "鹽酥雞", "雞排", "甜不辣", "胡椒餅", "水煎包", "燒餅油條", "棺材板", "蝦捲", "炸排骨飯", "麻油腰花", "溫體牛肉湯", "炒粄條", "客家鹹豬肉", "炒海瓜子", "鳳梨蝦球",
            "沙茶魷魚", "三杯中卷", "三杯杏鮑菇", "菜脯蛋", "鹹蛋苦瓜", "蒜炒空心菜", "薑絲大腸", "皮蛋瘦肉粥", "排骨苦瓜湯", "蛋包飯", "日式咖哩飯", "日式牛丼", "親子丼", "韭菜盒",
            // 取自 HowToCook 食譜中偏台式的菜
            "麻油拌麵", "蔥油拌麵", "蛋炒飯", "揚州炒飯", "炒年糕", "手抓餅", "鮮肉燒賣", "煎餃", "照燒雞腿飯", "玉米排骨湯", "排骨山藥玉米湯", "陳皮排骨湯", "紫菜蛋花湯", "番茄蛋花湯",
            "糖醋排骨", "糖醋里肌", "糖醋肉", "紅燒肉", "紅燒豬腳", "肉末蒸蛋", "冬瓜釀肉", "韭菜炒蛋", "番茄炒蛋", "白灼蝦", "清蒸鱸魚"
        ];

        public IReadOnlyList<string> DrinkList { get; } =
        [
            "可樂", "雪碧", "芬達", "紅茶", "綠茶", "奶茶", "咖啡", "果汁", "水", "運動飲料", "能量飲料", "酒", "魔爪", "紅牛", "茶裏王", "布丁奶茶", "康師傅冰紅茶", "維他露P", "養樂多", "蘋果西打",
            "葡萄汁", "柳橙汁", "檸檬水", "椰子水", "蜂蜜水", "薑母茶", "花茶", "氣泡水", "冰沙", "奶昔", "優酪乳", "豆漿", "杏仁茶", "紅豆牛奶", "綠豆湯", "薏仁水", "冬瓜茶", "洛神花茶", "金桔檸檬汁",
            "百香果汁", "芒果汁", "西瓜汁",
            // 台灣手搖與傳統飲品
            "珍珠奶茶", "波霸奶茶", "鮮奶茶", "紅茶拿鐵", "四季春茶", "烏龍茶", "高山茶", "青茶", "梅子綠茶", "蜂蜜檸檬", "愛玉檸檬", "仙草茶", "青草茶", "烏梅汁", "甘蔗汁", "麥茶",
            "米漿", "黑糖薑茶", "木瓜牛奶", "西瓜牛奶", "柚子茶", "多多綠茶", "冬瓜檸檬", "仙草奶凍", "酸梅湯", "楊枝甘露"
        ];

        /// <summary>
        /// 使用者項目快取: (UserId, WheelType, WheelEntryKind) -> 項目集合
        /// </summary>
        private readonly ConcurrentDictionary<(ulong UserId, WheelType Type, WheelEntryKind Kind), ConcurrentDictionary<string, byte>> _entries = new();

        public FoodWheelService()
        {
            using var db = new SupportContext();

            foreach (var entry in db.FoodWheelEntry.AsNoTracking())
            {
                var set = _entries.GetOrAdd((entry.UserId, (WheelType)entry.WheelType, (WheelEntryKind)entry.Kind), _ => new());
                set.TryAdd(entry.Item, 0);
            }
        }

        public IReadOnlyList<string> GetMasterList(WheelType type)
            => type == WheelType.Drink ? DrinkList : FoodList;

        /// <summary>
        /// 取得使用者指定種類 (黑名單 / 自訂) 的項目清單
        /// </summary>
        public IReadOnlyCollection<string> GetEntries(ulong userId, WheelType type, WheelEntryKind kind)
        {
            if (_entries.TryGetValue((userId, type, kind), out var set))
                return set.Keys.ToList();

            return Array.Empty<string>();
        }

        /// <summary>
        /// 取得使用者實際可抽的清單: (主清單 ∪ 自訂) − 黑名單
        /// </summary>
        public List<string> GetEffectiveItems(ulong userId, WheelType type)
        {
            var blacklist = _entries.TryGetValue((userId, type, WheelEntryKind.Blacklist), out var bl)
                ? new HashSet<string>(bl.Keys)
                : new HashSet<string>();

            var result = new List<string>();
            var seen = new HashSet<string>();

            foreach (var item in GetMasterList(type))
            {
                if (blacklist.Contains(item))
                    continue;
                if (seen.Add(item))
                    result.Add(item);
            }

            if (_entries.TryGetValue((userId, type, WheelEntryKind.Custom), out var custom))
            {
                foreach (var item in custom.Keys)
                {
                    if (blacklist.Contains(item))
                        continue;
                    if (seen.Add(item))
                        result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// 加入項目 (黑名單或自訂)，若已存在則回傳 false
        /// </summary>
        public async Task<bool> AddEntryAsync(ulong userId, WheelType type, WheelEntryKind kind, string item)
        {
            var set = _entries.GetOrAdd((userId, type, kind), _ => new());
            if (!set.TryAdd(item, 0))
                return false;

            using var db = new SupportContext();
            db.FoodWheelEntry.Add(new FoodWheelEntry
            {
                UserId = userId,
                WheelType = (int)type,
                Kind = (int)kind,
                Item = item
            });
            await db.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// 移除項目 (黑名單或自訂)，若不存在則回傳 false
        /// </summary>
        public async Task<bool> RemoveEntryAsync(ulong userId, WheelType type, WheelEntryKind kind, string item)
        {
            if (!_entries.TryGetValue((userId, type, kind), out var set) || !set.TryRemove(item, out _))
                return false;

            using var db = new SupportContext();
            var dbEntry = db.FoodWheelEntry.FirstOrDefault(x =>
                x.UserId == userId && x.WheelType == (int)type && x.Kind == (int)kind && x.Item == item);
            if (dbEntry != null)
            {
                db.FoodWheelEntry.Remove(dbEntry);
                await db.SaveChangesAsync();
            }

            return true;
        }
    }
}
