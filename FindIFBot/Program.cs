using FindIFBot.EF;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using FindIFBot.Services;
using FindIFBot.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var token = builder.Configuration["Telegram:BotToken"];

    return new TelegramBotClient(token);
});

builder.Services.AddDbContext<BotDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();

builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddScoped<IAdminWorkflowService, AdminWorkflowService>();
builder.Services.AddScoped<IStartHandler, StartHandler>();
builder.Services.AddScoped<IHistoryHandler, HistoryHandler>();

builder.Services.AddSingleton<IAppLogger, AppLogger>();
builder.Services.AddSingleton<IAdsPricingService, AdsPricingService>();
builder.Services.AddSingleton<IMessageStore, InMemoryMessageStore>();
builder.Services.AddSingleton<IUserRequestHistoryRepository, InMemoryUserRequestHistoryRepository>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
