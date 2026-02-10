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

builder.Services.AddRedisSseBackplane();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5174") // The URL of your React app
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Important for SSE/Cookies
    });
});

var app = builder.Build();

app.UseCors();

app.GenerateApiClientsFromOpenApi("client/src/generated-ts-client.ts", "./openapi.json").GetAwaiter().GetResult();

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

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
