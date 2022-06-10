﻿using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;

namespace Discord_Support_Bot.Interaction.Attribute
{
    public class RequireGuildOwnerAttribute : PreconditionAttribute
    {
        public RequireGuildOwnerAttribute()
        {
        }

        public override string ErrorMessage { get; } = "非伺服器擁有者不可使用本指令";

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.Interaction.User.Id == Program.ApplicatonOwner.Id) return Task.FromResult(PreconditionResult.FromSuccess());

            if (context.Interaction.User.Id == context.Guild.OwnerId) return Task.FromResult(PreconditionResult.FromSuccess());
            else return Task.FromResult(PreconditionResult.FromError("非伺服器擁有者不可使用本指令"));
        }
    }
}