using Timesoft.Solution.RealtimeHub.Configuration;
using Timesoft.Solution.RealtimeHub.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

const string MvcUiCorsPolicy = "MvcUi";
var allowedOrigins = ReadAllowedOrigins(builder.Configuration);

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

var signalRSettings = builder.Configuration.GetSignalRSettings();
builder.Services.AddRealtimeHubServices(builder.Configuration, signalRSettings);

var app = builder.Build();

app.Logger.LogInformation(
    "SignalR enabled: {SignalREnabled}; provider: {SignalRProvider}",
    signalRSettings.Enabled,
    signalRSettings.Provider);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(MvcUiCorsPolicy);

app.MapControllers();
if (signalRSettings.Enabled)
{
    app.MapHub<Timesoft.Solution.RealtimeHub.Hubs.NotificationHub>("/hubs/jobstatus");
}
app.MapGet("/", () => Results.Ok("Timesoft.Solution.RealtimeHub is running."));
app.Map("/error", () => Results.Problem("An unexpected realtime hub error occurred."));

app.Run();

static string[] ReadAllowedOrigins(IConfiguration configuration)
{
    var allowedOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .GetChildren()
        .SelectMany(section => (section.Value ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        .Select(origin => origin.Trim())
        .Where(origin => !string.IsNullOrWhiteSpace(origin) &&
                         !string.Equals(origin, "xxxx", StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("Cors:AllowedOrigins is missing.");
    }

    return allowedOrigins;
}
