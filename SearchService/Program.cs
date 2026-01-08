using System.Text.RegularExpressions;
using Common;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SearchService.Data;
using SearchService.Models;
using Typesense;
using Typesense.Setup;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddServiceDefaults();

// Integrate Wolverine into our application
// using extension method to add common code to handle messaging(RabbitMq) service.
await builder.UseWolverineWithRabbitMqAsync(opts =>
{
    // It listens from exchange named: questions and the service that's listening from the exchange is search
    opts.ListenToRabbitQueue("questions.search", cfg =>
    {
        // It will bind queue named: "questions.search" in questions exchange.
        // So when message is published to questions exchange than it will bind it to "questions.search" queue.
        // Then handler(method we have write in application) will pick up messages from this queue and process it
        cfg.BindExchange("questions"); 
    }); 
    opts.ApplicationAssembly = typeof(Program).Assembly; // Wolverine is going to go looking in this project the question service for any handlers.
});

/* Configuration of typesense resource
it will get typesense uri which is created by aspire host in search-svc container (i.e: services__typesense__typesense__0)
This is the way how we can create connection between resources which doesn't have Aspire host integration */
var typesenseUri = builder.Configuration["services:typesense:typesense:0"];
if(string.IsNullOrEmpty(typesenseUri))
    throw new InvalidOperationException("Typesense URI not found in config");

// It will get typesense API key which was set in AppHost user-secrets
var typesenseApiKey = builder.Configuration["typesense-api-key"];
if(string.IsNullOrEmpty(typesenseApiKey))
    throw new InvalidOperationException("Typesense API key not found in config");

var uri = new Uri(typesenseUri);
// Add typesense service with configuration to application which is provided by Typesense nuget pkg
// this will be available for dependency injection in our application
builder.Services.AddTypesenseClient(config =>
{
    config.ApiKey = typesenseApiKey; // it is configured in AppHost 
    config.Nodes = new List<Node>
    {
        new(uri.Host, uri.Port.ToString(), uri.Scheme)
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

// root parameter search endpoint
app.MapGet("/search", async (string query, ITypesenseClient client) =>
{
    // [aspire]something
    string? tag = null;
    var tagMatch = Regex.Match(query, @"\[(.*?)\]"); // to get text inside [square bracket]
    if (tagMatch.Success)
    {
        tag = tagMatch.Groups[1].Value; // get the value which was extracted using regex
        query = query.Replace(tagMatch.Value, "").Trim(); // remove tag from query for further search (to make normal string)
    }

    // it will configure search parameter for query string in provided columns (i.e. title and content for below code)
    var searchParams = new SearchParameters(query, "title,content");

    if (!string.IsNullOrEmpty(tag))
    {
        // it will further filter by tag if it exists
        searchParams.FilterBy = $"tags:=[{tag}]";
    }

    try
    {
        var result = await client.Search<SearchQuestion>("questions", searchParams);
        return Results.Ok(result.Hits.Select(hit => hit.Document));
    }
    catch (Exception e)
    {
        return Results.Problem("Typesense search failed: ", e.Message);
    }
});

app.MapGet("/search/similar-titles", async (string query, ITypesenseClient client) =>
{
    var searchParams = new SearchParameters(query, "title");

    try
    {
        var result = await client.Search<SearchQuestion>("questions", searchParams);
        return Results.Ok(result.Hits.Select(hit => hit.Document));
    }
    catch (Exception e)
    {
        return Results.Problem("Typesense search failed: ", e.Message);
    }
});

// service locator pattern to get typesense service and call static method to create typesense collection schema.
using var scope = app.Services.CreateScope();
var client = scope.ServiceProvider.GetRequiredService<ITypesenseClient>();
await SearchInitializer.EnsureIndexExists(client);

app.Run();
