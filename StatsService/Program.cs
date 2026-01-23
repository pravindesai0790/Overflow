using Common;
using Contracts;
using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;
using StatsService.Models;
using StatsService.Projections;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
await builder.UseWolverineWithRabbitMqAsync(opts =>
{
    opts.ApplicationAssembly = typeof(Program).Assembly;
});
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("statDb")!);

    // instead of Guid (default) we are using string as Id in event so need to specify it
    opts.Events.StreamIdentity = StreamIdentity.AsString;
    opts.Events.AddEventType<QuestionCreated>();

    // Creating index for performance purpose
    opts.Schema.For<TagDailyUsage>()
        .Index(x => x.Tag)
        .Index(x => x.Date);
    
    // Adding projection that will be executed inline (The projection will be updated in the same transaction as the events being captured)
    opts.Projections.Add(new TrendingTagsProjection(), ProjectionLifecycle.Inline);
}).UseLightweightSessions();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
