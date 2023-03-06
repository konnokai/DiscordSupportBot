using Discord.Commands;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Discord_Support_Bot.Command;
public class CommandHandler : ICommandService
{
    private readonly DiscordSocketClient Client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;

    public CommandHandler(IServiceProvider services, CommandService commands, DiscordSocketClient client)
    {
        _commands = commands;
        _services = services;
        Client = client;
    }

    public async Task InitializeAsync()
    {
        await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _services).ConfigureAwait(false);
        Client.MessageReceived += (msg) => { var _ = Task.Run(() => HandleCommandAsync(msg)); return Task.CompletedTask; };
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        var message = messageParam as SocketUserMessage;
        if (message == null || message.Author.IsBot)
            return;

        var guild = Client.Guilds.FirstOrDefault((x) => x.TextChannels.Any((x2) => x2.Id == message.Channel.Id));
        if (guild == null)
            return;

        if (message.Channel.Id == 550724236159877121) //僅限特定伺服器使用
        {
            if (message.Content.ToLower() == "~jia")
            {
                Log.FormatColorWrite($"{message.Author.Username} 通過智商測驗", ConsoleColor.Green);
                await message.DeleteAsync().ConfigureAwait(false);
            }
            else if (message.Author.Id != 284989733229297664)
            {
                await message.DeleteAsync().ConfigureAwait(false);
                if (message.Author.Id != 555699303343980563) Log.FormatColorWrite($"已刪除 {message.Author.Username} 說的 {message.Content}", ConsoleColor.DarkCyan);
            }
            return;
        }

        if (UserActivity.IsInited) await UserActivity.AddActivity(guild.Id, message.Author.Id).ConfigureAwait(false);

        int argPos = 0;
        if (message.HasStringPrefix("!!!", ref argPos))
        {
            var context = new SocketCommandContext(Client, message);

            if (_commands.Search(context, argPos).IsSuccess)
            {
                var result = await _commands.ExecuteAsync(context: context, argPos: argPos, services: _services).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    Log.Error($"[{context.Guild.Name}/{context.Message.Channel.Name}] {message.Author.Username} 執行 {context.Message} 發生錯誤");
                    Log.Error(result.ErrorReason);
                    await context.Channel.SendErrorAsync(result.ErrorReason).ConfigureAwait(false);
                }
                else
                {
                    Log.Info($"[{context.Guild.Name}/{context.Message.Channel.Name}] {message.Author.Username} 執行 {context.Message}");
                }

                try { if (context.Message.Author.Id == Program.ApplicatonOwner.Id || guild.Id == 429605944117297163) await message.DeleteAsync().ConfigureAwait(false); }
                catch { }
            }
        }
        else
        {
            string content = message.Content;
            ITextChannel channel = message.Channel as ITextChannel;
            IGuildUser guildUser = message.Author as IGuildUser;

            if (content == "<:notify:314000626608504832>") // :Thinking:
            {
                await channel.SendMessageAsync("<:notify:314000626608504832><:notify:314000626608504832><:notify:314000626608504832><:notify:314000626608504832><:notify:314000626608504832>").ConfigureAwait(false);
                return;
            }

            if (EmoteActivity.IsInited && Regex.IsMatch(content, @"(<a?:.*?:.*?>)"))
            {
                foreach (Match m in Regex.Matches(content, @"(<a?:.*?:.*?>)"))
                {
                    try
                    {
                        var emote = guild.Emotes.FirstOrDefault((x) => x.Id.ToString() == m.Value.TrimEnd('>').Split(new char[] { ':' })[2]);
                        if (emote != null) await EmoteActivity.AddActivityAsync(guild.Id, emote.Id).ConfigureAwait(false);
                    }
                    catch (Exception ex) { Log.Error(ex.Message); }
                }
            }
        }
    }
}