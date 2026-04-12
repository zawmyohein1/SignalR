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
builder.Services.AddSignalR();
builder.Services.AddSingleton<JobRealtimeSample.RealtimeHub.Services.BasicNotificationAuthService>();
builder.Services.AddSingleton<JobRealtimeSample.RealtimeHub.Services.DemoHubTokenService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(MvcUiCorsPolicy);

app.MapControllers();
app.MapHub<JobRealtimeSample.RealtimeHub.Hubs.JobStatusHub>("/hubs/jobstatus");
app.MapGet("/", () => Results.Ok("JobRealtimeSample realtime hub is running."));
app.Map("/error", () => Results.Problem("An unexpected realtime hub error occurred."));

app.Run();
