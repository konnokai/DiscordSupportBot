# 隱私權政策 / Privacy Policy

> 中文版本於上半部，English version below.

---

# 隱私權政策（繁體中文）

**適用對象：** 輔助小幫手（Discord Support Bot，以下簡稱「本 Bot」）
**最後更新日期：** 2026 年 6 月 12 日

本隱私權政策說明本 Bot 在您使用其功能時，會收集哪些資料、如何使用與儲存，以及您可如何要求刪除。使用本 Bot 即表示您同意本政策之內容。

## 1. 我們收集的資料

本 Bot 僅收集為提供各項功能所**必要**之資料，包含：

### 1.1 Discord 識別碼（IDs）
- 使用者 ID（User ID）
- 伺服器 ID（Guild ID）
- 頻道 ID（Channel ID）、訊息 ID（Message ID）

### 1.2 功能性資料
| 資料類型 | 來源功能 | 內容 |
|----------|----------|------|
| 發言計數 | 活躍度統計 | 各使用者於各伺服器的發言次數（僅次數，**不含訊息內容**） |
| 表情使用計數 | 表情統計 | 各伺服器自訂表情之使用次數 |
| 基金點數 | 基金系統 | 各使用者於各伺服器的點數分數 |
| 抽獎資料 | 抽獎系統 | 抽獎參與者的使用者 ID 清單、由管理員自行輸入之獎項說明文字 |
| 伺服器設定 | 各管理功能 | 自動語音頻道、成員數頻道、Nitro 數頻道、蜜罐頻道、連結修正網域對應、通知頻道等設定 |
| 使用者註冊資料 | `/id register`（特定伺服器專屬） | 使用者**主動提供**之遊戲帳號 ID 與遊玩平台 |

### 1.3 我們**不會**收集或長期儲存的資料
- 一般訊息之文字內容、附件、圖片。
- 訊息內容僅在處理當下被**即時讀取**（用於解析指令、計算表情使用次數、偵測並修正連結），處理完畢後即不予保留。
- 私人對話內容、密碼、付款資訊等敏感個人資料。

## 2. 我們如何使用這些資料

收集之資料僅用於下列目的：

- 提供並運作本 Bot 之各項功能（抽獎、排行榜、統計、連結修正、管理功能等）。
- 維持各伺服器的個別設定。
- 進行必要的錯誤排除與功能維護。

本 Bot **不會**將您的資料販售、出租或分享予任何第三方以作行銷用途。

## 3. 為何需要 Discord 特權 Intent

為提供上述功能，本 Bot 申請並使用下列 Discord 特權 Intent：

- **Message Content（訊息內容）**：用於前綴指令解析、連結修正功能讀取訊息中的網址，以及統計訊息中自訂表情的使用次數。訊息內容僅即時處理，不予儲存。
- **Server Members（伺服器成員）**：用於抽獎隨機抽選成員、顯示伺服器成員數、蜜罐功能踢出成員，以及監聽成員身分組變更以進行自動身分組管理。

本 Bot **未使用** Presence（在線狀態）Intent。

## 4. 資料儲存與安全

- 資料儲存於本 Bot 擁有者所管理之伺服器，使用 **Redis** 與 **SQLite** 資料庫保存。
- 本 Bot 擁有者採取合理之技術與管理措施保護所儲存之資料，惟無法保證網際網路傳輸或儲存之絕對安全。
- 資料僅保留至提供功能所需之期間；部分統計資料（如基金排行榜）可能依功能設計定期重置。

## 5. 資料分享

除下列情形外，本 Bot 不會對外揭露您的資料：

- 為遵循法律義務、法院命令或主管機關之合法要求。
- 為保護本 Bot、其使用者或公眾之權利、財產或安全之必要。

## 6. 您的權利

您可以要求：

- **查詢**本 Bot 所保存之關於您（或您伺服器）的資料。
- **刪除**您的資料。

行使方式：

- 將本 Bot 移除出伺服器，可停止後續資料收集；伺服器相關設定資料將失效。
- 如需主動刪除特定資料（例如 `/id register` 註冊之遊戲帳號資料、基金點數等），請透過下方聯絡方式提出請求。我們將在合理期間內處理。

## 7. 兒童隱私

本 Bot 不針對未達 Discord 服務條款所定最低年齡之使用者提供服務，亦不會在知情情況下收集該等使用者之資料。

## 8. 政策變更

