using Discord.Interactions;
using DiscordSupportBot.Interaction.Fund.Service;
using SkiaSharp;
using System.Diagnostics;
using FundType = DiscordSupportBot.Interaction.Fund.Service.FundService.FundType;

namespace DiscordSupportBot.Interaction.Fund
{
    public class Fund : TopLevelModule<FundService>
    {
        private static readonly HttpClient LeaderboardHttpClient = new();

        [RequireContext(ContextType.Guild)]
        [SlashCommand("add-fund", "對某人添加基金")]
        public async Task AddFundAsync([Summary("基金類型")] FundType fundType, [Summary("目標使用者")] IUser user)
        {
            var guildUser = Context.Guild.GetUser(user.Id);
            if (guildUser == null)
            {
                await Context.Interaction.SendErrorAsync("指定的使用者不在此伺服器中");
                return;
            }

            if (guildUser.IsBot)
            {
                await Context.Interaction.SendErrorAsync("無法對機器人添加基金");
                return;
            }

            await Context.Interaction.DeferAsync(false);

            await FundService.AddFundAndRespondAsync(
                Context.Interaction,
                fundType,
                Context.Guild.Id,
                Context.Channel.Id,
                Context.User.Id,
                user.Id);
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("fund-leaderboard", "基金排行榜")]
        public async Task FundLeaderBoardAsync([Summary("基金類型")] FundType fundType)
        {
            // 使用 ZSET 取得 top
            var top = await FundService.GetTopFundAsync(fundType, Context.Guild.Id);

            if (top.Count == 0)
            {
                await Context.Interaction.SendErrorAsync($"目前沒有任何人有{FundService.GetFundTypeName(fundType)}基金");
                return;
            }

            await Context.Interaction.SendConfirmAsync($"`{Context.Guild.Name}` {FundService.GetFundTypeName(fundType)}基金排行榜\n\n" +
                $"{string.Join('\n', top.Select((x, idx) => $"{idx + 1}. <@{x.UserId}>: {x.Score}"))}");
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("all-fund-leaderboard", "所有基金的前三名排行榜")]
        public async Task AllFundLeaderBoardAsync()
        {
            await Context.Interaction.DeferAsync(false);

            try
            {
                var fundTypes = Enum.GetValues(typeof(FundType)).Cast<FundType>();
                var leaderboard = new List<(string FundName, List<(string UserName, long Score, byte[] AvatarBytes)> Rankings)>();
                var users = new Dictionary<ulong, IUser>();
                var userNames = new Dictionary<ulong, string>();
                var avatarBytesByUser = new Dictionary<ulong, byte[]>();
                var avatarAttempts = new HashSet<ulong>();

                foreach (var fundType in fundTypes)
                {
                    var top3 = await FundService.GetTopFundAsync(fundType, Context.Guild.Id, 3);
                    if (top3.Count == 0)
                        continue;

                    var rankings = new List<(string UserName, long Score, byte[] AvatarBytes)>();
                    foreach (var entry in top3)
                    {
                        if (!users.TryGetValue(entry.UserId, out var user))
                        {
                            user = Context.Guild.GetUser(entry.UserId) ?? Program.Client.GetUser(entry.UserId);
                            if (user == null)
                            {
                                try { user = await Program.Client.Rest.GetUserAsync(entry.UserId); }
                                catch { }
                            }

                            users[entry.UserId] = user;
                        }

                        if (!userNames.TryGetValue(entry.UserId, out var userName))
                        {
                            userName = user is IGuildUser guildUser ? guildUser.DisplayName : user?.Username;
                            userName = string.IsNullOrWhiteSpace(userName) ? $"使用者 {entry.UserId}" : userName;
                            userNames[entry.UserId] = userName;
                        }

                        if (avatarAttempts.Add(entry.UserId) && user != null)
                        {
                            var avatarBytes = await DownloadLeaderboardAvatarAsync(user);
                            if (avatarBytes != null)
                                avatarBytesByUser[entry.UserId] = avatarBytes;
                        }

                        avatarBytesByUser.TryGetValue(entry.UserId, out var avatarBytesForEntry);
                        rankings.Add((userName, entry.Score, avatarBytesForEntry));
                    }

                    leaderboard.Add((FundService.GetFundTypeName(fundType), rankings));
                }

                if (leaderboard.Count == 0)
                {
                    await Context.Interaction.SendErrorAsync("目前沒有任何人有基金", true);
                }
                else
                {
                    const string fileName = "fund-leaderboard.png";
                    using var imageStream = new MemoryStream(BuildLeaderboardImage(leaderboard));
                    var embed = new EmbedBuilder()
                        .WithTitle($"`{Context.Guild.Name}` 所有基金前三名排行榜")
                        .WithOkColor()
                        .WithImageUrl($"attachment://{fileName}")
                        .Build();

                    await Context.Interaction.FollowupWithFileAsync(imageStream, fileName, embed: embed);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"all-fund-leaderboard: {Context.Guild.Id}");
                await Context.Interaction.SendErrorAsync("取得排行榜時發生錯誤，請稍後再試", true);
            }
        }

