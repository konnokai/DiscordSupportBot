using Discord.Interactions;
using Discord.WebSocket;
using DiscordSupportBot.Interaction;
using DiscordSupportBot.Interaction.Fund;
using DiscordSupportBot.Interaction.Fund.Service;
using DiscordSupportBot.Interaction.LinkFix;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordSupportBot.Tests;

public class CommandSignatureTests
{
    [Fact]
    public async Task Signature_ListsFundTypeChoices()
    {
        using var client = new DiscordSocketClient();
        var services = new ServiceCollection()
            .AddSingleton(client)
            .AddSingleton<FundService>()
            .BuildServiceProvider();

        var interactions = new InteractionService(client);
        await interactions.AddModuleAsync(typeof(Fund), services);

        var signature = InteractionHandler.BuildSignatureText(interactions.SlashCommands, interactions.ContextCommands);

        // 新增基金類型時選擇清單會變，簽章必須跟著變，Release 才會重新註冊指令
        Assert.Contains("變態", signature);
        Assert.Contains("色狗", signature);
        Assert.Contains("給給", signature);
        Assert.Contains("context|Message|對該訊息的作者添加基金", signature);
    }

    [Fact]
    public async Task Signature_ListsMessageCommands()
    {
        using var client = new DiscordSocketClient();
        var services = new ServiceCollection()
            .AddSingleton(client)
            .BuildServiceProvider();

        var interactions = new InteractionService(client);
        await interactions.AddModuleAsync(typeof(LinkFixMessageCommand), services);

        var signature = InteractionHandler.BuildSignatureText(interactions.SlashCommands, interactions.ContextCommands);

        // 訊息指令也要進簽章，Release 才會在新增或改名時重新註冊
        Assert.Contains("context|Message|刪除機器人訊息", signature);
    }

    [Fact]
    public async Task RerollLottery_HasOptionalExcludeWinnersOption()
    {
        using var client = new DiscordSocketClient();
        var services = new ServiceCollection()
            .AddSingleton(client)
            .BuildServiceProvider();

        var interactions = new InteractionService(client);
        await interactions.AddModuleAsync(typeof(DiscordSupportBot.Interaction.Lottery.Lottery), services);

        var reroll = Assert.Single(interactions.SlashCommands, (x) => x.Name == "reroll-lottery");
        var option = Assert.Single(reroll.Parameters, (x) => x.Name == "exclude-winners");
        var allWinnersOption = Assert.Single(reroll.Parameters, (x) => x.Name == "exclude-all-winners");

        Assert.False(option.IsRequired);
        Assert.Equal("重抽時排除目前名單中的得獎者，提高其他人的中獎機率", option.Description);
        Assert.False(allWinnersOption.IsRequired);
        Assert.Equal("累計排除所有曾得獎者；與 exclude-winners 同時開啟時以此為準", allWinnersOption.Description);
    }
}
