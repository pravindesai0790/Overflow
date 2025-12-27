var builder = DistributedApplication.CreateBuilder(args);

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

// question service configuration with keycloak service and postgres DB 
var questionService = builder.AddProject<Projects.QuestionService>("question-svc")
    .WithReference(keycloak) // add reference to Keycloak service so thet questionservice can know how to locate the Keycloak service.
    // add reference to postgres container so thet questionservice can know how to locate the DB and resources.
    // it also manages connection string.
    .WithReference(questionDb) 
    .WaitFor(keycloak) // wait to start keycloak service to start before starting questionservice 
    .WaitFor(questionDb);

// It will create docker container for typesense with typesense image version 29.0 and add provide configuration with port number
// This is the way how we can create resources which doesn't have Aspire host integration
var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
    .WithArgs("--data-dir", "/data", "--api-key", "xyz", "--enable-cors")
    .WithVolume("typesense-data", "/data")
    .WithEndpoint(8108, 8108, name: "typesense");

// it will get the typesense container refence to add later it to our project.
var typesenseContainer = typesense.GetEndpoint("typesense");

// search service configuration with typesense
var searchService = builder.AddProject<Projects.SearchService>("search-svc") 
    .WithReference(typesenseContainer)
    .WaitFor(typesense);

builder.Build().Run();