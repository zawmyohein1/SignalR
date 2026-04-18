using Timesoft.Solution.RealtimeHub.Configuration;

var builder = WebApplication.CreateBuilder(args);

const string MvcUiCorsPolicy = "MvcUi";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["https://localhost:5001", "http://localhost:5001", "https://localhost:5101", "http://localhost:5101"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(MvcUiCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddControllers();

var signalRProvider = builder.Configuration.GetSignalRProvider();
var signalRBuilder = builder.Services.AddSignalR();

if (signalRProvider == SignalRProvider.Azure)
{
    var connectionString = builder.Configuration["Azure:SignalR:ConnectionString"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "SignalR:Provider is Azure, but Azure:SignalR:ConnectionString is missing.");
    }

    signalRBuilder.AddAzureSignalR(connectionString);
}

builder.Services.AddSingleton<Timesoft.Solution.RealtimeHub.Services.BasicNotificationAuthService>();
builder.Services.AddSingleton<Timesoft.Solution.RealtimeHub.Services.DemoHubTokenService>();

var app = builder.Build();

app.Logger.LogInformation("SignalR provider: {SignalRProvider}", signalRProvider);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(MvcUiCorsPolicy);

app.MapControllers();
app.MapHub<Timesoft.Solution.RealtimeHub.Hubs.JobStatusHub>("/hubs/jobstatus");
app.MapGet("/", () => Results.Ok("Timesoft.Solution.RealtimeHub is running."));
app.Map("/error", () => Results.Problem("An unexpected realtime hub error occurred."));

app.Run();
