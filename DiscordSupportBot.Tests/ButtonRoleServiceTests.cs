using DiscordSupportBot.DataBase;
using DiscordSupportBot.DataBase.Table;
using DiscordSupportBot.Interaction.Admin.Service;

namespace DiscordSupportBot.Tests;

public class ButtonRoleServiceTests
{
    [Theory]
    [InlineData(ButtonRole.ReactionType.Normal)]
    [InlineData(ButtonRole.ReactionType.Verify)]
    public void GetAction_WithoutRole_Adds(ButtonRole.ReactionType type)
    {
        Assert.Equal(ButtonRoleService.ButtonRoleAction.Add, ButtonRoleService.GetAction([], 1, type));
        Assert.Equal(ButtonRoleService.ButtonRoleAction.Add, ButtonRoleService.GetAction([2], 1, type));
    }

    [Fact]
    public void GetAction_NormalAndHasRole_Removes()
    {
        Assert.Equal(
            ButtonRoleService.ButtonRoleAction.Remove,
            ButtonRoleService.GetAction([2, 1], 1, ButtonRole.ReactionType.Normal));
    }

    [Fact]
    public void GetAction_VerifyAndHasRole_KeepsRole()
    {
        Assert.Equal(
            ButtonRoleService.ButtonRoleAction.None,
            ButtonRoleService.GetAction([1], 1, ButtonRole.ReactionType.Verify));
    }

    [Fact]
    public void Model_HasUniqueMessageEmojiIndex()
    {
        using var db = new SupportContext();
        var entity = db.Model.FindEntityType(typeof(ButtonRole));

        Assert.NotNull(entity);
        var index = Assert.Single(entity!.GetIndexes());
        Assert.True(index.IsUnique);
        Assert.Equal(
            new[] { nameof(ButtonRole.MessageId), nameof(ButtonRole.EmojiName) },
            index.Properties.Select((x) => x.Name));
    }
}
