# FindIFBot

Telegram bot built on ASP.NET Core that receives updates via webhook and processes incoming messages through a REST endpoint.

## Overview

FindIFBot is a webhook-based Telegram bot designed to run as an HTTP service.  
Telegram sends updates directly to the application endpoint configured via `setWebhook`.  
Ngrok is used for local development to expose the local HTTP server to the internet.

## Architecture

- **Platform**: .NET / ASP.NET Core
- **Transport**: Telegram Webhook
- **Hosting (local)**: Kestrel over HTTP
- **Tunnel**: ngrok
- **Configuration**: `appsettings.json`

## Prerequisites

- .NET SDK (matching project target framework)
- Telegram Bot Token
- ngrok

## Required Tools

- **ngrok** – exposes local HTTP endpoint to Telegram servers

## Configuration

### appsettings.json

Update the webhook URL to match the ngrok public address:

```json
{
  "Telegram": {
    "WebhookUrl": "https://<ngrok-id>.ngrok-free.app/api/telegram/webhook"
  }
}
```

Ensure the local application is configured to run over HTTP on port 5199.

Running Locally
1. Start ngrok
Run in Command Prompt:

ngrok http 5199
Copy the generated https://*.ngrok-free.app URL.

2. Register Telegram Webhook
Run in PowerShell:

Invoke-WebRequest `
  -Method Post `
  -Uri "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" `
  -Body @{ url = "https://<ngrok-id>.ngrok-free.app/api/telegram/webhook" }
Replace:

<YOUR_BOT_TOKEN> with the real bot token

<ngrok-id> with the active ngrok subdomain

3. Update Configuration
Replace the ngrok URL in appsettings.json with the same value used in setWebhook.

4. Run the Application
Start the application in HTTP mode:

dotnet run
The bot is now reachable by Telegram through the ngrok tunnel.

Webhook Endpoint
Route: /api/telegram/webhook

Method: POST

Source: Telegram servers only

Notes
Ngrok URL changes on every restart unless a paid plan is used.

Webhook must be reset every time the ngrok URL changes.

The application must be running for Telegram to successfully deliver updates.

License
MIT
