# Graph Report - .  (2026-07-15)

## Corpus Check
- Corpus is ~27,488 words - fits in a single context window. You may not need a graph.

## Summary
- 1078 nodes · 1800 edges · 75 communities (54 shown, 21 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 20 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Food Wheel Interactions
- Activity Command Pipeline
- Private Thread Commands
- Private Thread State
- Utility Admin Commands
- Fund Management
- Command Preconditions
- Interaction Utilities
- Command Utilities
- Reaction Event Handling
- Link Fixing
- Help Command Matching
- Privacy and Data
- Streaming Status Service
- Normal Text Commands
- Database Entities
- Text Replacement Utilities
- Help Text Commands
- Project Dependencies
- Interaction Dispatch
- Private Thread Configuration
- Bot Configuration Clients
- Honey Pot Moderation
- Smart Text Replacement
- Database Model Snapshots
- Auto Voice Commands
- Voice Channel Events
- Entity Framework Context
- Logging Infrastructure
- Streaming Status Commands
- Administration Service
- Smart Embed Text
- Command Metadata Attributes
- Application Startup
- Initial Database Migration
- Current Database Snapshot
- Uptime Monitoring
- Discord Privileged Intents
- Guild Identity Commands
- Text Command Guild Guard
- Smart Text Arrays
- Honey Pot Commands
- Redis Connection
- Voice Channel Service
- Message Channel Extensions
- Interaction Guild Guard
- Guild Size Guard
- Guild Owner Guard
- Docker Deployment
- Interaction Architecture
- Help Rendering Service
- Mention Sanitization
- Bot Features Overview
- Array Extensions
- Interactive User Prompts
- Delete Channel Migration
- Nitro Channel Migration
- NC Channel Migration
- Lottery Rename Migration
- Miscellaneous Migration
- Twitter Removal Migration
- Honey Pot Migration
- Link Fix Migration
- Food Wheel Migration
- Streaming Status Migration
- Thread Mentions Migration
- Modal Submission Handling
- Nitro Migration Designer
- Lottery Migration Designer
- Misc Migration Designer
- Twitter Migration Designer
- Honey Pot Designer
- Link Fix Designer
- Streaming Status Designer
- Service Eligibility Terms

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
- `SmartEmbedTextBase` --references--> `SmartTextEmbedAuthor`  [EXTRACTED]
  DiscordSupportBot/Common/SmartText/SmartEmbedText.cs → DiscordSupportBot/Common/SmartText/SmartTextEmbedAuthor.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Privacy Policy Privileged Intent Set** — privacy_policy_message_content_intent, privacy_policy_server_members_intent, privacy_policy_presence_intent [EXTRACTED 1.00]
- **Docker Compose Service Runtime Configuration** — docker_compose_discord_support_bot_service, docker_compose_data_volume, docker_compose_environment_file, docker_compose_restart_policy, docker_compose_host_gateway, docker_compose_bridge_network [EXTRACTED 1.00]

## Communities (75 total, 21 thin omitted)

### Community 0 - "Food Wheel Interactions"
Cohesion: 0.06
Nodes (42): DiscordSupportBot.Interaction.FoodWheel.Service, DiscordSupportBot.Interaction.FoodWheel, IInteractionContext, SlashCommand, Task, BlacklistModule, CustomModule, DrinkWheelModule (+34 more)

### Community 1 - "Activity Command Pipeline"
Cohesion: 0.06
Nodes (28): DiscordSupportBot.Interaction.Activity, DiscordSupportBot.Command, DiscordSupportBot.DataBase.Activity, CommandService, DiscordSocketClient, IServiceProvider, SocketMessage, Task (+20 more)

### Community 2 - "Private Thread Commands"
Cohesion: 0.08
Nodes (29): AutocompleteHandler, CommandContextType, DiscordSupportBot.Interaction.AutoCreatePrivateThread.Service, DiscordSupportBot.Interaction.AutoCreatePrivateThread, DiscordSupportBot.Interaction.Lottery, AutocompletionResult, IAutocompleteInteraction, IInteractionContext (+21 more)

### Community 3 - "Private Thread State"
Cohesion: 0.13
Nodes (22): Action, ButtonLockState, ChannelId, Dictionary, AutoCreatePrivateThreadConfig, Config, DiscordSocketClient, Func (+14 more)

### Community 4 - "Utility Admin Commands"
Cohesion: 0.07
Nodes (26): Attachment, DiscordSupportBot.Interaction.Utility, DiscordSupportBot.Interaction.Admin, DiscordSupportBot.Interaction.Admin.Service, DefaultMemberPermissions, RequireBotPermission, RequireContext, RequireUserPermission (+18 more)

### Community 5 - "Fund Management"
Cohesion: 0.11
Nodes (22): ChannelIds, DiscordSupportBot.Interaction.Fund.Service, DiscordSupportBot.Interaction.Fund, DeletedCount, IMessage, IUser, RequireContext, SlashCommand (+14 more)

### Community 6 - "Command Preconditions"
Cohesion: 0.22
Nodes (14): Alias, Command, DiscordSocketClient, IUser, RequireBotPermission, RequireContext, RequireUserPermission, Summary (+6 more)

### Community 7 - "Interaction Utilities"
Cohesion: 0.09
Nodes (18): Assembly, DateTime, DiscordSocketClient, EmbedBuilder, Func, IEmote, IEnumerable, IInteractionContext (+10 more)

### Community 8 - "Command Utilities"
Cohesion: 0.12
Nodes (17): Assembly, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IEnumerable, IMessage (+9 more)

### Community 9 - "Reaction Event Handling"
Cohesion: 0.14
Nodes (17): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper (+9 more)

### Community 10 - "Link Fixing"
Cohesion: 0.11
Nodes (15): DiscordSupportBot.Interaction.LinkFix, DiscordSupportBot.Interaction.LinkFix.Service, RequireContext, RequireUserPermission, SlashCommand, Task, LinkFix, ConcurrentDictionary (+7 more)

### Community 11 - "Help Command Matching"
Cohesion: 0.10
Nodes (15): DiscordSupportBot.Interaction.Help, CommandInfo, CommandTextEqualityComparer, Func, CommonEqualityComparer, HelpService, InteractionService, IServiceProvider (+7 more)

### Community 12 - "Privacy and Data"
Cohesion: 0.08
Nodes (26): ZSET-only Fund Storage, Data Minimization, Data Sharing Exceptions, Access and Deletion Rights, Data Use Purposes, Discord Identifiers, Discord Support Bot, Functional Data (+18 more)

### Community 13 - "Streaming Status Service"
Cohesion: 0.15
Nodes (12): DiscordSupportBot.Interaction.StreamingStatus.Services, DiscordSocketClient, HashSet, HttpClient, SocketUser, SocketVoiceChannel, SocketVoiceState, string (+4 more)

### Community 14 - "Normal Text Commands"
Cohesion: 0.20
Nodes (13): DiscordSupportBot.Command.Normal, Alias, Command, DiscordSocketClient, RequireContext, Summary, Task, Normal (+5 more)

### Community 15 - "Database Entities"
Cohesion: 0.12
Nodes (13): DiscordSupportBot.DataBase.Table, AutoCreatePrivateThreadConfig, DateTime, DbEntity, FoodWheelEntry, Guild, GuildConfig, LinkFixConfig (+5 more)

### Community 16 - "Text Replacement Utilities"
Cohesion: 0.18
Nodes (10): ConcurrentDictionary, DiscordSocketClient, Func, ICommandContext, IEnumerable, IMessageChannel, IUser, Regex (+2 more)

### Community 17 - "Help Text Commands"
Cohesion: 0.16
Nodes (12): DiscordSupportBot.Command.Help, Alias, Command, CommandService, IServiceProvider, string, Summary, Task (+4 more)

### Community 18 - "Project Dependencies"
Cohesion: 0.11
Nodes (15): net8.0, Ben.Demystifier (0.4.1), Dapper (2.1.66), Discord.Net (3.19.1), JsonExtensions (1.2.0), Microsoft.Data.Sqlite.Core (9.0.8), Microsoft.EntityFrameworkCore.Design (9.0.8), Microsoft.EntityFrameworkCore.Sqlite (9.0.8) (+7 more)

### Community 19 - "Interaction Dispatch"
Cohesion: 0.15
Nodes (12): DiscordSocketClient, IInteractionContext, InteractionService, IServiceProvider, SlashCommandInfo, SocketMessageCommand, SocketMessageComponent, Task (+4 more)

### Community 20 - "Private Thread Configuration"
Cohesion: 0.26
Nodes (10): Channel, Config, DiscordSocketClient, SlashCommand, SocketTextChannel, Task, AutoCreatePrivateThread, IMentionable (+2 more)

### Community 21 - "Bot Configuration Clients"
Cohesion: 0.13
Nodes (10): DiscordSupportBot.HttpClients, DiscordSupportBot, BotConfig, DiscordSocketClient, HttpClient, DiscordWebhookClient, Message, string (+2 more)

### Community 22 - "Honey Pot Moderation"
Cohesion: 0.16
Nodes (7): DiscordSupportBot.Interaction.Admin.HoneyPot, HoneyPot, DiscordSocketClient, HashSet, SocketMessage, Task, HoneyPotService

### Community 23 - "Smart Text Replacement"
Cohesion: 0.14
Nodes (6): DiscordSupportBot.Common, IEnumerable, Replacer, SmartTextEmbedAuthor, SmartTextEmbedField, SmartTextEmbedFooter

### Community 24 - "Database Model Snapshots"
Cohesion: 0.14
Nodes (7): DiscordSupportBot.DataBase, ModelBuilder, DeleteTimeChannel, ModelBuilder, NCChannel, ModelBuilder, AddFoodWheelEntry

### Community 25 - "Auto Voice Commands"
Cohesion: 0.22
Nodes (10): AutoVoiceChannelService, DiscordSupportBot.Interaction.AutoVoiceChannel, DefaultMemberPermissions, IVoiceChannel, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand (+2 more)

### Community 26 - "Voice Channel Events"
Cohesion: 0.23
Nodes (8): ChannelEvent, DiscordSocketClient, SocketUser, SocketVoiceChannel, SocketVoiceState, Task, AutoVoiceChannelService, IGuildUser

### Community 27 - "Entity Framework Context"
Cohesion: 0.15
Nodes (11): DbContext, DbContextOptionsBuilder, DbSet, AutoCreatePrivateThreadConfig, ModelBuilder, SupportContext, FoodWheelEntry, GuildConfig (+3 more)

### Community 28 - "Logging Infrastructure"
Cohesion: 0.23
Nodes (6): ConsoleColor, object, Task, Log, Exception, LogMessage

### Community 29 - "Streaming Status Commands"
Cohesion: 0.24
Nodes (9): DiscordSupportBot.Interaction.StreamingStatus, DefaultMemberPermissions, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand, Task, StreamingStatus (+1 more)

### Community 30 - "Administration Service"
Cohesion: 0.21
Nodes (7): DiscordSupportBot.Command.Administration, DiscordSocketClient, ITextChannel, SocketCommandContext, Task, AdministraionService, ResetType

### Community 31 - "Smart Embed Text"
Cohesion: 0.29
Nodes (5): EmbedBuilder, SmartEmbedArrayElementText, SmartEmbedText, SmartEmbedTextBase, IEmbed

### Community 32 - "Command Metadata Attributes"
Cohesion: 0.20
Nodes (8): Attribute, DiscordSupportBot.Interaction.Attribute, DiscordSupportBot.Interaction.Help.Service, NotRequirementAttribute, string, CommandExampleAttribute, string, CommandSummaryAttribute

### Community 33 - "Application Startup"
Cohesion: 0.22
Nodes (6): ConsoleCancelEventArgs, Console_CancelKeyPress(), Main(), TimerHandler(), TimerHandler5(), UpdateStatusFlags

### Community 34 - "Initial Database Migration"
Cohesion: 0.20
Nodes (6): Discord_Support_Bot.Migrations, MigrationBuilder, ModelBuilder, InitialCreate, InitialCreate, Migration

### Community 35 - "Current Database Snapshot"
Cohesion: 0.20
Nodes (6): DiscordSupportBot.Migrations, ModelBuilder, AddAutoCreatePrivateThreadMentions, ModelBuilder, SupportContextModelSnapshot, ModelSnapshot

### Community 36 - "Uptime Monitoring"
Cohesion: 0.24
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 37 - "Discord Privileged Intents"
Cohesion: 0.20
Nodes (10): Message Content Intent, Message Content Non-retention, Presence Intent, Discord Privileged Intents, Server Members Intent, Message Content Intent, No Regular Message Content Storage, Presence Intent (+2 more)

### Community 38 - "Guild Identity Commands"
Cohesion: 0.33
Nodes (5): DiscordSupportBot.Interaction.NC_Guild_Only, IUser, SlashCommand, Task, Id

### Community 39 - "Text Command Guild Guard"
Cohesion: 0.22
Nodes (7): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute, PreconditionAttribute

### Community 40 - "Smart Text Arrays"
Cohesion: 0.28
Nodes (4): EmbedBuilder, SmartEmbedTextArray, SmartPlainText, SmartText

### Community 41 - "Honey Pot Commands"
Cohesion: 0.33
Nodes (7): DefaultMemberPermissions, ITextChannel, RequireBotPermission, RequireContext, RequireUserPermission, SlashCommand, Task

### Community 42 - "Redis Connection"
Cohesion: 0.25
Nodes (6): ConnectionMultiplexer, string, RedisConnection, IDatabase, IServer, Lazy

### Community 43 - "Voice Channel Service"
Cohesion: 0.25
Nodes (6): DiscordSupportBot.Interaction.AutoVoiceChannel.Services, ConcurrentDictionary, HashSet, IVoiceChannel, ChannelEvent, Ext

### Community 44 - "Message Channel Extensions"
Cohesion: 0.36
Nodes (6): IMessageChannel, IReadOnlyCollection, IUserMessage, Task, Embed, MessageComponent

### Community 45 - "Interaction Guild Guard"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute

### Community 46 - "Guild Size Guard"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 47 - "Guild Owner Guard"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 48 - "Docker Deployment"
Cohesion: 0.25
Nodes (8): Bridge Network Mode, Data Volume Mount, discord-support-bot Service, Environment File Configuration, Host Docker Internal Gateway, Unless-stopped Restart Policy, Bot Data Persistence, Docker Compose Deployment

### Community 50 - "Help Rendering Service"
Cohesion: 0.57
Nodes (3): EmbedBuilder, SlashCommandInfo, HelpService

### Community 52 - "Bot Features Overview"
Cohesion: 0.40
Nodes (6): Bot Invitation, Jun112561, Administration Features, Discord Support Bot, Emote Usage Statistics, Message Activity Statistics

### Community 53 - "Array Extensions"
Cohesion: 0.33
Nodes (3): Func, IReadOnlyCollection, ArrayExtensions

### Community 54 - "Interactive User Prompts"
Cohesion: 0.53
Nodes (4): Task, TopLevelModule, InteractionModuleBase, SocketInteractionContext

### Community 74 - "Service Eligibility Terms"
Cohesion: 0.50
Nodes (4): Children's Privacy, Discord Community Guidelines, Discord Terms of Service, Service Eligibility

## Knowledge Gaps
- **69 isolated node(s):** `ResetType`, `Guild`, `PlayerPlatform`, `net8.0`, `Ben.Demystifier (0.4.1)` (+64 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **21 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordSupportBot.DataBase` connect `Database Model Snapshots` to `Activity Command Pipeline`, `Nitro Migration Designer`, `Lottery Migration Designer`, `Misc Migration Designer`, `Twitter Migration Designer`, `Honey Pot Designer`, `Link Fix Designer`, `Streaming Status Designer`, `Current Database Snapshot`?**
  _High betweenness centrality (0.166) - this node is a cross-community bridge._
- **Why does `SupportContext` connect `Entity Framework Context` to `Database Model Snapshots`, `Voice Channel Events`?**
  _High betweenness centrality (0.127) - this node is a cross-community bridge._
- **Why does `DiscordSupportBot.Command` connect `Activity Command Pipeline` to `Text Command Guild Guard`?**
  _High betweenness centrality (0.124) - this node is a cross-community bridge._
- **What connects `ResetType`, `Guild`, `PlayerPlatform` to the rest of the system?**
  _73 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Food Wheel Interactions` be split into smaller, more focused modules?**
  _Cohesion score 0.06390977443609022 - nodes in this community are weakly interconnected._
- **Should `Activity Command Pipeline` be split into smaller, more focused modules?**
  _Cohesion score 0.05519480519480519 - nodes in this community are weakly interconnected._
- **Should `Private Thread Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.08350951374207188 - nodes in this community are weakly interconnected._