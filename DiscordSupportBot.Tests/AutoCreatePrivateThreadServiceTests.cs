using DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service;

namespace DiscordSupportBot.Tests;

public class AutoCreatePrivateThreadServiceTests
{
    [Fact]
    public void GetThreadReuseAction_PrefersLockThenArchive()
    {
        Assert.Equal(
            AutoCreatePrivateThreadService.ThreadReuseAction.Reuse,
            AutoCreatePrivateThreadService.GetThreadReuseAction(isArchived: false, isLocked: false));
        Assert.Equal(
            AutoCreatePrivateThreadService.ThreadReuseAction.Unarchive,
            AutoCreatePrivateThreadService.GetThreadReuseAction(isArchived: true, isLocked: false));
        Assert.Equal(
            AutoCreatePrivateThreadService.ThreadReuseAction.Locked,
            AutoCreatePrivateThreadService.GetThreadReuseAction(isArchived: false, isLocked: true));
        Assert.Equal(
            AutoCreatePrivateThreadService.ThreadReuseAction.Locked,
            AutoCreatePrivateThreadService.GetThreadReuseAction(isArchived: true, isLocked: true));
    }

    [Theory]
    [InlineData(true, "123", 999UL, 123UL, 999UL, true)]
    [InlineData(true, "123", 111UL, 123UL, 999UL, false)]
    [InlineData(true, "456", 999UL, 123UL, 999UL, false)]
    [InlineData(false, "123", 999UL, 123UL, 999UL, false)]
    public void IsBotOwnedThread_ReturnsExpected(
        bool isPrivateThread,
        string name,
        ulong ownerId,
        ulong userId,
        ulong botId,
        bool expected)
    {
        Assert.Equal(
            expected,
            AutoCreatePrivateThreadService.IsBotOwnedThread(isPrivateThread, name, ownerId, userId, botId));
    }
}
