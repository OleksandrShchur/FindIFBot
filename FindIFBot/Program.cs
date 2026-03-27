using System.Threading.RateLimiting;
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
using Serilog;
using Serilog.Events;
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

builder.Services.Configure<MaintenanceOptions>(builder.Configuration.GetSection("Maintenance"));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global limit - 50 requests per 10 seconds per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 50,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0
            }));

    // Maintenance endpoints - only 5 calls per minute
    options.AddPolicy("maintenance", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(
        path: "logs/log-.txt", // creates logs/log-20260212.txt etc.
        rollingInterval: RollingInterval.Day, // new file every day
        rollOnFileSizeLimit: true,
        outputTemplate: "{Timestamp:dd.MM.yyyy HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 10, // keep last 10 days
        fileSizeLimitBytes: 10 * 1024 * 1024 // 10 MB per file
    )
    .CreateLogger();

builder.Host.UseSerilog();

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
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddScoped<IAdminWorkflowService, AdminWorkflowService>();
builder.Services.AddScoped<IAsyncCommandHandler, StartHandler>();
builder.Services.AddScoped<IAsyncCommandHandler, HistoryHandler>();
builder.Services.AddScoped<SupportUsHandler>();
builder.Services.AddScoped<ChannelLinkHandler>();

// Singletons
builder.Services.AddSingleton(typeof(IAppLogger<>), typeof(AppLogger<>));
builder.Services.AddSingleton<IMessageStore, InMemoryMessageStore>();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();
app.Run();
