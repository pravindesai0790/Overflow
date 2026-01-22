using System.Net.Sockets;
using System.Text.Json.Serialization;
using Common;
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
builder.Services.AddKeyCloakAuthentication(); // using extension method to add common code to handle Authentication configuration.

// To handle circular dependency in entity framework core c#
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// it will make connection to the postgres database.
// no need of connectionstring Aspire will take care of it.
builder.AddNpgsqlDbContext<QuestionDbContext>("questionDb");

// using extension method to add common code to handle messaging(RabbitMq) service.
await builder.UseWolverineWithRabbitMqAsync(opts =>
{
    //opts.PublishAllMessages().ToRabbitExchange("questions"); // It publishes all message to rabbitmq's "questions" exchange. (update:- it is now handled by UseConventionalRouting)
    opts.ApplicationAssembly = typeof(Program).Assembly; // Wolverine is going to go looking in this project the question service for any handlers.
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