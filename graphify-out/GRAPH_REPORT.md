# Graph Report - DiscordSupportBot  (2026-08-31)

## Corpus Check
- 120 files · ~30,404 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1289 nodes · 2167 edges · 76 communities
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 56 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `6813d2cf`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- FoodWheelService
- .SendAsync
- AutoVoiceChannelService
- ReactionEventWrapper
- SupportContext
- AutoCreatePrivateThreadService
- IInteractionService
- FundService
- Administration
- Extensions
- Extensions
- .HandleCommandAsync
- SmartText
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
- BotConfig
- AddFoodWheelEntry
- .SetAutoVoiceChannelAsync
- .Info
- .GenerateSuggestionsAsync
- .GetDbContext
- EmoteActivity
- InitialCreate
- DiscordSupportBot.DataBase
- .Init
- Discord Privileged Intents
- DiscordSupportBot.Interaction.Attribute
- RedisConnection
- SmartEmbedTextBase
- discord-support-bot Service
- TopLevelModule
- StreamingStatusService
- UserActivity
- Discord Support Bot
- .CheckPermissionsAsync
- DiscordSupportBot.Common
- FundType
- SmartEmbedText
- DeleteTimeChannel
- AddChannelNitroInfo
- DiscordSupportBot.Migrations
- Migration
- Misc
- RemoveTwitter
- AddHoneyPotChannel
- AddLinkFix
- Lottery
- AddStreamingStatus
- AddAutoCreatePrivateThreadMentions
- Activity
- EmoteTable
- NCChannelCOD
- TopLevelModule
- Repository Instructions
- Replacer
- GuildConfig
- Discord Terms of Service
- .SetHoneyPotAsync
- DiscordSupportBot.Interaction
- DiscordSupportBot.DataBase.Table
- AutoCreatePrivateThreadConfig
- ArrayExtensions
- FoodWheelEntry
- SmartTextEmbedField
- LinkFixConfig
- DbEntity
- UpdateStatusFlags

## God Nodes (most connected - your core abstractions)
1. `FoodWheelService` - 43 edges
2. `SupportContext` - 35 edges
3. `AutoCreatePrivateThreadService` - 31 edges
4. `Administration` - 25 edges
5. `FundService` - 24 edges
6. `DiscordSupportBot.Migrations` - 23 edges
7. `SmartEmbedTextBase` - 19 edges
8. `Fund` - 19 edges
9. `Extensions` - 18 edges
10. `ReplacementBuilder` - 17 edges

## Surprising Connections (you probably didn't know these)
- `ZSET-only Fund Storage` --semantically_similar_to--> `Redis Fund Leaderboard SortedSet`  [INFERRED] [semantically similar]
  .github/copilot-instructions.md → README.md
- `Discord Support Bot` --references--> `Bot Invitation`  [EXTRACTED]
  DiscordSupportBot/Data/HelpDescription.txt → Data/HelpDescription.txt
- `Discord Support Bot` --references--> `Jun112561`  [EXTRACTED]
  DiscordSupportBot/Data/HelpDescription.txt → Data/HelpDescription.txt
- `Docker Compose Deployment` --references--> `discord-support-bot Service`  [EXTRACTED]
  README.md → docker-compose.yml
- `Replacer` --references--> `Text`  [EXTRACTED]
  DiscordSupportBot/Common/Replacements/Replacer.cs → DiscordSupportBot/Common/SmartText/SmartPlainText.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Privacy Policy Privileged Intent Set** — privacy_policy_message_content_intent, privacy_policy_server_members_intent, privacy_policy_presence_intent [EXTRACTED 1.00]
- **Docker Compose Service Runtime Configuration** — docker_compose_discord_support_bot_service, docker_compose_data_volume, docker_compose_environment_file, docker_compose_restart_policy, docker_compose_host_gateway, docker_compose_bridge_network [EXTRACTED 1.00]

## Communities (76 total, 0 thin omitted)

### Community 0 - "FoodWheelService"
Cohesion: 0.05
Nodes (55): DiscordSupportBot.Interaction.FoodWheel.Service, DiscordSupportBot.Interaction.FoodWheel, IInteractionContext, SlashCommand, Task, BlacklistModule, CustomModule, DrinkWheelModule (+47 more)

### Community 1 - ".SendAsync"
Cohesion: 0.17
Nodes (9): DiscordSupportBot.Extensions, IMessageChannel, IReadOnlyCollection, IUserMessage, MessageComponent, Task, MessageChannelExtensions, EmbedBuilder (+1 more)

