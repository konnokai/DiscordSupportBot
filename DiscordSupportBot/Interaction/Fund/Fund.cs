using Discord.Interactions;
using DiscordSupportBot.Interaction.Fund.Service;
using SkiaSharp;
using System.Diagnostics;
using FundType = DiscordSupportBot.Interaction.Fund.Service.FundService.FundType;

namespace DiscordSupportBot.Interaction.Fund
{
    public class Fund : TopLevelModule<FundService>
    {
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
                var leaderboard = new List<(string FundName, List<(string UserName, long Score)> Rankings)>();
                var userNames = new Dictionary<ulong, string>();

                foreach (var fundType in fundTypes)
                {
                    var top3 = await FundService.GetTopFundAsync(fundType, Context.Guild.Id, 3);
                    if (top3.Count == 0)
                        continue;

                    var rankings = new List<(string UserName, long Score)>();
                    foreach (var entry in top3)
                    {
                        if (!userNames.TryGetValue(entry.UserId, out var userName))
                        {
                            IUser user = Context.Guild.GetUser(entry.UserId) ?? Program.Client.GetUser(entry.UserId);
                            if (user == null)
                            {
                                try { user = await Program.Client.Rest.GetUserAsync(entry.UserId); }
                                catch { }
                            }

                            userName = user is IGuildUser guildUser ? guildUser.DisplayName : user?.Username;
                            userName = string.IsNullOrWhiteSpace(userName) ? $"使用者 {entry.UserId}" : userName;
                            userNames[entry.UserId] = userName;
                        }

                        rankings.Add((userName, entry.Score));
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
            IReadOnlyList<(string FundName, List<(string UserName, long Score)> Rankings)> leaderboard)
        {
            const int imageWidth = 1200;
            const int horizontalPadding = 48;
            const int columnGap = 24;
            const int cardHeight = 156;
            const int rowGap = 24;
            const int columnCount = 2;

            var cardWidth = (imageWidth - horizontalPadding * 2 - columnGap) / columnCount;
            var rowCount = (leaderboard.Count + columnCount - 1) / columnCount;
            var imageHeight = horizontalPadding * 2 + rowCount * cardHeight + (rowCount - 1) * rowGap;

            using var bitmap = new SKBitmap(imageWidth, imageHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(bitmap);
            using var typeface = CreateLeaderboardTypeface();
            using var backgroundPaint = new SKPaint { Color = new SKColor(24, 27, 38), IsAntialias = true };
            using var cardPaint = new SKPaint { Color = new SKColor(31, 36, 52), IsAntialias = true };
            using var borderPaint = new SKPaint
            {
                Color = new SKColor(58, 67, 91),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            using var accentPaint = new SKPaint { Color = new SKColor(0, 229, 132), IsAntialias = true };
            using var fundFont = new SKFont(typeface, 28) { Embolden = true };
            using var userFont = new SKFont(typeface, 25);
            using var scoreFont = new SKFont(typeface, 23);
            using var rankFont = new SKFont(typeface, 17);
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
            using var scorePillPaint = new SKPaint { Color = new SKColor(15, 20, 32), IsAntialias = true };
            using var rankPaint = new SKPaint { IsAntialias = true };
            using var rankTextPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true
            };

            var rankColors = new[]
            {
                new SKColor(245, 180, 74),
                new SKColor(170, 190, 210),
                new SKColor(194, 126, 76)
            };

            canvas.DrawRect(0, 0, imageWidth, imageHeight, backgroundPaint);

            for (var index = 0; index < leaderboard.Count; index++)
            {
                var column = index % columnCount;
                var row = index / columnCount;
                var x = horizontalPadding + column * (cardWidth + columnGap);
                var y = horizontalPadding + row * (cardHeight + rowGap);
                var cardRect = new SKRect(x, y, x + cardWidth, y + cardHeight);

                canvas.DrawRoundRect(cardRect, 18, 18, cardPaint);
                canvas.DrawRoundRect(cardRect, 18, 18, borderPaint);
                canvas.DrawRoundRect(new SKRect(x, y, x + 7, y + cardHeight), 4, 4, accentPaint);
                canvas.DrawText(leaderboard[index].FundName, x + 28, y + 36, fundFont, fundPaint);

                for (var rank = 0; rank < leaderboard[index].Rankings.Count; rank++)
                {
                    var ranking = leaderboard[index].Rankings[rank];
                    var lineY = y + 82 + rank * 34;
                    var scoreText = ranking.Score.ToString("N0");
                    var scoreWidth = scoreFont.MeasureText(scoreText, scorePaint);
                    var scoreRight = x + cardWidth - 24;
                    var scoreLeft = scoreRight - scoreWidth - 24;

                    rankPaint.Color = rankColors[rank];
                    canvas.DrawCircle(x + 40, lineY - 9, 14, rankPaint);
                    canvas.DrawText((rank + 1).ToString(), x + 40, lineY - 3, SKTextAlign.Center, rankFont, rankTextPaint);

                    canvas.DrawRoundRect(scoreLeft, lineY - 25, scoreRight, lineY + 7, 9, 9, scorePillPaint);
                    canvas.DrawText(scoreText, scoreLeft + 12, lineY - 3, scoreFont, scorePaint);

                    var availableUserWidth = scoreLeft - x - 84;
                    var userText = FitLeaderboardText(userFont, userPaint, $"@{ranking.UserName}", availableUserWidth);
                    canvas.DrawText(userText, x + 68, lineY - 3, userFont, userPaint);
                }
            }

            using var encodedImage = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            return encodedImage.ToArray();
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

        private static string FitLeaderboardText(SKFont font, SKPaint paint, string text, float maxWidth)
        {
            const string suffix = "...";
            if (font.MeasureText(text, paint) <= maxWidth)
                return text;

            var length = text.Length;
            while (length > 0 && font.MeasureText(text[..length] + suffix, paint) > maxWidth)
                length--;

            return length == 0 ? suffix : text[..length] + suffix;
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