本政策可能隨時更新。修改後之版本將於本文件發布時生效，並更新上方之「最後更新日期」。建議您定期查閱本政策。

## 9. 聯絡方式

如對本隱私權政策、或對您資料之處理有任何疑問或請求，請聯絡：

- **Email：** contact@konnokai.me
- **開發者：** konnokai
- **專案：** https://github.com/konnokai/DiscordSupportBot

---

*本文件僅供本 Bot 使用，並非法律意見。如有正式法律需求，建議諮詢專業律師。*

<br>

---

# Privacy Policy (English)

**Applies to:** Discord Support Bot (the "Bot")
**Last updated:** June 12, 2026

This Privacy Policy explains what data the Bot collects when you use its features, how that data is used and stored, and how you can request its deletion. By using the Bot, you agree to this Policy.

## 1. Data We Collect

The Bot collects only the data **necessary** to provide its features, namely:

### 1.1 Discord Identifiers (IDs)
- User IDs
- Guild (server) IDs
- Channel IDs and Message IDs

### 1.2 Functional Data
| Data type | Source feature | Contents |
|-----------|----------------|----------|
| Message counts | Activity statistics | Per-user message counts per server (counts only — **no message content**) |
| Emote usage counts | Emote statistics | Usage counts of each server's custom emotes |
| Fund points | Fund system | Per-user point scores per server |
| Giveaway data | Giveaway system | Participant user-ID lists and prize description text entered by administrators |
| Server settings | Administrative features | Auto voice channel, member-count channel, Nitro-count channel, honeypot channel, link-fix domain mappings, notification channels, etc. |
| User-registered data | `/id register` (server-specific) | Game account ID and platform that users **voluntarily** provide |

### 1.3 Data We Do **Not** Collect or Store Long-Term
- The text content, attachments, or images of regular messages.
- Message content is only **read in real time** during processing (to parse commands, count emote usage, and detect/fix links) and is not retained afterwards.
- Private conversation content, passwords, payment information, or other sensitive personal data.

## 2. How We Use Data

Collected data is used solely to:

- Provide and operate the Bot's features (giveaways, leaderboards, statistics, link fixing, administrative features, etc.).
- Maintain each server's individual settings.
- Perform necessary troubleshooting and maintenance.

The Bot does **not** sell, rent, or share your data with any third party for marketing purposes.

## 3. Why Privileged Intents Are Needed

To provide the features above, the Bot requests and uses the following Discord privileged intents:

- **Message Content** — used for prefix-command parsing, for the link-fix feature to read URLs within messages, and to count custom-emote usage in messages. Message content is processed in real time and is not stored.
- **Server Members** — used to randomly select members for giveaways, display server member counts, kick members for the honeypot feature, and listen for member role changes to perform automatic role management.

The Bot does **not** use the Presence intent.

## 4. Data Storage and Security

- Data is stored on infrastructure managed by the Bot owner, using **Redis** and **SQLite** databases.
- The Bot owner applies reasonable technical and organizational measures to protect stored data, but cannot guarantee the absolute security of internet transmission or storage.
- Data is retained only for as long as needed to provide the relevant feature; some statistics (such as the fund leaderboard) may be periodically reset by design.

## 5. Data Sharing

The Bot will not disclose your data externally except:

- To comply with a legal obligation, court order, or lawful request from a competent authority.
- Where necessary to protect the rights, property, or safety of the Bot, its users, or the public.

## 6. Your Rights

You may request to:

- **Access** the data the Bot holds about you (or your server).
- **Delete** your data.

How to exercise these rights:

- Removing the Bot from a server stops further data collection; that server's settings data becomes inactive.
- To proactively delete specific data (for example, game-account data registered via `/id register`, or fund points), please submit a request via the contact details below. We will process it within a reasonable period.

## 7. Children's Privacy

The Bot is not directed at users below the minimum age required by the Discord Terms of Service, and does not knowingly collect data from such users.

## 8. Changes to This Policy

This Policy may be updated at any time. The updated version takes effect upon publication of this document, and the "Last updated" date above will be revised accordingly. We encourage you to review this Policy periodically.

## 9. Contact

If you have any questions or requests regarding this Privacy Policy or the handling of your data, please contact:

- **Email:** contact@konnokai.me
- **Developer:** konnokai
- **Project:** https://github.com/konnokai/DiscordSupportBot

---

*This document is provided solely for use with the Bot and does not constitute legal advice. For formal legal needs, please consult a qualified attorney.*
