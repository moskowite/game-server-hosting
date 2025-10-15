using GameServerHosting.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// Controller for managing background jobs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobSchedulerService _jobScheduler;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        IJobSchedulerService jobScheduler,
        ILogger<JobsController> logger)
    {
        _jobScheduler = jobScheduler;
        _logger = logger;
    }

    /// <summary>
    /// Manually schedules a provisioning job for an order
    /// </summary>
    /// <param name="orderId">The order ID to provision</param>
    /// <param name="delaySeconds">Optional delay before starting the job</param>
    /// <returns>The scheduled job key</returns>
    [HttpPost("provisioning/{orderId}")]
    [ProducesResponseType(typeof(JobScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobScheduleResponse>> ScheduleProvisioningJob(
        Guid orderId,
        [FromQuery] int delaySeconds = 0)
    {
        try
        {
            _logger.LogInformation(
                "Manually scheduling provisioning job for order {OrderId}",
                orderId);

            var jobKey = await _jobScheduler.ScheduleProvisioningJobAsync(orderId, delaySeconds);

            return Ok(new JobScheduleResponse
            {
                JobKey = jobKey,
                OrderId = orderId,
                ScheduledAt = DateTime.UtcNow,
                DelaySeconds = delaySeconds,
                Message = "Provisioning job scheduled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to schedule provisioning job for order {OrderId}",
                orderId);
            return StatusCode(500, new { error = "Failed to schedule job", message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a scheduled provisioning job
    /// </summary>
    /// <param name="jobKey">The job key to cancel</param>
    /// <returns>Success status</returns>
    [HttpDelete("provisioning")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelProvisioningJob([FromQuery] string jobKey)
    {
        try
        {
            var cancelled = await _jobScheduler.CancelProvisioningJobAsync(jobKey);

            if (!cancelled)
            {
                return NotFound(new { message = "Job not found or already completed" });
            }

            return Ok(new { message = "Job cancelled successfully", jobKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobKey}", jobKey);
            return StatusCode(500, new { error = "Failed to cancel job", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the status of a scheduled job
    /// </summary>
    /// <param name="jobKey">The job key to check</param>
    /// <returns>Job status information</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(JobStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobStatus>> GetJobStatus([FromQuery] string jobKey)
    {
        try
        {
            var status = await _jobScheduler.GetJobStatusAsync(jobKey);

            if (status == null)
            {
                return NotFound(new { message = "Job not found" });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for job {JobKey}", jobKey);
            return StatusCode(500, new { error = "Failed to get job status", message = ex.Message });
        }
    }
}

/// <summary>
/// Response for job scheduling requests
/// </summary>
public class JobScheduleResponse
{
    public string JobKey { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DelaySeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
