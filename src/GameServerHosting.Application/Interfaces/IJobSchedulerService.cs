namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Service interface for scheduling background jobs
/// </summary>
public interface IJobSchedulerService
{
    /// <summary>
    /// Schedules a server provisioning job for a specific order
    /// </summary>
    /// <param name="orderId">The order ID to provision</param>
    /// <param name="delaySeconds">Optional delay before starting the job</param>
    /// <returns>The scheduled job key</returns>
    Task<string> ScheduleProvisioningJobAsync(Guid orderId, int delaySeconds = 0);

    /// <summary>
    /// Cancels a scheduled provisioning job
    /// </summary>
    /// <param name="jobKey">The job key to cancel</param>
    /// <returns>True if the job was cancelled</returns>
    Task<bool> CancelProvisioningJobAsync(string jobKey);

    /// <summary>
    /// Gets the status of a scheduled job
    /// </summary>
    /// <param name="jobKey">The job key to check</param>
    /// <returns>Job status information</returns>
    Task<JobStatus?> GetJobStatusAsync(string jobKey);
}

/// <summary>
/// Represents the status of a scheduled job
/// </summary>
public class JobStatus
{
    public string JobKey { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime? NextFireTime { get; set; }
    public DateTime? PreviousFireTime { get; set; }
    public int RefireCount { get; set; }
    public Dictionary<string, object> JobData { get; set; } = new();
}
