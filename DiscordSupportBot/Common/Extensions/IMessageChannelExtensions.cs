using DiscordSupportBot.Common;

namespace DiscordSupportBot.Extensions;

public static class MessageChannelExtensions
{
    // main overload that all other send methods reduce to
    public static Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        string? plainText,
        Embed? embed = null,
        IReadOnlyCollection<Embed>? embeds = null,
        bool sanitizeAll = false,
        MessageComponent? components = null)
    {
        plainText = sanitizeAll
            ? plainText?.SanitizeAllMentions() ?? ""
            : plainText?.SanitizeMentions() ?? "";

        return channel.SendMessageAsync(plainText,
            embed: embed,
            embeds: embeds is null
                ? null
                : embeds as Embed[] ?? embeds.ToArray(),
            components: components,
            options: new()
            {
                RetryMode = RetryMode.AlwaysRetry
            });
    }

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        SmartText text,
        bool sanitizeAll = false)
        => text switch
        {
            SmartEmbedText set => channel.SendAsync(set.PlainText,
                set.IsValid ? set.GetEmbed().Build() : null,
                sanitizeAll: sanitizeAll),
            SmartPlainText st => channel.SendAsync(st.Text,
                default,
                sanitizeAll: sanitizeAll),
            SmartEmbedTextArray arr => channel.SendAsync(arr.Content,
                embeds: arr.GetEmbedBuilders().Map(e => e.Build())),
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static Task EditAsync(
        this IUserMessage userMessage,
        string? plainText,
        Embed? embed = null,
        IReadOnlyCollection<Embed>? embeds = null,
        bool sanitizeAll = false)
    {
        plainText = sanitizeAll
            ? plainText?.SanitizeAllMentions() ?? ""
            : plainText?.SanitizeMentions() ?? "";

        return userMessage.ModifyAsync((act) =>
        {
            act.Content = plainText;
            act.Embed = embed;
            act.Embeds = embeds is null
                ? null
                : embeds as Embed[] ?? embeds.ToArray();
        }, options: new()
        {
            RetryMode = RetryMode.AlwaysRetry
        });
    }

    public static Task EditAsync(
    this IUserMessage userMessage,
    SmartText text,
    bool sanitizeAll = false)
    => text switch
    {
        SmartEmbedText set => userMessage.EditAsync(set.PlainText,
            set.IsValid ? set.GetEmbed().Build() : null,
            sanitizeAll: sanitizeAll),
        SmartPlainText st => userMessage.EditAsync(st.Text,
            default,
            sanitizeAll: sanitizeAll),
        SmartEmbedTextArray arr => userMessage.EditAsync(arr.Content,
            embeds: arr.GetEmbedBuilders().Map(e => e.Build())),
        _ => throw new ArgumentOutOfRangeException(nameof(text))
    };


    public static string SanitizeMentions(this string str, bool sanitizeRoleMentions = false)
    {
        str = str.Replace("@everyone", "@everyοne", StringComparison.InvariantCultureIgnoreCase)
                 .Replace("@here", "@һere", StringComparison.InvariantCultureIgnoreCase);
        if (sanitizeRoleMentions)
            str = str.SanitizeRoleMentions();

        return str;
    }

    public static string SanitizeRoleMentions(this string str)
        => str.Replace("<@&", "<ම&", StringComparison.InvariantCultureIgnoreCase);

    public static string SanitizeAllMentions(this string str)
        => str.SanitizeMentions().SanitizeRoleMentions();
}