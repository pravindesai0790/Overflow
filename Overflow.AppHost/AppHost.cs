var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithDataVolume("keycloak-data")
    .WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

var postgres = builder.AddPostgres("postgres", port: 5432) // it will create postgres service in container
    .WithDataVolume("postgres-data")
    .WithPgAdmin();

var questionDb = postgres.AddDatabase("questionDb"); // it will create postgres DB as questionDB

var questionService = builder.AddProject<Projects.QuestionService>("question-svc")
    .WithReference(keycloak) // add reference to Keycloak service so thet questionservice can know how to locate the Keycloak service.
    .WithReference(questionDb)
    .WaitFor(keycloak) // wait to start keycloak service to start before starting questionservice 
    .WaitFor(questionDb);

builder.Build().Run();