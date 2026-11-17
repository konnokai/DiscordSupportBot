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

    [Theory]
    [InlineData("https://fixvx.com/konnokai/status/123", true)]
    [InlineData("||https://fixvx.com/konnokai/status/123||", true)]
    [InlineData("https://a.com/x 後面還有字", false)]
    [InlineData("前面有字 https://a.com/x", false)]
    [InlineData("按鈕用戶組", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLinkFixContent_OnlyMatchesUrlOnlyContent(string? content, bool expected)
    {
        Assert.Equal(expected, LinkFixService.IsLinkFixContent(content));
    }
}
