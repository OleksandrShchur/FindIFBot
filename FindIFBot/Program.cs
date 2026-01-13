using FindIFBot.Handlers;
using FindIFBot.Persistence;
using FindIFBot.Services;
using FindIFBot.Services.Admin;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var token = builder.Configuration["Telegram:BotToken"];

    return new TelegramBotClient(token);
});

builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddSingleton<IUserSessionRepository, InMemoryUserSessionRepository>();
builder.Services.AddSingleton<IMessageStore, InMemoryMessageStore>();
builder.Services.AddScoped<IAdminWorkflowService, AdminWorkflowService>();
builder.Services.AddSingleton<IAdsPricingService, AdsPricingService>();
builder.Services.AddScoped<IStartHandler, StartHandler>();

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
