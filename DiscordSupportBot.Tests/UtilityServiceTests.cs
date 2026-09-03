using DiscordSupportBot.Interaction.Utility;

namespace DiscordSupportBot.Tests;

public class UtilityServiceTests
{
    [Fact]
    public void CalculateAlipayPayment_AppliesFeesAndCeilingsAtRequiredSteps()
    {
        var calculation = UtilityService.CalculateAlipayPayment(1008m, 4.746m);

        Assert.True(calculation.HasOverseasFee);
        Assert.Equal(30.24m, calculation.OverseasFee);
        Assert.Equal(1038.24m, calculation.CnyAmountAfterOverseasFee);
        Assert.Equal(4.8m, calculation.RoundedExchangeRate);
        Assert.Equal(4983.552m, calculation.TwdAmountBeforeCreditCardFee);
        Assert.Equal(74.75328m, calculation.CreditCardFee);
        Assert.Equal(5058.30528m, calculation.TwdAmountAfterCreditCardFee);
        Assert.Equal(5100m, calculation.FinalTwdAmount);

        Assert.False(UtilityService.CalculateAlipayPayment(199m, 4.746m).HasOverseasFee);
        Assert.Equal(6m, UtilityService.CalculateAlipayPayment(200m, 4.746m).OverseasFee);
    }

    [Fact]
    public void ParseCnySellSpotRate_ReturnsRateAndUpdateTime()
    {
        var spotRate = UtilityService.ParseCnySellSpotRate(
            """
            {
              "updateTime": "資料時間 2026-09-03 11:20:01",
              "result": [
                { "Currency": "CNY", "Type": "買入", "PromptExchange": "4.6960" },
                { "Currency": "CNY", "Type": "賣出", "PromptExchange": "4.7460" }
              ]
            }
            """);

        Assert.Equal(4.7460m, spotRate.ExchangeRate);
        Assert.Equal("資料時間 2026-09-03 11:20:01", spotRate.UpdateTime);

        var cachedJson = System.Text.Json.JsonSerializer.Serialize(spotRate);
        var restored = System.Text.Json.JsonSerializer.Deserialize<UtilityService.SpotRateSnapshot>(cachedJson);

        Assert.Equal(spotRate, restored);
    }
}
