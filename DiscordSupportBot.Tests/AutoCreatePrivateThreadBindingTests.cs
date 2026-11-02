using DiscordSupportBot.DataBase;
using DiscordSupportBot.DataBase.Table;

namespace DiscordSupportBot.Tests;

public class AutoCreatePrivateThreadBindingTests
{
    [Fact]
    public void Binding_HasUniqueChannelUserIndex()
    {
        using var db = new SupportContext();
        var entity = db.Model.FindEntityType(typeof(AutoCreatePrivateThreadBinding));

        Assert.NotNull(entity);
        var index = Assert.Single(entity!.GetIndexes());
        Assert.True(index.IsUnique);
        Assert.Equal(
            new[] { nameof(AutoCreatePrivateThreadBinding.ChannelId), nameof(AutoCreatePrivateThreadBinding.UserId) },
            index.Properties.Select(x => x.Name));
    }
}
