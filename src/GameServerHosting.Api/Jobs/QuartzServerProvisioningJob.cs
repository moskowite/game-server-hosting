using Quartz;
using Microsoft.Extensions.Options;
using GameServerHosting.Api.Models;
using GameServerHosting.Api.Services;

namespace GameServerHosting.Api.Jobs;

/// <summary>
/// Quartz.NET job wrapper for provisioning game servers with retry logic
/// This job can be triggered manually or scheduled to run automatically
/// </summary>
[DisallowConcurrentExecution]
public class QuartzServerProvisioningJob : IJob
{
    private readonly ServerProvisioningJob _provisioningJob;
    private readonly ILogger<QuartzServerProvisioningJob> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzServerProvisioningJob(
        ServerProvisioningJob provisioningJob,
        ILogger<QuartzServerProvisioningJob> logger,
        IConfiguration configuration,
        ISchedulerFactory schedulerFactory)
    {
        _provisioningJob = provisioningJob;
        _logger = logger;
        _configuration = configuration;
        _schedulerFactory = schedulerFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var orderId = context.JobDetail.JobDataMap.GetGuid("orderId");
        
        if (orderId == Guid.Empty)
        {
            _logger.LogError("OrderId is required but was not provided in job data");
            throw new JobExecutionException("OrderId is required");
        }

        // Get retry configuration
        var maxRetries = _configuration.GetValue<int>("Jobs:Provisioning:MaxRetries", 3);
        var retryIntervals = _configuration.GetSection("Jobs:Provisioning:RetryIntervalSeconds")
            .Get<int[]>() ?? new[] { 60, 300, 900 };
        
        var currentAttempt = context.RefireCount;

        _logger.LogInformation(
            "Quartz job executing provisioning for order {OrderId} (Attempt {Attempt} of {MaxAttempts})",
            orderId,
            currentAttempt + 1,
            maxRetries + 1);

        try
        {
            await _provisioningJob.ExecuteAsync(orderId);
            
            _logger.LogInformation(
                "Quartz job completed successfully for order {OrderId}",
                orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Quartz job failed for order {OrderId} on attempt {Attempt} of {MaxAttempts}",
                orderId,
                currentAttempt + 1,
                maxRetries + 1);

            // Determine if we should retry
            if (currentAttempt < maxRetries)
            {
                // Calculate retry delay
                var retryDelaySeconds = QuartzRetryPolicy.GetRetryDelay(currentAttempt, retryIntervals);
                
                _logger.LogWarning(
                    "Will retry provisioning for order {OrderId} in {RetryDelay} seconds (Attempt {NextAttempt} of {MaxAttempts})",
                    orderId,
                    retryDelaySeconds,
                    currentAttempt + 2,
                    maxRetries + 1);

                // Schedule retry
                await ScheduleRetryAsync(context, orderId, currentAttempt + 1, retryDelaySeconds);
                
                // Don't rethrow - we've handled the retry
                return;
            }
            else
            {
                _logger.LogError(
                    "Provisioning failed for order {OrderId} after {MaxAttempts} attempts. No more retries.",
                    orderId,
                    maxRetries + 1);
                
                // Final failure - rethrow to mark job as failed
                throw new JobExecutionException(ex, refireImmediately: false);
            }
        }
    }

    private async Task ScheduleRetryAsync(IJobExecutionContext context, Guid orderId, int attemptNumber, int delaySeconds)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            
            // Create a new job for the retry
            var retryJobKey = new JobKey($"provision-{orderId}-retry-{attemptNumber}", "provisioning");
            
            var retryJob = JobBuilder.Create<QuartzServerProvisioningJob>()
                .WithIdentity(retryJobKey)
                .WithDescription($"Retry {attemptNumber} for order {orderId}")
                .UsingJobData("orderId", orderId.ToString())
                .StoreDurably(false)
                .RequestRecovery(true)
                .Build();

            var retryTrigger = TriggerBuilder.Create()
                .WithIdentity($"provision-trigger-{orderId}-retry-{attemptNumber}", "provisioning")
                .StartAt(DateTimeOffset.UtcNow.AddSeconds(delaySeconds))
                .Build();

            await scheduler.ScheduleJob(retryJob, retryTrigger);
            
            _logger.LogInformation(
                "Scheduled retry job {JobKey} for order {OrderId} to run in {Delay} seconds",
                retryJobKey,
                orderId,
                delaySeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to schedule retry for order {OrderId}",
                orderId);
            throw;
        }
    }
}
