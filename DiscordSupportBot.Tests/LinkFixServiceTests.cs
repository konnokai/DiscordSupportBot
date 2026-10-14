using DiscordSupportBot.Interaction.LinkFix.Service;

namespace DiscordSupportBot.Tests;

public class LinkFixServiceTests
{
    [Theory]
    [InlineData("https://a.com/x", false)]
    [InlineData("||https://a.com/x||", true)]
    [InlineData("前文 ||https://a.com/x||", true)]
    [InlineData("||前文 https://a.com/x||", true)]
    [InlineData("||前文\n第二行\nhttps://a.com/x||", true)]
    [InlineData("||前文|| https://a.com/x", false)]
    [InlineData("https://a.com/x ||後文||", false)]
    public void IsInSpoiler_ChecksUrlPosition(string content, bool expected)
    {
        var index = content.IndexOf("https://", StringComparison.Ordinal);

        Assert.Equal(expected, LinkFixService.IsInSpoiler(content, index));
    }
}
