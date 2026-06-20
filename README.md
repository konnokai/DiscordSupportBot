# 輔助小幫手（Discord Support Bot）

一套以 [Discord.Net](https://github.com/discord-net/Discord.Net) 開發的多功能 Discord 社群輔助機器人，提供抽獎、基金點數、活躍度統計、連結修正、自動頻道與多項伺服器管理功能。

> [!NOTE]
> 本專案包含大量**特定伺服器專用**的指令與寫死的 ID，建議自行 fork、修改後再編譯部署。

## ✨ 功能總覽

| 分類 | 功能 | 主要指令 |
|------|------|----------|
| 🎲 抽獎 | 建立／開獎／刪除抽獎、顯示參與名單 | `/lottery create-lottery`、`start-lottery`、`delete-lottery`、`show-participant-list` |
| 💰 基金 | 對成員加基金、單一／全部排行榜、每月自動重置 | `/add-fund`、`/fund-leaderboard`、`/all-fund-leaderboard`、訊息選單「對該訊息的作者添加基金」 |
| 📊 統計 | 發言活躍度、表情使用排行與使用量 | `/message-activity`、`/emote-activity`、`/emote-use-count` |
| 🍔 隨機 | 食物轉盤 | `/food-wheel` |
| 🔗 連結修正 | 自動將無法預覽的網址轉換為可嵌入版本 | `/link-fix`、`/link-fix-list` |
| 🔊 語音 | 加入指定頻道後自動建立專屬語音頻道 | `/set-auto-voice-channel`、`/remove-auto-voice-channel` |
| 🔴 直播狀態 | 成員於語音頻道直播時自動設定頻道狀態，停播自動清除 | `/toggle-streaming-status`、`/set-streaming-status-template` |
| 🧵 討論串 | 按鈕觸發自動建立私密討論串 | `/auto-create-private-thread` |
| 🛡️ 管理 | 蜜罐反洗版、自動身分組、Bot 代發／編輯訊息 | `/set-honeypot`、`/remove-honeypot`、`/auto-grant-role`、`/send-message-to-this-channel`、`/edit-message` |
| 🎮 專屬 | 遊戲帳號 Id 註冊／查詢（特定伺服器） | `/id register`、`/id search`、`/id set-platform` |
| 🧰 工具 | 延遲檢測、狀態、邀請連結、說明系統 | `/utility ping`、`/utility status`、`/utility invite`、`/help ...` |

此外亦包含背景自動作業：自動更新 Bot 狀態、成員數／Nitro 數顯示頻道、定期儲存統計資料、每月重置基金排行榜等。

## 🔐 需要的 Discord 特權 Intent

於 [Discord Developer Portal](https://discord.com/developers/applications) 的 **Bot → Privileged Gateway Intents** 需開啟：

- **Message Content Intent** — 前綴指令解析、連結修正、表情使用統計
- **Server Members Intent** — 抽獎抽選、成員數顯示、蜜罐踢出、自動身分組
- **Presence Intent** — 偵測成員在語音頻道的直播狀態，以自動設定／清除頻道狀態

> 詳見 [服務條款](./TERMS_OF_SERVICE.md) 與 [隱私權政策](./PRIVACY_POLICY.md)。

## 🛠️ 技術棧

- .NET 8 / C#
- [Discord.Net](https://github.com/discord-net/Discord.Net) 3.19
- Redis（[StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)）— 即時統計與排行榜
- SQLite + Entity Framework Core 9 — 持久化設定與統計資料

## 📦 環境需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- 一個可連線的 [Redis](https://redis.io/) 伺服器
- 一個 Discord Bot Token（[如何取得](https://discord.com/developers/applications)）

## ⚙️ 設定

首次啟動會自動產生 `bot_config_example.json`。請複製為 `bot_config.json` 並填入設定：

```json
{
  "DiscordToken": "你的 Discord Bot Token",
  "WebHookUrl": "用於接收 Bot 通知的 Discord Webhook 連結",
  "RedisOption": "127.0.0.1:6379,syncTimeout=3000",
  "TestSlashCommandGuildId": 0,
  "UptimeKumaPushUrl": null
}
```

| 欄位 | 必填 | 說明 |
|------|:---:|------|
| `DiscordToken` | ✅ | Discord Bot Token |
| `WebHookUrl` | ✅ | Discord Webhook，用於 Bot 加入伺服器等事件通知 |
| `RedisOption` | ✅ | Redis 連線字串（預設 `127.0.0.1:6379,syncTimeout=3000`） |
| `TestSlashCommandGuildId` | | 測試用伺服器 ID，設定後斜線指令僅註冊至該伺服器以便即時測試 |
| `UptimeKumaPushUrl` | | [Uptime Kuma](https://github.com/louislam/uptime-kuma) 推送監控網址 |

## 🚀 執行

### 本機執行

```bash
dotnet restore
dotnet run --project DiscordSupportBot
```

### Docker Compose（推薦）

專案已附 [`docker-compose.yml`](./docker-compose.yml)，設定透過 `.env` 檔載入。

1. 於專案根目錄建立 `.env`（變數名稱與上方 JSON 欄位相同）：

   ```dotenv
   DiscordToken=你的 Discord Bot Token
   WebHookUrl=你的 Discord Webhook 連結
   RedisOption=host.docker.internal:6379,syncTimeout=3000
   ```

2. 啟動：

   ```bash
   docker compose up -d
   ```

   查看日誌 / 停止：

   ```bash
   docker compose logs -f
   docker compose down
   ```

> - `./Data` 會掛載至容器 `/app/Data`，SQLite 等持久化資料存放於此。
> - 已設定 `restart: unless-stopped` 自動重啟，並透過 `host.docker.internal` 連線至宿主機服務（如本機 Redis）。

### Docker（手動）

不使用 Compose 時，設定改由**環境變數**讀取（變數名稱與上方 JSON 欄位相同）。

```bash
docker build -t discord-support-bot .

docker run -d --name discord-support-bot \
  --restart unless-stopped \
  -v "$(pwd)/Data:/app/Data" \
  -e DiscordToken="你的 Token" \
  -e WebHookUrl="你的 Webhook" \
  -e RedisOption="host.docker.internal:6379,syncTimeout=3000" \
  discord-support-bot
```

> 容器時區預設為 `Asia/Taipei`。

## 🗄️ 資料儲存

- **Redis**：發言／表情即時計數、基金排行榜（SortedSet）、通知頻道集合等。
- **SQLite**：伺服器設定（`GuildConfig`）、連結修正規則（`LinkFixConfig`）、抽獎（`Lottery`）、遊戲帳號註冊（`NCChannelCOD`）、表情與使用者活躍度彙整等。

收集的資料以 Discord ID 與各類計數／分數為主，**不儲存一般訊息內容**。詳見[隱私權政策](./PRIVACY_POLICY.md)。

## 📄 法律文件

- [服務條款 / Terms of Service](./TERMS_OF_SERVICE.md)
- [隱私權政策 / Privacy Policy](./PRIVACY_POLICY.md)

## 📬 聯絡

- **開發者：** konnokai
- **Email：** contact@konnokai.me
- **網站：** https://konnokai.me
