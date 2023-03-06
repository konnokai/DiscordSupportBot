using Discord.Commands;
using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Exporting;

namespace Discord_Support_Bot.Command.Administration
{
    public class AdministraionService : ICommandService
    {
        private DiscordSocketClient _client;
        private BotConfig _botConfig;
        internal readonly DiscordClient _discordClient;
        internal readonly HttpClient httpClient = new HttpClient();
        internal readonly ChannelExporter _channelExporter;
        internal readonly HashSet<ulong> _exportedChannelId = new HashSet<ulong>();

        public AdministraionService(DiscordSocketClient client, BotConfig botConfig)
        {
            _client = client;
            _botConfig = botConfig;
            _channelExporter = new ChannelExporter(new DiscordClient(botConfig.DiscordToken));
        }

        public async Task ClearUser(ITextChannel textChannel)
        {
            IEnumerable<IMessage> msgs = (await textChannel.GetMessagesAsync(100).FlattenAsync().ConfigureAwait(false))
                  .Where((item) => item.Author.Id == _client.CurrentUser.Id);

            await textChannel.DeleteMessagesAsync(msgs).ConfigureAwait(false);
        }

        public async Task CheckRole(SocketCommandContext context, ulong rid)
        {
            try
            {
                SocketRole role = context.Guild.GetRole(rid);
                if (role == null) await context.Channel.SendMessageAsync("找不到該用戶組");
                var embed = new EmbedBuilder()
                 .WithDescription($"即將給予 `{role.Name}` 到未擁有用戶組的成員" +
                 $"\n請確認是否正確");

                if (await context.PromptUserConfirmAsync(embed))
                {
                    await context.Channel.SendMessageAsync("Working...");

                    List<SocketGuildUser> socketGuildUsers = new List<SocketGuildUser>(context.Guild.Users.Where((x) => x.Roles.Count == 1));
                    int i = 0;

                    foreach (var item in socketGuildUsers)
                    {
                        await item.AddRoleAsync(role);
                        Log.FormatColorWrite($"已給予 {item.Username}");

                        i++;
                    }

                    await context.Channel.SendMessageAsync($"已完成，本次給予了 {i.ToString()} 人");
                }
            }
            catch (Exception ex) { await context.Channel.SendMessageAsync(ex.Message); }
        }

        public async Task CheckRole(SocketCommandContext context)
        {
            try
            {
                SocketRole role = context.Guild.GetRole(464078572592693248); // 菜鳥紳士
                SocketRole role2 = context.Guild.GetRole(464085428941619220); // 新人甲甲

                await context.Channel.SendMessageAsync("Working...");

                List<SocketGuildUser> socketGuildUsers = new List<SocketGuildUser>(context.Guild.Users.Where((x) => x.Roles.Any(x2 => x2 == role) && x.Roles.Any((x2) => x2 == role2)));
                int i = 0;

                foreach (var item in socketGuildUsers)
                {
                    await item.RemoveRoleAsync(role2);
                    Log.FormatColorWrite($"已移除 {item.Username}");

                    i++;
                }

                await context.Channel.SendMessageAsync($"已完成，本次移除了 {i} 人");
            }
            catch (Exception ex) { await context.Channel.SendMessageAsync(ex.Message); }
        }

        public async Task Kick(SocketCommandContext context, ulong rid, bool kickSwitch, string text)
        {
            SocketRole role = context.Guild.GetRole(rid);
            if (role == null) await context.Channel.SendMessageAsync("找不到該用戶組");

            var embed = new EmbedBuilder()
                .WithDescription("即將剔除 `" + (kickSwitch ? "包含" : "不包含") + $"` `{role.Name}` 用戶組的成員" +
                (text != "" ? $"\n剔除訊息為\n```js\n{text}\n```" : "\n不發送剔除訊息") +
                $"\n請確認是否正確");

            if (await context.PromptUserConfirmAsync(embed))
            {
                await context.Channel.SendMessageAsync("Working");

                int i = 0;

                try
                {
                    List<SocketGuildUser> socketGuildUsers =
                        new List<SocketGuildUser>(context.Guild.Users.Where((x) => kickSwitch ? x.Roles.Any((x2) => x2.Id == rid) : !x.Roles.Any((x2) => x2.Id == rid)));

                    foreach (var item in socketGuildUsers)
                    {
                        if (text != "")
                        {
                            try { await item.SendMessageAsync(text); }
                            catch (Exception) { }
                        }

                        await item.KickAsync();
                        Log.FormatColorWrite("已剔除 " + item.Username);

                        i++;
                    }
                }
                catch (Exception ex) { await context.Channel.SendMessageAsync(ex.Message); }

                await context.Channel.SendMessageAsync($"已完成，本次剔除了 {i} 人");
            }
            else await context.Channel.SendMessageAsync("已取消");
        }
    }
}
