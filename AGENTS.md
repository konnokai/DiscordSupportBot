# Repository Instructions

## Project Shape

- This is a .NET 8 C# Discord bot. `DiscordSupportBot.sln` contains the executable project and the xUnit project `DiscordSupportBot.Tests`.
- `DiscordSupportBot/Program.cs` is the entrypoint. It initializes configuration, SQLite, Redis, and both Discord command frameworks.
- Slash commands and interaction services are under `DiscordSupportBot/Interaction`; legacy prefix commands are under `DiscordSupportBot/Command` and use the `!!!` prefix.
- Modules are loaded from the executable entry assembly. Interaction services implementing `IInteractionService` and command services implementing `ICommandService` are discovered by the existing reflection/DI setup.

## Commands

Run from the repository root:

```text
dotnet restore
dotnet build DiscordSupportBot.sln --configuration Release
dotnet test DiscordSupportBot.Tests/DiscordSupportBot.Tests.csproj --configuration Release
dotnet test DiscordSupportBot.Tests/DiscordSupportBot.Tests.csproj --configuration Release --filter "FullyQualifiedName~<test-or-class-name>"
```

## Runtime

- Local run: `dotnet run --project DiscordSupportBot`.
- Non-Docker runs require root `bot_config.json`. If it is missing, startup writes `bot_config_example.json` and exits; `DiscordToken` and `WebHookUrl` are required, and `RedisOption` defaults to `127.0.0.1:6379,syncTimeout=3000`.
- Docker Compose reads root `.env`, mounts root `./Data` to `/app/Data`, and uses `host.docker.internal` for host services. Use `docker compose up -d`, `docker compose logs -f`, and `docker compose down`.
- Runtime data paths are relative to the executable base directory. Local runs use `DiscordSupportBot/bin/<Configuration>/net8.0/Data`; Docker uses `/app/Data`.
- Enable Discord Message Content and Server Members privileged intents. Presence is requested only when `IsEnablePresenceIntent` is enabled.
- Debug slash-command registration uses `TestSlashCommandGuildId` for fast guild-scoped testing. Release registration is gated by `Data/CommandSignature.bin`; command name, description, or parameter changes trigger registration.
- Never commit `bot_config.json` or `.env`; use `.env_sample` for the variable names.

## Persistence

- `RedisConnection` always uses Redis database 2. Fund leaderboards are ZSET-only; do not add a Hash as a second fund-storage source.
- SQLite files and runtime artifacts are under the runtime `Data` directory. Startup calls `EnsureCreated()` only when `Data/DataBase.db` is absent and does not apply EF migrations automatically; handle schema changes explicitly and keep `DiscordSupportBot/Migrations` consistent.

## Graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- Do not automatically run `graphify update .` after modifying code. Remind the user to run it manually to keep the graph current.
- `graphify-out/` is a local build artifact and is ignored by `.gitignore`. Never stage or commit files under it.
