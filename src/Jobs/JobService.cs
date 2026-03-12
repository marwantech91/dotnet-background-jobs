using Hangfire;
using Microsoft.Extensions.Logging;

namespace BackgroundJobs.Jobs;

public interface IJobService
{
    Task ProcessOrderAsync(Guid orderId, CancellationToken ct = default);
    Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
    Task GenerateDailyReportAsync(DateTime date, CancellationToken ct = default);
    Task CleanupOldRecordsAsync(int daysOld, CancellationToken ct = default);
    Task SyncExternalDataAsync(string source, CancellationToken ct = default);
}

public class JobService : IJobService
{
    private readonly ILogger<JobService> _logger;

    public JobService(ILogger<JobService> logger)
    {
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    [Queue("orders")]
    public async Task ProcessOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing order {OrderId}", orderId);

        try
        {
            // Simulate order processing
            await Task.Delay(2000, ct);

            // Process payment
            _logger.LogInformation("Payment processed for order {OrderId}", orderId);

            // Update inventory
            _logger.LogInformation("Inventory updated for order {OrderId}", orderId);

            // Queue notification
            BackgroundJob.Enqueue<IJobService>(x =>
                x.SendEmailAsync("customer@example.com", "Order Confirmed", $"Order {orderId} confirmed", default));

            _logger.LogInformation("Order {OrderId} completed", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order {OrderId}", orderId);
            throw;
        }
    }

    [AutomaticRetry(Attempts = 5)]
    [Queue("notifications")]
    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("Sending email to {To}: {Subject}", to, subject);

        // Simulate email sending
        await Task.Delay(500, ct);

        _logger.LogInformation("Email sent to {To}", to);
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [Queue("reports")]
    public async Task GenerateDailyReportAsync(DateTime date, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating daily report for {Date}", date.ToShortDateString());

        // Simulate report generation
        await Task.Delay(5000, ct);

        _logger.LogInformation("Daily report generated for {Date}", date.ToShortDateString());
    }

    [AutomaticRetry(Attempts = 2)]
    [Queue("maintenance")]
    public async Task CleanupOldRecordsAsync(int daysOld, CancellationToken ct = default)
    {
        _logger.LogInformation("Cleaning up records older than {Days} days", daysOld);

        // Simulate cleanup
        await Task.Delay(3000, ct);

        _logger.LogInformation("Cleanup completed");
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 300, 600, 1200 })]
    [Queue("sync")]
    public async Task SyncExternalDataAsync(string source, CancellationToken ct = default)
    {
        _logger.LogInformation("Syncing data from {Source}", source);

        // Simulate external API sync
        await Task.Delay(4000, ct);

        _logger.LogInformation("Sync completed from {Source}", source);
    }
}

// Job scheduler for recurring jobs
public class JobScheduler
{
    public static void ConfigureRecurringJobs()
    {
        // Daily report at 6 AM
        RecurringJob.AddOrUpdate<IJobService>(
            "daily-report",
            x => x.GenerateDailyReportAsync(DateTime.Today.AddDays(-1), default),
            "0 6 * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
        );

        // Cleanup every Sunday at midnight
        RecurringJob.AddOrUpdate<IJobService>(
            "weekly-cleanup",
            x => x.CleanupOldRecordsAsync(30, default),
            Cron.Weekly(DayOfWeek.Sunday),
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
        );

        // Sync external data every hour
        RecurringJob.AddOrUpdate<IJobService>(
            "hourly-sync",
            x => x.SyncExternalDataAsync("external-api", default),
            Cron.Hourly,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
        );
    }

    public static void RemoveJob(string jobId)
    {
        RecurringJob.RemoveIfExists(jobId);
    }

    public static void TriggerJob(string jobId)
    {
        RecurringJob.TriggerJob(jobId);
    }
}
