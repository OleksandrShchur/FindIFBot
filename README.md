# FindIFBot

Telegram bot for the **«Франківськ Питає»** community channel. Users submit publication requests through the bot; moderators review and approve them before they are posted to the channel.

Built on **ASP.NET Core 9** with a webhook-based update pipeline, **Entity Framework Core** persistence, and an admin moderation workflow.

## Features

- **Ask flow** — users send text and up to 10 photos; submissions are validated, confirmed, and forwarded to admins for review
- **Channel subscription check** — users must subscribe to the output channel before starting the ask flow
- **Admin moderation** — inline callbacks to approve, reject, or mark submissions as duplicates; approved posts are published to the channel
- **Request history** — users can view their past submissions grouped by status (`/history`)
- **Media groups** — photo albums are buffered and processed as a single submission
- **Durable pending submissions** — in-progress ask content survives app restarts (stored in SQL Server)
- **Operational tooling** — Serilog file logging, Telegram log forwarding, and maintenance endpoints for daily log export and statistics

### User commands

| Command | Description |
|---------|-------------|
| `/start` | Welcome message and reply keyboard |
| `/ask` | Start a publication request |
| `/history` | View submission history |
| `/help` | Bot help |
| `/policy` | Community rules |
| `/support` | Support the project |
| `/channel` | Link to the channel |

Commands are also available via Ukrainian reply-keyboard buttons.

## Tech stack

- .NET 9 / ASP.NET Core Web API
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) 22.x
- Entity Framework Core + SQL Server
- Serilog (rolling daily file logs)
- Built-in ASP.NET Core rate limiting
- xUnit, NSubstitute, FluentAssertions (tests)

## Project structure

```
FindIFBot/
├── FindIFBot/              # Main web application
│   ├── Controllers/        # Webhook, health check, maintenance
│   ├── Handlers/           # Command handlers (/start, /history, …)
│   ├── Services/           # Ask flow, admin workflow, message dispatch
│   ├── EF/                 # DbContext, entities, repositories, migrations
│   └── Configuration/      # Typed options (Telegram, Submission, History, …)
├── tests/
│   ├── FindIFBot.UnitTests/
│   └── FindIFBot.IntegrationTests/
└── FindIFBot.sln
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or remote)
- Telegram Bot Token ([BotFather](https://t.me/BotFather))
- [ngrok](https://ngrok.com/) (for local webhook development)

## Configuration

Copy `FindIFBot/appsettings.json` values into `FindIFBot/appsettings.Development.json` (gitignored) for local secrets, or use environment variables / user secrets.

### `Telegram`

| Key | Description |
|-----|-------------|
| `BotToken` | Bot token from BotFather (**required**) |
| `AdminId` | Telegram user ID of the admin moderator |
| `UserOutputChannel` | Channel ID or `@username` where approved posts are published |
| `LinkToChannel` | Public link shown to users |
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

Migrations live in `FindIFBot/Migrations/`. The schema includes user sessions, request history, and pending submissions.

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
| `POST` | `/api/maintenance/daily-statistics` | Send daily statistics to Telegram |

Maintenance endpoints require the `X-Maintenance-Key` header matching `Maintenance:SecretKey` and are rate-limited to 5 requests per minute per IP.

## Testing

```bash
dotnet test FindIFBot.sln
```

- **Unit tests** — controllers, dispatchers, ask flow, admin publishing, templates
- **Integration tests** — HTTP endpoints and EF repositories (SQLite in-memory)

## Architecture overview

```
Telegram → POST /api/telegram/webhook
         → CommandDispatcher
              ├─ CallbackQuery → AskFlowService / AdminWorkflowService
              └─ Message       → MessageDispatchService
                                    ├─ command routing (/start, /history, …)
                                    ├─ ask flow (session state machine)
                                    └─ media group buffering → admin notification
```

Logs are written to `FindIFBot/logs/` (rolling daily files, 10-day retention).

## License

MIT — see [LICENSE](LICENSE).
