using Discord.Interactions;
using System.Security.Cryptography;

namespace DiscordSupportBot.Interaction.Lottery
{
    [Group("lottery", "抽獎系統")]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class Lottery : TopLevelModule
    {
        private readonly DiscordSocketClient _client;
        // 抽獎的檢查、寫入與公告要序列化，避免兩個管理員同時開獎產生兩份結果
        private static readonly SemaphoreSlim DrawLock = new(1, 1);

        private static AutocompletionResult GetSuggestions(IAutocompleteInteraction interaction, Func<DataBase.Table.Lottery, bool> filter)
        {
            using var db = new SupportContext();
            string input = interaction.Data.Current?.Value?.ToString() ?? "";
            var results = db.Lottery.Where((x) => x.GuildId == interaction.GuildId).ToList()
                .Where(filter)
                .Where((x) => string.IsNullOrEmpty(input)
                    || (x.AwardContext ?? "").Contains(input, StringComparison.OrdinalIgnoreCase)
                    || (x.Context ?? "").Contains(input, StringComparison.OrdinalIgnoreCase)
                    || x.Guid.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending((x) => x.CreateTime)
                .Take(25)
                .Select((x) => new AutocompleteResult(FormatAutocompleteName(x), x.Guid));
            return AutocompletionResult.FromSuccess(results);
        }

        // Discord 的選項名稱上限是 100 字，AwardContext 最長 200 字
        internal static string FormatAutocompleteName(DataBase.Table.Lottery lottery)
        {
            string name = $"({lottery.CreateTime:yyyy/MM/dd HH:mm:ss}) {lottery.AwardContext}";
            if (name.Length <= 100)
                return name;

            int cut = 97;
            // 不要切在 emoji 的 high surrogate 後面，否則會留下無效字元
            if (char.IsHighSurrogate(name[cut - 1]))
                cut--;
            return name[..cut] + "...";
        }

        public class ShowEndedLotteryAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
                => Task.FromResult(GetSuggestions(autocompleteInteraction, (x) => x.EndTime <= DateTime.Now && !HasWinners(x.WinnerList)));
        }

        public class ShowDrawnLotteryAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
                => Task.FromResult(GetSuggestions(autocompleteInteraction, (x) => HasWinners(x.WinnerList)));
        }