        private static byte[] BuildLeaderboardImage(
            IReadOnlyList<(string FundName, List<(string UserName, long Score, byte[] AvatarBytes)> Rankings)> leaderboard)
        {
            const int imageWidth = 1200;
            const int horizontalPadding = 36;
            const int columnGap = 18;
            const int cardHeight = 178;
            const int rowGap = 24;
            const int columnCount = 3;
            const int scoreColumnWidth = 120;
            const int avatarSize = 30;
            const int contentOffsetY = 8;

            var cardWidth = (imageWidth - horizontalPadding * 2 - columnGap) / columnCount;
            var rowCount = (leaderboard.Count + columnCount - 1) / columnCount;
            var imageHeight = horizontalPadding * 2 + rowCount * cardHeight + (rowCount - 1) * rowGap;

            using var bitmap = new SKBitmap(imageWidth, imageHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(bitmap);
            using var typeface = CreateLeaderboardTypeface();
            using var emojiTypeface = CreateLeaderboardEmojiTypeface();
            canvas.Clear(new SKColor(20, 24, 34));
            using var cellPaint = new SKPaint
            {
                Color = new SKColor(31, 38, 52),
                IsAntialias = true
            };
            using var borderPaint = new SKPaint
            {
                Color = new SKColor(91, 103, 126),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            using var fundFont = new SKFont(typeface, 28) { Embolden = true };
            using var scoreFont = new SKFont(typeface, 23);
            using var rankFont = new SKFont(emojiTypeface, 30);
            using var fundPaint = new SKPaint
            {
                Color = new SKColor(241, 245, 249),
                IsAntialias = true
            };
            using var userPaint = new SKPaint
            {
                Color = new SKColor(226, 232, 240),
                IsAntialias = true
            };
            using var scorePaint = new SKPaint
            {
                Color = new SKColor(191, 219, 254),
                IsAntialias = true
            };
            using var rankTextPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true
            };
            var rankEmojis = new[] { "🥇", "🥈", "🥉" };

            for (var index = 0; index < leaderboard.Count; index++)
            {
                var column = index % columnCount;
                var row = index / columnCount;
                var x = horizontalPadding + column * (cardWidth + columnGap);
                var y = horizontalPadding + row * (cardHeight + rowGap);
                var cellRect = new SKRect(x, y, x + cardWidth, y + cardHeight);

                canvas.DrawRect(cellRect, cellPaint);
                canvas.DrawRect(cellRect, borderPaint);
                canvas.DrawText(leaderboard[index].FundName, x + 18, y + 31 + contentOffsetY, SKTextAlign.Left, fundFont, fundPaint);

                for (var rank = 0; rank < leaderboard[index].Rankings.Count; rank++)
                {
                    var ranking = leaderboard[index].Rankings[rank];
                    var lineY = y + 78 + contentOffsetY + rank * 34;
                    var scoreText = ranking.Score.ToString("N0");
                    var scoreWidth = scoreFont.MeasureText(scoreText, scorePaint);
                    var scoreRight = x + cardWidth - 18;
                    var scoreLeft = x + cardWidth - scoreColumnWidth;
                    var nameLeft = x + 56;
                    var nameDrawLeft = nameLeft + avatarSize + 12;
                    var nameWidth = scoreLeft - nameDrawLeft - 10;

                    canvas.DrawText(rankEmojis[rank], x + 18, lineY, SKTextAlign.Left, rankFont, rankTextPaint);

                    if (ranking.AvatarBytes != null)
                        DrawLeaderboardAvatar(canvas, ranking.AvatarBytes, nameLeft, lineY - avatarSize + 5, avatarSize);

                    var userText = LimitLeaderboardName(ranking.UserName);
                    var userFontSize = GetLeaderboardNameFontSize(
                        typeface,
                        emojiTypeface,
                        userPaint,
                        nameWidth);
                    var nameBaseline = GetLeaderboardTextBaseline(
                        typeface,
                        userFontSize,
                        lineY - avatarSize / 2f + 5);
                    DrawLeaderboardText(canvas, userText, nameDrawLeft, nameBaseline, userFontSize, typeface, emojiTypeface, userPaint);
                    canvas.DrawText(scoreText, scoreRight - scoreWidth, lineY, SKTextAlign.Left, scoreFont, scorePaint);
                }
            }

            using var encodedImage = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            return encodedImage.ToArray();
        }

        private static async Task<byte[]> DownloadLeaderboardAvatarAsync(IUser user)
        {
            var avatarUrl = user.GetAvatarUrl(ImageFormat.Png, 64);
            if (string.IsNullOrWhiteSpace(avatarUrl))
                return null;

            try
            {
                return await LeaderboardHttpClient.GetByteArrayAsync(avatarUrl);
            }
            catch
            {
                return null;
            }
        }

        private static bool DrawLeaderboardAvatar(SKCanvas canvas, byte[] avatarBytes, float x, float y, float size)
        {
            try
            {
                using var avatar = SKBitmap.Decode(avatarBytes);
                if (avatar == null)
                    return false;

                using var clipPathBuilder = new SKPathBuilder();
                clipPathBuilder.AddCircle(x + size / 2, y + size / 2, size / 2, SKPathDirection.Clockwise);
                using var clipPath = clipPathBuilder.Detach();
                canvas.Save();
                canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);
                canvas.DrawBitmap(avatar, new SKRect(x, y, x + size, y + size), SKSamplingOptions.Default);
                canvas.Restore();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static float GetLeaderboardNameFontSize(
            SKTypeface typeface,
            SKTypeface emojiTypeface,
            SKPaint paint,
            float maxWidth)
        {
            const float defaultSize = 25;
            const string widthSample = "一一一一一一一一一";
            var measuredWidth = MeasureLeaderboardText(widthSample, defaultSize, typeface, emojiTypeface, paint);
            if (measuredWidth > maxWidth && measuredWidth > 0)
                return Math.Max(10, defaultSize * maxWidth / measuredWidth);

            return defaultSize;
        }

        private static float GetLeaderboardTextBaseline(SKTypeface typeface, float fontSize, float centerY)
        {
            using var font = new SKFont(typeface, fontSize);
            var metrics = font.Metrics;
            return centerY - (metrics.Ascent + metrics.Descent) / 2;
        }

        private static string LimitLeaderboardName(string userName)
            => string.Concat(userName.EnumerateRunes().Take(9));

        private static float MeasureLeaderboardText(
            string text,
            float fontSize,
            SKTypeface typeface,
            SKTypeface emojiTypeface,
            SKPaint paint)
        {
            using var font = new SKFont(typeface, fontSize);
            using var emojiFont = new SKFont(emojiTypeface, fontSize);
            var width = 0f;

            foreach (var rune in text.EnumerateRunes())
            {
                var runeText = rune.ToString();
                var selectedFont = IsLeaderboardEmoji(rune.Value) ? emojiFont : font;
                width += selectedFont.MeasureText(runeText, paint);
            }

            return width;
        }

        private static void DrawLeaderboardText(
            SKCanvas canvas,
            string text,
            float x,
            float baseline,
            float fontSize,
            SKTypeface typeface,
            SKTypeface emojiTypeface,
            SKPaint paint)
        {
            using var font = new SKFont(typeface, fontSize);
            using var emojiFont = new SKFont(emojiTypeface, fontSize);

            foreach (var rune in text.EnumerateRunes())
            {
                var runeText = rune.ToString();
                var selectedFont = IsLeaderboardEmoji(rune.Value) ? emojiFont : font;
                canvas.DrawText(runeText, x, baseline, SKTextAlign.Left, selectedFont, paint);
                x += selectedFont.MeasureText(runeText, paint);
            }
        }

        private static bool IsLeaderboardEmoji(int codePoint)
        {
            return codePoint is 0x200D or 0xFE0E or 0xFE0F
                || codePoint is >= 0x1F000 and <= 0x1FAFF
                || codePoint is >= 0x2600 and <= 0x27BF;
        }

        private static SKTypeface CreateLeaderboardTypeface()
        {
            // The Linux no-dependencies asset omits Fontconfig, so load the container font directly.
            var fontFiles = new[]
            {
                (Path: "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc", Index: 3),
                (Path: "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc", Index: 0),
                (Path: "/usr/share/fonts/opentype/noto/NotoSansCJKtc-Regular.otf", Index: 0)
            };

            foreach (var fontFile in fontFiles)
            {
                if (File.Exists(fontFile.Path))
                {
                    var typeface = SKTypeface.FromFile(fontFile.Path, fontFile.Index);
                    if (typeface != null)
                        return typeface;
                }
            }

            var preferredFamilies = new[]
            {
                "Microsoft JhengHei UI",
                "Microsoft JhengHei",
                "Noto Sans CJK TC",
                "Noto Sans TC",
                "Arial"
            };
            var installedFamilies = SKFontManager.Default.GetFontFamilies();
            var family = preferredFamilies.FirstOrDefault(preferred =>
                installedFamilies.Any(installed => string.Equals(installed, preferred, StringComparison.OrdinalIgnoreCase)));

            return family == null
                ? SKTypeface.Default
                : SKFontManager.Default.MatchFamily(family);
        }

        private static SKTypeface CreateLeaderboardEmojiTypeface()
        {
            var fontFile = "/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf";
            if (File.Exists(fontFile))
            {
                var typeface = SKTypeface.FromFile(fontFile);
                if (typeface != null)
                    return typeface;
            }

            var preferredFamilies = new[] { "Segoe UI Emoji", "Noto Color Emoji", "Apple Color Emoji" };
            var installedFamilies = SKFontManager.Default.GetFontFamilies();
            var family = preferredFamilies.FirstOrDefault(preferred =>
                installedFamilies.Any(installed => string.Equals(installed, preferred, StringComparison.OrdinalIgnoreCase)));

            return family == null
                ? SKTypeface.Default
                : SKFontManager.Default.MatchFamily(family);
        }

        [RequireContext(ContextType.Guild)]
        [MessageCommand("對該訊息的作者添加基金")]
        public async Task AddFundMessageCommandAsync(IMessage message)
        {
            if (message.Author == null)
            {
                await Context.Interaction.SendErrorAsync("無法取得該訊息的作者");
                return;
            }

            var guildUser = Context.Guild.GetUser(message.Author.Id);
            if (guildUser == null)
            {
                await Context.Interaction.SendErrorAsync("指定的使用者不在此伺服器中");
                return;
            }

            if (guildUser.IsBot)
            {
                await Context.Interaction.SendErrorAsync("無法對機器人添加基金");
                return;
            }

            var selectMenuBuilder = new SelectMenuBuilder()
                .WithCustomId("select_fund_type")
                .WithRequired(true);

            foreach (var item in Enum.GetNames(typeof(FundType)))
            {
                selectMenuBuilder.AddOption(FundService.GetFundTypeName(Enum.Parse<FundType>(item, true)), item);
            }

            var modalBuilder = new ModalBuilder();
            modalBuilder.WithTitle("添加說謊基金");
            modalBuilder.WithCustomId($"add_lying_fund:{Context.Guild.Id}:{message.Author.Id}");

            if (!string.IsNullOrEmpty(message.Content))
            {
                var realContext = message.CleanContent;
                var isLongLengthContext = message.CleanContent.Length >= 150;
                if (isLongLengthContext)
                {
                    realContext = $"{message.CleanContent[..Math.Min(150, message.Content.Length)]}" +
                        $"... (已忽略後續大於 150 字元的訊息)";
                }

                modalBuilder.AddTextDisplay($"訊息內容: \r\n" +
                    $"```" +
                    $"{realContext}" +
                    $"```");
            }
            else if (message.Attachments.FirstOrDefault() != null)
            {
                modalBuilder.AddTextDisplay($"附件網址: \r\n" +
                    $"{message.Attachments.First().Url}");
            }
            else
            {
                modalBuilder.AddTextDisplay("無法顯示訊息內容，但不影響添加基金");
            }

            modalBuilder.AddTextInput("勿編輯，此攔供訊息跳轉用", "jump_url", required: true, value: message.GetJumpUrl());

            modalBuilder.AddSelectMenu("添加類型", selectMenuBuilder);

            await Context.Interaction.RespondWithModalAsync(modalBuilder.Build());
        }
    }
}
