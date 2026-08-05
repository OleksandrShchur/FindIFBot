# FindIFBot

[![build-and-test](https://github.com/OleksandrShchur/FindIFBot/actions/workflows/ci.yml/badge.svg)](https://github.com/OleksandrShchur/FindIFBot/actions/workflows/ci.yml)

Telegram bot for the **«Франківськ Питає»** community channel. Users submit publication requests through the bot; moderators review and approve them before they are posted to the channel.

Built on **ASP.NET Core 9** with a webhook-based update pipeline, **Entity Framework Core** persistence, and an admin moderation workflow.

## Features

- **Ask flow** — users send text and up to 10 photos; submissions are validated, confirmed, and forwarded to admins for review
- **Text formatting & links** — inline formatting and `text_link` entities are preserved through storage, admin review, and channel publish
- **Back to menu** — ask flow includes a main-menu button so users can exit without finishing a submission
- **Channel subscription check** — users must subscribe to the output channel before starting the ask flow
- **Request IDs** — every submission gets a stable ID shown to the user and to admins across moderation messages
- **Working hours messaging** — after submit, users see an immediate-review note during Kyiv 09:00–22:00, or an off-hours notice outside that window
- **Admin moderation** — action buttons live on a separate message; approve, reject, duplicate, ads, or needs-attention; approved posts are published to the channel
- **Admin context tables** — each moderation thread includes user profile and per-user request status counts (HTML tables)
- **Moderation queue** — admins can list pending submissions with links back to the original moderation thread (`/pending`)
- **Request history** — users see a status-count summary plus past submissions grouped by status (`/history`)
- **Ads & collaboration** — dedicated policy and direct-chat link for advertising inquiries (`/ads`)
- **Bot version** — `/version` reports the current bot build number (`BotVersion.Current`)
- **Media groups** — photo albums are buffered and processed as a single submission
- **Durable pending submissions** — in-progress ask content (including message entities) survives app restarts (stored in SQL Server)
- **Operational tooling** — Serilog file logging, Telegram log forwarding, and maintenance endpoints for daily log export and statistics

### User commands

| Command | Description |
|---------|-------------|
| `/start` | Welcome message and reply keyboard |
| `/ask` | Start a publication request |
| `/history` | View status summary and submission history |
| `/ads` | Advertising and collaboration policy with direct-chat link |
| `/help` | Bot help |
| `/policy` | Community rules |
| `/support` | Support the project |
| `/channel` | Link to the channel |
| `/version` | Current bot version |
| `/pending` | Admin only — list submissions awaiting moderation |

Commands are also available via Ukrainian reply-keyboard buttons (except `/version`).

## Tech stack

- .NET 9 SDK (pinned in `global.json`, currently **9.0.316**) / ASP.NET Core Web API
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) **22.10.2.1**
- Entity Framework Core **9.0.12** + SQL Server
- Serilog (rolling daily file logs, 10-day retention)
- Built-in ASP.NET Core rate limiting (50 requests / 10 s globally; 5 / min on maintenance endpoints)
- xUnit, NSubstitute, FluentAssertions, `Microsoft.AspNetCore.Mvc.Testing` (tests)

## Project structure

```
FindIFBot/
├── .github/
│   ├── workflows/ci.yml    # build-and-test on push/PR to master
│   └── scripts/            # branch protection helper (gh CLI)
├── FindIFBot/              # Main web application
│   ├── Controllers/        # Webhook, health check, maintenance
│   ├── Handlers/           # Command handlers (/start, /history, /version, …)
│   ├── Services/           # Ask flow, admin workflow, message dispatch
│   ├── Helpers/            # BotCommands, BotVersion, KyivWorkingHours, keyboards
│   ├── EF/                 # DbContext, entities, repositories, migrations
│   └── Configuration/      # Typed options (Telegram, Submission, History, …)
├── tests/
│   ├── FindIFBot.UnitTests/
│   └── FindIFBot.IntegrationTests/
├── global.json             # Pinned .NET SDK version
└── FindIFBot.sln
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or remote)
- Telegram Bot Token ([BotFather](https://t.me/BotFather))
- [ngrok](https://ngrok.com/) (for local webhook development)

## Configuration

Copy `FindIFBot/appsettings.json` values into a local override such as `FindIFBot/appsettings.Development.json`, `appsettings.dev.json`, or `appsettings.prod.json` (all gitignored) for secrets, or use environment variables / user secrets.

### `Telegram`

| Key | Description |
|-----|-------------|
| `BotToken` | Bot token from BotFather (**required**) |
| `AdminId` | Telegram user ID of the admin moderator |
| `UserOutputChannel` | Channel ID or `@username` where approved posts are published |
| `LinkToChannel` | Public link shown to users |
| `DirectChatLink` | Direct-message link for ads/collaboration and moderation follow-ups |
| `ChatInviteLink` | Invite link used during subscription checks |
| `BotUsername` | Bot username (without `@`) |
| `LogsOutputChannel` | Channel for operational logs |
| `LogsThreadId` | Forum topic ID for general logs |
| `ErrorLogsThreadId` | Forum topic ID for error logs |
| `AllMessagesThreadId` | Forum topic ID for all-message logs |
| `RetryMaxAttempts` | Telegram API retry count |
| `BankLink` | Support payment link |
| `CardNumber` | Support card number |

### `ConnectionStrings`

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=...;Database=FindIFBot;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### Other sections

```json
"Submission": {
  "MaxCaptionLength": 970,
  "MaxTextLength": 4040,
  "MaxAlbumPhotoCount": 10
},
"History": {
  "MaxItemsPerSection": 10
},
"Maintenance": {
  "SecretKey": "<secret-for-maintenance-endpoints>"
}
```

## Database setup

Apply EF Core migrations before first run:

```bash
dotnet ef database update --project FindIFBot/FindIFBot.csproj
```

Migrations live in `FindIFBot/Migrations/`. The schema includes user sessions, request history (with `PublishedAtUtc` for channel publish time), pending submissions (with `EntitiesJson` for preserved text entities), admin moderation message references (`AdminInfoMessageId`), and daily channel statistics (`ChannelDailyStatistics`).

For DB-first / manual SQL Server updates, equivalent scripts live in `FindIFBot/Migrations/Scripts/` (apply the latest script, then keep `__EFMigrationsHistory` in sync).

## Build and run

```bash
dotnet build FindIFBot.sln
dotnet run --project FindIFBot/FindIFBot.csproj --launch-profile http
```

The app listens on **http://localhost:5199** when using the `http` launch profile.

## Local webhook setup

Telegram delivers updates via webhook. For local development, expose the app with ngrok.

### 1. Start ngrok

```bash
ngrok http 5199
```

Copy the generated `https://*.ngrok-free.app` URL.

### 2. Register the Telegram webhook

```powershell
Invoke-WebRequest `
  -Method Post `
  -Uri "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" `
  -Body @{ url = "https://<ngrok-id>.ngrok-free.app/api/telegram/webhook" }
```

Replace `<YOUR_BOT_TOKEN>` and `<ngrok-id>` with your values.

### 3. Run the application

```bash
dotnet run --project FindIFBot/FindIFBot.csproj --launch-profile http
```

The bot is reachable by Telegram through the ngrok tunnel.

> **Note:** The free ngrok URL changes on every restart unless you use a paid plan. Re-run `setWebhook` whenever the URL changes.

## HTTP endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/telegram/webhook` | Telegram update receiver (always returns `200 OK`) |
| `GET`, `HEAD` | `/api/healthcheck` | Health check |
| `POST` | `/api/maintenance/process-yesterday-logs` | Upload yesterday's log files to Telegram |
| `POST` | `/api/maintenance/daily-statistics` | Snapshot Kyiv-day channel stats to DB and send a table summary to the logs channel |

Maintenance endpoints require the `X-Maintenance-Key` header matching `Maintenance:SecretKey` and are rate-limited to 5 requests per minute per IP.

`daily-statistics` records one row per Kyiv calendar day (`BotUserCount`, `ChannelSubscriberCount`, `PostsCount`) and upserts on retry. Cron should run late in the Kyiv evening so “posts today” is nearly complete. Channel post views are not tracked (not available via Bot API).

## CI

GitHub Actions runs **build-and-test** on every push and pull request to `master`:

1. Restore `FindIFBot.sln` using the SDK from `global.json`
2. Release build
3. Run all unit and integration tests

Workflow: [`.github/workflows/ci.yml`](.github/workflows/ci.yml)

To require the check on `master` (one-time, after CI has run at least once):

```powershell
pwsh .github/scripts/configure-branch-protection.ps1
```

## Testing

```bash
dotnet test FindIFBot.sln
```

- **Unit tests** — controllers, dispatchers, ask flow, admin publishing/moderation, ads/collaboration, history stats, working hours, version, admin pending queue, keyboards
- **Integration tests** — HTTP endpoints and EF repositories (SQLite in-memory)

## Architecture overview

```
Telegram → POST /api/telegram/webhook
         → CommandDispatcher
              ├─ CallbackQuery → AskFlowService / AdminWorkflowService
              └─ Message       → MessageDispatchService
                                    ├─ command routing (/start, /history, /version, …)
                                    ├─ ask flow (session state machine)
                                    └─ media group buffering → admin notification
                                                         → user notify (working-hours aware)
```

Working hours are evaluated in Europe/Kyiv (09:00–22:00) via `KyivWorkingHours`. Bot build number is centralized in `Helpers/BotVersion.cs`.

Logs are written to `FindIFBot/logs/` (rolling daily files, 10-day retention).

## License

MIT — see [LICENSE](LICENSE).
