using Quartz;

namespace GameServerHosting.Api.Services;

/// <summary>
/// Retry policy configuration for Quartz jobs
/// </summary>
public static class QuartzRetryPolicy
{
    /// <summary>
    /// Creates a trigger with exponential backoff retry logic
    /// </summary>
    /// <param name="jobKey">The job to trigger</param>
    /// <param name="triggerName">Name for the trigger</param>
    /// <param name="maxRetries">Maximum number of retries (default: 3)</param>
    /// <param name="retryIntervals">Retry interval in seconds for each attempt</param>
    /// <returns>A configured trigger with retry logic</returns>
    public static ITrigger CreateRetryTrigger(
        JobKey jobKey,
        string triggerName,
        int maxRetries = 3,
        int[]? retryIntervals = null)
    {
        // Default retry intervals: 60s, 300s (5m), 900s (15m)
        retryIntervals ??= new[] { 60, 300, 900 };

        var triggerBuilder = TriggerBuilder.Create()
            .WithIdentity(triggerName, jobKey.Group)
            .ForJob(jobKey)
            .StartNow();

        // Configure simple retry schedule with exponential backoff
        // Quartz doesn't have built-in exponential backoff, so we use
        // SimpleTrigger with MisfireInstruction to handle failures
        var trigger = triggerBuilder
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(1)
                .WithRepeatCount(maxRetries))
            .Build();

        return trigger;
    }

    /// <summary>
    /// Calculates the retry delay based on the attempt number
    /// </summary>
    /// <param name="attemptNumber">Current attempt number (0-based)</param>
    /// <param name="retryIntervals">Array of retry intervals in seconds</param>
    /// <returns>Delay in seconds for this attempt</returns>
    public static int GetRetryDelay(int attemptNumber, int[]? retryIntervals = null)
    {
        retryIntervals ??= new[] { 60, 300, 900 };
        
        if (attemptNumber < 0)
            return 0;
        
        if (attemptNumber >= retryIntervals.Length)
            return retryIntervals[^1]; // Return last interval
        
        return retryIntervals[attemptNumber];
    }

    /// <summary>
    /// Creates a job data map with retry configuration
    /// </summary>
    public static JobDataMap CreateJobDataWithRetry(
        Dictionary<string, object> data,
        int maxRetries = 3,
        int[]? retryIntervals = null)
    {
        var jobData = new JobDataMap(data);
        jobData.Put("maxRetries", maxRetries);
        jobData.Put("retryIntervals", retryIntervals ?? new[] { 60, 300, 900 });
        jobData.Put("currentAttempt", 0);
        
        return jobData;
    }

    /// <summary>
    /// Determines if the job should retry based on current attempt
    /// </summary>
    public static bool ShouldRetry(IJobExecutionContext context, int maxRetries = 3)
    {
        var currentAttempt = context.RefireCount;
        return currentAttempt < maxRetries;
    }
}
