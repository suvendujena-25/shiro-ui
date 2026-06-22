using Shiro.Api.Models;
using Shiro.Api.Services;
using Shiro.Api.Tools;

const string angularDevelopmentCorsPolicy = "AngularDevelopment";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
builder.Services.AddCors(options =>
{
    options.AddPolicy(angularDevelopmentCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200",
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IToolRouter, ToolRouter>();
builder.Services.AddScoped<IToolExecutor, FakeToolExecutor>();
builder.Services.AddSingleton<IDeviceInfoService, DeviceInfoService>();
var aiProvider = builder.Configuration.GetSection("AI").Get<AiOptions>()?.Provider ?? "Ollama";

if (aiProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IAiChatClient, OpenAiResponsesChatClient>((serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.BaseUrl);
    });
}
else
{
    builder.Services.AddHttpClient<IAiChatClient, OllamaChatClient>((serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.BaseUrl);
    });
}
builder.Services.AddSingleton<IApprovalService, InMemoryApprovalService>();
builder.Services.AddSingleton<IAuditLogService, InMemoryAuditLogService>();
builder.Services.AddSingleton<ITaskService, InMemoryTaskService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(angularDevelopmentCorsPolicy);

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
