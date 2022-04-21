//using DSharpPlus.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;

//namespace Discord_Support_Bot.Command.OwnerCommand
//{
//    class Command_CheckRole
//    {
//        static Thread thread;
//        private static bool isWork = false;

//        static public string CommandName => "!!!checkrole";
//        static private string HelpName => "!!!checkrole";
//        static private string HelpText => "檢查用戶組權限";

//        static public bool CanExcute(string text) => text.ToLower().StartsWith(CommandName);

//        static public void OnAction(DSharpPlus.EventArgs.MessageCreateEventArgs e)
//        {
//            if (e.Guild.Id == 463657254105645056)
//            {
//                if (isWork) e.Message.RespondAsync("已經在執行了");
//                else
//                {
//                    thread = new Thread(Thread);
//                    thread.Start(e);
//                }
//            }
//            return;
//        }

//        static public void AddField(DiscordEmbedBuilder discordEmbedBuilder)
//        {
//            discordEmbedBuilder.AddField(HelpName, HelpText, HelpText.Length < 13);
//        }

//        static private async void Thread(object obj)
//        {
//            DSharpPlus.EventArgs.MessageCreateEventArgs e = obj as DSharpPlus.EventArgs.MessageCreateEventArgs;
//            isWork = true;

//            DiscordMessage discordMessage = await e.Message.RespondAsync("檢查用戶組中...");
//            try
//            {
//                int grantCount = 0, revokeCount = 0, revokeNewCount = 0;

//                DiscordRole roleMuted1 = e.Guild.GetRole(568223415778148352); //Muted
//                DiscordRole rolePasser = e.Guild.GetRole(541117962896146445); //路人甲
//                DiscordRole rolePasserByJiaJia = e.Guild.GetRole(491838110460674058); //路過的甲甲
//                DiscordRole roleNewJiaJia = e.Guild.GetRole(464085428941619220); //新人甲甲
//                DiscordRole roleMinecraft = e.Guild.GetRole(601294612291780618); //Minecraft
//                DiscordRole roleMinecraftLinked = e.Guild.GetRole(601405828377083908); //Minecraft Linked

//                IEnumerable<DiscordMember> discordMembers = e.Guild.GetAllMembersAsync().GetAwaiter().GetResult();

//                IEnumerable<DiscordMember> NeedGrantRoleMembers = discordMembers.Where((x) => x.Roles.Count() == 0 || (x.Roles.Count() == 1 && x.Roles.Any((x2) => x2.Id == 534871710474960896)));
//                foreach (DiscordMember item in NeedGrantRoleMembers)
//                {
//                    grantCount++;
//                    await item.GrantRoleAsync(rolePasser);
//                    Console.WriteLine(string.Format("({0}/{1}) 給予了 {2} {3}", grantCount.ToString(), NeedGrantRoleMembers.Count().ToString(), item.DisplayName, rolePasser.Name));
//                }

//                IEnumerable<DiscordMember> NeedRevokeRoleMembers = discordMembers.Where((x) => x.Roles.Count() >= 2 && x.Roles.Contains(rolePasserByJiaJia) && !x.Roles.Contains(roleMuted1));
//                foreach (DiscordMember item in NeedRevokeRoleMembers)
//                {
//                    revokeCount++;
//                    await item.RevokeRoleAsync(rolePasserByJiaJia);
//                    Console.WriteLine(string.Format("({0}/{1}) 移除了 {2} {3}", revokeCount.ToString(), NeedRevokeRoleMembers.Count().ToString(), item.DisplayName, rolePasserByJiaJia.Name));
//                }

//                NeedGrantRoleMembers = discordMembers.Where((x) => (x.Roles.Count() == 1 && x.Roles.Contains(roleMinecraft) ||
//                    (x.Roles.Count() == 2 && x.Roles.Contains(roleMinecraft) && x.Roles.Contains(roleMinecraftLinked))));
//                foreach (DiscordMember item in NeedGrantRoleMembers)
//                {
//                    grantCount++;
//                    await item.GrantRoleAsync(rolePasser);
//                    Console.WriteLine(string.Format("({0}/{1}) 給予了 {2} {3}", grantCount.ToString(), NeedGrantRoleMembers.Count().ToString(), item.DisplayName, rolePasser.Name));
//                }

//                //NeedRevokeRoleMembers = discordMembers.Where((x) => x.Roles.Count() >= 2 && x.Roles.Contains(roleNewJiaJia) &&
//                //    !x.Roles.Any((x2) => x2.Id == 544679831602987008 || x2.Id == 541652838237995040 || x2.Id == 464047563839111168));
//                //foreach (DiscordMember item in NeedRevokeRoleMembers)
//                //{
//                //    revokeNewCount++;
//                //    await item.RevokeRoleAsync(rolePasserByJiaJia);
//                //    Console.WriteLine(string.Format("({0}/{1}) 移除了 {2} {3}", revokeNewCount.ToString(), NeedRevokeRoleMembers.Count(), item.DisplayName, rolePasserByJiaJia.Name));
//                //}

//                await e.Message.RespondAsync($"檢查完成!" +
//                    $"\n給予了 {grantCount.ToString()} 個路人甲" +
//                    $"\n移除了 {revokeCount.ToString()} 個路過的甲甲" +
//                    $"\n移除了 {revokeNewCount.ToString()} 個新人甲甲");
//                await Command_Bye.OnAction(e);
//            }
//            catch (Exception ex) { await e.Message.RespondAsync("錯誤\n" + ex.Message); await Command_Bye.OnAction(e); }
//            finally
//            {
//                await discordMessage.DeleteAsync();
//                isWork = false;
//            }
//        }
//    }
//}
