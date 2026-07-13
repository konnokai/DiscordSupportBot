using Microsoft.EntityFrameworkCore;
using OpenCCNET;
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
            "牛肉炒飯", "麻辣香鍋", "花雕雞飯", "紅燒牛肉麵", "炸醬米粉", "鹽水雞飯", "蔥油餅夾肉鬆", "滷肉飯加蛋", "牛肉湯麵", "麻辣燙", "炸雞排飯", "炒米粉", "牛肉燴飯", "蒜香雞腿飯", "洨"
        ];

        public IReadOnlyList<string> DrinkList { get; } =
        [
            "可樂", "雪碧", "芬達", "紅茶", "綠茶", "奶茶", "咖啡", "果汁", "水", "運動飲料", "能量飲料", "酒", "魔爪", "紅牛", "茶裏王", "布丁奶茶", "康師傅冰紅茶", "維他露P", "養樂多", "蘋果西打",
            "葡萄汁", "柳橙汁", "檸檬水", "椰子水", "蜂蜜水", "薑母茶", "花茶", "氣泡水", "冰沙", "奶昔", "優酪乳", "豆漿", "杏仁茶", "紅豆牛奶", "綠豆湯", "薏仁水", "冬瓜茶", "洛神花茶", "金桔檸檬汁",
            "百香果汁", "芒果汁", "西瓜汁"
        ];

        /// <summary>
        /// 使用者項目快取: (UserId, WheelType, WheelEntryKind) -> 項目集合
        /// </summary>
        private readonly ConcurrentDictionary<(ulong UserId, WheelType Type, WheelEntryKind Kind), ConcurrentDictionary<string, byte>> _entries = new();

        /// <summary>
        /// 主清單快照 (硬編碼 ∪ 遠端食譜)，刷新時整個 reference 原子替換
        /// </summary>
        private sealed record RecipeSnapshot(
            IReadOnlyList<string> Food,
            IReadOnlyList<string> Drink,
            IReadOnlyDictionary<string, string> Links);

        private volatile RecipeSnapshot _snapshot;

        private sealed class RemoteRecipe
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("category")] public string Category { get; set; }
            [JsonProperty("source_path")] public string SourcePath { get; set; }
        }

        private const string RecipesUrl = "https://eat.ryanuo.cc/recipes.json";
        private static readonly HttpClient _httpClient = new();
        private static readonly HashSet<string> _foodCategories = ["荤菜", "素菜", "主食", "水产", "早餐", "汤与粥", "甜品"];
        private static bool _openCcInitialized;

        public FoodWheelService()
        {
            using var db = new SupportContext();

            foreach (var entry in db.FoodWheelEntry.AsNoTracking())
            {
                var set = _entries.GetOrAdd((entry.UserId, (WheelType)entry.WheelType, (WheelEntryKind)entry.Kind), _ => new());
                set.TryAdd(entry.Item, 0);
            }

            _snapshot = new(FoodList, DrinkList, new Dictionary<string, string>());

            Task.Run(RefreshRemoteRecipesAsync);

            // 下次台灣時間 (UTC+8) 週一 15:00
            var nowTw = DateTime.UtcNow.AddHours(8);
            var next = nowTw.Date.AddDays(((int)DayOfWeek.Monday - (int)nowTw.DayOfWeek + 7) % 7).AddHours(15);
            if (next <= nowTw) next = next.AddDays(7);
            _ = new Timer((_) => _ = RefreshRemoteRecipesAsync(), null, next - nowTw, TimeSpan.FromDays(7));
        }

        public IReadOnlyList<string> GetMasterList(WheelType type)
            => type == WheelType.Drink ? _snapshot.Drink : _snapshot.Food;

        /// <summary>
        /// 取得遠端食譜項目對應的 HowToCook 食譜連結
        /// </summary>
        public bool TryGetRecipeLink(string item, out string url)
            => _snapshot.Links.TryGetValue(item, out url);

        /// <summary>
        /// 抓取遠端食譜清單，轉繁體後與硬編碼清單合併去重
        /// </summary>
        private async Task RefreshRemoteRecipesAsync()
        {
            try
            {
                if (!_openCcInitialized)
                {
                    // 字典目錄以執行檔位置為準，避免工作目錄不同時找不到資源
                    // MaxMatch 不需載入 Jieba，啟動快且短菜名的轉換結果實測比 Jieba 分詞更準確
                    ZhConverter.Initialize(
                        Path.Combine(AppContext.BaseDirectory, "Dictionary"),
                        Path.Combine(AppContext.BaseDirectory, "JiebaResource"),
                        segmentMode: SegmentMode.MaxMatch);
                    _openCcInitialized = true;
                }

                var json = await _httpClient.GetStringAsync(RecipesUrl);
                var recipes = JsonConvert.DeserializeObject<List<RemoteRecipe>>(json) ?? [];

                var food = new List<string>(FoodList); var foodSeen = new HashSet<string>(FoodList);
                var drink = new List<string>(DrinkList); var drinkSeen = new HashSet<string>(DrinkList);
                var links = new Dictionary<string, string>();

                foreach (var recipe in recipes)
                {
                    List<string> target; HashSet<string> seen;
                    if (recipe.Category == "饮料") { target = drink; seen = drinkSeen; }
                    else if (_foodCategories.Contains(recipe.Category)) { target = food; seen = foodSeen; }
                    else continue; // 排除 半成品加工 / 酱料和其它材料

                    var name = ZhConverter.HansToTW(recipe.Name, true);
                    if (!seen.Add(name))
                        continue;

                    target.Add(name);
                    links[name] = "https://github.com/Anduin2017/HowToCook/blob/master/" +
                        string.Join('/', recipe.SourcePath.Split('/').Select(Uri.EscapeDataString));
                }

                _snapshot = new(food, drink, links);
                Log.Info($"食譜清單已更新: 食物 {food.Count} 項, 飲料 {drink.Count} 項");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RefreshRemoteRecipes"); // 失敗保留上一份快照 (至少有硬編碼清單)
            }
        }

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
