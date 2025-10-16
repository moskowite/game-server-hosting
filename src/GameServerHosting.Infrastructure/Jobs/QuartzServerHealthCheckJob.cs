using Quartz;
using Microsoft.Extensions.Logging;

namespace GameServerHosting.Infrastructure.Jobs;

/// <summary>
/// Quartz.NET job wrapper for periodic server health checks
/// This job runs on a schedule to monitor all running servers
/// </summary>
[DisallowConcurrentExecution]
public class QuartzServerHealthCheckJob : IJob
{
    private readonly ServerHealthCheckJob _healthCheckJob;
    private readonly ILogger<QuartzServerHealthCheckJob> _logger;

    public QuartzServerHealthCheckJob(
        ServerHealthCheckJob healthCheckJob,
        ILogger<QuartzServerHealthCheckJob> logger)
    {
        _healthCheckJob = healthCheckJob;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation(
            "Quartz job executing health checks (Scheduled run: {FireTime})",
            context.FireTimeUtc);

        try
        {
            await _healthCheckJob.ExecuteAsync();
            
            _logger.LogInformation("Quartz health check job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quartz health check job failed");
            
            // For health checks, we'll log the error but not retry
            // The next scheduled run will handle it
            throw;
        }
    }
}
