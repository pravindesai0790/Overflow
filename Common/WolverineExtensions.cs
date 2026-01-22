using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Common;

public static class WolverineExtensions
{
    public static async Task UseWolverineWithRabbitMqAsync(this IHostApplicationBuilder builder, Action<WolverineOptions> configureMessaging)
    {
        // it will not retry to start RabbitMQ service in Design time (i.e, while creating DB migration)
        var isEfDesignTime = AppDomain.CurrentDomain.FriendlyName.StartsWith("ef", StringComparison.OrdinalIgnoreCase);

        if (!isEfDesignTime)
        {
            // this will catch message broker exception (not starting) and retry to start it as configured 
            var retryPolicy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, timeSpan, retryCount) =>
                    {
                        Console.WriteLine($"Retry attempt {retryCount} failed. Retrying in " + $"{timeSpan.Seconds} seconds...");
                    });

            // retry code if messaging service (i.e, RabbitMQ) fail to start before we start executing Wolverine integration
            await retryPolicy.ExecuteAsync(async () =>
            {
                var endpoint = builder.Configuration.GetConnectionString("messaging")
                               ?? throw new InvalidOperationException("messaging (RabbitMQ) connection string not found...");

                var factory = new ConnectionFactory
                {
                    Uri = new Uri(endpoint)
                };
                await using var connectio = await factory.CreateConnectionAsync();
            });
        }
        
        // this is going to setup Open telemetry for RabbitMQ via Wolverine.
        // So it's going to publish the traces so that it will be able to see what's going on between our different services
        builder.Services.AddOpenTelemetry().WithTracing(traceProviderBuilder =>
        {
            traceProviderBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(builder.Environment.ApplicationName))
                .AddSource("Wolverine");
        });
        
        // Integrate Wolverine into our application. it will create exchanges and queue in RabbitMQ (It is dependent on RabbitMQ to start that's why above policy is added)
        builder.UseWolverine(opts =>
        {
            opts.UseRabbitMqUsingNamedConnection("messaging")
                .AutoProvision()
                /*
                 * 'UseConventionalRouting' means is that we don't configure so much, and we don't tell Wolverine where to locate the queues and exchanges inside RabbitMQ.
                 * Instead, it uses the classes that we provide, and it figures out what exchanges and which queues it should have.
                 * And that gives us a more automated way of using RabbitMQ effectively.
                 */
                .UseConventionalRouting();
                //.DeclareExchange("questions"); // It publishes all message to rabbitmq's "questions" exchange.
            
            configureMessaging(opts);
        });
    }
}