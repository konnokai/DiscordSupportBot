# Graph Report - DiscordSupportBot  (2026-08-07)

## Corpus Check
- 116 files · ~27,986 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1092 nodes · 1842 edges · 68 communities (50 shown, 18 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 20 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `5be96863`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- FoodWheelService
- DiscordSupportBot.Common
- AutoVoiceChannelService
- .HandleCommandAsync
- .GenerateSuggestionsAsync
- AutoCreatePrivateThreadService
- IInteractionService
- FundService
- Administration
- Extensions
- Extensions
- DiscordSupportBot.DataBase.Table
- ReactionEventWrapper
- LinkFixService
- Help
- Discord Support Bot
- HoneyPotService
- Normal
- ReplacementBuilder
- Help
- DiscordSupportBot.csproj
- InteractionHandler
- .GetConfiguredChannelAsync
- .Info
- DiscordSupportBot.DataBase
- .SetAutoVoiceChannelAsync
- Log
- .ToggleStreamingStatusAsync
- .FormatColorWrite
- .Warn
- Migration
- DiscordSupportBot.Migrations
- UptimeKumaClient
- Discord Privileged Intents
- DiscordSupportBot.Interaction.Attribute
- RedisConnection
- .CheckRequirementsAsync
- discord-support-bot Service
- Id
- .EvaluateAsync
- SupportContext
- Discord Support Bot
- .CheckPermissionsAsync
- .CheckRequirementsAsync
- .CheckRequirementsAsync
- Utility
- DiscordWebhookClient
- DeleteTimeChannel
- AddChannelNitroInfo
- NCChannel
- RenameLottery
- Misc
- RemoveTwitter
- AddHoneyPotChannel
- AddLinkFix
- AddFoodWheelEntry
- AddStreamingStatus
- AddAutoCreatePrivateThreadMentions
- 20210611152100_AddChannelNitroInfo.Designer.cs
- 20220813051631_RenameLottery.Designer.cs
- 20231024080730_Misc.Designer.cs
- 20231024084646_RemoveTwitter.Designer.cs
- 20250825091521_AddHoneyPotChannel.Designer.cs
- 20260323025714_AddLinkFix.Designer.cs
- 20260620112334_AddStreamingStatus.Designer.cs
- Discord Terms of Service
- .SetHoneyPotAsync
- StreamingStatusService

## God Nodes (most connected - your core abstractions)
1. `FoodWheelService` - 37 edges
2. `AutoCreatePrivateThreadService` - 27 edges
3. `Administration` - 26 edges
4. `DiscordSupportBot.Migrations` - 23 edges
5. `Extensions` - 18 edges
6. `FundService` - 18 edges
7. `StreamingStatusService` - 17 edges
8. `Extensions` - 15 edges
9. `ReplacementBuilder` - 14 edges
10. `DiscordSupportBot.DataBase` - 14 edges

## Surprising Connections (you probably didn't know these)
- `ZSET-only Fund Storage` --semantically_similar_to--> `Redis Fund Leaderboard SortedSet`  [INFERRED] [semantically similar]
  .github/copilot-instructions.md → README.md
- `Discord Support Bot` --references--> `Bot Invitation`  [EXTRACTED]
  DiscordSupportBot/Data/HelpDescription.txt → Data/HelpDescription.txt
- `Discord Support Bot` --references--> `Jun112561`  [EXTRACTED]
  DiscordSupportBot/Data/HelpDescription.txt → Data/HelpDescription.txt
- `Docker Compose Deployment` --references--> `discord-support-bot Service`  [EXTRACTED]
  README.md → docker-compose.yml
- `Discord Support Bot` --references--> `Discord Support Bot`  [EXTRACTED]
  README.md → PRIVACY_POLICY.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Privacy Policy Privileged Intent Set** — privacy_policy_message_content_intent, privacy_policy_server_members_intent, privacy_policy_presence_intent [EXTRACTED 1.00]
- **Docker Compose Service Runtime Configuration** — docker_compose_discord_support_bot_service, docker_compose_data_volume, docker_compose_environment_file, docker_compose_restart_policy, docker_compose_host_gateway, docker_compose_bridge_network [EXTRACTED 1.00]

## Communities (68 total, 18 thin omitted)

### Community 0 - "FoodWheelService"
Cohesion: 0.06
Nodes (43): DiscordSupportBot.Interaction.FoodWheel.Service, DiscordSupportBot.Interaction.FoodWheel, IInteractionContext, SlashCommand, Task, BlacklistModule, CustomModule, DrinkWheelModule (+35 more)

### Community 1 - "DiscordSupportBot.Common"
Cohesion: 0.05
Nodes (26): DiscordSupportBot.Common, DiscordSupportBot.Extensions, Func, IReadOnlyCollection, ArrayExtensions, IMessageChannel, IReadOnlyCollection, IUserMessage (+18 more)

### Community 2 - "AutoVoiceChannelService"
Cohesion: 0.12
Nodes (16): ChannelEvent, DiscordSupportBot.Interaction.AutoVoiceChannel.Services, ConcurrentDictionary, DiscordSocketClient, HashSet, int, IVoiceChannel, SocketUser (+8 more)

### Community 3 - ".HandleCommandAsync"
Cohesion: 0.06
Nodes (27): DiscordSupportBot.Interaction.Activity, DiscordSupportBot.Command, DiscordSupportBot.DataBase.Activity, CommandService, DiscordSocketClient, IServiceProvider, SocketMessage, Task (+19 more)

### Community 4 - ".GenerateSuggestionsAsync"
Cohesion: 0.09
Nodes (29): AutocompleteHandler, CommandContextType, DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service, DiscordSupportBot.Interaction.AutoCreatePrivateThread, DiscordSupportBot.Interaction.Lottery, AutocompletionResult, IAutocompleteInteraction, IInteractionContext (+21 more)

### Community 5 - "AutoCreatePrivateThreadService"
Cohesion: 0.13
Nodes (22): Action, ButtonLockState, ChannelId, Dictionary, AutoCreatePrivateThreadConfig, Config, DiscordSocketClient, Func (+14 more)

### Community 6 - "IInteractionService"
Cohesion: 0.07
Nodes (25): Attachment, DiscordSupportBot.Interaction.Admin, DiscordSupportBot.Interaction.Admin.Service, DefaultMemberPermissions, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand (+17 more)

### Community 7 - "FundService"
Cohesion: 0.10
Nodes (24): ChannelIds, DiscordSupportBot.Interaction.Fund.Service, DiscordSupportBot.Interaction.Fund, DeletedCount, IMessage, IUser, RequireContext, SlashCommand (+16 more)

### Community 8 - "Administration"
Cohesion: 0.20
Nodes (15): Alias, Command, DiscordSocketClient, int, IUser, RequireBotPermission, RequireContext, RequireUserPermission (+7 more)

### Community 9 - "Extensions"
Cohesion: 0.09
Nodes (19): Assembly, DateTime, DiscordSocketClient, EmbedBuilder, Func, IDiscordInteraction, IEmote, IEnumerable (+11 more)

### Community 10 - "Extensions"
Cohesion: 0.12
Nodes (17): Assembly, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IEnumerable, IMessage (+9 more)

### Community 11 - "DiscordSupportBot.DataBase.Table"
Cohesion: 0.14
Nodes (12): DiscordSupportBot.DataBase.Table, AutoCreatePrivateThreadConfig, DateTime, DbEntity, FoodWheelEntry, Guild, GuildConfig, LinkFixConfig (+4 more)

### Community 12 - "ReactionEventWrapper"
Cohesion: 0.14
Nodes (17): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper (+9 more)

### Community 13 - "LinkFixService"
Cohesion: 0.11
Nodes (15): DiscordSupportBot.Interaction.LinkFix, DiscordSupportBot.Interaction.LinkFix.Service, RequireContext, RequireUserPermission, SlashCommand, Task, LinkFix, ConcurrentDictionary (+7 more)

### Community 14 - "Help"
Cohesion: 0.12
Nodes (13): DiscordSupportBot.Interaction.Help, Func, CommonEqualityComparer, HelpService, InteractionService, IServiceProvider, SlashCommand, SlashCommandInfo (+5 more)

### Community 15 - "Discord Support Bot"
Cohesion: 0.08
Nodes (26): ZSET-only Fund Storage, Data Minimization, Data Sharing Exceptions, Access and Deletion Rights, Data Use Purposes, Discord Identifiers, Discord Support Bot, Functional Data (+18 more)

### Community 16 - "HoneyPotService"
Cohesion: 0.18
Nodes (6): DiscordSupportBot.Interaction.Admin.HoneyPot, DiscordSocketClient, HashSet, SocketMessage, Task, HoneyPotService

### Community 17 - "Normal"
Cohesion: 0.18
Nodes (14): DiscordSupportBot.Command.Normal, Alias, Command, DiscordSocketClient, RequireContext, Summary, Task, Normal (+6 more)

### Community 18 - "ReplacementBuilder"
Cohesion: 0.18
Nodes (10): ConcurrentDictionary, DiscordSocketClient, Func, ICommandContext, IEnumerable, IMessageChannel, IUser, Regex (+2 more)

### Community 19 - "Help"
Cohesion: 0.13
Nodes (14): DiscordSupportBot.Command.Help, Alias, Command, CommandInfo, CommandService, IServiceProvider, string, Summary (+6 more)

### Community 20 - "DiscordSupportBot.csproj"
Cohesion: 0.11
Nodes (15): net8.0, Ben.Demystifier (0.4.1), Dapper (2.1.66), Discord.Net (3.19.1), JsonExtensions (1.2.0), Microsoft.Data.Sqlite.Core (9.0.8), Microsoft.EntityFrameworkCore.Design (9.0.8), Microsoft.EntityFrameworkCore.Sqlite (9.0.8) (+7 more)

### Community 21 - "InteractionHandler"
Cohesion: 0.09
Nodes (18): DiscordSupportBot.Interaction, IInteractionService, DiscordSocketClient, IInteractionContext, InteractionService, IServiceProvider, SlashCommandInfo, SocketMessageCommand (+10 more)

### Community 22 - ".GetConfiguredChannelAsync"
Cohesion: 0.26
Nodes (10): Channel, Config, DiscordSocketClient, SlashCommand, SocketTextChannel, Task, AutoCreatePrivateThread, IMentionable (+2 more)

### Community 23 - ".Info"
Cohesion: 0.21
Nodes (7): DiscordSupportBot, BotConfig, NotRequirementAttribute, Main(), string, Type, Utility

### Community 24 - "DiscordSupportBot.DataBase"
Cohesion: 0.14
Nodes (7): DiscordSupportBot.DataBase, ModelBuilder, DeleteTimeChannel, ModelBuilder, NCChannel, ModelBuilder, AddFoodWheelEntry

### Community 25 - ".SetAutoVoiceChannelAsync"
Cohesion: 0.22
Nodes (10): AutoVoiceChannelService, DiscordSupportBot.Interaction.AutoVoiceChannel, DefaultMemberPermissions, IVoiceChannel, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand (+2 more)

### Community 26 - "Log"
Cohesion: 0.22
Nodes (5): object, Task, Log, Exception, LogMessage

### Community 27 - ".ToggleStreamingStatusAsync"
Cohesion: 0.24
Nodes (9): DiscordSupportBot.Interaction.StreamingStatus, DefaultMemberPermissions, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand, Task, StreamingStatus (+1 more)

### Community 28 - ".FormatColorWrite"
Cohesion: 0.23
Nodes (7): ConsoleColor, DiscordSupportBot.Command.Administration, DiscordSocketClient, ITextChannel, SocketCommandContext, Task, AdministraionService

### Community 29 - ".Warn"
Cohesion: 0.27
Nodes (7): ConsoleCancelEventArgs, Console_CancelKeyPress(), TimerHandler(), TimerHandler3(), TimerHandler4(), TimerHandler5(), UpdateStatusFlags

### Community 30 - "Migration"
Cohesion: 0.20
Nodes (6): Discord_Support_Bot.Migrations, MigrationBuilder, ModelBuilder, InitialCreate, InitialCreate, Migration

### Community 31 - "DiscordSupportBot.Migrations"
Cohesion: 0.20
Nodes (6): DiscordSupportBot.Migrations, ModelBuilder, AddAutoCreatePrivateThreadMentions, ModelBuilder, SupportContextModelSnapshot, ModelSnapshot

### Community 32 - "UptimeKumaClient"
Cohesion: 0.22
Nodes (8): bool, DiscordSocketClient, HttpClient, int, string, Task, Timer, UptimeKumaClient

### Community 33 - "Discord Privileged Intents"
Cohesion: 0.20
Nodes (10): Message Content Intent, Message Content Non-retention, Presence Intent, Discord Privileged Intents, Server Members Intent, Message Content Intent, No Regular Message Content Storage, Presence Intent (+2 more)

### Community 34 - "DiscordSupportBot.Interaction.Attribute"
Cohesion: 0.22
Nodes (7): Attribute, DiscordSupportBot.Interaction.Attribute, DiscordSupportBot.Interaction.Help.Service, string, CommandExampleAttribute, string, CommandSummaryAttribute

### Community 35 - "RedisConnection"
Cohesion: 0.25
Nodes (6): ConnectionMultiplexer, string, RedisConnection, IDatabase, IServer, Lazy

### Community 36 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 37 - "discord-support-bot Service"
Cohesion: 0.25
Nodes (8): Bridge Network Mode, Data Volume Mount, discord-support-bot Service, Environment File Configuration, Host Docker Internal Gateway, Unless-stopped Restart Policy, Bot Data Persistence, Docker Compose Deployment

### Community 38 - "Id"
Cohesion: 0.29
Nodes (6): DiscordSupportBot.Interaction.NC_Guild_Only, IUser, SlashCommand, Task, Id, PlayerPlatform

### Community 39 - ".EvaluateAsync"
Cohesion: 0.29
Nodes (6): SocketUser, SocketVoiceChannel, SocketVoiceState, Task, SocketGuildUser, SocketPresence

### Community 40 - "SupportContext"
Cohesion: 0.15
Nodes (11): DbContext, DbContextOptionsBuilder, DbSet, AutoCreatePrivateThreadConfig, ModelBuilder, SupportContext, FoodWheelEntry, GuildConfig (+3 more)

### Community 41 - "Discord Support Bot"
Cohesion: 0.40
Nodes (6): Bot Invitation, Jun112561, Administration Features, Discord Support Bot, Emote Usage Statistics, Message Activity Statistics

### Community 42 - ".CheckPermissionsAsync"
Cohesion: 0.22
Nodes (7): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute, PreconditionAttribute

### Community 43 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute

### Community 44 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 45 - "Utility"
Cohesion: 0.27
Nodes (6): DiscordSupportBot.Interaction.Utility, DiscordSocketClient, SlashCommand, Task, Utility, UtilityService

### Community 46 - "DiscordWebhookClient"
Cohesion: 0.33
Nodes (5): DiscordSupportBot.HttpClients, DiscordSocketClient, HttpClient, DiscordWebhookClient, Message

### Community 65 - "Discord Terms of Service"
Cohesion: 0.50
Nodes (4): Children's Privacy, Discord Community Guidelines, Discord Terms of Service, Service Eligibility

### Community 66 - ".SetHoneyPotAsync"
Cohesion: 0.31
Nodes (8): DefaultMemberPermissions, ITextChannel, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand, Task, HoneyPot

### Community 67 - "StreamingStatusService"
Cohesion: 0.15
Nodes (8): DiscordSupportBot.Interaction.StreamingStatus.Services, DiscordSocketClient, HashSet, HttpClient, int, string, Timer, StreamingStatusService

## Knowledge Gaps
- **62 isolated node(s):** `Guild`, `net8.0`, `Ben.Demystifier (0.4.1)`, `Dapper (2.1.66)`, `Discord.Net (3.19.1)` (+57 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **18 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordSupportBot.DataBase` connect `DiscordSupportBot.DataBase` to `20260620112334_AddStreamingStatus.Designer.cs`, `.HandleCommandAsync`, `DiscordSupportBot.Migrations`, `20210611152100_AddChannelNitroInfo.Designer.cs`, `20220813051631_RenameLottery.Designer.cs`, `20231024080730_Misc.Designer.cs`, `20231024084646_RemoveTwitter.Designer.cs`, `20250825091521_AddHoneyPotChannel.Designer.cs`, `20260323025714_AddLinkFix.Designer.cs`?**
  _High betweenness centrality (0.156) - this node is a cross-community bridge._
- **Why does `SupportContext` connect `SupportContext` to `DiscordSupportBot.DataBase`, `StreamingStatusService`?**
  _High betweenness centrality (0.124) - this node is a cross-community bridge._
- **What connects `Guild`, `net8.0`, `Ben.Demystifier (0.4.1)` to the rest of the system?**
  _62 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FoodWheelService` be split into smaller, more focused modules?**
  _Cohesion score 0.06460206460206461 - nodes in this community are weakly interconnected._
- **Should `DiscordSupportBot.Common` be split into smaller, more focused modules?**
  _Cohesion score 0.0531986531986532 - nodes in this community are weakly interconnected._
- **Should `AutoVoiceChannelService` be split into smaller, more focused modules?**
  _Cohesion score 0.12121212121212122 - nodes in this community are weakly interconnected._
- **Should `.HandleCommandAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.05589225589225589 - nodes in this community are weakly interconnected._