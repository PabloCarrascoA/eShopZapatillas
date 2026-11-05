using System;
namespace Catalog.API.Integration;

public class RabbitMqExchange
{
    public string Exchange { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public string RoutingKeys { get; set; } = string.Empty;
}