using Discord.Interactions;
using Discord.WebSocket;
using DiscordSupportBot.Interaction;
using DiscordSupportBot.Interaction.Fund;
using DiscordSupportBot.Interaction.Fund.Service;
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

        var signature = InteractionHandler.BuildSignatureText(interactions.SlashCommands);

        // 新增基金類型時選擇清單會變，簽章必須跟著變，Release 才會重新註冊指令
        Assert.Contains("變態", signature);
        Assert.Contains("色狗", signature);
    }
}
