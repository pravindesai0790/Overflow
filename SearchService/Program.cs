using Typesense.Setup;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddServiceDefaults();

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

app.Run();
