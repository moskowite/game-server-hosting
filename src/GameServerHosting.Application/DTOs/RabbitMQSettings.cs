namespace GameServerHosting.Application.DTOs;

/// <summary>
/// Configuration settings for RabbitMQ connection
/// </summary>
public class RabbitMQSettings
{
    /// <summary>
    /// RabbitMQ host address
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port (default 5672)
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = "guest";
}
