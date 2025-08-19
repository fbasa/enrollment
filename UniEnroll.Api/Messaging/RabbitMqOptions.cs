﻿namespace UniEnroll.Api.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string Exchange { get; init; } = "email";
    public string RoutingKey { get; init; } = "email.outgoing";
}