### Community 2 - "AutoVoiceChannelService"
Cohesion: 0.15
Nodes (13): DiscordSupportBot.Interaction.AutoVoiceChannel.Services, ConcurrentDictionary, DiscordSocketClient, HashSet, IVoiceChannel, Timer, AutoVoiceChannelService, ChannelEvent (+5 more)

### Community 3 - "ReactionEventWrapper"
Cohesion: 0.14
Nodes (17): Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper, Message (+9 more)

### Community 4 - "SupportContext"
Cohesion: 0.05
Nodes (46): AutocompleteHandler, CommandContextType, DiscordSupportBot.Interaction.StreamingStatus, DiscordSupportBot.Interaction.NC_Guild_Only, DiscordSupportBot.Interaction.Lottery, DbContext, DbContextOptionsBuilder, DbSet (+38 more)

### Community 5 - "AutoCreatePrivateThreadService"
Cohesion: 0.12
Nodes (25): Action, ButtonLockState, ChannelId, "AutoCreatePrivateThreadConfig", Config, DiscordSocketClient, Func, IEnumerable (+17 more)

### Community 6 - "IInteractionService"
Cohesion: 0.07
Nodes (26): Attachment, DiscordSupportBot.Interaction.Utility, DiscordSupportBot.Interaction.Admin, DiscordSupportBot.Interaction.Admin.Service, DefaultMemberPermissions, RequireBotPermission, RequireContext, RequireUserPermission (+18 more)

### Community 7 - "FundService"
Cohesion: 0.05
Nodes (43): AvatarBytes, ChannelIds, ChoiceDisplayAttribute, DiscordSupportBot.Interaction.Fund.Service, DiscordSupportBot.Tests, DiscordSupportBot.Interaction.Fund, DeletedCount, FundType (+35 more)

### Community 8 - "Administration"
Cohesion: 0.19
Nodes (17): Alias, Command, DiscordSocketClient, IUser, RequireBotPermission, RequireContext, RequireUserPermission, Summary (+9 more)

### Community 9 - "Extensions"
Cohesion: 0.09
Nodes (20): Assembly, DateTime, DiscordSocketClient, EmbedBuilder, Func, IDiscordInteraction, IEmote, IEnumerable (+12 more)

### Community 10 - "Extensions"
Cohesion: 0.12
Nodes (18): Assembly, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IEnumerable, IMessage (+10 more)

### Community 11 - ".HandleCommandAsync"
Cohesion: 0.16
Nodes (8): DiscordSupportBot.Command, CommandService, DiscordSocketClient, IServiceProvider, SocketMessage, Task, CommandHandler, ICommandService

### Community 12 - "SmartText"
Cohesion: 0.13
Nodes (12): SmartEmbedTextArray, Content, Embeds, IsValid, SmartPlainText, Text, SmartText, IsEmbed (+4 more)

### Community 13 - "LinkFixService"
Cohesion: 0.11
Nodes (16): DiscordSupportBot.Interaction.LinkFix, DiscordSupportBot.Interaction.LinkFix.Service, Dictionary, RequireContext, RequireUserPermission, SlashCommand, Task, LinkFix (+8 more)

### Community 14 - "Help"
Cohesion: 0.20
Nodes (9): DiscordSupportBot.Interaction.Help, HelpService, InteractionService, IServiceProvider, SlashCommand, SlashCommandInfo, Task, CommandTextEqualityComparer (+1 more)

### Community 15 - "Discord Support Bot"
Cohesion: 0.08
Nodes (26): ZSET-only Fund Storage, Data Minimization, Data Sharing Exceptions, Access and Deletion Rights, Data Use Purposes, Discord Identifiers, Discord Support Bot, Functional Data (+18 more)

### Community 16 - "HoneyPotService"
Cohesion: 0.18
Nodes (6): DiscordSupportBot.Interaction.Admin.HoneyPot, DiscordSocketClient, HashSet, SocketMessage, Task, HoneyPotService

### Community 17 - "Normal"
Cohesion: 0.21
Nodes (12): DiscordSupportBot.Command.Normal, Alias, Command, DiscordSocketClient, RequireContext, Summary, Task, Normal (+4 more)

