using System.Net.Sockets;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using QuestionService.Data;
using QuestionService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TagService>();

// To handle circular dependency in entity framework core c#
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// it will make connection with keycloak service with configuration.
builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(serviceName: "keycloak", realm: "overflow", options =>
    {
        options.RequireHttpsMetadata = false;
        options.Audience = "overflow";
    });

// it will make connection to the postgres database.
// no need of connectionstring Aspire will take care of it.
builder.AddNpgsqlDbContext<QuestionDbContext>("questionDb");

// this is going to setup Open telemetry for RabbitMQ via Wolverine.
// So it's going to publish the traces so that it will be able to see what's going on between our different services
builder.Services.AddOpenTelemetry().WithTracing(traceProviderBuilder =>
{
    traceProviderBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(builder.Environment.ApplicationName))
        .AddSource("Wolverine");
});

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

// Integrate Wolverine into our application. it will create exchanges and queue in RabbitMQ (It is dependent on RabbitMQ to start that why above policy is added)
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("messaging").AutoProvision();
    opts.PublishAllMessages().ToRabbitExchange("questions"); // It publishes all message to rabbitmq's "questions" exchange.
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapDefaultEndpoints();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider; // it provides access to all services available inside our app including DBContext
try
{
    var context = services.GetRequiredService<QuestionDbContext>();
    await context.Database.MigrateAsync();
}
catch (Exception e)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(e, "An error occurred while migrating or seeding the DB.");
}

app.Run();