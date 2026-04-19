using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Timesoft.Solution.RealtimeHub.Configuration;
using Timesoft.Solution.RealtimeHub.Options;
using Timesoft.Solution.RealtimeHub.Services;

namespace Timesoft.Solution.RealtimeHub.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRealtimeHubServices(
        this IServiceCollection services,
        IConfiguration configuration,
        SignalRProvider signalRProvider)
    {
        services.AddControllers();
        services.Configure<ServiceBusOptions>(configuration.GetSection("ServiceBus"));

        var signalRBuilder = services.AddSignalR();

        if (signalRProvider == SignalRProvider.Azure)
        {
            var connectionString = configuration["Azure:SignalR:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "SignalR:Provider is Azure, but Azure:SignalR:ConnectionString is missing.");
            }

            signalRBuilder.AddAzureSignalR(connectionString);
        }

        services.AddSingleton<HubTokenService>();
        services.AddSingleton<NotificationPublisher>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                throw new InvalidOperationException("ServiceBus:ConnectionString is missing.");
            }

            return new ServiceBusClient(options.ConnectionString);
        });
        services.AddHostedService<NotificationConsumer>();

        return services;
    }
}
