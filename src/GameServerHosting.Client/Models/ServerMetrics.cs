namespace GameServerHosting.Client.Models;

/// <summary>
/// Server resource metrics
/// </summary>
public class ServerMetrics
{
    /// <summary>
    /// CPU usage percentage (0-100)
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Memory usage percentage (0-100)
    /// </summary>
    public double MemoryUsagePercent { get; set; }

    /// <summary>
    /// Disk usage percentage (0-100)
    /// </summary>
    public double DiskUsagePercent { get; set; }

    /// <summary>
    /// Network inbound bytes per second
    /// </summary>
    public long NetworkInBytesPerSec { get; set; }

    /// <summary>
    /// Network outbound bytes per second
    /// </summary>
    public long NetworkOutBytesPerSec { get; set; }

    /// <summary>
    /// Timestamp of metrics
    /// </summary>
    public DateTime Timestamp { get; set; }
}
