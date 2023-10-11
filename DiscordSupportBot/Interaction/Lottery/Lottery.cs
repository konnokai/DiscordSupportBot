using Discord.Interactions;

namespace DiscordSupportBot.Interaction.Lottery
{
    [Group("lottery", "抽獎系統")]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class Lottery : TopLevelModule
    {
        private readonly DiscordSocketClient _client;
        public class ShowEndedLotteryAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                using var db = new SupportContext();
                if (!db.Lottery.Any())
                    return AutocompletionResult.FromSuccess();

                var results = db.Lottery.ToList().Where((x) => x.EndTime <= DateTime.Now && x.GuildId == autocompleteInteraction.GuildId).Select((x) => new AutocompleteResult($"({x.CreateTime:yyyy/MM/dd HH:mm:ss}) {x.AwardContext}", x.Guid));
                if (!results.Any())
                    return AutocompletionResult.FromSuccess();

                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }

        public class ShowAllLotteryAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                using var db = new SupportContext();
                if (!db.Lottery.Any())
                    return AutocompletionResult.FromSuccess();

                var results = db.Lottery.ToList().Where((x) => x.GuildId == autocompleteInteraction.GuildId).Select((x) => new AutocompleteResult($"({x.CreateTime:yyyy/MM/dd HH:mm:ss}) {x.AwardContext}", x.Guid));
                if (!results.Any())
                    return AutocompletionResult.FromSuccess();

                return AutocompletionResult.FromSuccess(results.Take(25));
            }
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

                    var lottery = new SQLite.Table.Lottery();
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
                            db.Lottery.Update(lottery);
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
                            db.Lottery.Update(lottery);
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

        [SlashCommand("create-lottery", "建立一個抽獎訊息")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [EnabledInDm(false)]
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
        [EnabledInDm(false)]
        [RequireContext(ContextType.Guild)]
        public async Task StartLotteryAsync([Summary("ended-lottery", "要抽哪個獎項? 僅會顯示已無法參與的抽獎"), Autocomplete(typeof(ShowEndedLotteryAutocompleteHandler))] string guid)
        {
            await DeferAsync(true);

            await Context.Guild.DownloadUsersAsync();

            using var db = new SupportContext();
            var lottery = db.Lottery.FirstOrDefault((x) => x.Guid == guid);
            if (lottery == null)
            {
                await Context.Interaction.SendErrorAsync("錯誤，該獎項無資料，原則上不會遇到這錯誤才對...", true);
                return;
            }

            try
            {
                List<ulong> participantList = JsonConvert.DeserializeObject<List<ulong>>(lottery.ParticipantList);
                List<ulong> awardList = new();
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
                else if (lottery.MaxAward >= participantList.Count)
                {
                    await Context.Interaction.SendConfirmAsync("參與人數未超過抽出人數，將直接讓參與成員得獎", true);
                    awardList = participantList;
                }
                else
                {
                    for (int i = 0; i < lottery.MaxAward; i++)
                    {
                        var award = participantList[RandomNumber.Between(0, participantList.Count - 1)];
                        awardList.Add(award);
                        participantList.Remove(award);
                    }
                    await Context.Interaction.SendConfirmAsync($"已抽出", true, true);
                }

                await Context.Channel.SendConfirmAsync($"抽獎內容: {lottery.Context}\n\n" +
                    $"建立時間: {lottery.CreateTime:yyyy/MM/dd HH:mm:ss}\n\n" +
                    $"得獎人員:\n" +
                    string.Join('\n', awardList.Select((x) =>
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
                    })));

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
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync($"抽獎錯誤: {ex.Message}", true);
                Log.Error($"StartLotteryAsync ({guid}): {ex}");
            }
        }

        [SlashCommand("delete-lottery", "刪除抽獎")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [EnabledInDm(false)]
        [RequireContext(ContextType.Guild)]
        public async Task DeleteLotteryAsync([Summary("lottery", "要抽的獎項"), Autocomplete(typeof(ShowAllLotteryAutocompleteHandler))] string guid)
        {
            await DeferAsync(true);

            using var db = new SupportContext();
            var lottery = db.Lottery.FirstOrDefault((x) => x.Guid == guid);
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
        [EnabledInDm(false)]
        [RequireContext(ContextType.Guild)]
        public async Task ShowParticipantListAsync([Summary("lottery", "要抽的獎項"), Autocomplete(typeof(ShowAllLotteryAutocompleteHandler))] string guid, [Summary("page", "頁數")] int page = 1)
        {
            await DeferAsync(true);

            page -= 1;
            if (page < 0)
            {
                await Context.Interaction.SendErrorAsync("頁數需大於1");
                return;
            }

            await Context.Guild.DownloadUsersAsync();

            using var db = new SupportContext();
            var lottery = db.Lottery.FirstOrDefault((x) => x.Guid == guid);
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
                    return new EmbedBuilder().WithOkColor()
                        .WithTitle($"`{lottery.AwardContext}` 已參與的成員")
                        .WithDescription(string.Join('\n', resultList))
                        .AddField("本次參與抽獎結束時間", lottery.EndTime.ConvertDateTimeToDiscordMarkdown())
                        .AddField("本次抽出人數", lottery.MaxAward.ToString(), true)
                        .AddField("已參與人數", participantList.Count); ;
                }, participantList.Count, 25, true, true, true);
            }
        }
    }
}