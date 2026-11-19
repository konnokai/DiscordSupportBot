using Discord.Interactions;
using StackExchange.Redis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DiscordSupportBot.Interaction.Fund.Service
{
    public class FundService : IInteractionService, IDisposable
    {
        private const string AddOneCustomIdPrefix = "fund-add-one:";
        private const string AddOneCooldownKeyPrefix = "SupportBot:Fund:AddOneCooldown";
        private const string MessageStateCacheKeyPrefix = "SupportBot:Fund:MessageState";
        private static readonly TimeSpan MessageStateCacheTtl = TimeSpan.FromMinutes(5);

        public enum FundType
        {
            [ChoiceDisplay("說謊")]
            Lying,
            [ChoiceDisplay("暈船")]
            Dizzy,
            [ChoiceDisplay("色狗")]
            HentaiDog,
            [ChoiceDisplay("渣男")]
            FuckBoy,
            [ChoiceDisplay("抖M")]
            Masochism,
            [ChoiceDisplay("小丑")]
            Clown,
            [ChoiceDisplay("爛笑話")]
            BadJoke,
            [ChoiceDisplay("炸寢")]
            SleepBomb,
            [ChoiceDisplay("怪人")]
            Freak,
            [ChoiceDisplay("錯字")]
            Typo,
            [ChoiceDisplay("作夢")]
            Dreaming,
            [ChoiceDisplay("藏圖")]
            HiddenImage,
            // 只能加在最後：按鈕的 customId 用 (int)FundType，插在中間會讓舊訊息的按鈕指到別的基金
            [ChoiceDisplay("變態")]
            Hentai,
            [ChoiceDisplay("給給")]
            Gayge,
        }

        private readonly DiscordSocketClient _client;
        // ponytail: one process-wide lock prevents duplicate service instances from editing stale message state.
        private static readonly SemaphoreSlim AddOneMessageLock = new(1, 1);
        private int _initialized;

        internal sealed record FundMessageState(string Title, string Description);

        internal sealed record FundAddResult(
            FundType FundType,
            ulong ExecuteUserId,
            ulong RecipientUserId,
            long IncrementAmount,
            long PreviousAmount,
            long NewAmount,
            long? PreviousRank,
            long? NewRank,
            bool IsSunday,
            bool WasOwnerRedirected,
            IReadOnlyList<ulong> OvertakenUserIds)
        {
            public bool RankImproved => PreviousRank.HasValue && NewRank.HasValue && NewRank < PreviousRank;
        }

        public FundService(DiscordSocketClient client)
        {
            _client = client;
        }

        public void Initialize()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            _client.ModalSubmitted += _client_ModalSubmitted;
            _client.ButtonExecuted += HandleAddOneButtonAsync;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _initialized, 0) == 0)
                return;

            _client.ModalSubmitted -= _client_ModalSubmitted;
            _client.ButtonExecuted -= HandleAddOneButtonAsync;
        }

        private async Task _client_ModalSubmitted(SocketModal arg)
        {
            if (arg.HasResponded)
                return;

            if (!arg.Data.CustomId.StartsWith("add_lying_fund"))
                return;

            await arg.DeferAsync(false);

            try
            {
                var customIdParts = arg.Data.CustomId.Split(':');
                var guildId = ulong.Parse(customIdParts[1]);
                var targetUserId = ulong.Parse(customIdParts[2]);
                var messageId = ulong.Parse(customIdParts[3]);
                var fundType = Enum.Parse<FundType>(arg.Data.Components
                    .First(x => x.CustomId == "select_fund_type").Values.First());
                var jumpUrl = $"https://discord.com/channels/{guildId}/{arg.ChannelId.Value}/{messageId}";

                await AddFundAndRespondAsync(
                    arg,
                    fundType,
                    guildId,
                    arg.ChannelId.Value,
                    arg.User.Id,
                    targetUserId,
                    $"[點我回原訊息]({jumpUrl})\n\n");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"FundService-ModalSubmitted: {arg.Data.CustomId}");
                // 第一則 followup 會蓋掉公開的「思考中」；先刪掉才能用 ephemeral 送錯誤
                try { await arg.DeleteOriginalResponseAsync(); }
                catch { }
                await arg.SendErrorAsync($"處理過程發生錯誤: {ex.Message}", true);
            }
        }

        private Task HandleAddOneButtonAsync(SocketMessageComponent arg)
        {
            if (arg.HasResponded || !arg.Data.CustomId.StartsWith(AddOneCustomIdPrefix, StringComparison.Ordinal))
                return Task.CompletedTask;

            // Discord.Net 會等 ButtonExecuted 的所有 handler 做完才處理下一個事件。
            // 在 gateway 上等 Redis 或 REST 會拖慢後面的互動，超過 3 秒才回覆就只剩 10062
            _ = Task.Run(() => HandleAddOneButtonCoreAsync(arg));
            return Task.CompletedTask;
        }

        private async Task HandleAddOneButtonCoreAsync(SocketMessageComponent arg)
        {
            string cooldownKey = null;
            var fundAttempted = false;
            try
            {
                await arg.DeferAsync(false);

                if (arg.GuildId == null || !TryParseAddOneCustomId(arg.Data.CustomId, out var fundType, out var targetUserId, out var cooldownScopeId))
                {
                    await arg.SendErrorAsync("基金按鈕資料無效", true);
                    return;
                }

                var key = GetAddOneCooldownKey(cooldownScopeId == 0 ? arg.Message.Id : cooldownScopeId, arg.User.Id);
                var canUseButton = await RedisConnection.RedisDb.StringSetAsync(
                    key,
                    1,
                    expiry: TimeSpan.FromHours(1),
                    when: When.NotExists);
                if (!canUseButton)
                {
                    await arg.SendErrorAsync("無法連續使用此按鈕", true);
                    return;
                }

                // 只刪自己搶到的 key，避免錯誤時刪掉別人已設好的冷卻
                cooldownKey = key;

                await AddOneMessageLock.WaitAsync();
                try
                {
                    var wasOwnerRedirected = CheckIsAddOwner(fundType, arg.GuildId.Value, arg.User.Id, targetUserId, out var needAddUserId);
                    if (needAddUserId == 0)
                    {
                        // 沒有對象可以亂彈，這次不入帳也不佔冷卻
                        await RedisConnection.RedisDb.KeyDeleteAsync(cooldownKey);
                        await arg.SendErrorAsync("找不到可以亂彈的對象，這次不執行", true);
                        return;
                    }

                    // AddFundAsync 內含 ZINCRBY，出錯時無法確定有沒有入帳，保守保留冷卻
                    fundAttempted = true;
                    var result = await AddFundAsync(fundType, arg.GuildId.Value, arg.ChannelId.Value, arg.User.Id, needAddUserId, wasOwnerRedirected);
                    await AppendFundResultAsync(arg, result);
                }
                finally
                {
                    AddOneMessageLock.Release();
                }
            }
            catch (Exception ex)
            {
                // 入帳前就失敗的點擊不該被冷卻鎖住，讓使用者可以重試
                if (cooldownKey != null && !fundAttempted)
                {
                    try
                    {
                        await RedisConnection.RedisDb.KeyDeleteAsync(cooldownKey);
                    }
                    // RedisTimeoutException 繼承 TimeoutException 而不是 RedisException，這裡要全部接住。
                    // 背景工作的例外沒有人會看到，漏掉就只剩冷卻沒刪、使用者沒收到錯誤
                    catch (Exception deleteEx)
                    {
                        Log.Error(deleteEx, $"FundService-RemoveCooldown: {cooldownKey}");
                    }
                }

                Log.Error(ex, $"FundService-AddOneButton: {arg.GuildId} | {arg.User.Id} | {arg.Data.CustomId}");

                // DeferAsync 本身失敗時還沒回應過，只能用 RespondAsync
                try
                {
                    await arg.SendErrorAsync($"處理過程發生錯誤: {ex.Message}", arg.HasResponded);
                }
                catch (Exception sendEx)
                {
                    Log.Error(sendEx, $"FundService-AddOneButtonErrorReply: {arg.GuildId} | {arg.User.Id}");
                }
            }
        }

        private async Task AppendFundResultAsync(SocketMessageComponent interaction, FundAddResult result)
        {
            var state = await GetCachedMessageStateAsync(interaction.Message.Id);

            if (state == null)
            {
                var originalEmbed = interaction.Message.Embeds.FirstOrDefault();
                state = new FundMessageState(originalEmbed?.Title ?? string.Empty, originalEmbed?.Description ?? string.Empty);
            }

            var title = result.WasOwnerRedirected
                ? FormatFundTitle(result.FundType, string.Empty, true)
                : string.IsNullOrEmpty(state.Title) ? FormatFundTitle(
                    result.FundType,
                    await GetUserDisplayNameAsync(interaction.GuildId ?? throw new InvalidOperationException("無法取得基金所屬伺服器"), result.RecipientUserId)) : state.Title;
            var description = result.WasOwnerRedirected
                ? AppendOwnerRedirectDescription(state.Description, result)
                : AppendSingleRecipientDescription(state.Description, result);
            description = description[^Math.Min(description.Length, EmbedBuilder.MaxDescriptionLength)..];

            await CacheMessageStateAsync(interaction.Message.Id, new FundMessageState(title, description));

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description);

            if (description.Contains("🏆", StringComparison.Ordinal))
                embed.WithColor(Color.Gold);
            else
                embed.WithOkColor();

            // 走互動 token 更新，沒有頻道讀取權限也能更新原本的訊息
            await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed.Build());
        }

        internal async Task AddFundAndRespondAsync(
            IDiscordInteraction interaction,
            FundType fundType,
            ulong guildId,
            ulong channelId,
            ulong executeUserId,
            ulong targetUserId,
            string messagePrefix = "")
        {
            var cooldownScopeId = interaction.Id;
            var cooldownKey = GetAddOneCooldownKey(cooldownScopeId, executeUserId);
            await RedisConnection.RedisDb.StringSetAsync(
                cooldownKey,
                1,
                expiry: TimeSpan.FromHours(1),
                when: When.NotExists);

            var wasOwnerRedirected = CheckIsAddOwner(fundType, guildId, executeUserId, targetUserId, out var needAddUserId);
            if (needAddUserId == 0)
            {
                // 沒有對象可以亂彈，這次不入帳也不佔冷卻
                await RedisConnection.RedisDb.KeyDeleteAsync(cooldownKey);
                // 先刪掉公開的「思考中」，不然第一則 followup 會蓋掉它並變成公開訊息
                await interaction.DeleteOriginalResponseAsync();
                await interaction.SendErrorAsync("找不到可以亂彈的對象，這次不執行", true);
                return;
            }

            var result = await AddFundAsync(fundType, guildId, channelId, executeUserId, needAddUserId, wasOwnerRedirected);
            var recipientDisplayName = wasOwnerRedirected
                ? string.Empty
                : await GetUserDisplayNameAsync(guildId, targetUserId);
            var description = wasOwnerRedirected
                ? BuildOwnerRedirectDescription(
                    string.IsNullOrWhiteSpace(messagePrefix)
                        ? FormatOwnerRedirectTransaction(result)
                        : $"{messagePrefix.TrimEnd()}\n\n{FormatOwnerRedirectTransaction(result)}",
                    1,
                    result.IncrementAmount)
                : BuildSingleRecipientDescription(
                    string.IsNullOrWhiteSpace(messagePrefix)
                        ? FormatSingleRecipientTransaction(result)
                        : $"{messagePrefix.TrimEnd()}\n\n{FormatSingleRecipientTransaction(result)}",
                    1,
                    result.IncrementAmount,
                    result.PreviousAmount,
                    result.NewAmount,
                    result.RankImproved ? result.PreviousRank + 1 : null,
                    result.RankImproved ? result.NewRank + 1 : null);

            var components = new ComponentBuilder()
                .WithButton("讓他飛", $"{AddOneCustomIdPrefix}{(int)fundType}:{targetUserId}:{cooldownScopeId}", ButtonStyle.Success)
                .Build();

            var title = FormatFundTitle(fundType, recipientDisplayName, wasOwnerRedirected);

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description);

            if (result.RankImproved)
                embed.WithColor(Color.Gold);
            else
                embed.WithOkColor();

            var message = await interaction.FollowupAsync(embed: embed.Build(), components: components);
            await CacheMessageStateAsync(message.Id, new FundMessageState(title, description));
        }

        private static async Task<FundMessageState> GetCachedMessageStateAsync(ulong messageId)
        {
            try
            {
                var value = await RedisConnection.RedisDb.StringGetAsync(GetMessageStateCacheKey(messageId));
                if (!value.HasValue)
                    return null;

                var state = System.Text.Json.JsonSerializer.Deserialize<FundMessageState>(value.ToString());
                return state is { Title: not null, Description: not null } ? state : null;
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
            catch (RedisException ex)
            {
                Log.Error(ex, $"FundService-GetMessageStateCache: {messageId}");
                return null;
            }
        }

        private static async Task CacheMessageStateAsync(ulong messageId, FundMessageState state)
        {
            try
            {
                await RedisConnection.RedisDb.StringSetAsync(
                    GetMessageStateCacheKey(messageId),
                    System.Text.Json.JsonSerializer.Serialize(state),
                    MessageStateCacheTtl);
            }
            catch (RedisException ex)
            {
                Log.Error(ex, $"FundService-SetMessageStateCache: {messageId}");
            }
        }

        private static string GetMessageStateCacheKey(ulong messageId)
            => $"{MessageStateCacheKeyPrefix}:{messageId}";

        internal static string FormatFundTitle(FundType fundType, string recipientDisplayName, bool ownerRedirected = false)
            => ownerRedirected
                ? $"🎲 {GetFundTypeName(fundType)}基金亂彈"
                : $"💰 {GetFundTypeName(fundType)}基金入帳｜{recipientDisplayName}";

        internal static string AppendSingleRecipientDescription(string description, FundAddResult result)
        {
            var summaryIndex = description.IndexOf("\n\n本次入帳：", StringComparison.Ordinal);
            var rankIndex = description.IndexOf("\n\n🏆 排名變動：", StringComparison.Ordinal);
            var detailEnd = rankIndex >= 0 ? rankIndex : summaryIndex;

            // Old live messages have no structured summary. Keep them and start the new format from this click.
            if (summaryIndex < 0)
            {
                var details = string.IsNullOrWhiteSpace(description)
                    ? FormatSingleRecipientTransaction(result)
                    : $"{description.TrimEnd()}\n\n{FormatSingleRecipientTransaction(result)}";
                return BuildSingleRecipientDescription(
                    details,
                    1,
                    result.IncrementAmount,
                    result.PreviousAmount,
                    result.NewAmount,
                    result.RankImproved ? result.PreviousRank + 1 : null,
                    result.RankImproved ? result.NewRank + 1 : null);
            }

            var count = ParseSummaryNumber(description, "本次入帳：", " 筆") + 1;
            var total = ParseSummaryNumber(description, "共 +", "\n") + result.IncrementAmount;
            var initialAmount = ParseSummaryNumber(description, "基金餘額：", " → ");
            var initialRank = rankIndex >= 0
                ? ParseSummaryNumber(description, "🏆 排名變動：第 ", " 名")
                : result.RankImproved ? result.PreviousRank + 1 : null;
            var finalRank = result.RankImproved
                ? result.NewRank + 1
                : rankIndex >= 0 ? ParseSummaryNumber(description, " → 第 ", " 名") : null;
            var transaction = FormatSingleRecipientTransaction(result);

            return BuildSingleRecipientDescription(
                $"{description[..detailEnd].TrimEnd()}\n{transaction}",
                count,
                total,
                initialAmount,
                result.NewAmount,
                initialRank,
                finalRank);
        }

        internal static string AppendOwnerRedirectDescription(string description, FundAddResult result)
        {
            var ownerSummaryIndex = description.IndexOf("\n\n亂彈紀錄：", StringComparison.Ordinal);
            var singleSummaryIndex = description.IndexOf("\n\n本次入帳：", StringComparison.Ordinal);
            var singleRankIndex = description.IndexOf("\n\n🏆 排名變動：", StringComparison.Ordinal);
            var detailEnd = ownerSummaryIndex >= 0
                ? ownerSummaryIndex
                : singleRankIndex >= 0 ? singleRankIndex : singleSummaryIndex;
            var transaction = FormatOwnerRedirectTransaction(result);

            if (detailEnd < 0)
            {
                var details = string.IsNullOrWhiteSpace(description)
                    ? transaction
                    : $"{description.TrimEnd()}\n\n{transaction}";
                return BuildOwnerRedirectDescription(details, 1, result.IncrementAmount);
            }

            var count = ParseSummaryNumber(
                description,
                ownerSummaryIndex >= 0 ? "亂彈紀錄：" : "本次入帳：",
                ownerSummaryIndex >= 0 ? " 次" : " 筆") + 1;
            var total = ParseSummaryNumber(
                description,
                ownerSummaryIndex >= 0 ? "累計 +" : "共 +",
                ownerSummaryIndex >= 0 ? string.Empty : "\n") + result.IncrementAmount;

            return BuildOwnerRedirectDescription(
                $"{description[..detailEnd].TrimEnd()}\n{transaction}",
                count,
                total);
        }

        private async Task<string> GetUserDisplayNameAsync(ulong guildId, ulong userId)
        {
            var guildUser = _client.GetGuild(guildId)?.GetUser(userId);
            if (!string.IsNullOrWhiteSpace(guildUser?.DisplayName))
                return guildUser.DisplayName;

            try
            {
                var fetchedGuildUser = await _client.Rest.GetGuildUserAsync(guildId, userId);
                if (!string.IsNullOrWhiteSpace(fetchedGuildUser?.DisplayName))
                    return fetchedGuildUser.DisplayName;
            }
            catch
            {
            }

            return _client.GetUser(userId)?.Username ?? $"使用者 {userId}";
        }

        internal static string BuildSingleRecipientDescription(
            string details,
            long count,
            long total,
            long initialAmount,
            long finalAmount,
            long? initialRank,
            long? finalRank)
        {
            var rankSummary = initialRank.HasValue && finalRank.HasValue && finalRank < initialRank
                ? $"\n\n🏆 排名變動：第 {initialRank:N0} 名 → 第 {finalRank:N0} 名（↑{initialRank - finalRank:N0}）"
                : string.Empty;

            return $"{details}{rankSummary}\n\n本次入帳：{count:N0} 筆，共 +{total:N0}\n基金餘額：{initialAmount:N0} → {finalAmount:N0}";
        }

        internal static string BuildOwnerRedirectDescription(string details, long count, long total)
        {
            // ponytail: this counts visible history; persist recipient IDs only if counts must survive embed truncation.
            var recipientCount = Regex.Matches(details, @"→ <@(\d+)>")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .Distinct()
                .Count();

            return $"{details}\n\n亂彈紀錄：{count:N0} 次｜命中：{recipientCount:N0} 人｜累計 +{total:N0}";
        }

        private static string FormatSingleRecipientTransaction(FundAddResult result)
        {
            var rank = result.RankImproved
                ? $"  🏆 升至第 {result.NewRank + 1:N0} 名{FormatOvertakenUsers(result)}"
                : string.Empty;
            var sunday = result.IsSunday ? "  ☀️ 星期日加倍" : string.Empty;
            return $"<@{result.ExecuteUserId}>      +{result.IncrementAmount:N0}{rank}{sunday}";
        }

        private static string FormatOwnerRedirectTransaction(FundAddResult result)
        {
            var rank = result.PreviousRank.HasValue && result.NewRank.HasValue
                ? result.RankImproved
                    ? $"｜🏆 排名：{result.PreviousRank + 1:N0} → {result.NewRank + 1:N0}{FormatOvertakenUsers(result)}"
                    : $"｜排名：第 {result.NewRank + 1:N0} 名"
                : string.Empty;
            var sunday = result.IsSunday ? "｜☀️ 星期日加倍" : string.Empty;
            return $"<@{result.ExecuteUserId}> +{result.IncrementAmount:N0} → <@{result.RecipientUserId}>\n　餘額：{result.PreviousAmount:N0} → {result.NewAmount:N0}{rank}{sunday}";
        }

        private static string FormatOvertakenUsers(FundAddResult result)
            => result.OvertakenUserIds.Count == 0
                ? string.Empty
                : $"，超過 {string.Join("、", result.OvertakenUserIds.Select(userId => $"<@{userId}>"))}";

        private static long ParseSummaryNumber(string text, string prefix, string suffix)
        {
            var start = text.IndexOf(prefix, StringComparison.Ordinal);
            if (start < 0)
                throw new InvalidOperationException("基金訊息摘要格式無效");

            start += prefix.Length;
            var end = string.IsNullOrEmpty(suffix)
                ? text.Length
                : text.IndexOf(suffix, start, StringComparison.Ordinal);
            if (end < 0 || !long.TryParse(text[start..end].Replace(",", string.Empty), out var value))
                throw new InvalidOperationException("基金訊息摘要數值無效");

            return value;
        }

        internal static bool TryParseAddOneCustomId(string customId, out FundType fundType, out ulong targetUserId, out ulong cooldownScopeId)
        {
            fundType = default;
            targetUserId = default;
            cooldownScopeId = default;

            var values = customId[AddOneCustomIdPrefix.Length..].Split(':');
            if ((values.Length != 2 && values.Length != 3) ||
                !int.TryParse(values[0], out var fundTypeValue) ||
                !Enum.IsDefined(typeof(FundType), fundTypeValue) ||
                !ulong.TryParse(values[1], out targetUserId) ||
                (values.Length == 3 && !ulong.TryParse(values[2], out cooldownScopeId)))
            {
                return false;
            }

            fundType = (FundType)fundTypeValue;
            return true;
        }

        private static string GetAddOneCooldownKey(ulong scopeId, ulong userId)
            => $"{AddOneCooldownKeyPrefix}:{scopeId}:{userId}";

        // resultUserId 為 0 代表找不到可亂彈的對象，呼叫端要拒絕這次執行
        internal static bool CheckIsAddOwner(FundType fundType, ulong guildId, ulong executeUserId, ulong targetUserId, out ulong resultUserId)
        {
            resultUserId = targetUserId;

            if (targetUserId == Program.ApplicatonOwner.Id)
            {
                // 只用 ZSET -> 取得成員列表（降冪）
                resultUserId = PickOwnerRedirectRecipient(
                    RedisConnection.RedisDb
                        .SortedSetRangeByRank(GetFundLeaderboardRedisKey(fundType, guildId), 0, -1, Order.Descending)
                        .Select(entry => entry.ToString()),
                    executeUserId,
                    Program.ApplicatonOwner.Id,
                    Program.Client.GetGuild(guildId)?.Users
                        .Where(user => !user.IsBot)
                        .Select(user => user.Id));

                return true;
            }

            return false;
        }

        // Owner 不能當亂彈對象，否則基金會又彈回 Owner 身上
        internal static ulong PickOwnerRedirectRecipient(
            IEnumerable<string> leaderboardUserIds,
            ulong executeUserId,
            ulong ownerUserId,
            IEnumerable<ulong> guildUserIds)
        {
            var candidates = leaderboardUserIds
                .Select(entry => ulong.TryParse(entry, out var userId) ? userId : 0)
                .Append(executeUserId) // 將指令執行者也加入隨機選擇的範圍
                .Where(userId => userId != 0 && userId != ownerUserId)
                .ToList();

            // 排行榜只剩 Owner 時改抽伺服器成員，避免基金彈回 Owner
            if (candidates.Count == 0 && guildUserIds != null)
                candidates = guildUserIds.Where(userId => userId != 0 && userId != ownerUserId).ToList();

            // 連伺服器成員都沒有時回傳 0，呼叫端要拒絕這次執行，不能彈回 Owner
            return candidates.Count == 0
                ? 0
                : candidates[Random.Shared.Next(candidates.Count)];
        }

        const long MinIncrementAmount = 200;
        const long MaxIncrementAmount = 1000;
        const string NotifyChannelsKey = "SupportBot:Fund:NotifyChannels";

        private static async Task<FundAddResult> AddFundAsync(
            FundType fundType,
            ulong guildId,
            ulong channelId,
            ulong executeUserId,
            ulong userId,
            bool wasOwnerRedirected)
        {
            await RedisConnection.RedisDb.SetAddAsync(NotifyChannelsKey, channelId.ToString());
            var key = GetFundLeaderboardRedisKey(fundType, guildId);
            var isSunday = DateTime.Now.DayOfWeek == DayOfWeek.Sunday;
            var incrementAmount = Random.Shared.NextInt64(MinIncrementAmount, MaxIncrementAmount + 1) * (isSunday ? 2 : 1);

            // 獲取增加前的排名 (SortedSetRankAsync 回傳 0-based index)
            var oldRank = await RedisConnection.RedisDb.SortedSetRankAsync(key, userId.ToString(), Order.Descending);
            var oldAmount = (long)(await RedisConnection.RedisDb.SortedSetScoreAsync(key, userId.ToString()) ?? 0);

            // 單純使用 ZSET 作為唯一來源（score = 總額）
            var newAmount = (long)await RedisConnection.RedisDb.SortedSetIncrementAsync(key, userId.ToString(), incrementAmount);

            // 獲取增加後的排名
            var newRank = await RedisConnection.RedisDb.SortedSetRankAsync(key, userId.ToString(), Order.Descending);
            var overtakenUserIds = new List<ulong>();
            if (oldRank.HasValue && newRank.HasValue && newRank < oldRank)
            {
                var overtakenUsers = await RedisConnection.RedisDb.SortedSetRangeByRankAsync(
                    key,
                    newRank.Value + 1,
                    oldRank.Value,
                    Order.Descending);
                foreach (var overtakenUser in overtakenUsers)
                {
                    if (ulong.TryParse(overtakenUser.ToString(), out var overtakenUserId))
                        overtakenUserIds.Add(overtakenUserId);
                }
            }

            return new FundAddResult(
                fundType,
                executeUserId,
                userId,
                incrementAmount,
                oldAmount,
                newAmount,
                oldRank,
                newRank,
                isSunday,
                wasOwnerRedirected,
                overtakenUserIds);
        }

        // 取得某基金前 N 名 (依 score 降冪)
        internal static async Task<List<(ulong UserId, long Score)>> GetTopFundAsync(FundType fundType, ulong guildId, int top = 0)
        {
            var key = GetFundLeaderboardRedisKey(fundType, guildId);
            var entries = await RedisConnection.RedisDb.SortedSetRangeByRankWithScoresAsync(key, 0, top - 1, Order.Descending);

            var list = new List<(ulong, long)>();
            foreach (var entry in entries)
            {
                if (ulong.TryParse((string?)entry.Element, out var uid))
                {
                    list.Add((uid, (long)entry.Score));
                }
            }
            return list;
        }

        internal static string GetFundTypeName(FundType fundType)
        {
            return fundType.GetType()
                .GetField(fundType.ToString())
                .GetCustomAttribute<ChoiceDisplayAttribute>()
                .Name;
        }

        // 取得排行榜 ZSET 的 key
        internal static string GetFundLeaderboardRedisKey(FundType fundType, ulong guildId)
        {
            return $"SupportBot:Fund:Leaderboard:{fundType}:{guildId}";
        }

        // 重置所有基金排行榜（刪除所有相關 ZSET key），並回傳曾觸發的 channel 清單
        internal static async Task<(long DeletedCount, List<ulong> ChannelIds)> ResetAllLeaderboardsAsync()
        {
            var channelEntries = await RedisConnection.RedisDb.SetMembersAsync(NotifyChannelsKey);
            var channelIds = channelEntries
                .Select(e => ulong.TryParse((string?)e, out var id) ? id : 0UL)
                .Where(id => id != 0)
                .ToList();

            var keys = RedisConnection.RedisServer.Keys(database: 2, pattern: "SupportBot:Fund:Leaderboard:*").ToArray();
            long deleted = 0;
            if (keys.Length > 0)
                deleted = await RedisConnection.RedisDb.KeyDeleteAsync(keys);

            var messageStateKeys = RedisConnection.RedisServer.Keys(database: 2, pattern: $"{MessageStateCacheKeyPrefix}:*").ToArray();
            if (messageStateKeys.Length > 0)
                await RedisConnection.RedisDb.KeyDeleteAsync(messageStateKeys);

            await RedisConnection.RedisDb.KeyDeleteAsync(NotifyChannelsKey);

            return (deleted, channelIds);
        }
    }
}