### Community 18 - "ReplacementBuilder"
Cohesion: 0.17
Nodes (11): ConcurrentDictionary, DiscordSocketClient, Func, ICommandContext, IEnumerable, IMessageChannel, IUser, Match (+3 more)

### Community 19 - "Help"
Cohesion: 0.09
Nodes (20): DiscordSupportBot.Command.Help, DiscordSupportBot.Command.Administration, DiscordSocketClient, ITextChannel, SocketCommandContext, Task, AdministraionService, Alias (+12 more)

### Community 20 - "DiscordSupportBot.csproj"
Cohesion: 0.08
Nodes (23): net8.0, Microsoft.NET.Sdk, DiscordSupportBot.Tests, net8.0, Microsoft.NET.Sdk, Ben.Demystifier (0.4.1), Dapper (2.1.79), Discord.Net (3.20.1) (+15 more)

### Community 21 - "InteractionHandler"
Cohesion: 0.14
Nodes (13): DiscordSocketClient, IInteractionContext, InteractionService, IServiceProvider, SlashCommandInfo, SocketMessageCommand, SocketMessageComponent, Task (+5 more)

### Community 22 - ".GetConfiguredChannelAsync"
Cohesion: 0.26
Nodes (10): Channel, Config, DiscordSocketClient, SlashCommand, SocketTextChannel, Task, AutoCreatePrivateThread, Success (+2 more)

### Community 23 - "BotConfig"
Cohesion: 0.07
Nodes (23): DiscordSupportBot.HttpClients, DiscordSupportBot, BotConfig, DiscordToken, IsEnablePresenceIntent, RedisOption, TestSlashCommandGuildId, TwitterClientBearerToken (+15 more)

### Community 24 - "AddFoodWheelEntry"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddFoodWheelEntry

### Community 25 - ".SetAutoVoiceChannelAsync"
Cohesion: 0.22
Nodes (10): AutoVoiceChannelService, DiscordSupportBot.Interaction.AutoVoiceChannel, DefaultMemberPermissions, IVoiceChannel, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand (+2 more)

### Community 26 - ".Info"
Cohesion: 0.15
Nodes (11): ConsoleCancelEventArgs, ConsoleColor, Task, Log, Console_CancelKeyPress(), Main(), TimerHandler(), TimerHandler3() (+3 more)

### Community 27 - ".GenerateSuggestionsAsync"
Cohesion: 0.19
Nodes (11): DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service, DiscordSupportBot.Interaction.AutoCreatePrivateThread, AutocompletionResult, DiscordSocketClient, IAutocompleteInteraction, IInteractionContext, IMessage, IParameterInfo (+3 more)

### Community 28 - ".GetDbContext"
Cohesion: 0.18
Nodes (6): ChannelEvent, SocketUser, SocketVoiceChannel, SocketVoiceState, Task, IGuildUser

### Community 29 - "EmoteActivity"
Cohesion: 0.29
Nodes (6): List, Task, EmoteActivity, ConnectString, IsInited, TimerHandler4()

### Community 30 - "InitialCreate"
Cohesion: 0.28
Nodes (4): Discord_Support_Bot.Migrations, MigrationBuilder, ModelBuilder, InitialCreate

### Community 31 - "DiscordSupportBot.DataBase"
Cohesion: 0.22
Nodes (5): DiscordSupportBot.DataBase, DateTime, ModelBuilder, SupportContextModelSnapshot, ModelSnapshot

### Community 32 - ".Init"
Cohesion: 0.39
Nodes (5): DiscordSocketClient, HttpClient, Task, Timer, UptimeKumaClient

### Community 33 - "Discord Privileged Intents"
Cohesion: 0.20
Nodes (10): Message Content Intent, Message Content Non-retention, Presence Intent, Discord Privileged Intents, Server Members Intent, Message Content Intent, No Regular Message Content Storage, Presence Intent (+2 more)

### Community 34 - "DiscordSupportBot.Interaction.Attribute"
Cohesion: 0.05
Nodes (34): Attribute, DiscordSupportBot.Interaction.Attribute, DiscordSupportBot.Interaction.Help.Service, NotRequirementAttribute, CommandExampleAttribute, ExpArray, CommandSummaryAttribute, Summary (+26 more)

### Community 35 - "RedisConnection"
Cohesion: 0.20
Nodes (8): ConnectionMultiplexer, RedisConnection, Instance, RedisDb, RedisServer, IDatabase, IServer, Lazy

