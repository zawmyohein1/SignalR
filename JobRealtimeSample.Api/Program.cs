var builder = WebApplication.CreateBuilder(args);

const string MvcUiCorsPolicy = "MvcUi";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["https://localhost:5001", "http://localhost:5001", "https://localhost:5101", "http://localhost:5101"];

builder.Services.Configure<JobRealtimeSample.Api.Options.RealtimeHubOptions>(
    builder.Configuration.GetSection("RealtimeHub"));
builder.Services.Configure<JobRealtimeSample.Api.Options.GenericJobOptions>(
    builder.Configuration.GetSection("GenericJob"));
builder.Services.Configure<JobRealtimeSample.Api.Options.LeaveCalculationOptions>(
    builder.Configuration.GetSection("LeaveCalculation"));
builder.Services.Configure<JobRealtimeSample.Api.Options.HubTokenOptions>(
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
builder.Services
    .AddHttpClient(nameof(JobRealtimeSample.Api.Services.RealtimeNotifier))
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        if (builder.Environment.IsDevelopment())
        {
            // The sample uses three local HTTPS endpoints. This keeps API-to-hub
            // callbacks working even before the developer certificate is trusted.
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    });
builder.Services.AddSingleton<JobRealtimeSample.Api.Services.RealtimeNotifier>();
builder.Services.AddSingleton<JobRealtimeSample.Api.Services.XmlLeaveCalculationStore>();
builder.Services.AddSingleton<JobRealtimeSample.Api.Services.DemoHubTokenService>();
builder.Services.AddSingleton<JobRealtimeSample.Api.Services.BackgroundLeaveCalculationRunner>();
builder.Services.AddSingleton<JobRealtimeSample.Api.Vendors.LeaveCalculationsVendor>();

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
