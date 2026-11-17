using Discord.Interactions;
using System.Reflection;

#if DEBUG
using System.Text.RegularExpressions;
#endif

namespace DiscordSupportBot.Interaction
{
    class InteractionHandler : IInteractionService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;

        /// <summary>
        /// 會被全球註冊的 Slash 指令與訊息／使用者指令「規格」雜湊（指令名稱、描述，以及各參數的位置/名稱/型別/是否必填/描述/選項）。
        /// <para>只要規格有任何變動，雜湊即改變，供啟動時判斷是否需要重新註冊全球指令（取代僅比對指令總數的舊做法）。</para>
        /// </summary>
        public string CommandSignature
            => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(BuildSignatureText(_interactions.SlashCommands, _interactions.ContextCommands))));

        internal static string BuildSignatureText(IReadOnlyCollection<SlashCommandInfo> slashCommands, IReadOnlyCollection<ContextCommandInfo> contextCommands)
        {
            var sb = new StringBuilder();

            var commands = slashCommands
                .Where((cmd) => !cmd.Module.DontAutoRegister) // 只納入會被全球註冊的指令（排除伺服器專屬指令）
                .Select((cmd) =>
                {
                    string groupPrefix = string.IsNullOrEmpty(cmd.Module.SlashGroupName) ? "" : cmd.Module.SlashGroupName + " ";
                    // 參數維持宣告順序（即 Discord 上的位置），逐一記錄位置/名稱/型別/是否必填/描述/選項
                    // 選項要納入，否則只改 enum 成員（例如新增基金類型）時不會重新註冊
                    var parameters = cmd.Parameters
                        .Select((p, index) =>
                        {
                            var choices = FormatChoices(p);
                            return $"{index}:{p.Name}:{p.ParameterType.Name}:{(p.IsRequired ? "R" : "O")}:{(p.IsAutocomplete ? "A" : "N")}:{p.Description}{choices}";
                        })
                        .ToList();
                    return (Name: groupPrefix + cmd.Name, cmd.Description, Parameters: parameters);
                })
                .OrderBy((x) => x.Name, StringComparer.Ordinal) // 指令間排序以求穩定；參數內部維持位置順序
                .ToList();

            foreach (var cmd in commands)
            {
                sb.Append(cmd.Name).Append('|').Append(cmd.Description).Append('|');
                foreach (var p in cmd.Parameters)
                    sb.Append(p).Append(';');
                sb.Append('\n');
            }

            // 訊息／使用者指令出現在右鍵選單，名稱一樣要納入，否則新增時不會重新註冊
            foreach (var cmd in contextCommands
                .Where((cmd) => !cmd.Module.DontAutoRegister)
                .OrderBy((cmd) => cmd.Name, StringComparer.Ordinal)
                .ThenBy((cmd) => cmd.CommandType))
                sb.Append("context|").Append(cmd.CommandType).Append('|').Append(cmd.Name).Append('\n');

            return sb.ToString();
        }

        // 選項不一定存在 ParameterInfo.Choices：enum 參數是註冊時才由 Discord.Net 的 EnumConverter 展開，
        // 所以這裡照它的規則自己列一次，否則只改 enum 成員時簽章不會變。
        private static string FormatChoices(SlashCommandParameterInfo parameter)
        {
            if (parameter.Choices.Count > 0)
                return ":" + string.Join(',', parameter.Choices.Select((c) => $"{c.Name}={c.Value}"));

            var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            if (!type.IsEnum)
                return string.Empty;

            var choices = Enum.GetNames(type)
                .SelectMany((name) => type.GetMember(name))
                .Where((member) => !member.IsDefined(typeof(HideAttribute), true))
                .Select((member) => $"{member.GetCustomAttribute<ChoiceDisplayAttribute>()?.Name ?? member.Name}={member.Name}");

            return ":" + string.Join(',', choices);
        }

        public InteractionHandler(IServiceProvider services, InteractionService interactions, DiscordSocketClient client)
        {
            _client = client;
            _interactions = interactions;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            await _interactions.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: _services);

            #region 檢查指令是否符合Discord的Regex規範
