using Discord.Interactions;
using DiscordSupportBot.DataBase.Table;
using System.Diagnostics;
using FundType = DiscordSupportBot.Interaction.Fund.Service.FundService.FundType;

namespace DiscordSupportBot.Interaction.Fund
{
    public class Fund : TopLevelModule<Service.FundService>
    {
        [RequireContext(ContextType.Guild)]
        [SlashCommand("add-fund", "對某人添加基金")]
        public async Task AddFundAsync([Summary("基金類型")] FundType fundType, [Summary("目標使用者")] IUser user)
        {
            if (Context.Guild.GetUser(user.Id) == null)
            {
                await Context.Interaction.SendErrorAsync("指定的使用者不在此伺服器中");
                return;
            }

            var userId = user.Id;
            var message = string.Empty;
            if (userId == Program.ApplicatonOwner.Id)
            {
                message = "無法對 Owner 添加基金，反擊!\n";
                userId = Context.User.Id;
            }

            message += await _service.AddFundAsync(fundType, Context.Guild.Id, userId);
            await Context.Interaction.SendConfirmAsync(message);
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("fund-leaderboard", "基金排行榜")]
        public async Task FundLeaderBoardAsync([Summary("基金類型")] FundType fundType)
        {
            var hashEntries = await RedisConnection.RedisDb.HashGetAllAsync(_service.GetFundRedisKey(fundType, Context.Guild.Id));

            if (hashEntries.Length == 0)
            {
                await Context.Interaction.SendErrorAsync($"目前沒有任何人有{_service.GetFundTypeName(fundType)}基金");
                return;
            }

            await Context.Interaction.SendConfirmAsync($"`{Context.Guild.Name}` {_service.GetFundTypeName(fundType)}基金排行榜\n\n" +
                $"{string.Join('\n', hashEntries.OrderByDescending((x) => x.Value).Select((x) => $"<@{x.Name}>: {x.Value}"))}");
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("all-fund-leaderboard", "所有基金的前三名排行榜")]
        public async Task AllFundLeaderBoardAsync()
        {
            await Context.Interaction.DeferAsync(false);

            try
            {
                var fundTypes = Enum.GetValues(typeof(FundType)).Cast<FundType>();
                var embed = new EmbedBuilder()
                    .WithTitle($"`{Context.Guild.Name}` 所有基金前三名排行榜")
                    .WithOkColor();

                bool hasAny = false;
                foreach (var fundType in fundTypes)
                {
                    var hashEntries = await RedisConnection.RedisDb.HashGetAllAsync(_service.GetFundRedisKey(fundType, Context.Guild.Id));
                    if (hashEntries.Length > 0)
                    {
                        hasAny = true;
                        embed.AddField(_service.GetFundTypeName(fundType), string.Join('\n', hashEntries.OrderByDescending((x) => x.Value).Take(3).Select((x) => $"<@{x.Name}>: {x.Value}")), true);
                    }
                }

                if (!hasAny)
                {
                    await Context.Interaction.SendErrorAsync("目前沒有任何人有基金", true);
                }
                else
                {
                    await Context.Interaction.FollowupAsync(embed: embed.Build());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"all-fund-leaderboard: {Context.Guild.Id}");
                await Context.Interaction.SendErrorAsync("取得排行榜時發生錯誤，請稍後再試", true);
            }
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

            if (Context.Guild.GetUser(message.Author.Id) == null)
            {
                await Context.Interaction.SendErrorAsync("指定的使用者不在此伺服器中");
                return;
            }

            var selectMenuBuilder = new SelectMenuBuilder()
                .WithCustomId("select_fund_type")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithRequired(true)
                .WithPlaceholder("選擇基金類型");

            foreach (var item in Enum.GetNames(typeof(FundType)))
            {
                selectMenuBuilder.AddOption(_service.GetFundTypeName(Enum.Parse<FundType>(item, true)), item);
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

            modalBuilder.AddSelectMenu("添加類型", selectMenuBuilder);

            await Context.Interaction.RespondWithModalAsync(modalBuilder.Build());
        }
    }
}