### Community 36 - "SmartEmbedTextBase"
Cohesion: 0.15
Nodes (12): SmartEmbedArrayElementText, Color, SmartEmbedTextBase, Author, Description, Fields, Footer, Image (+4 more)

### Community 37 - "discord-support-bot Service"
Cohesion: 0.25
Nodes (8): Bridge Network Mode, Data Volume Mount, discord-support-bot Service, Environment File Configuration, Host Docker Internal Gateway, Unless-stopped Restart Policy, Bot Data Persistence, Docker Compose Deployment

### Community 38 - "TopLevelModule"
Cohesion: 0.32
Nodes (5): Task, TopLevelModule, _service, InteractionModuleBase, SocketInteractionContext

### Community 39 - "StreamingStatusService"
Cohesion: 0.16
Nodes (13): DiscordSupportBot.Interaction.StreamingStatus.Services, DiscordSocketClient, HashSet, HttpClient, SocketUser, SocketVoiceChannel, SocketVoiceState, Task (+5 more)

### Community 40 - "UserActivity"
Cohesion: 0.21
Nodes (8): List, Task, UserActivity, ConnectString, IsInited, UserTable, ActivityNum, UserID

### Community 41 - "Discord Support Bot"
Cohesion: 0.40
Nodes (6): Bot Invitation, Jun112561, Administration Features, Discord Support Bot, Emote Usage Statistics, Message Activity Statistics

### Community 42 - ".CheckPermissionsAsync"
Cohesion: 0.20
Nodes (8): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute, ErrorMessage, GuildId

### Community 43 - "DiscordSupportBot.Common"
Cohesion: 0.15
Nodes (8): DiscordSupportBot.Common, SmartTextEmbedAuthor, IconUrl, Name, Url, SmartTextEmbedFooter, IconUrl, Text

### Community 44 - "FundType"
Cohesion: 0.15
Nodes (13): FundType, BadJoke, Clown, Dizzy, Dreaming, Freak, FuckBoy, HentaiDog (+5 more)

### Community 46 - "SmartEmbedText"
Cohesion: 0.27
Nodes (5): EmbedBuilder, SmartEmbedText, Color, PlainText, IEmbed

### Community 47 - "DeleteTimeChannel"
Cohesion: 0.29
Nodes (3): MigrationBuilder, ModelBuilder, DeleteTimeChannel

### Community 48 - "AddChannelNitroInfo"
Cohesion: 0.29
Nodes (3): MigrationBuilder, ModelBuilder, AddChannelNitroInfo

### Community 49 - "DiscordSupportBot.Migrations"
Cohesion: 0.22
Nodes (6): DiscordSupportBot.Migrations, DateTime, MigrationBuilder, DateTime, ModelBuilder, NCChannel

### Community 50 - "Migration"
Cohesion: 0.24
Nodes (6): DateTime, MigrationBuilder, DateTime, ModelBuilder, RenameLottery, Migration

### Community 51 - "Misc"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, Misc

### Community 52 - "RemoveTwitter"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, RemoveTwitter

### Community 53 - "AddHoneyPotChannel"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddHoneyPotChannel

### Community 54 - "AddLinkFix"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddLinkFix

### Community 55 - "Lottery"
Cohesion: 0.20
Nodes (10): DateTime, Lottery, AwardContext, Context, CreateTime, EndTime, Guid, GuildId (+2 more)

### Community 56 - "AddStreamingStatus"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddStreamingStatus

### Community 57 - "AddAutoCreatePrivateThreadMentions"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddAutoCreatePrivateThreadMentions

### Community 58 - "Activity"
Cohesion: 0.39
Nodes (5): DiscordSupportBot.Interaction.Activity, RequireContext, SlashCommand, Task, Activity

### Community 59 - "EmoteTable"
Cohesion: 0.22
Nodes (5): DiscordSupportBot.DataBase.Activity, EmoteTable, ActivityNum, EmoteID, EmoteName

### Community 60 - "NCChannelCOD"
Cohesion: 0.22
Nodes (8): NCChannelCOD, CODId, DiscordUserId, Platform, PlayerPlatform, PC, PS, XBox

### Community 61 - "TopLevelModule"
Cohesion: 0.32
Nodes (6): EmbedBuilder, SocketCommandContext, Task, TopLevelModule, _service, ModuleBase

### Community 62 - "Repository Instructions"
Cohesion: 0.29
Nodes (6): Commands, Graphify, Persistence, Project Shape, Repository Instructions, Runtime

