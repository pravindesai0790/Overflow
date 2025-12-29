using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("production")
    .WithDashboard(dashboard => dashboard.WithHostPort(8080));

#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithDataVolume("keycloak-data")
    .WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

// it will create postgres service and PgAdmin in docker container with help of Aspire Host postgres integration
var postgres = builder.AddPostgres("postgres", port: 5432) 
    .WithDataVolume("postgres-data")
    .WithPgAdmin();

// it will create postgres DB as questionDB 
var questionDb = postgres.AddDatabase("questionDb"); 

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume("rabbitmq-data")
    .WithManagementPlugin(port: 15672);

// question service configuration with keycloak service and postgres DB 
var questionService = builder.AddProject<Projects.QuestionService>("question-svc")
    .WithReference(keycloak) // add reference to Keycloak service so thet questionservice can know how to locate the Keycloak service.
    // add reference to postgres container so thet questionservice can know how to locate the DB and resources.
    // it also manages connection string.
    .WithReference(questionDb) 
    .WithReference(rabbitmq)
    .WaitFor(keycloak) // wait to start keycloak service to start before starting questionservice 
    .WaitFor(questionDb)
    .WaitFor(rabbitmq);

// It will get the secret stored using "dotnet user-secrets" in AppHost
//var typesenseApiKey = builder.AddParameter("typesense-api-key", secret: true);

var typesenseApiKey = builder.Environment.IsDevelopment()
    ? builder.Configuration["Parameters:typesense-api-key"] // it will get it from user secrets directly
      ?? throw new InvalidOperationException("Could not get typesense api key")
    : "${TYPESENSE_API_KEY}"; // if we're running in docker

// It will create docker container for typesense with typesense image version 29.0 and add provide configuration with port number
// This is the way how we can create resources which doesn't have Aspire host integration
var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
    .WithArgs("--data-dir", "/data", "--api-key", typesenseApiKey, "--enable-cors")
    .WithVolume("typesense-data", "/data")
    .WithEnvironment("TYPESENSE_API_KEY", typesenseApiKey) // this will make available inside docker compose file for the typesense service
    .WithHttpEndpoint(8108, 8108, name: "typesense");

// it will get the typesense container refence to add later it to our project.
var typesenseContainer = typesense.GetEndpoint("typesense");

// search service configuration with typesense
var searchService = builder.AddProject<Projects.SearchService>("search-svc") 
    .WithEnvironment("typesense-api-key", typesenseApiKey) // passing typesenseApiKey to search service as environment variable.
    .WithReference(typesenseContainer)
    .WithReference(rabbitmq)
    .WaitFor(typesense)
    .WaitFor(rabbitmq);

// Configuration of a reverse proxy (gateway to API services) using YARP in Aspire
#pragma warning disable ASPIRECERTIFICATES001
var yarp = builder.AddYarp("gateway")
    .WithConfiguration(yarpBuilder =>
    {
        yarpBuilder.AddRoute("/questions/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/tags/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/search/{**catch-all}", searchService);
    })
    .WithEnvironment("ASPNETCORE_URLS", "http://*:8001")
    .WithEndpoint(port: 8001, targetPort: 8001, scheme: "http", name: "gateway", isExternal: true) 
    ?.WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

builder.Build().Run();