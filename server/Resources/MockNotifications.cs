using System.Text.Json;
using ModelContextProtocol.Protocol;
using PureHttpTransport;
using PureHttpTransport.Models;

namespace PureHttpMcpServer.Resources;

public class MockNotifications : BackgroundService
{
    private readonly ILogger<MockNotifications> _logger;

    public MockNotifications(ILogger<MockNotifications> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5)); // every 5 seconds

        _logger.LogInformation("MockNotifications service is starting.");

        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                await UpdateExistingResourcesAsync(token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MockNotifications service is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in the MockNotifications service.");
        }
    }

    private async Task UpdateExistingResourcesAsync(CancellationToken token)
    {
        var subscriptions = MockResources.Subscriptions();
        _logger.LogDebug("Updating existing resources with current timestamp - {Count} subscriptions", subscriptions.Count);

        // Iterate through existing resources and update them
        foreach (var resource in PureHttpTransport.ResourcesEndpoints.MockResources?.ListResources() ?? Enumerable.Empty<Resource>())
        {
            // Update the resource's metadata to simulate a change
            resource.Meta!["LastUpdated"] = DateTime.UtcNow.ToString("O");
            if (subscriptions.Contains(resource.Uri))
            {
                ResourceUpdatedNotificationParams notificationParams = new() { Uri = resource.Uri };
                _logger.LogInformation("Sending ResourceUpdatedNotifcation to the client");
                var notification = new ResourceUpdatedNotification
                {
                    Params = notificationParams
                };
                NotificationsEndpoints.EnqueueNotification(notification);
            }
        }

        await Task.CompletedTask; // Placeholder for async work
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MockNotifications service is stopping.");
        await base.StopAsync(stoppingToken);
    }
}
