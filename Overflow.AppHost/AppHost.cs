var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithDataVolume("keycloak-data")
    .WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

var questionService = builder.AddProject<Projects.QuestionService>("question-svc")
    .WithReference(keycloak) // add reference to Keycloak service so thet questionservice can know how to locate the Keycloak service.
    .WaitFor(keycloak); // wait to start keycloak service to start before starting questionservice 

builder.Build().Run();