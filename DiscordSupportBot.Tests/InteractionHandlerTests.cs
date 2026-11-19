using Discord.Interactions;
using DiscordSupportBot.Interaction;

namespace DiscordSupportBot.Tests;

public class InteractionHandlerTests
{
    [Fact]
    public async Task ComponentCommandExecuted_UnknownCustomId_IsIgnored()
    {
        // 基金等按鈕由 ButtonExecuted 事件處理，InteractionService 找不到指令是正常情況
        var handler = new InteractionHandler(null!, null!, null!);
        var result = ExecuteResult.FromError(InteractionCommandError.UnknownCommand, "No component command");

        await handler.ComponentCommandExecuted(null!, null!, result);
    }
}
