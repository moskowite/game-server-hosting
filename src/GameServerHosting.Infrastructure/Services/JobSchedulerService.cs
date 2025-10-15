using GameServerHosting.Application.Interfaces;
using Quartz;

namespace GameServerHosting.Infrastructure.Services;

/// <summary>
/// Service for scheduling and managing background jobs using Quartz.NET
/// </summary>
public class JobSchedulerService : IJobSchedulerService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<JobSchedulerService> _logger;

    public JobSchedulerService(
        ISchedulerFactory schedulerFactory,
        ILogger<JobSchedulerService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task<string> ScheduleProvisioningJobAsync(Guid orderId, int delaySeconds = 0)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            // Create a unique job key for this provisioning request
            var jobKey = new JobKey($"provision-{orderId}", "provisioning");

            // Check if a job already exists for this order
            if (await scheduler.CheckExists(jobKey))
            {
                _logger.LogWarning(
                    "Provisioning job already exists for order {OrderId}",
                    orderId);
                return jobKey.ToString();
            }

            // Create the job
            var job = JobBuilder.Create<Jobs.QuartzServerProvisioningJob>()
                .WithIdentity(jobKey)
                .WithDescription($"Provision server for order {orderId}")
                .UsingJobData("orderId", orderId.ToString())
                .StoreDurably(false)
                .RequestRecovery(true) // Recover after scheduler restart
                .Build();

            // Create the trigger
            var triggerBuilder = TriggerBuilder.Create()
                .WithIdentity($"provision-trigger-{orderId}", "provisioning")
                .WithDescription($"Trigger for provisioning order {orderId}");

            if (delaySeconds > 0)
            {
                triggerBuilder.StartAt(DateTimeOffset.UtcNow.AddSeconds(delaySeconds));
            }
            else
            {
                triggerBuilder.StartNow();
            }

            var trigger = triggerBuilder.Build();

            // Schedule the job
            await scheduler.ScheduleJob(job, trigger);

            _logger.LogInformation(
                "Scheduled provisioning job for order {OrderId} with {Delay}s delay",
                orderId,
                delaySeconds);

            return jobKey.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to schedule provisioning job for order {OrderId}",
                orderId);
            throw;
        }
    }

    public async Task<bool> CancelProvisioningJobAsync(string jobKey)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            
            // Parse the job key
            var parts = jobKey.Split('.');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid job key format: {JobKey}", jobKey);
                return false;
            }

            var key = new JobKey(parts[1], parts[0]);
            var deleted = await scheduler.DeleteJob(key);

            if (deleted)
            {
                _logger.LogInformation("Cancelled job {JobKey}", jobKey);
            }
            else
            {
                _logger.LogWarning("Job {JobKey} not found or already completed", jobKey);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobKey}", jobKey);
            throw;
        }
    }

    public async Task<JobStatus?> GetJobStatusAsync(string jobKey)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            
            // Parse the job key
            var parts = jobKey.Split('.');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid job key format: {JobKey}", jobKey);
                return null;
            }

            var key = new JobKey(parts[1], parts[0]);
            var jobDetail = await scheduler.GetJobDetail(key);

            if (jobDetail == null)
            {
                return null;
            }

            var triggers = await scheduler.GetTriggersOfJob(key);
            var trigger = triggers.FirstOrDefault();

            var status = new JobStatus
            {
                JobKey = jobKey,
                JobType = jobDetail.JobType.Name,
                State = trigger != null 
                    ? (await scheduler.GetTriggerState(trigger.Key)).ToString()
                    : "Unknown",
                NextFireTime = trigger?.GetNextFireTimeUtc()?.UtcDateTime,
                PreviousFireTime = trigger?.GetPreviousFireTimeUtc()?.UtcDateTime,
                JobData = jobDetail.JobDataMap.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value)
            };

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for job {JobKey}", jobKey);
            throw;
        }
    }
}
