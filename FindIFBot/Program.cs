using FindIFBot.Configuration;
using FindIFBot.EF;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using FindIFBot.Services;
using FindIFBot.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Configuration
builder.Services
    .AddOptions<TelegramOptions>()
    .Bind(builder.Configuration.GetSection("Telegram"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.BotToken), "Telegram BotToken is required")
    .ValidateOnStart();

// Telegram client
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
    return new TelegramBotClient(options.BotToken);
});

// Database
builder.Services.AddDbContext<BotDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IUserRequestHistoryRepository, UserRequestHistoryRepository>();

// Handlers / workflows
builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddScoped<IAdminWorkflowService, AdminWorkflowService>();
builder.Services.AddScoped<IStartHandler, StartHandler>();
builder.Services.AddScoped<IHistoryHandler, HistoryHandler>();

// Singletons
builder.Services.AddSingleton<IAppLogger, AppLogger>();
builder.Services.AddSingleton<IAdsPricingService, AdsPricingService>();
builder.Services.AddSingleton<IMessageStore, InMemoryMessageStore>();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
