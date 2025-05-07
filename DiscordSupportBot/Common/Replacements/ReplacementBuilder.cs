#nullable disable
using Discord.Commands;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DiscordSupportBot.Common;

public class ReplacementBuilder
{
    private static readonly Regex _rngRegex = new("%rng(?:(?<from>(?:-)?\\d+)-(?<to>(?:-)?\\d+))?%",
        RegexOptions.Compiled);

    private readonly ConcurrentDictionary<Regex, Func<Match, string>> _regex = new();

    private readonly ConcurrentDictionary<string, Func<string>> _reps = new();

    public ReplacementBuilder()
        => WithRngRegex();

    public ReplacementBuilder WithDefault(
        IUser usr,
        IMessageChannel ch,
        SocketGuild g,
        DiscordSocketClient client)
        => WithUser(usr).WithChannel(ch).WithServer(client, g).WithClient(client);

    public ReplacementBuilder WithDefault(ICommandContext ctx)
        => WithDefault(ctx.User, ctx.Channel, ctx.Guild as SocketGuild, (DiscordSocketClient)ctx.Client);

    public ReplacementBuilder WithMention(DiscordSocketClient client)
    {
        _reps.TryAdd("%bot.mention%", () => client.CurrentUser.Mention);
        return this;
    }

    public ReplacementBuilder WithClient(DiscordSocketClient client)
    {
        WithMention(client);

        _reps.TryAdd("%bot.status%", () => client.Status.ToString());
        _reps.TryAdd("%bot.latency%", () => client.Latency.ToString());
        _reps.TryAdd("%bot.name%", () => client.CurrentUser.Username);
        _reps.TryAdd("%bot.fullname%", () => client.CurrentUser.ToString());
        _reps.TryAdd("%bot.discrim%", () => client.CurrentUser.Discriminator);
        _reps.TryAdd("%bot.id%", () => client.CurrentUser.Id.ToString());
        _reps.TryAdd("%bot.avatar%", () => client.CurrentUser.GetAvatarUrl().ToString());

        WithStats(client);
        return this;
    }

    public ReplacementBuilder WithServer(DiscordSocketClient client, SocketGuild g)
    {
        _reps.TryAdd("%server%", () => g is null ? "DM" : g.Name);
        _reps.TryAdd("%server.id%", () => g is null ? "DM" : g.Id.ToString());
        _reps.TryAdd("%server.name%", () => g is null ? "DM" : g.Name);
        _reps.TryAdd("%server.icon%", () => g is null ? null : g.IconUrl);
        _reps.TryAdd("%server.members%", () => g is { } sg ? sg.MemberCount.ToString() : "?");
        _reps.TryAdd("%server.boosters%", () => g.PremiumSubscriptionCount.ToString());
        _reps.TryAdd("%server.boost_level%", () => ((int)g.PremiumTier).ToString());
        return this;
    }

    public ReplacementBuilder WithChannel(IMessageChannel ch)
    {
        _reps.TryAdd("%channel%", () => ch.Name);
        _reps.TryAdd("%channel.mention%", () => (ch as ITextChannel)?.Mention ?? "#" + ch.Name);
        _reps.TryAdd("%channel.name%", () => ch.Name);
        _reps.TryAdd("%channel.id%", () => ch.Id.ToString());
        _reps.TryAdd("%channel.created%", () => ch.CreatedAt.ToString("HH:mm dd.MM.yyyy"));
        _reps.TryAdd("%channel.nsfw%", () => (ch as ITextChannel)?.IsNsfw.ToString() ?? "-");
        _reps.TryAdd("%channel.topic%", () => (ch as ITextChannel)?.Topic ?? "-");
        return this;
    }

    public ReplacementBuilder WithUser(IUser user)
    {
        WithManyUsers(new[] { user });
        return this;
    }

    public ReplacementBuilder WithManyUsers(IEnumerable<IUser> users)
    {
        _reps.TryAdd("%user%", () => string.Join(" ", users.Select(user => user.Mention)));
        _reps.TryAdd("%user.mention%", () => string.Join(" ", users.Select(user => user.Mention)));
        _reps.TryAdd("%user.fullname%", () => string.Join(" ", users.Select(user => user.ToString())));
        _reps.TryAdd("%user.name%", () => string.Join(" ", users.Select(user => user.Username)));
        _reps.TryAdd("%user.discrim%", () => string.Join(" ", users.Select(user => user.Discriminator)));
        _reps.TryAdd("%user.avatar%", () => string.Join(" ", users.Select(user => user.GetAvatarUrl().ToString())));
        _reps.TryAdd("%user.id%", () => string.Join(" ", users.Select(user => user.Id.ToString())));
        _reps.TryAdd("%user.created_time%",
            () => string.Join(" ", users.Select(user => user.CreatedAt.ToString("HH:mm"))));
        _reps.TryAdd("%user.created_date%",
            () => string.Join(" ", users.Select(user => user.CreatedAt.ToString("dd.MM.yyyy"))));
        _reps.TryAdd("%user.joined_time%",
            () => string.Join(" ", users.Select(user => (user as IGuildUser)?.JoinedAt?.ToString("HH:mm") ?? "-")));
        _reps.TryAdd("%user.joined_date%",
            () => string.Join(" ",
                users.Select(user => (user as IGuildUser)?.JoinedAt?.ToString("dd.MM.yyyy") ?? "-")));
        return this;
    }

    private ReplacementBuilder WithStats(DiscordSocketClient c)
    {
        _reps.TryAdd("%shard.servercount%", () => c.Guilds.Count.ToString());
        _reps.TryAdd("%shard.usercount%", () => c.Guilds.Sum(g => g.MemberCount).ToString());
        _reps.TryAdd("%shard.id%", () => c.ShardId.ToString());
        return this;
    }

    public ReplacementBuilder WithRngRegex()
    {
        var rng = new Random();
        _regex.TryAdd(_rngRegex,
            match =>
            {
                if (!int.TryParse(match.Groups["from"].ToString(), out var from))
                    from = 0;
                if (!int.TryParse(match.Groups["to"].ToString(), out var to))
                    to = 0;

                if (from == 0 && to == 0)
                    return rng.Next(0, 11).ToString();

                if (from >= to)
                    return string.Empty;

                return rng.Next(from, to + 1).ToString();
            });
        return this;
    }

    public ReplacementBuilder WithOverride(string key, Func<string> output)
    {
        _reps.AddOrUpdate(key, output, delegate { return output; });
        return this;
    }

    public Replacer Build()
        => new(_reps.Select(x => (x.Key, x.Value)).ToArray(), _regex.Select(x => (x.Key, x.Value)).ToArray());
}