        public class ShowAllLotteryAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
                => Task.FromResult(GetSuggestions(autocompleteInteraction, (x) => true));
        }

        /// <summary>從參與者抽出不重複的得獎者</summary>
        internal static List<ulong> DrawWinners(IReadOnlyList<ulong> participants, int count)
        {
            var pool = participants.ToList();
            var winners = new List<ulong>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = RandomNumberGenerator.GetInt32(pool.Count);
                winners.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return winners;
        }

        internal static bool HasWinners(string? winnerList)
            => !string.IsNullOrEmpty(winnerList) && winnerList != "[]";

        internal static List<ulong> GetWinners(string? winnerList)
            => HasWinners(winnerList) ? JsonConvert.DeserializeObject<List<ulong>>(winnerList) : [];

        /// <summary>重抽時可選擇排除目前名單中的得獎者</summary>
        internal static List<ulong> ExcludePreviousWinners(IReadOnlyList<ulong> participants, string? winnerList)
            => participants.Except(GetWinners(winnerList)).ToList();

        /// <summary>累計的得獎名單；舊資料沒有歷史欄位時，用目前名單補上</summary>
        internal static List<ulong> GetWinnerHistory(string? historyList, string? currentWinnerList)
        {
            var history = GetWinners(historyList);
            foreach (var winner in GetWinners(currentWinnerList))
                if (!history.Contains(winner))
                    history.Add(winner);
            return history;
        }

        internal static List<ulong> MergeWinnerHistory(string? historyList, string? currentWinnerList, IEnumerable<ulong> newWinners)
        {
            var history = GetWinnerHistory(historyList, currentWinnerList);
            foreach (var winner in newWinners)
                if (!history.Contains(winner))
                    history.Add(winner);
            return history;
        }

        public Lottery(DiscordSocketClient client)
        {
            _client = client;
            client.ModalSubmitted += async (modal) =>
            {
                if (modal.HasResponded || modal.Data.CustomId != "create-lottery")
                    return;

                try
                {
                    string context = modal.Data.Components.First((x) => x.CustomId == "context").Value;
                    string awardContext = modal.Data.Components.First((x) => x.CustomId == "award-context").Value;
                    string endDate = modal.Data.Components.First((x) => x.CustomId == "end-date").Value;
                    string endTime = modal.Data.Components.First((x) => x.CustomId == "end-time").Value;
                    string maxAwardStr = modal.Data.Components.First((x) => x.CustomId == "max-award").Value;

                    if (!DateTime.TryParse($"{endDate} {endTime}", out DateTime endDateTime))
                    {
                        await modal.SendErrorAsync($"結束時間格式錯誤");
                        return;
                    }

                    if (endDateTime <= DateTime.Now)
                    {
                        await modal.SendErrorAsync($"結束時間不可比現在還早");
                        return;
                    }

                    if (!int.TryParse(maxAwardStr, out int maxAward) || maxAward <= 0)
                    {
                        await modal.SendErrorAsync($"獎項抽出數量錯誤，請輸入阿拉伯數字且需為正數");
                        return;
                    }

                    var lottery = new DataBase.Table.Lottery();
                    lottery.Context = context;
                    lottery.AwardContext = awardContext;
                    lottery.EndTime = endDateTime;
                    lottery.MaxAward = maxAward;
                    lottery.GuildId = modal.GuildId.Value;
                    if (string.IsNullOrEmpty(lottery.AwardContext))
                        lottery.AwardContext = $"未知的獎項";

                    using SupportContext supportContext = new SupportContext();
                    supportContext.Add(lottery);
                    supportContext.SaveChanges();

                    var embed = new EmbedBuilder().WithOkColor().WithTitle("注意，抽獎訊息")
                        .WithDescription(context)
                        .AddField("本次參與抽獎結束時間", endDateTime.ConvertDateTimeToDiscordMarkdown())
                        .AddField("本次抽出人數", maxAward.ToString(), true)
                        .AddField("已參與人數", "無人參加").Build();

                    var component = new ComponentBuilder()
                        .WithButton("點我參加", $"join-lottery:{lottery.Guid}", ButtonStyle.Success)
                        .WithButton("取消參加", $"leave-lottery:{lottery.Guid}", ButtonStyle.Danger).Build();

                    await modal.SendConfirmAsync("已建立", false, true);
                    await modal.Channel.SendMessageAsync(embed: embed, components: component);
                }
                catch (Exception ex)
                {
                    await modal.SendErrorAsync($"建立抽獎訊息出錯\n{ex.Message}");
                    Log.Error($"建立抽獎訊息錯誤: {ex}");
                }
            };

            client.ButtonExecuted += async (button) =>
            {
                if (button.HasResponded || !button.Data.CustomId.Contains("lottery:"))
                    return;

                await button.DeferAsync(true);

                string lotteryGuid = button.Data.CustomId.Replace("join-lottery:", "").Replace("leave-lottery:", "");

                using var db = new SupportContext();
                var lottery = db.Lottery.FirstOrDefault((x) => x.Guid == lotteryGuid);
                if (lottery == null)
                {
                    await button.ModifyOriginalResponseAsync((x) => x.Components = new ComponentBuilder()
                                    .WithButton("已結束", $"no-lottery", ButtonStyle.Success, disabled: true).Build());
                    await button.SendErrorAsync("該抽獎不存在，可能已經結束", true);
                    return;
                }

                if (lottery.EndTime <= DateTime.Now)
                {
                    await button.ModifyOriginalResponseAsync((x) => x.Components = new ComponentBuilder()
                                    .WithButton("已結束", $"ended-lottery:{lottery.Guid}", ButtonStyle.Success, disabled: true).Build());
                    await button.SendErrorAsync("該抽獎已結束，無法參加", true);
                    return;
                }

                List<ulong> participantList = JsonConvert.DeserializeObject<List<ulong>>(lottery.ParticipantList);
                if (button.Data.CustomId.StartsWith("join-lottery:"))
                {
                    try
                    {
                        if (participantList.Any((x) => x == button.User.Id))
                        {
                            await button.SendErrorAsync("你已參加本次抽獎", true);
                            return;
                        }
                        else
                        {
                            participantList.Add(button.User.Id);
                            lottery.ParticipantList = JsonConvert.SerializeObject(participantList);
                            db.SaveChanges();
                            await button.SendConfirmAsync("參加成功，請等待獎項抽出", true, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        await button.SendErrorAsync($"參與抽獎失敗");
                        Log.Error($"{button.User.Id} ({button.Data.CustomId}) 參與抽獎失敗: {ex}");
                    }
                }
                else if (button.Data.CustomId.StartsWith("leave-lottery:"))
                {
                    try
                    {
                        if (!participantList.Any((x) => x == button.User.Id))
                        {
                            await button.SendErrorAsync("你尚未參加本次抽獎", true);
                            return;
                        }
                        else
                        {
                            participantList.Remove(button.User.Id);
                            lottery.ParticipantList = JsonConvert.SerializeObject(participantList);
                            db.SaveChanges();
                            await button.SendConfirmAsync("已離開本次抽獎", true, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        await button.SendErrorAsync($"離開抽獎失敗");
                        Log.Error($"{button.User.Id} ({button.Data.CustomId}) 離開抽獎失敗: {ex}");
                    }
                }

                try
                {
                    await button.ModifyOriginalResponseAsync((x) => x.Embed = new EmbedBuilder().WithOkColor().WithTitle("注意，抽獎訊息")
                        .WithDescription(lottery.Context)
                        .AddField("本次參與抽獎結束時間", lottery.EndTime.ConvertDateTimeToDiscordMarkdown())
                        .AddField("本次抽出人數", lottery.MaxAward.ToString(), true)
                        .AddField("已參與人數", participantList.Count == 0 ? "無人參加" : participantList.Count).Build());
                }
                catch (Exception)
                {

                    throw;
                }
            };
        }

        private async Task AnnounceWinnersAsync(DataBase.Table.Lottery lottery, List<ulong> winners, string header)
        {
            var lines = winners.Select((x) =>
            {
                try
                {
                    var user = Context.Guild.GetUser(x);
                    return $"{user} ({user.Mention})";
                }
                catch
                {
                    return $"(<@{x}>)";
                }
            }).ToList();
            if (lines.Count == 0)
                return;

            string prefix = $"抽獎內容: {lottery.Context}\n\n" +
                $"建立時間: {lottery.CreateTime:yyyy/MM/dd HH:mm:ss}\n\n" +
                $"{header}:\n";
            int maxChunkLength = Math.Max(500, 3900 - prefix.Length);

            var chunks = new List<string>();
            var current = new StringBuilder();
            foreach (var line in lines)
            {
                if (current.Length > 0 && current.Length + line.Length + 1 > maxChunkLength)
                {
                    chunks.Add(current.ToString());
                    current.Clear();
                }
                current.AppendLine(line);
            }
            chunks.Add(current.ToString());

            for (int i = 0; i < chunks.Count; i++)
            {
                string content = (i == 0 ? prefix : $"{header}（續）:\n") + chunks[i];
                await Context.Channel.SendConfirmAsync(content);
            }
        }

        [SlashCommand("create-lottery", "建立一個抽獎訊息")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [CommandContextType(InteractionContextType.Guild)]
        [RequireContext(ContextType.Guild)]
        public async Task CreateLotteryAsync()
        {
            await RespondWithModalAsync(new ModalBuilder()
                .WithTitle("建立新抽獎訊息")
                .WithCustomId("create-lottery")
                .AddTextInput("抽獎內容", "context", TextInputStyle.Paragraph, "內容...", 1, 3000, true)
                .AddTextInput("抽獎獎項", "award-context", TextInputStyle.Short, "提示用，僅管理者可見", 1, 200, false)
                .AddTextInput("結束日期", "end-date", TextInputStyle.Short, DateTime.Now.ToString("yyyy/MM/dd"), 10, 10, true, DateTime.Now.AddDays(1).ToString("yyyy/MM/dd"))
                .AddTextInput("結束時間", "end-time", TextInputStyle.Short, DateTime.Now.ToString("HH:mm:ss"), 8, 8, true, DateTime.Now.AddHours(1).ToString("HH:00:00"))
                .AddTextInput("此獎項最多抽出幾人", "max-award", TextInputStyle.Short, "最小為1人", 1, 3, true, "1")
                .Build());
        }

        [SlashCommand("start-lottery", "開始抽獎")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [CommandContextType(InteractionContextType.Guild)]
        [RequireContext(ContextType.Guild)]
        public async Task StartLotteryAsync([Summary("ended-lottery", "要抽哪個獎項? 僅會顯示已無法參與的抽獎"), Autocomplete(typeof(ShowEndedLotteryAutocompleteHandler))] string guid)
        {
            await DeferAsync(true);

            await Context.Guild.DownloadUsersAsync();

            await DrawLock.WaitAsync();
            try
            {
                using var db = new SupportContext();
                var lottery = db.Lottery.FirstOrDefault((x) => x.Guid == guid && x.GuildId == Context.Guild.Id);
                if (lottery == null)
                {
                    await Context.Interaction.SendErrorAsync("錯誤，該獎項無資料，原則上不會遇到這錯誤才對...", true);
                    return;
                }

                if (HasWinners(lottery.WinnerList))
                {
                    await Context.Interaction.SendErrorAsync("此抽獎已開獎，如要重新抽取請使用 /lottery reroll-lottery", true);
                    return;
                }

                if (lottery.EndTime > DateTime.Now)
                {
                    await Context.Interaction.SendErrorAsync("此抽獎尚未結束，無法開獎", true);
                    return;
                }

                try
                {
                    List<ulong> participantList = JsonConvert.DeserializeObject<List<ulong>>(lottery.ParticipantList);
                    if (!participantList.Any())
                    {
                        await Context.Interaction.SendErrorAsync("該抽獎無人參加，將直接移除此抽獎", true);
                        try
                        {
                            db.Lottery.Remove(lottery);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            await Context.Interaction.SendErrorAsync($"抽獎資料庫保存失敗: {ex.Message}", true);
                            Log.Error($"StartLotteryAsync-SaveChanges ({guid}): {ex}");
                        }
                        return;
                    }

                    if (lottery.MaxAward >= participantList.Count)
                        await Context.Interaction.SendConfirmAsync("參與人數未超過抽出人數，將直接讓參與成員得獎", true);
                    else
                        await Context.Interaction.SendConfirmAsync("已抽出", true, true);

                    List<ulong> awardList = DrawWinners(participantList, lottery.MaxAward);
                    lottery.WinnerList = JsonConvert.SerializeObject(awardList);
                    lottery.WinnerHistoryList = lottery.WinnerList;
                    db.SaveChanges();

                    try
                    {
                        await AnnounceWinnersAsync(lottery, awardList, "得獎人員");
                    }
                    catch (Exception ex)
                    {
                        await Context.Interaction.SendErrorAsync($"得獎名單已保存，但公告失敗: {ex.Message}", true);
                        Log.Error($"StartLotteryAsync-Announce ({guid}): {ex}");
                    }
                }
                catch (Exception ex)
                {
                    await Context.Interaction.SendErrorAsync($"抽獎錯誤: {ex.Message}", true);
                    Log.Error($"StartLotteryAsync ({guid}): {ex}");
                }
            }
            finally
            {
                DrawLock.Release();
            }
        }

        [SlashCommand("reroll-lottery", "重新抽取已開獎的抽獎（可排除上次或累計得獎者）")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [CommandContextType(InteractionContextType.Guild)]
        [RequireContext(ContextType.Guild)]
        public async Task RerollLotteryAsync([Summary("lottery", "要重新抽取的抽獎"), Autocomplete(typeof(ShowDrawnLotteryAutocompleteHandler))] string guid,
            [Summary("exclude-winners", "重抽時排除目前名單中的得獎者，提高其他人的中獎機率")] bool excludeWinners = false,
            [Summary("exclude-all-winners", "累計排除所有曾得獎者；與 exclude-winners 同時開啟時以此為準")] bool excludeAllWinners = false)
        {
            await DeferAsync(true);

            await Context.Guild.DownloadUsersAsync();

            await DrawLock.WaitAsync();
            try
            {
                using var db = new SupportContext();
                var lottery = db.Lottery.FirstOrDefault((x) => x.Guid == guid && x.GuildId == Context.Guild.Id);
                if (lottery == null)
                {
                    await Context.Interaction.SendErrorAsync("錯誤，該獎項無資料，原則上不會遇到這錯誤才對...", true);
                    return;
                }

                if (!HasWinners(lottery.WinnerList))
                {
                    await Context.Interaction.SendErrorAsync("此抽獎尚未開獎，請先使用 /lottery start-lottery", true);
                    return;
                }

                try
                {
                    List<ulong> participantList = JsonConvert.DeserializeObject<List<ulong>>(lottery.ParticipantList);
                    if (!participantList.Any())
                    {
                        await Context.Interaction.SendErrorAsync("該抽獎無人參加，無法重新抽取", true);
                        return;
                    }

                    List<ulong> candidateList = participantList;
                    if (excludeAllWinners)
                        candidateList = participantList.Except(GetWinnerHistory(lottery.WinnerHistoryList, lottery.WinnerList)).ToList();
                    else if (excludeWinners)
                        candidateList = ExcludePreviousWinners(participantList, lottery.WinnerList);

                    if (!candidateList.Any())
                    {
                        await Context.Interaction.SendErrorAsync("所有參加者都已經得獎，沒有其他人可以重抽", true);
                        return;
                    }

                    if (candidateList.Count < lottery.MaxAward)
                        await Context.Interaction.SendConfirmAsync($"剩餘可抽人數只有 {candidateList.Count} 人，將全數抽出", true, true);

                    List<ulong> awardList = DrawWinners(candidateList, lottery.MaxAward);
                    lottery.WinnerHistoryList = JsonConvert.SerializeObject(MergeWinnerHistory(lottery.WinnerHistoryList, lottery.WinnerList, awardList));
                    lottery.WinnerList = JsonConvert.SerializeObject(awardList);
                    db.SaveChanges();

                    await Context.Interaction.SendConfirmAsync("已重新抽出", true, true);
                    string header = excludeAllWinners
                        ? "重新抽取的得獎人員（已排除所有曾得獎者）"
                        : excludeWinners ? "重新抽取的得獎人員（已排除上次得獎者）" : "重新抽取的得獎人員";
                    try
                    {
                        await AnnounceWinnersAsync(lottery, awardList, header);
                    }
                    catch (Exception ex)
                    {
                        await Context.Interaction.SendErrorAsync($"得獎名單已保存，但公告失敗: {ex.Message}", true);
                        Log.Error($"RerollLotteryAsync-Announce ({guid}): {ex}");
                    }
                }
                catch (Exception ex)
                {
                    await Context.Interaction.SendErrorAsync($"重新抽取錯誤: {ex.Message}", true);
                    Log.Error($"RerollLotteryAsync ({guid}): {ex}");
                }
            }
            finally
            {
                DrawLock.Release();
            }
        }

        [SlashCommand("delete-lottery", "刪除抽獎")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [CommandContextType(InteractionContextType.Guild)]
        [RequireContext(ContextType.Guild)]
        public async Task DeleteLotteryAsync([Summary("lottery", "要抽的獎項"), Autocomplete(typeof(ShowAllLotteryAutocompleteHandler))] string guid)
        {
            await DeferAsync(true);

            using var db = new SupportContext();
            var lottery = db.Lottery.FirstOrDefault((x) => x.Guid == guid && x.GuildId == Context.Guild.Id);
            if (lottery == null)
            {
                await Context.Interaction.SendErrorAsync("錯誤，該獎項無資料，原則上不會遇到這錯誤才對...", true);
                return;
            }

            db.Lottery.Remove(lottery);
            db.SaveChanges();

            await Context.Interaction.SendConfirmAsync("已移除", true, true);
        }

        [SlashCommand("show-participant-list", "顯示抽獎參與成員清單")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [CommandContextType(InteractionContextType.Guild)]
        [RequireContext(ContextType.Guild)]
        public async Task ShowParticipantListAsync([Summary("lottery", "要抽的獎項"), Autocomplete(typeof(ShowAllLotteryAutocompleteHandler))] string guid, [Summary("page", "頁數")] int page = 1)
        {
            await DeferAsync(true);

            page -= 1;
            if (page < 0)
            {
                await Context.Interaction.SendErrorAsync("頁數最小為 1", true);
                return;
            }

            await Context.Guild.DownloadUsersAsync();

            using var db = new SupportContext();
            var lottery = db.Lottery.FirstOrDefault((x) => x.Guid == guid && x.GuildId == Context.Guild.Id);
            if (lottery == null)
            {
                await Context.Interaction.SendErrorAsync("錯誤，該獎項無資料，原則上不會遇到這錯誤才對...", true);
                return;
            }

            List<ulong> participantList = JsonConvert.DeserializeObject<List<ulong>>(lottery.ParticipantList);
            if (!participantList.Any())
            {
                await Context.Interaction.SendConfirmAsync("該獎項尚無人參加", true);
                return;
            }
            else
            {
                page = Math.Min(page, (participantList.Count - 1) / 25);

                await Context.SendPaginatedConfirmAsync(page, (page) =>
                {
                    var resultList = participantList.Skip(page * 25).Take(25).Select((x) =>
                    {
                        try
                        {
                            var user = Context.Guild.GetUser(x);
                            return $"{user} ({user.Mention})";
                        }
                        catch
                        {
                            return $"(<@{x}>)";
                        }
                    });

                    var embed = new EmbedBuilder().WithOkColor()
                        .WithTitle($"`{lottery.AwardContext}` 已參與的成員")
                        .WithDescription(string.Join('\n', resultList))
                        .AddField("本次參與抽獎結束時間", lottery.EndTime.ConvertDateTimeToDiscordMarkdown())
                        .AddField("本次抽出人數", lottery.MaxAward.ToString(), true)
                        .AddField("已參與人數", participantList.Count);

                    var winnerList = GetWinners(lottery.WinnerList);
                    if (winnerList.Any())
                    {
                        string winnersText = string.Join('\n', winnerList.Take(20).Select((x) => $"<@{x}>"));
                        if (winnerList.Count > 20)
                            winnersText += $"\n…另有 {winnerList.Count - 20} 人";
                        embed.AddField("本次得獎人員", winnersText);
                    }

                    return embed;
                }, participantList.Count, 25, true, true, true);
            }
        }
    }
}