#if DEBUG
            bool isError = false;
            Regex regex = new Regex(@"^[\w-]{1,32}$");
            var list = _interactions.Modules.Select(module => module.SlashCommands.Select((x) => new KeyValuePair<string, List<string>>(x.Name, x.Parameters.Select((x2) => x2.Name).ToList())));
            foreach (var item in list)
            {
                foreach (var item2 in item)
                {
                    if (!regex.IsMatch(item2.Key))
                    {
                        isError = true;
                        Log.Error(item2.Key);
                        continue;
                    }

                    foreach (var item3 in item2.Value)
                    {
                        if (!regex.IsMatch(item3))
                        {
                            isError = true;
                            Log.Error($"{item2.Key}: {item3}");
                        }
                    }
                }
            }
            if (isError) Environment.Exit(3);
#endif
            #endregion

            _client.InteractionCreated += (slash) => { var _ = Task.Run(() => HandleInteraction(slash)); return Task.CompletedTask; };
            _client.MessageCommandExecuted += MessageCommandExecuted;
            _client.ButtonExecuted += _client_ButtonExecuted;
            _interactions.SlashCommandExecuted += SlashCommandExecuted;
            _interactions.ContextCommandExecuted += ContextCommandExecuted;
        }

        private Task _client_ButtonExecuted(SocketMessageComponent button)
        {
            Log.Info($"\"{button.User}\" Click Button: {button.Data.CustomId}");
            return Task.CompletedTask;
        }

        private Task MessageCommandExecuted(SocketMessageCommand arg)
        {
            Log.Info($"[{arg.GetGuild()}/{arg.Channel.Name}] {arg.User.Username} 執行訊息指令 `{arg.CommandName}`");
            return Task.CompletedTask;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                var ctx = new SocketInteractionContext(_client, arg);
                await _interactions.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, IResult arg3)
        {
            string slashCommand = $"/{arg1}";
            var commandData = arg2.Interaction.Data as SocketSlashCommandData;
            if (commandData.Options.Count > 0) slashCommand += GetOptionsValue(commandData.Options.First());

            var guild = arg2.Guild?.Name ?? "私人訊息";
            if (arg3.IsSuccess)
            {
                Log.Info($"[{guild}/{arg2.Channel.Name}] {arg2.User.Username} 執行 `{slashCommand}`");
            }
            else
            {
                Log.Error($"[{guild}/{arg2.Channel.Name}] {arg2.User.Username} 執行 `{slashCommand}` 發生錯誤\r\n{arg3.ErrorReason}");
                _ = SendCommandErrorAsync(arg2, arg3);
            }

            return Task.CompletedTask;
        }

        // 右鍵指令沒有回應時使用者只會看到「應用程式沒有回應」，錯誤一律回覆給使用者
        private Task ContextCommandExecuted(ContextCommandInfo arg1, IInteractionContext arg2, IResult arg3)
        {
            var guild = arg2.Guild?.Name ?? "私人訊息";
            if (arg3.IsSuccess)
            {
                Log.Info($"[{guild}/{arg2.Channel.Name}] {arg2.User.Username} 執行訊息指令 `{arg1.Name}`");
                return Task.CompletedTask;
            }

            Log.Error($"[{guild}/{arg2.Channel.Name}] {arg2.User.Username} 執行訊息指令 `{arg1.Name}` 發生錯誤\r\n{arg3.ErrorReason}");
            return SendCommandErrorAsync(arg2, arg3);
        }

        private static Task SendCommandErrorAsync(IInteractionContext context, IResult result)
        {
            return result.Error switch
            {
                InteractionCommandError.UnmetPrecondition => context.Interaction.SendErrorAsync(result.ErrorReason),
                InteractionCommandError.UnknownCommand => context.Interaction.SendErrorAsync("未知的指令，也許被移除或變更了"),
                InteractionCommandError.BadArgs => context.Interaction.SendErrorAsync("輸入的參數錯誤"),
                _ => context.Interaction.SendErrorAsync("未知的錯誤，請向Bot擁有者回報"),
            };
        }

        private string GetOptionsValue(SocketSlashCommandDataOption socketSlashCommandDataOption)
        {
            try
            {
                if (socketSlashCommandDataOption.Type != ApplicationCommandOptionType.SubCommand && socketSlashCommandDataOption.Type != ApplicationCommandOptionType.SubCommandGroup && !socketSlashCommandDataOption.Options.Any())
                    return $" {socketSlashCommandDataOption.Value}";

                if (socketSlashCommandDataOption.Type == ApplicationCommandOptionType.SubCommand || socketSlashCommandDataOption.Type == ApplicationCommandOptionType.SubCommandGroup) GetOptionsValue(socketSlashCommandDataOption.Options.First());
                return " " + string.Join(' ', socketSlashCommandDataOption.Options.Select(option => option.Value));
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