### Community 63 - "Replacer"
Cohesion: 0.25
Nodes (7): Func, IEnumerable, Match, Regex, Replacer, Key, Replacement

### Community 64 - "GuildConfig"
Cohesion: 0.25
Nodes (8): GuildConfig, AutoVoiceChannel, ChannelMemberId, ChannelNitroId, EnableStreamingStatus, GuildId, HoneyPotChannelId, StreamingStatusTemplate

### Community 65 - "Discord Terms of Service"
Cohesion: 0.50
Nodes (4): Children's Privacy, Discord Community Guidelines, Discord Terms of Service, Service Eligibility

### Community 66 - ".SetHoneyPotAsync"
Cohesion: 0.31
Nodes (8): DefaultMemberPermissions, ITextChannel, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand, Task, HoneyPot

### Community 67 - "DiscordSupportBot.Interaction"
Cohesion: 0.17
Nodes (5): DiscordSupportBot.Interaction, Func, CommonEqualityComparer, IInteractionService, IEqualityComparer

### Community 68 - "DiscordSupportBot.DataBase.Table"
Cohesion: 0.29
Nodes (3): DiscordSupportBot.DataBase.Table, Guild, name

### Community 69 - "AutoCreatePrivateThreadConfig"
Cohesion: 0.29
Nodes (6): AutoCreatePrivateThreadConfig, ChannelId, GuildId, MentionRoleIds, MentionUserIds, MessageId

### Community 70 - "ArrayExtensions"
Cohesion: 0.33
Nodes (3): Func, IReadOnlyCollection, ArrayExtensions

### Community 71 - "FoodWheelEntry"
Cohesion: 0.33
Nodes (5): FoodWheelEntry, Item, Kind, UserId, WheelType

### Community 72 - "SmartTextEmbedField"
Cohesion: 0.40
Nodes (4): SmartTextEmbedField, Inline, Name, Value

### Community 73 - "LinkFixConfig"
Cohesion: 0.40
Nodes (4): LinkFixConfig, GuildId, NewDomain, OldDomain

### Community 74 - "DbEntity"
Cohesion: 0.50
Nodes (4): DateTime, DbEntity, AddedAt, Id

### Community 75 - "UpdateStatusFlags"
Cohesion: 0.50
Nodes (4): UpdateStatusFlags, Guild, Info, Member

## Knowledge Gaps
- **215 isolated node(s):** `net8.0`, `Microsoft.NET.Test.Sdk (18.9.0)`, `xunit (2.9.3)`, `xunit.runner.visualstudio (4.0.0)`, `Microsoft.NET.Sdk` (+210 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 471 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `SupportContext` connect `SupportContext` to `FoodWheelService`, `AutoCreatePrivateThreadService`, `Administration`, `LinkFixService`, `.SetAutoVoiceChannelAsync`, `.Info`, `.GetDbContext`, `DiscordSupportBot.DataBase`?**
  _High betweenness centrality (0.233) - this node is a cross-community bridge._
- **Why does `DiscordSupportBot.DataBase` connect `DiscordSupportBot.DataBase` to `DeleteTimeChannel`, `AddChannelNitroInfo`, `DiscordSupportBot.Migrations`, `Misc`, `RemoveTwitter`, `AddHoneyPotChannel`, `AddLinkFix`, `AddFoodWheelEntry`, `AddAutoCreatePrivateThreadMentions`, `AddStreamingStatus`, `EmoteTable`?**
  _High betweenness centrality (0.179) - this node is a cross-community bridge._
- **Why does `FoodWheelService` connect `FoodWheelService` to `IInteractionService`?**
  _High betweenness centrality (0.115) - this node is a cross-community bridge._
- **Are the 22 inferred relationships involving `SupportContext` (e.g. with `.SetMemberNumberChannel()` and `.SetNitroNumberChannel()`) actually correct?**
  _`SupportContext` has 22 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net8.0`, `Microsoft.NET.Test.Sdk (18.9.0)`, `xunit (2.9.3)` to the rest of the system?**
  _215 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FoodWheelService` be split into smaller, more focused modules?**
  _Cohesion score 0.052184769038701624 - nodes in this community are weakly interconnected._
- **Should `ReactionEventWrapper` be split into smaller, more focused modules?**
  _Cohesion score 0.13675213675213677 - nodes in this community are weakly interconnected._