using DiscordSupportBot.Interaction.Fund.Service;

namespace DiscordSupportBot.Tests;

public class FundServiceTests
{
    [Fact]
    public void BuildFundDescription_WithoutRankChange_OmitsRankSummary()
    {
        var description = FundService.BuildFundDescription(
            "<@1>      +500",
            1,
            500,
            130_800,
            131_300,
            null,
            null);

        Assert.DoesNotContain("排名變動", description);
        Assert.Contains("本次入帳：1 筆，共 +500", description);
        Assert.Contains("基金餘額：130,800 → 131,300", description);
    }

    [Fact]
    public void AppendFundDescription_FirstRankChange_AddsSummary()
    {
        var before = FundService.BuildFundDescription(
            "<@1>      +500",
            1,
            500,
            130_800,
            131_300,
            null,
            null);
        var result = new FundService.FundAddResult(
            FundService.FundType.Lying,
            2,
            99,
            600,
            131_300,
            131_900,
            20,
            19,
            false,
            false);

        var description = FundService.AppendFundDescription(before, "💰 說謊基金入帳｜<@99>", result);

        Assert.Contains("<@2>      +600  🏆 升至第 20 名", description);
        Assert.Contains("🏆 排名變動：第 21 名 → 第 20 名（↑1）", description);
        Assert.Contains("本次入帳：2 筆，共 +1,100", description);
        Assert.Contains("基金餘額：130,800 → 131,900", description);
    }

    [Fact]
    public void AppendFundDescription_MultipleRankChanges_UpdatesSingleSummary()
    {
        var before = FundService.BuildFundDescription(
            "<@1>      +500\n<@2>      +600  🏆 升至第 20 名",
            2,
            1_100,
            130_800,
            131_900,
            21,
            20);
        var result = new FundService.FundAddResult(
            FundService.FundType.Lying,
            3,
            99,
            700,
            131_900,
            132_600,
            19,
            18,
            false,
            false);

        var description = FundService.AppendFundDescription(before, "💰 說謊基金入帳｜<@99>", result);

        Assert.Equal(1, description.Split("🏆 排名變動：").Length - 1);
        Assert.Contains("<@3>      +700  🏆 升至第 19 名", description);
        Assert.Contains("🏆 排名變動：第 21 名 → 第 19 名（↑2）", description);
        Assert.Contains("本次入帳：3 筆，共 +1,800", description);
        Assert.Contains("基金餘額：130,800 → 132,600", description);
    }

    [Theory]
    [InlineData("fund-add-one:0:123", 0UL)]
    [InlineData("fund-add-one:0:123:456", 456UL)]
    public void TryParseAddOneCustomId_AcceptsExistingButtonFormats(string customId, ulong expectedScopeId)
    {
        var parsed = FundService.TryParseAddOneCustomId(customId, out var fundType, out var targetUserId, out var scopeId);

        Assert.True(parsed);
        Assert.Equal(FundService.FundType.Lying, fundType);
        Assert.Equal(123UL, targetUserId);
        Assert.Equal(expectedScopeId, scopeId);
    }

    [Fact]
    public void FormatFundTitle_IncludesFundType()
    {
        Assert.Equal("💰 說謊基金入帳｜<@99>", FundService.FormatFundTitle(FundService.FundType.Lying, 99));
    }
}
