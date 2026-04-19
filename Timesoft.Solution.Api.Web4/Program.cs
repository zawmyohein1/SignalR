var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

const string MvcUiCorsPolicy = "MvcUi";
var allowedOrigins = ReadAllowedOrigins(builder.Configuration);

builder.Services.Configure<Timesoft.Solution.Api.Web4.Options.ServiceBusOptions>(
    builder.Configuration.GetSection("ServiceBus"));
builder.Services.Configure<Timesoft.Solution.Api.Web4.Options.LeaveCalculationOptions>(
    builder.Configuration.GetSection("LeaveCalculation"));
builder.Services.Configure<Timesoft.Solution.Api.Web4.Options.HubAccessTokenOptions>(
    builder.Configuration.GetSection("HubToken"));

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(MvcUiCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Timesoft.Solution.Api.Web4.Options.ServiceBusOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.ConnectionString))
    {
        throw new InvalidOperationException("ServiceBus:ConnectionString is missing.");
    }

    return new Azure.Messaging.ServiceBus.ServiceBusClient(options.ConnectionString);
});
builder.Services.AddSingleton<Timesoft.Solution.Api.Web4.Services.NotificationPublisher>();
builder.Services.AddSingleton<Timesoft.Solution.Api.Web4.Services.LeaveCalculationStore>();
builder.Services.AddSingleton<Timesoft.Solution.Api.Web4.Services.HubTokenService>();
builder.Services.AddSingleton<Timesoft.Solution.Api.Web4.Services.LeaveCalculationRunner>();
builder.Services.AddSingleton<Timesoft.Solution.Api.Web4.Services.LeaveCalculationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(MvcUiCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Map("/error", () => Results.Problem("An unexpected API error occurred."));

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
