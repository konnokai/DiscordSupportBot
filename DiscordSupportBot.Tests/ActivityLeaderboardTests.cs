using Discord;
using DiscordSupportBot.DataBase.Activity;
using DiscordSupportBot.Interaction.Activity;

namespace DiscordSupportBot.Tests;

public class ActivityLeaderboardTests
{
    private const ulong OwnerId = 999;
    private static readonly Dictionary<ulong, ActivityLeaderboard.UserDisplay> NoUsers = [];

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(25, 0)]
    [InlineData(26, 1)]
    [InlineData(100, 3)]
    public void GetLastPage_UsesPageSizeOf25(int count, int expectedLastPage)
    {
        Assert.Equal(expectedLastPage, ActivityLeaderboard.GetLastPage(count));
    }

    [Theory]
    [InlineData(-1, 100, 0)]
    [InlineData(2, 100, 2)]
    [InlineData(10, 100, 3)]
    [InlineData(5, 0, 0)]
    public void ClampPage_KeepsPageInRange(int page, int count, int expected)
    {
        Assert.Equal(expected, ActivityLeaderboard.ClampPage(page, count));
    }

    [Theory]
    [InlineData(1, "🥇")]
    [InlineData(3, "🥉")]
    [InlineData(4, "4\\.")]
    [InlineData(100, "100\\.")]
    public void FormatRank_UsesMedalsThenEscapedNumbers(int rank, string expected)
    {
        Assert.Equal(expected, ActivityLeaderboard.FormatRank(rank));
    }

    [Fact]
    public void SortUsers_BreaksTiesById()
    {
        var sorted = ActivityLeaderboard.SortUsers([new() { UserID = 3, ActivityNum = 5 }, new() { UserID = 1, ActivityNum = 5 }, new() { UserID = 2, ActivityNum = 9 }]);

        Assert.Equal([2UL, 1UL, 3UL], sorted.Select((x) => x.UserID));
    }

    [Fact]
    public void BuildMessagePage_LastPageOf100Users_ShowsRanks76To100()
    {
        var users = CreateUsers(100);

        var lines = GetListLines(ActivityLeaderboard.BuildMessagePage("Guild", users, 3, OwnerId, NoUsers));

        Assert.Equal(25, lines.Length);
        Assert.StartsWith("76\\. ", lines[0]);
        Assert.StartsWith("100\\. ", lines[^1]);
    }

    [Fact]
    public void BuildMessagePage_FirstPage_PutsFirstPlaceInSectionWithAvatar()
    {
        var users = CreateUsers(30);
        var displays = new Dictionary<ulong, ActivityLeaderboard.UserDisplay> { [users[0].UserID] = new("Top*User", "https://cdn.example/avatar.png") };

        var component = ActivityLeaderboard.BuildMessagePage("Guild", users, 0, OwnerId, displays);
        var section = Assert.Single(Flatten(component).OfType<SectionComponent>());
        var thumbnail = Assert.IsType<ThumbnailComponent>(section.Accessory);
        var lines = GetListLines(component);

        Assert.Equal("https://cdn.example/avatar.png", thumbnail.Media.Url);
        Assert.Contains("🥇 **Top\\*User**", Assert.Single(section.Components.OfType<TextDisplayComponent>()).Content);
        Assert.Equal(24, lines.Length);
        Assert.StartsWith("🥈", lines[0]);
    }

    [Fact]
    public void BuildMessagePage_WithoutAvatar_UsesPlainTextForFirstPlace()
    {
        var component = ActivityLeaderboard.BuildMessagePage("Guild", CreateUsers(3), 0, OwnerId, NoUsers);

        Assert.Empty(Flatten(component).OfType<SectionComponent>());
        Assert.Contains(GetTextDisplays(component), (x) => x.StartsWith("🥇 **<@1>**"));
    }

    [Fact]
    public void BuildMessagePage_UnresolvedUsers_ShowMentionWithoutSkippingRanks()
    {
        var users = CreateUsers(30);
        var displays = new Dictionary<ulong, ActivityLeaderboard.UserDisplay> { [users[1].UserID] = new("Known", null) };

        var lines = GetListLines(ActivityLeaderboard.BuildMessagePage("Guild", users, 0, OwnerId, displays));

        Assert.Equal("🥈 Known · 29 則", lines[0]);
        Assert.Equal("🥉 <@3> · 28 則", lines[1]);
        Assert.Equal(24, lines.Length);
    }

    [Fact]
    public void BuildMessagePage_HighlightsOwnerAndShowsOwnerRank()
    {
        var users = CreateUsers(40);
        users[29].UserID = OwnerId;

        var component = ActivityLeaderboard.BuildMessagePage("Guild", users, 1, OwnerId, NoUsers);
        var lines = GetListLines(component);

        Assert.Equal($"**30\\. <@{OwnerId}> · 11 則**", lines[4]);
        Assert.Contains("-# 你的排名：第 30 名 · 11 則", GetTextDisplays(component));
    }

    [Fact]
    public void BuildMessagePage_FormatsCountsWithThousandsSeparator()
    {
        var users = new List<UserTable> { new() { UserID = 1, ActivityNum = 12345 }, new() { UserID = 2, ActivityNum = 1234 } };

        var component = ActivityLeaderboard.BuildMessagePage("Guild", users, 0, OwnerId, NoUsers);

        Assert.Contains(GetTextDisplays(component), (x) => x.Contains("12,345 則訊息"));
        Assert.Equal("🥈 <@2> · 1,234 則", Assert.Single(GetListLines(component)));
    }

    [Fact]
    public void BuildMessagePage_PageButtons_HaveExpectedIdsAndDisabledState()
    {
        var users = CreateUsers(100);
        users[60].UserID = OwnerId;

        var first = GetButtons(ActivityLeaderboard.BuildMessagePage("Guild", users, 0, OwnerId, NoUsers));
        var ownPage = GetButtons(ActivityLeaderboard.BuildMessagePage("Guild", users, 2, OwnerId, NoUsers));
        var last = GetButtons(ActivityLeaderboard.BuildMessagePage("Guild", users, 3, OwnerId, NoUsers));

        Assert.True(first[0].IsDisabled);
        Assert.Equal("1 / 4", first[1].Label);
        Assert.Equal($"act:msg:{OwnerId}:1", first[2].CustomId);
        Assert.False(first[2].IsDisabled);
        Assert.Equal($"act-me:{OwnerId}", first[3].CustomId);
        Assert.False(first[3].IsDisabled);

        Assert.True(ownPage[3].IsDisabled);

        Assert.Equal($"act:msg:{OwnerId}:2", last[0].CustomId);
        Assert.True(last[2].IsDisabled);

        foreach (var buttons in new[] { first, ownPage, last })
            Assert.Equal(buttons.Count, buttons.Select((x) => x.CustomId).Distinct().Count());
    }

    [Fact]
    public void BuildMessagePage_OwnerWithoutRecord_HasNoMyRankButton()
    {
        var component = ActivityLeaderboard.BuildMessagePage("Guild", CreateUsers(30), 0, OwnerId, NoUsers);

        Assert.Equal(3, GetButtons(component).Count);
        Assert.Contains("-# 你還沒有發言紀錄", GetTextDisplays(component));
    }

    [Fact]
    public void BuildPage_SinglePage_HasNoActionRow()
    {
        var users = CreateUsers(25);
        users[0].UserID = OwnerId;

        Assert.Empty(Flatten(ActivityLeaderboard.BuildMessagePage("Guild", users, 0, OwnerId, NoUsers)).OfType<ActionRowComponent>());
        Assert.Empty(Flatten(ActivityLeaderboard.BuildEmotePage("Guild", CreateEmotes(25, "e"), 0, 0, OwnerId)).OfType<ActionRowComponent>());
    }

    [Fact]
    public void BuildPage_SanitizesGuildNameAndUsesComponentsV2Container()
    {
        var component = ActivityLeaderboard.BuildMessagePage("a*b_c", CreateUsers(1), 0, OwnerId, NoUsers);

        // 第一層有非 ActionRow 的元件，Discord.Net 送出時才會自動加上 ComponentsV2 flag
        Assert.Contains(component.Components, (x) => x.Type != ComponentType.ActionRow);
        Assert.StartsWith("## a\\*b\\_c 發言排行榜\n-# 共 1 位 · 第 1 / 1 頁", GetTextDisplays(component)[0]);
    }

    [Fact]
    public void BuildEmotePage_ShowsUnusedCountAndUsesEmoteImage()
    {
        var emotes = CreateEmotes(30, "e");

        var component = ActivityLeaderboard.BuildEmotePage("Guild", emotes, 7, 0, OwnerId);
        var section = Assert.Single(Flatten(component).OfType<SectionComponent>());

        Assert.Equal(emotes[0].EmoteUrl, Assert.IsType<ThumbnailComponent>(section.Accessory).Media.Url);
        Assert.Equal("🥇\n1,029 次", Assert.Single(section.Components.OfType<TextDisplayComponent>()).Content);
        Assert.Contains("-# 另有 7 個表情未使用", GetTextDisplays(component));
        Assert.Equal($"act:emote:{OwnerId}:1", GetButtons(component)[2].CustomId);
        Assert.Equal(3, GetButtons(component).Count);
    }

    [Fact]
    public void BuildEmotePage_WithoutEmoteUrl_ShowsEmoteInText()
    {
        var emotes = CreateEmotes(3, "e");
        emotes[0].EmoteUrl = null;

        var component = ActivityLeaderboard.BuildEmotePage("Guild", emotes, 0, 0, OwnerId);

        Assert.Empty(Flatten(component).OfType<SectionComponent>());
        Assert.Contains($"🥇 {emotes[0].EmoteName}\n1,002 次", GetTextDisplays(component));
    }

    [Fact]
    public void BuildEmotePage_WithoutUnusedEmotes_HasNoFooter()
    {
        var component = ActivityLeaderboard.BuildEmotePage("Guild", CreateEmotes(3, "e"), 0, 0, OwnerId);

        Assert.DoesNotContain(GetTextDisplays(component), (x) => x.StartsWith("-# 另有"));
    }

    [Fact]
    public void BuildEmotePage_WorstCase_StaysWithinDiscordLimits()
    {
        // 名稱 32 字的動態表情、七位數次數，是一行最長的情況
        var emotes = CreateEmotes(200, new string('x', 32), animated: true, baseCount: 9_000_000);

        foreach (var page in new[] { 0, 1, 7 })
        {
            var component = ActivityLeaderboard.BuildEmotePage(new string('名', 100), emotes, 999, page, OwnerId);
            var texts = GetTextDisplays(component);

            Assert.True(Flatten(component).Count() <= 40);
            Assert.All(texts, (x) => Assert.True(x.Length <= 4000));
            Assert.True(texts.Sum((x) => x.Length) <= 4000);
        }
    }

    // 第 i 名的 UserID 是 i，次數是 count - i + 1，已經照名次排好
    [Theory]
    [InlineData("act:msg:999:2", "ChangePageAsync")]
    [InlineData("act:emote:999:-1", "ChangePageAsync")]
    [InlineData("act-me:999", "JumpToMyRankAsync")]
    public async Task ButtonCustomIds_RouteToActivityHandlers(string customId, string expectedMethod)
    {
        using var client = new Discord.WebSocket.DiscordSocketClient();
        using var interactions = new Discord.Interactions.InteractionService(client);
        await interactions.AddModuleAsync<Interaction.Activity.Activity>(null);

        var result = interactions.SearchComponentCommand(FakeComponentInteraction.Create(customId));

        Assert.True(result.IsSuccess, result.ErrorReason);
        Assert.Equal(expectedMethod, result.Command.MethodName);
    }

    [Fact]
    public async Task PageIndicatorButton_DoesNotRouteToAnyHandler()
    {
        using var client = new Discord.WebSocket.DiscordSocketClient();
        using var interactions = new Discord.Interactions.InteractionService(client);
        await interactions.AddModuleAsync<Interaction.Activity.Activity>(null);

        Assert.False(interactions.SearchComponentCommand(FakeComponentInteraction.Create("act-page")).IsSuccess);
    }

    // 路由比對只會讀 Data.CustomId，用 DispatchProxy 做最小的假物件就夠了
    public class FakeComponentInteraction : System.Reflection.DispatchProxy
    {
        private Func<string, object?> _getter = (_) => null;

        public static IComponentInteraction Create(string customId)
        {
            var data = System.Reflection.DispatchProxy.Create<IComponentInteractionData, FakeComponentInteraction>();
            ((FakeComponentInteraction)(object)data)._getter = (name) => name == "get_CustomId" ? customId : null;

            var interaction = System.Reflection.DispatchProxy.Create<IComponentInteraction, FakeComponentInteraction>();
            ((FakeComponentInteraction)(object)interaction)._getter = (name) => name == "get_Data" ? data : null;
            return interaction;
        }

        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
            => _getter(targetMethod?.Name ?? "");
    }

    private static List<UserTable> CreateUsers(int count)
        => Enumerable.Range(1, count).Select((i) => new UserTable { UserID = (ulong)i, ActivityNum = count - i + 1 }).ToList();

    private static List<EmoteTable> CreateEmotes(int count, string name, bool animated = false, int baseCount = 1000)
        => Enumerable.Range(1, count).Select((i) =>
        {
            var id = 1_234_567_890_123_456_000UL + (ulong)i;
            return new EmoteTable
            {
                EmoteID = id,
                EmoteName = $"<{(animated ? "a" : "")}:{name}:{id}>",
                EmoteUrl = $"https://cdn.discordapp.com/emojis/{id}.{(animated ? "gif" : "png")}",
                ActivityNum = baseCount + count - i,
            };
        }).ToList();

    private static IEnumerable<IMessageComponent> Flatten(MessageComponent component)
        => component.Components.SelectMany(Flatten);

    private static IEnumerable<IMessageComponent> Flatten(IMessageComponent component)
    {
        yield return component;

        var children = component switch
        {
            ContainerComponent container => container.Components,
            SectionComponent section => [.. section.Components, section.Accessory],
            ActionRowComponent row => row.Components,
            _ => [],
        };

        foreach (var child in children.SelectMany(Flatten))
            yield return child;
    }

    private static List<string> GetTextDisplays(MessageComponent component)
        => Flatten(component).OfType<TextDisplayComponent>().Select((x) => x.Content).ToList();

    private static List<ButtonComponent> GetButtons(MessageComponent component)
        => Flatten(component).OfType<ButtonComponent>().ToList();

    // 容器第一層的 TextDisplay 只有標題（##）、頁尾（-#）、沒縮圖時的第一名（🥇）和名次清單，剩下的那個就是清單
    private static string[] GetListLines(MessageComponent component)
    {
        var container = Assert.Single(component.Components.OfType<ContainerComponent>());
        var list = container.Components.OfType<TextDisplayComponent>()
            .Single((x) => !x.Content.StartsWith("##") && !x.Content.StartsWith("-#") && !x.Content.StartsWith("🥇"));
        return list.Content.Split('\n');
    }
}
