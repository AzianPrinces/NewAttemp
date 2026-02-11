using api.Service;
using ExampleApp.Quickstart;
using StackExchange.Redis;
using StateleSSE.AspNetCore;
using StateleSSE.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(0); 
});
builder.Services.AddInMemorySseBackplane();
builder.Services.AddControllers();

builder.Services.AddOpenApiDocument(config => 
{
    config.AddStringConstants<GreetingEntity>();
});

var redisConn = builder.Configuration.GetSection("RedisConfig")["ConnectionString"];

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisConn);
    config.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddSingleton<SseService>();

builder.Services.AddRedisSseBackplane();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // The URL of your React app
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Important for SSE/Cookies
    });
});

var app = builder.Build();

app.UseCors();

//Generate Api client 
var root = Directory.GetParent(app.Environment.ContentRootPath)!.FullName;
var clientPath = Path.Combine(root, "..", "client", "src", "generated.ts.client.ts");
var openApiPath = Path.Combine(app.Environment.ContentRootPath, "openapi.json");
app.GenerateApiClientsFromOpenApi(clientPath, openApiPath).GetAwaiter().GetResult();

var backplane = app.Services.GetRequiredService<ISseBackplane>();                                                                                                                         
backplane.OnClientDisconnected += async (_, e) =>                                                                                                                                         
{
    foreach (var groupName in e.Groups )
    {
        await backplane.Clients.SendToGroupAsync(groupName, new
        {
            message = $"User {e.ConnectionId} has disconnected."
        });
    }                                                                                                                                                                         
};

app.UseOpenApi();
app.UseSwaggerUi();

app.UseAuthorization();
app.UseAuthentication();


app.MapControllers();

app.Run();
