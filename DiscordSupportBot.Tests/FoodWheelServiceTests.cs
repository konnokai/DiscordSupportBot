using DiscordSupportBot.Interaction.FoodWheel;
using DiscordSupportBot.Interaction.FoodWheel.Service;

namespace DiscordSupportBot.Tests;

public class FoodWheelServiceTests
{
    [Fact]
    public void PickItem_ExcludesRecentItems()
    {
        string[] items = ["牛肉麵", "滷肉飯", "便當"];
        var recent = new HashSet<string> { "牛肉麵", "滷肉飯" };

        for (int i = 0; i < 50; i++)
        {
            var picked = FoodWheelService.PickItem(items, recent, out var startNewRound);

            Assert.Equal("便當", picked);
            Assert.False(startNewRound);
        }
    }

    [Fact]
    public void PickItem_WhenEverythingWasDrawnRecently_StartsNewRound()
    {
        string[] items = ["牛肉麵", "滷肉飯", "便當"];
        var recent = new HashSet<string>(items);

        var picked = Enumerable.Range(0, 200)
            .Select((_) =>
            {
                var item = FoodWheelService.PickItem(items, recent, out var startNewRound);

                Assert.True(startNewRound);
                return item;
            })
            .ToHashSet();

        // 全部排除時要通知呼叫端重新一輪，才會抽得到東西
        Assert.Equal(items.ToHashSet(), picked);
    }

    [Fact]
    public void PickItem_WithoutRecentItems_DoesNotStartNewRound()
    {
        string[] items = ["牛肉麵", "滷肉飯"];
        var recent = new HashSet<string>();

        var picked = Enumerable.Range(0, 100)
            .Select((_) =>
            {
                var item = FoodWheelService.PickItem(items, recent, out var startNewRound);

                Assert.False(startNewRound);
                return item;
            })
            .ToHashSet();

        Assert.Equal(items.ToHashSet(), picked);
    }
}
