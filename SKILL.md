---
name: findifbot-maintainer
summary: Guidance for making safe, consistent changes in FindIFBot (ASP.NET Core Telegram webhook bot).
---

# FindIFBot Skill

## Mission
Maintain and extend FindIFBot as a webhook-driven Telegram bot with ASP.NET Core, preserving current behavior for message routing, ask flow, and admin moderation.

## Tech Stack
- .NET 9 (`net9.0`)
- ASP.NET Core Web API controllers
- Telegram.Bot SDK
- Entity Framework Core + SQL Server
- Serilog file logging
- Built-in rate limiting middleware

## Startup And DI
- Entry point: `FindIFBot/Program.cs`
- Controllers enabled with `AddControllers()` and mapped via `app.MapControllers()`.
- `TelegramOptions` is validated on startup (BotToken must be present).
- `MaintenanceOptions` is bound from configuration.
- Global IP rate limit: 50 requests / 10 seconds.
- Maintenance policy rate limit: 5 requests / minute.
- Uses `UseSerilogRequestLogging()`, `UseHttpsRedirection()`, `UseAuthorization()`, `UseRateLimiter()`.

## Runtime Flow
1. Telegram sends updates to `POST /api/telegram/webhook`.
2. `TelegramWebhookController` forwards to `ICommandDispatcher`.
3. `CommandDispatcher` routes:
   - Callback queries to ask flow or admin workflow.
   - Messages to `IMessageDispatchService`.
4. `MessageDispatchService` handles:
   - media group buffering,
   - session-aware ask flow,
   - command routing,
   - validation and confirmation before submission.
5. History and sessions are persisted with EF repositories.

## Key Modules
- `FindIFBot/Controllers`: webhook, health, maintenance endpoints.
- `FindIFBot/Services/Admin`: admin callbacks, moderation, publishing, notifications.
- `FindIFBot/Services/Ask`: ask flow + subscription checks.
- `FindIFBot/Services/Messages`: message dispatch, storage, media groups, command router.
- `FindIFBot/EF`: DbContext, entities, repositories, migrations.
- `FindIFBot/Handlers`: text and async command handlers.
- `FindIFBot/Helpers/Logs`: app logger wrapper and log types.

## Data Model Notes
- `UserSession` keyed by `UserId`; stores finite state (`Idle`, `WaitingForAskQuery`, `ConfirmAskContent`).
- `UserRequest` keyed by `Guid`; includes status, timestamps, channel links, and indexes on user/status/time.

## Configuration Contract
Core options expected in `Telegram` config include:
- `BotToken`, `WebhookUrl`, `AdminId`
- `UserOutputChannel`, `LinkToChannel`, `ChatInviteLink`
- `BotUsername`, `LogsOutputChannel`
- thread IDs for logs/messages
- retry and support/payment links (`RetryMaxAttempts`, `BankLink`, `CardNumber`)

Maintenance config:
- `Maintenance:SecretKey` used by `X-Maintenance-Key` header (constant-time comparison).

## Behavior Guardrails
- Always return HTTP 200 from webhook controller even on processing exceptions (prevents Telegram retries).
- Keep command normalization logic case-insensitive and trimmed.
- Preserve Ukrainian user-facing message tone and existing command aliases.
- Keep ask flow state transitions explicit and session writes consistent.
- Do not bypass validation for ask submissions (text/photo limits and media checks).

## Common Change Patterns
- Add new user command:
  1. Add handler (or service-backed handler).
  2. Register DI if needed in `Program.cs`.
  3. Extend `MessageCommandRouter` mapping and aliases.
  4. Update keyboard buttons if command is user-visible.
- Add admin callback action:
  1. Extend callback parser/data model.
  2. Handle branch in admin workflow service.
  3. Ensure callback authorization is enforced.
- Add persistence field:
  1. Update EF entity + DbContext mapping.
  2. Generate migration.
  3. Update repository/service usage.

## Build And Run
From repo root:
- Restore/build: `dotnet build FindIFBot.sln`
- Run app: `dotnet run --project FindIFBot/FindIFBot.csproj`
- Apply migrations (example):
  - `dotnet ef database update --project FindIFBot/FindIFBot.csproj`

## Local Webhook Workflow
- Run app on local port (current docs use 5199).
- Expose with ngrok.
- Set Telegram webhook to `https://<ngrok>/api/telegram/webhook`.
- Keep webhook URL and app configuration synchronized.

## Logging And Ops
- Serilog writes rolling daily files under `logs/`.
- App logger can forward errors to Telegram logs thread/channel.
- Maintenance endpoints:
  - `POST /api/maintenance/process-yesterday-logs`
  - `POST /api/maintenance/daily-statistics`
  - both require `X-Maintenance-Key` and are rate-limited.

## Quality Checklist For Future Edits
- Build succeeds with no new warnings/errors.
- New DI registrations are complete and correct lifetimes are used.
- State machine transitions are valid for each message/callback path.
- Telegram parse mode/markup combinations are valid.
- Repository calls are async where expected and avoid unnecessary blocking.
- Endpoint behavior (status codes, retry semantics, rate limits) remains intentional.
