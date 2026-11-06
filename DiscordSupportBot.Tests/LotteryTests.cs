using DiscordSupportBot.Interaction.Lottery;
using LotteryModule = DiscordSupportBot.Interaction.Lottery.Lottery;

namespace DiscordSupportBot.Tests;

public class LotteryTests
{
    [Fact]
    public void DrawWinners_DrawsRequestedCountWithoutDuplicates()
    {
        List<ulong> participants = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        var winners = LotteryModule.DrawWinners(participants, 3);

        Assert.Equal(3, winners.Count);
        Assert.Equal(winners.Count, winners.Distinct().Count());
        Assert.All(winners, (winner) => Assert.Contains(winner, participants));
    }

    [Fact]
    public void DrawWinners_MoreAwardsThanParticipants_ReturnsEveryParticipantOnce()
    {
        List<ulong> participants = [1, 2, 3];

        var winners = LotteryModule.DrawWinners(participants, 5);

        Assert.Equal(participants.OrderBy((x) => x), winners.OrderBy((x) => x));
    }

    [Fact]
    public void DrawWinners_NoParticipants_ReturnsEmpty()
    {
        Assert.Empty(LotteryModule.DrawWinners([], 3));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("[]", false)]
    [InlineData("[123]", true)]
    public void HasWinners_TreatsLegacyEmptyValuesAsNotDrawn(string? winnerList, bool expected)
    {
        Assert.Equal(expected, LotteryModule.HasWinners(winnerList));
    }

    [Fact]
    public void ExcludePreviousWinners_RemovesCurrentWinners()
    {
        List<ulong> participants = [1, 2, 3, 4];

        var candidates = LotteryModule.ExcludePreviousWinners(participants, "[2,4]");

        Assert.Equal([1UL, 3UL], candidates);
    }

    [Fact]
    public void ExcludePreviousWinners_NoWinners_ReturnsAllParticipants()
    {
        List<ulong> participants = [1, 2, 3];

        var candidates = LotteryModule.ExcludePreviousWinners(participants, "[]");

        Assert.Equal(participants, candidates);
    }

    [Fact]
    public void GetWinnerHistory_LegacyEmptyHistory_UsesCurrentWinners()
    {
        var history = LotteryModule.GetWinnerHistory(null, "[1,2]");

        Assert.Equal([1UL, 2UL], history);
    }

    [Fact]
    public void GetWinnerHistory_MergesHistoryAndCurrentWithoutDuplicates()
    {
        var history = LotteryModule.GetWinnerHistory("[1,2]", "[2,3]");

        Assert.Equal([1UL, 2UL, 3UL], history);
    }

    [Fact]
    public void MergeWinnerHistory_AddsOnlyNewWinners()
    {
        var history = LotteryModule.MergeWinnerHistory("[1,2]", "[3]", [2UL, 3UL, 4UL]);

        Assert.Equal([1UL, 2UL, 3UL, 4UL], history);
    }

    [Fact]
    public void DrawWinners_CanDrawParticipantsBeyondTheFirst256()
    {
        List<ulong> participants = Enumerable.Range(1, 1000).Select((x) => (ulong)x).ToList();

        var drawn = new HashSet<ulong>();
        for (int i = 0; i < 5000; i++)
            drawn.UnionWith(LotteryModule.DrawWinners(participants, 1));

        Assert.True(drawn.Count > 256, $"只抽到 {drawn.Count} 個不同的人");
    }

    [Fact]
    public void FormatAutocompleteName_DoesNotCutEmojiInHalf()
    {
        // 前綴 22 字 + 74 個 a，讓 emoji 的 high surrogate 剛好落在第 97 個字元
        var lottery = new DiscordSupportBot.DataBase.Table.Lottery
        {
            AwardContext = new string('a', 74) + "😀" + new string('b', 50),
        };

        string name = LotteryModule.FormatAutocompleteName(lottery);

        Assert.True(name.Length <= 100);
        Assert.False(char.IsHighSurrogate(name[^4]));
    }
}
