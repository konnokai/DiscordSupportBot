using Discord.Interactions;
using Discord_Support_Bot.Interaction.Attribute;
using RequireGuild = Discord_Support_Bot.Interaction.Attribute.RequireGuildAttribute;

namespace Discord_Support_Bot.Interaction.NC_Guild_Only
{
    [DontAutoRegister]
    [Group("id", "Id")]
    [RequireGuild(490086730029072394)]
    public class Id : TopLevelModule
    {
        [SlashCommand("register", "註冊你的帳號Id")]
        public async Task RegisterAsync([Summary("Id", "若為空則移除註冊資訊")] string id = "")
        {
            await DeferAsync(true);

            using var db = new SupportContext();
            var user = db.NCchannelCOD.FirstOrDefault((x) => x.DiscordUserId == Context.User.Id);

            if (string.IsNullOrEmpty(id))
            {
                if (user == null)
                {
                    await Context.Interaction.SendErrorAsync("請輸入Id進行註冊", true, true);
                    return;
                }
                else if (await PromptUserConfirmAsync("未輸入Id，是否要移除註冊資訊?"))
                {
                    db.NCchannelCOD.Remove(user);
                    await Context.Interaction.SendConfirmAsync("已移除", true, true);
                }
                else return;
            }
            else
            {
                if (user == null)
                {
                    db.NCchannelCOD.Add(new NCchannelCOD() { DiscordUserId = Context.User.Id, CODId = id });
                    await Context.Interaction.SendConfirmAsync("註冊成功，你可以使用 `/id set-platform` 來設定你的主要遊玩平台(預設為PC)", true, true);
                }
                else if (await PromptUserConfirmAsync("已註冊，是否要覆蓋註冊Id?"))
                {
                    user.CODId = id;
                    db.NCchannelCOD.Update(user);
                    await Context.Interaction.SendConfirmAsync("已更新你的註冊Id", true, true);
                }
                else return;
            }
            db.SaveChanges();
        }

        [SlashCommand("search", "查詢使用者的資訊")]
        public async Task SearchAsync([Summary("使用者", "若無輸入則顯示自己的資訊")]IUser dcUser = null)
        {
            await DeferAsync(true);

            if (dcUser == null)
                dcUser = Context.User;

            using var db = new SupportContext();
            var user = db.NCchannelCOD.FirstOrDefault((x) => x.DiscordUserId == dcUser.Id);

            if (user == null)
            {
                await Context.Interaction.SendErrorAsync($"{dcUser} 未註冊", true, true);
            }
            else
            {
                await Context.Interaction.SendConfirmAsync($"{dcUser} 的資訊:\n" +
                    $"Cod Id: {user.CODId}\n" +
                    $"主要遊玩平台: {user.Platform}", true, true);
            }
        }

        [SlashCommand("set-platform", "設定你的主要遊玩平台")]
        public async Task SetPlatformAsync(NCchannelCOD.PlayerPlatform playerPlatform)
        {
            await DeferAsync(true);

            using var db = new SupportContext();
            var user = db.NCchannelCOD.FirstOrDefault((x) => x.DiscordUserId == Context.User.Id);

            if (user == null)
            {
                await Context.Interaction.SendErrorAsync("未註冊，請輸入 `/id register 你的Id` 後再使用本指令", true);
            }
            else
            {
                user.Platform = playerPlatform;
                db.NCchannelCOD.Update(user);
                db.SaveChanges();
                await Context.Interaction.SendConfirmAsync($"已設定你的主要遊玩平台為: {playerPlatform}", true, true);
            }
        }
    }
}