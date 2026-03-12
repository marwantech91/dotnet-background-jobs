# .NET Background Jobs

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![Hangfire](https://img.shields.io/badge/Hangfire-1.8-4A90E2?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

.NET background job processor with Hangfire, retry policies, scheduling, and monitoring dashboard.

## Features

- **Hangfire** - Reliable job processing
- **Retry Policies** - Automatic retries
- **Scheduling** - Cron expressions
- **Dashboard** - Web UI monitoring
- **Queues** - Priority queues
- **Distributed** - Multi-server support

## Quick Start

```csharp
// Program.cs
builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();

// Dashboard
app.UseHangfireDashboard("/hangfire");
```

## Job Types

### Fire-and-Forget

```csharp
BackgroundJob.Enqueue(() => SendEmail("user@example.com", "Welcome!"));

// With service injection
BackgroundJob.Enqueue<IEmailService>(x => x.SendWelcomeEmail("user@example.com"));
```

### Delayed Jobs

```csharp
BackgroundJob.Schedule(() => SendReminder(userId), TimeSpan.FromDays(7));

BackgroundJob.Schedule<INotificationService>(
    x => x.SendFollowUp(orderId),
    DateTimeOffset.Now.AddHours(24)
);
```

### Recurring Jobs

```csharp
RecurringJob.AddOrUpdate(
    "daily-report",
    () => GenerateDailyReport(),
    Cron.Daily
);

RecurringJob.AddOrUpdate<IReportService>(
    "weekly-cleanup",
    x => x.CleanupOldRecords(),
    "0 0 * * 0" // Every Sunday at midnight
);
```

### Continuations

```csharp
var jobId = BackgroundJob.Enqueue(() => ProcessOrder(orderId));
BackgroundJob.ContinueJobWith(jobId, () => SendConfirmation(orderId));
```

## Job Services

```csharp
public interface IJobService
{
    Task ProcessOrderAsync(Guid orderId);
    Task SendNotificationAsync(Guid userId, string message);
    Task GenerateReportAsync(DateTime date);
}

public class JobService : IJobService
{
    private readonly ILogger<JobService> _logger;
    private readonly IOrderRepository _orders;

    public JobService(ILogger<JobService> logger, IOrderRepository orders)
    {
        _logger = logger;
        _orders = orders;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessOrderAsync(Guid orderId)
    {
        _logger.LogInformation("Processing order {OrderId}", orderId);

        var order = await _orders.GetByIdAsync(orderId);
        // Process order...

        _logger.LogInformation("Order {OrderId} processed", orderId);
    }

    [Queue("notifications")]
    public async Task SendNotificationAsync(Guid userId, string message)
    {
        // Send notification...
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task GenerateReportAsync(DateTime date)
    {
        // Generate report (only one instance at a time)
    }
}
```

## Retry Policies

```csharp
// Global retry policy
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
{
    Attempts = 5,
    DelaysInSeconds = new[] { 60, 120, 300, 600, 1200 },
    OnAttemptsExceeded = AttemptsExceededAction.Delete
});

// Per-job retry
[AutomaticRetry(Attempts = 10, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public void CriticalJob() { }

// No retry
[AutomaticRetry(Attempts = 0)]
public void FireAndForgetJob() { }
```

## Queues

```csharp
// Configure queues
services.AddHangfireServer(options =>
{
    options.Queues = new[] { "critical", "default", "low" };
    options.WorkerCount = 10;
});

// Enqueue to specific queue
[Queue("critical")]
public void CriticalJob() { }

BackgroundJob.Enqueue(() => LowPriorityJob(), "low");
```

## Dashboard Authorization

```csharp
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.IsInRole("Admin");
    }
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

## Monitoring

```csharp
// Get job statistics
var monitor = JobStorage.Current.GetMonitoringApi();

var stats = monitor.GetStatistics();
Console.WriteLine($"Enqueued: {stats.Enqueued}");
Console.WriteLine($"Processing: {stats.Processing}");
Console.WriteLine($"Succeeded: {stats.Succeeded}");
Console.WriteLine($"Failed: {stats.Failed}");
```

## License

MIT
