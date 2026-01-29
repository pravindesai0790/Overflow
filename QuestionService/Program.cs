using System.Net.Sockets;
using System.Text.Json.Serialization;
using Common;
using Contracts;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using QuestionService.Data;
using QuestionService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
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

/*// it will make connection to the postgres database.
// no need of connectionstring Aspire will take care of it.
// removed due to message durability configuration as below
builder.AddNpgsqlDbContext<QuestionDbContext>("questionDb");
*/

var connString = builder.Configuration.GetConnectionString("questionDb");
builder.Services.AddDbContext<QuestionDbContext>(options =>
{
    options.UseNpgsql(connString);
}, optionsLifetime: ServiceLifetime.Singleton);

// using extension method to add common code to handle messaging(RabbitMq) service.
await builder.UseWolverineWithRabbitMqAsync(opts =>
{
    //opts.PublishAllMessages().ToRabbitExchange("questions"); // It publishes all message to rabbitmq's "questions" exchange. (update:- it is now handled by UseConventionalRouting)
    opts.ApplicationAssembly = typeof(Program).Assembly; // Wolverine is going to go looking in this project the question service for any handlers.
    // configuration for message durability
    opts.PersistMessagesWithPostgresql(connString!);
    opts.UseEntityFrameworkCoreTransactions();
    opts.PublishMessage<QuestionCreated>().ToRabbitExchange("Contracts.QuestionCreated").UseDurableOutbox();
    opts.PublishMessage<QuestionUpdated>().ToRabbitExchange("Contracts.QuestionUpdated").UseDurableOutbox();
    opts.PublishMessage<QuestionDeleted>().ToRabbitExchange("Contracts.QuestionDeleted").UseDurableOutbox();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapDefaultEndpoints();

await app.MigrationDbContextAsync<QuestionDbContext>();

app.Run();