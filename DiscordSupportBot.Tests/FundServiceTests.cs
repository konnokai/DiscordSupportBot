using DiscordSupportBot.Interaction.Fund.Service;

namespace DiscordSupportBot.Tests;

public class FundServiceTests
{
    [Fact]
    public void BuildSingleRecipientDescription_WithoutRankChange_OmitsRankSummary()
    {
        var description = FundService.BuildSingleRecipientDescription(
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
    public void AppendSingleRecipientDescription_FirstRankChange_AddsSummary()
    {
        var before = FundService.BuildSingleRecipientDescription(
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
            false,
            [88]);

        var description = FundService.AppendSingleRecipientDescription(before, result);

        Assert.Contains("<@2>      +600  🏆 升至第 20 名，超過 <@88>", description);
        Assert.Contains("🏆 排名變動：第 21 名 → 第 20 名（↑1）", description);
        Assert.Contains("本次入帳：2 筆，共 +1,100", description);
        Assert.Contains("基金餘額：130,800 → 131,900", description);
    }

    [Fact]
    public void AppendSingleRecipientDescription_MultipleRankChanges_UpdatesSingleSummary()
    {
        var before = FundService.BuildSingleRecipientDescription(
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
            false,
            Array.Empty<ulong>());

        var description = FundService.AppendSingleRecipientDescription(before, result);

        Assert.Equal(1, description.Split("🏆 排名變動：").Length - 1);
        Assert.Contains("<@3>      +700  🏆 升至第 19 名", description);
        Assert.Contains("🏆 排名變動：第 21 名 → 第 19 名（↑2）", description);
        Assert.Contains("本次入帳：3 筆，共 +1,800", description);
        Assert.Contains("基金餘額：130,800 → 132,600", description);
    }

    [Fact]
    public void AppendOwnerRedirectDescription_ReplacesSingleRecipientSummary()
    {
        var before = FundService.BuildSingleRecipientDescription(
            "<@1>      +500 → <@98>",
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
            true,
            [77]);

        var description = FundService.AppendOwnerRedirectDescription(before, result);

        Assert.Contains("<@1>      +500 → <@98>", description);
        Assert.Contains("<@2> +600 → <@99>", description);
        Assert.Contains("餘額：131,300 → 131,900｜🏆 排名：21 → 20，超過 <@77>", description);
        Assert.Contains("亂彈紀錄：2 次｜命中：2 人｜累計 +1,100", description);
        Assert.DoesNotContain("本次入帳", description);
        Assert.DoesNotContain("基金餘額", description);
    }

    [Fact]
    public void BuildOwnerRedirectDescription_RepeatedRecipient_CountsOnce()
    {
        var description = FundService.BuildOwnerRedirectDescription(
            "<@1> +500 → <@99>\n　餘額：0 → 500\n<@2> +600 → <@99>\n　餘額：500 → 1,100",
            2,
            1_100);

        Assert.Contains("亂彈紀錄：2 次｜命中：1 人｜累計 +1,100", description);
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
    public void FormatFundTitle_UsesDisplayName()
    {
        Assert.Equal("💰 說謊基金入帳｜測試使用者", FundService.FormatFundTitle(FundService.FundType.Lying, "測試使用者"));
    }

    [Fact]
    public void FormatFundTitle_OwnerRedirect_UsesSeparateTitle()
    {
        Assert.Equal("🎲 說謊基金 Owner 亂彈", FundService.FormatFundTitle(FundService.FundType.Lying, string.Empty, true));
    }
}
