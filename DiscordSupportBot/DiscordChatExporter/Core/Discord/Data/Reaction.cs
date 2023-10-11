using DiscordChatExporter.Core.Utils.Extensions;
using System.Text.Json;

namespace DiscordChatExporter.Core.Discord.Data;

// https://discord.com/developers/docs/resources/channel#reaction-object
public record Reaction(Emoji Emoji, int Count)
{
    public static Reaction Parse(JsonElement json)
    {
        var emoji = json.GetProperty("emoji").Pipe(Emoji.Parse);
        var count = json.GetProperty("count").GetInt32();

        return new Reaction(emoji, count);
    }
}