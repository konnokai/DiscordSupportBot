# Graph Report - .  (2026-07-31)

## Corpus Check
- Corpus is ~27,493 words - fits in a single context window. You may not need a graph.

## Summary
- 1082 nodes · 1795 edges · 69 communities (47 shown, 22 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 20 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Community 0
- Community 1
- Community 2
- Community 3
- Community 4
- Community 5
- Community 6
- Community 7
- Community 8
- Community 9
- Community 10
- Community 11
- Community 12
- Community 13
- Community 14
- Community 15
- Community 16
- Community 17
- Community 18
- Community 19
- Community 20
- Community 21
- Community 22
- Community 23
- Community 24
- Community 25
- Community 26
- Community 27
- Community 28
- Community 29
- Community 30
- Community 31
- Community 32
- Community 33
- Community 34
- Community 35
- Community 36
- Community 37
- Community 38
- Community 39
- Community 40
- Community 41
- Community 42
- Community 43
- Community 44
- Community 45
- Community 46
- Community 47
- Community 48
- Community 49
- Community 50
- Community 51
- Community 52
- Community 53
- Community 54
- Community 55
- Community 56
- Community 57
- Community 58
- Community 59
- Community 60
- Community 61
- Community 62
- Community 63
- Community 64
- Community 65
- Community 66
- Community 67
- Community 68

## God Nodes (most connected - your core abstractions)
1. `FoodWheelService` - 34 edges
2. `AutoCreatePrivateThreadService` - 26 edges
3. `Administration` - 25 edges
4. `DiscordSupportBot.Migrations` - 23 edges
5. `Extensions` - 18 edges
6. `Extensions` - 15 edges
7. `StreamingStatusService` - 15 edges
8. `ReplacementBuilder` - 14 edges
9. `DiscordSupportBot.DataBase` - 14 edges
10. `AutoCreatePrivateThreadConfigSnapshot` - 13 edges

## Surprising Connections (you probably didn't know these)
- `ZSET-only Fund Storage` --semantically_similar_to--> `Redis Fund Leaderboard SortedSet`  [INFERRED] [semantically similar]
  .github/copilot-instructions.md → README.md
- `Discord Support Bot` --references--> `Bot Invitation`  [EXTRACTED]
  DiscordSupportBot/Data/HelpDescription.txt → Data/HelpDescription.txt
- `Discord Support Bot` --references--> `Jun112561`  [EXTRACTED]
  DiscordSupportBot/Data/HelpDescription.txt → Data/HelpDescription.txt
- `Docker Compose Deployment` --references--> `discord-support-bot Service`  [EXTRACTED]
  README.md → docker-compose.yml
- `InteractionHandler` --implements--> `IInteractionService`  [EXTRACTED]
  DiscordSupportBot/Interaction/InteractionHandler.cs → DiscordSupportBot/Interaction/IInteractionService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Privacy Policy Privileged Intent Set** — privacy_policy_message_content_intent, privacy_policy_server_members_intent, privacy_policy_presence_intent [EXTRACTED 1.00]
- **Docker Compose Service Runtime Configuration** — docker_compose_discord_support_bot_service, docker_compose_data_volume, docker_compose_environment_file, docker_compose_restart_policy, docker_compose_host_gateway, docker_compose_bridge_network [EXTRACTED 1.00]

## Communities (69 total, 22 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.06
Nodes (42): DiscordSupportBot.Interaction.FoodWheel.Service, DiscordSupportBot.Interaction.FoodWheel, IInteractionContext, SlashCommand, Task, BlacklistModule, CustomModule, DrinkWheelModule (+34 more)

### Community 1 - "Community 1"
Cohesion: 0.05
Nodes (27): DiscordSupportBot.Common, DiscordSupportBot.Interaction.Admin.Service, DiscordSupportBot.Extensions, Func, IReadOnlyCollection, ArrayExtensions, IMessageChannel, IReadOnlyCollection (+19 more)

### Community 2 - "Community 2"
Cohesion: 0.05
Nodes (37): ChannelEvent, DiscordSupportBot.Interaction.AutoVoiceChannel.Services, DiscordSupportBot.Interaction.StreamingStatus.Services, DbContext, DbContextOptionsBuilder, DbSet, AutoCreatePrivateThreadConfig, ModelBuilder (+29 more)

### Community 3 - "Community 3"
Cohesion: 0.06
Nodes (28): DiscordSupportBot.Interaction.Activity, DiscordSupportBot.Command, DiscordSupportBot.DataBase.Activity, CommandService, DiscordSocketClient, IServiceProvider, SocketMessage, Task (+20 more)

### Community 4 - "Community 4"
Cohesion: 0.08
Nodes (29): AutocompleteHandler, CommandContextType, DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service, DiscordSupportBot.Interaction.AutoCreatePrivateThread, DiscordSupportBot.Interaction.Lottery, AutocompletionResult, IAutocompleteInteraction, IInteractionContext (+21 more)

### Community 5 - "Community 5"
Cohesion: 0.13
Nodes (22): Action, ButtonLockState, ChannelId, Dictionary, AutoCreatePrivateThreadConfig, Config, DiscordSocketClient, Func (+14 more)

### Community 6 - "Community 6"
Cohesion: 0.06
Nodes (28): Attachment, DiscordSupportBot.Interaction.Utility, DiscordSupportBot.Interaction.Admin, DefaultMemberPermissions, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand (+20 more)

### Community 7 - "Community 7"
Cohesion: 0.11
Nodes (22): ChannelIds, DiscordSupportBot.Interaction.Fund.Service, DiscordSupportBot.Interaction.Fund, DeletedCount, IMessage, IUser, RequireContext, SlashCommand (+14 more)

### Community 8 - "Community 8"
Cohesion: 0.22
Nodes (14): Alias, Command, DiscordSocketClient, IUser, RequireBotPermission, RequireContext, RequireUserPermission, Summary (+6 more)

### Community 9 - "Community 9"
Cohesion: 0.09
Nodes (18): Assembly, DateTime, DiscordSocketClient, EmbedBuilder, Func, IEmote, IEnumerable, IInteractionContext (+10 more)

### Community 10 - "Community 10"
Cohesion: 0.12
Nodes (17): Assembly, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IEnumerable, IMessage (+9 more)

### Community 11 - "Community 11"
Cohesion: 0.10
Nodes (17): DiscordSupportBot.DataBase.Table, AutoCreatePrivateThreadConfig, DateTime, DbEntity, FoodWheelEntry, Guild, GuildConfig, LinkFixConfig (+9 more)

### Community 12 - "Community 12"
Cohesion: 0.14
Nodes (17): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper (+9 more)

### Community 13 - "Community 13"
Cohesion: 0.11
Nodes (15): DiscordSupportBot.Interaction.LinkFix, DiscordSupportBot.Interaction.LinkFix.Service, RequireContext, RequireUserPermission, SlashCommand, Task, LinkFix, ConcurrentDictionary (+7 more)

### Community 14 - "Community 14"
Cohesion: 0.10
Nodes (15): DiscordSupportBot.Interaction.Help, CommandInfo, CommandTextEqualityComparer, Func, CommonEqualityComparer, HelpService, InteractionService, IServiceProvider (+7 more)

### Community 15 - "Community 15"
Cohesion: 0.08
Nodes (26): ZSET-only Fund Storage, Data Minimization, Data Sharing Exceptions, Access and Deletion Rights, Data Use Purposes, Discord Identifiers, Discord Support Bot, Functional Data (+18 more)

### Community 16 - "Community 16"
Cohesion: 0.11
Nodes (14): DiscordSupportBot.Interaction.Admin.HoneyPot, DefaultMemberPermissions, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand, Task, HoneyPot (+6 more)

### Community 17 - "Community 17"
Cohesion: 0.20
Nodes (13): DiscordSupportBot.Command.Normal, Alias, Command, DiscordSocketClient, RequireContext, Summary, Task, Normal (+5 more)

### Community 18 - "Community 18"
Cohesion: 0.18
Nodes (10): ConcurrentDictionary, DiscordSocketClient, Func, ICommandContext, IEnumerable, IMessageChannel, IUser, Regex (+2 more)

### Community 19 - "Community 19"
Cohesion: 0.16
Nodes (12): DiscordSupportBot.Command.Help, Alias, Command, CommandService, IServiceProvider, string, Summary, Task (+4 more)

### Community 20 - "Community 20"
Cohesion: 0.11
Nodes (15): net8.0, Ben.Demystifier (0.4.1), Dapper (2.1.66), Discord.Net (3.19.1), JsonExtensions (1.2.0), Microsoft.Data.Sqlite.Core (9.0.8), Microsoft.EntityFrameworkCore.Design (9.0.8), Microsoft.EntityFrameworkCore.Sqlite (9.0.8) (+7 more)

### Community 21 - "Community 21"
Cohesion: 0.15
Nodes (12): DiscordSocketClient, IInteractionContext, InteractionService, IServiceProvider, SlashCommandInfo, SocketMessageCommand, SocketMessageComponent, Task (+4 more)

### Community 22 - "Community 22"
Cohesion: 0.26
Nodes (10): Channel, Config, DiscordSocketClient, SlashCommand, SocketTextChannel, Task, AutoCreatePrivateThread, IMentionable (+2 more)

### Community 23 - "Community 23"
Cohesion: 0.13
Nodes (10): DiscordSupportBot.HttpClients, DiscordSupportBot, BotConfig, DiscordSocketClient, HttpClient, DiscordWebhookClient, Message, string (+2 more)

### Community 24 - "Community 24"
Cohesion: 0.14
Nodes (7): DiscordSupportBot.DataBase, ModelBuilder, DeleteTimeChannel, ModelBuilder, NCChannel, ModelBuilder, AddFoodWheelEntry

### Community 25 - "Community 25"
Cohesion: 0.22
Nodes (10): AutoVoiceChannelService, DiscordSupportBot.Interaction.AutoVoiceChannel, DefaultMemberPermissions, IVoiceChannel, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand (+2 more)

### Community 26 - "Community 26"
Cohesion: 0.23
Nodes (6): ConsoleColor, object, Task, Log, Exception, LogMessage

### Community 27 - "Community 27"
Cohesion: 0.24
Nodes (9): DiscordSupportBot.Interaction.StreamingStatus, DefaultMemberPermissions, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand, Task, StreamingStatus (+1 more)

### Community 28 - "Community 28"
Cohesion: 0.21
Nodes (7): DiscordSupportBot.Command.Administration, DiscordSocketClient, ITextChannel, SocketCommandContext, Task, AdministraionService, ResetType

### Community 29 - "Community 29"
Cohesion: 0.22
Nodes (6): ConsoleCancelEventArgs, Console_CancelKeyPress(), Main(), TimerHandler(), TimerHandler5(), UpdateStatusFlags

### Community 30 - "Community 30"
Cohesion: 0.20
Nodes (6): Discord_Support_Bot.Migrations, MigrationBuilder, ModelBuilder, InitialCreate, InitialCreate, Migration

### Community 31 - "Community 31"
Cohesion: 0.20
Nodes (6): DiscordSupportBot.Migrations, ModelBuilder, AddAutoCreatePrivateThreadMentions, ModelBuilder, SupportContextModelSnapshot, ModelSnapshot

### Community 32 - "Community 32"
Cohesion: 0.24
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 33 - "Community 33"
Cohesion: 0.20
Nodes (10): Message Content Intent, Message Content Non-retention, Presence Intent, Discord Privileged Intents, Server Members Intent, Message Content Intent, No Regular Message Content Storage, Presence Intent (+2 more)

### Community 34 - "Community 34"
Cohesion: 0.25
Nodes (6): Attribute, NotRequirementAttribute, string, CommandExampleAttribute, string, CommandSummaryAttribute

### Community 35 - "Community 35"
Cohesion: 0.25
Nodes (6): ConnectionMultiplexer, string, RedisConnection, IDatabase, IServer, Lazy

### Community 36 - "Community 36"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 37 - "Community 37"
Cohesion: 0.25
Nodes (8): Bridge Network Mode, Data Volume Mount, discord-support-bot Service, Environment File Configuration, Host Docker Internal Gateway, Unless-stopped Restart Policy, Bot Data Persistence, Docker Compose Deployment

### Community 38 - "Community 38"
Cohesion: 0.33
Nodes (4): DiscordSupportBot.Interaction.NC_Guild_Only, DiscordSupportBot.Interaction.Attribute, DiscordSupportBot.Interaction.Help.Service, RequireGuildAttribute

### Community 40 - "Community 40"
Cohesion: 0.57
Nodes (3): EmbedBuilder, SlashCommandInfo, HelpService

### Community 41 - "Community 41"
Cohesion: 0.40
Nodes (6): Bot Invitation, Jun112561, Administration Features, Discord Support Bot, Emote Usage Statistics, Message Activity Statistics

### Community 42 - "Community 42"
Cohesion: 0.33
Nodes (5): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task

### Community 43 - "Community 43"
Cohesion: 0.33
Nodes (5): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task

### Community 44 - "Community 44"
Cohesion: 0.33
Nodes (5): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task

### Community 45 - "Community 45"
Cohesion: 0.53
Nodes (4): Task, TopLevelModule, InteractionModuleBase, SocketInteractionContext

### Community 46 - "Community 46"
Cohesion: 0.40
Nodes (3): RequireGuildAttribute, RequireGuildMemberCountAttribute, PreconditionAttribute

### Community 65 - "Community 65"
Cohesion: 0.50
Nodes (4): Children's Privacy, Discord Community Guidelines, Discord Terms of Service, Service Eligibility

## Knowledge Gaps
- **69 isolated node(s):** `ResetType`, `Guild`, `PlayerPlatform`, `net8.0`, `Ben.Demystifier (0.4.1)` (+64 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **22 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordSupportBot.DataBase` connect `Community 24` to `Community 64`, `Community 3`, `Community 31`, `Community 58`, `Community 59`, `Community 60`, `Community 61`, `Community 62`, `Community 63`?**
  _High betweenness centrality (0.185) - this node is a cross-community bridge._
- **Why does `DiscordSupportBot.Command` connect `Community 3` to `Community 46`?**
  _High betweenness centrality (0.126) - this node is a cross-community bridge._
- **Why does `SendMessageService` connect `Community 6` to `Community 1`?**
  _High betweenness centrality (0.121) - this node is a cross-community bridge._
- **What connects `ResetType`, `Guild`, `PlayerPlatform` to the rest of the system?**
  _73 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.06390977443609022 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.05137844611528822 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.05064935064935065 - nodes in this community are weakly interconnected._