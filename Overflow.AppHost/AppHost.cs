using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("production")
    .WithDashboard(dashboard => dashboard.WithHostPort(8080));

#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithDataVolume("keycloak-data")
    /*cmd to export keycloak realm json file
     *   -> docker run --rm -v keycloak-data:/opt/keycloak/data -v ${PWD}/realms:/opt/keycloak/export quay.io/keycloak/keycloak:26.4 export --realm overflow --dir /opt/keycloak/export --users realm_file
     */
    .WithRealmImport("../infra/realms") // to import exported realms 

    /*
     * WARNING: With HTTPS not enabled, `proxy-headers` unset, and `hostname-strict=false`, the server is running in an insecure context.
     * Secure contexts are required for full functionality, including cross-origin cookies. Also, if you are using a proxy,
     * requests from the proxy to the server will fail CORS checks with 403s because the wrong origin will be determined.
     * Make sure `proxy-headers` are configured properly. Key material not provided to set up HTTPS. Please configure your keys/certificates,
     * or if HTTPS access is not needed see the `http-enabled` option. If you meant to start the server in development mode, see the `start-dev` command.
     */
    .WithEnvironment("KC_HTTP_ENABLED", "true") // Docker publishing error for https 
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    // .WithEndpoint(6001, 8080, "keycloak", isExternal: true) // to access keycloak management externally in browser
    .WithEnvironment("VIRTUAL_HOST", "id.overflow.local") // environment variable to our services which need extrnal access. "id.overflow.local" configred in our host file in window
    .WithEnvironment("VIRTUAL_PORT", "8080") // routing our requrest coming from port 80 (nginx reverse proxy) effectively to the gateway (i.e, 8080)
    .WithoutHttpsCertificate(); // to remove unhealthy status from Aspire Host  
#pragma warning restore ASPIRECERTIFICATES001

// it will create postgres service and PgAdmin in docker container with help of Aspire Host postgres integration
var postgres = builder.AddPostgres("postgres", port: 5432) 
    .WithDataVolume("postgres-data")
    .WithPgWeb();

// it will create postgres DB as questionDB 
var questionDb = postgres.AddDatabase("questionDb"); 
var profileDb = postgres.AddDatabase("profileDb"); 
var statDb = postgres.AddDatabase("statDb"); 

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

var typesenseApiKey = builder.AddParameter("typesense-api-key", secret: true);

// var typesenseApiKey = builder.Environment.IsDevelopment()
//     ? builder.Configuration["Parameters:typesense-api-key"] // it will get it from user secrets directly
//       ?? throw new InvalidOperationException("Could not get typesense api key")
//     : "${TYPESENSE_API_KEY}"; // if we're running in docker

// It will create docker container for typesense with typesense image version 29.0 and add provide configuration with port number
// This is the way how we can create resources which doesn't have Aspire host integration
var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
    .WithVolume("typesense-data", "/data")
    .WithEnvironment("TYPESENSE_API_KEY", typesenseApiKey) // this will make available inside docker compose file for the typesense service
    .WithEnvironment("TYPESENSE_DATA_DIR", "/data")
    .WithEnvironment("TYPESENSE_ENABLE_CORS", "true")
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

var profileService = builder.AddProject<Projects.ProfileService>("profile-svc")
    .WithReference(keycloak) 
    .WithReference(profileDb) 
    .WithReference(rabbitmq)
    .WaitFor(keycloak)
    .WaitFor(profileDb)
    .WaitFor(rabbitmq);

var statService = builder.AddProject<Projects.StatsService>("stat-svc")
    .WithReference(statDb) 
    .WithReference(rabbitmq)
    .WaitFor(statDb)
    .WaitFor(rabbitmq);

// Configuration of a reverse proxy (gateway to API services) using YARP in Aspire
// which will proxy requests to our individual internal services
#pragma warning disable ASPIRECERTIFICATES001
var yarp = builder.AddYarp("gateway")
    .WithConfiguration(yarpBuilder =>
    {
        yarpBuilder.AddRoute("/questions/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/test/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/tags/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/search/{**catch-all}", searchService);
        yarpBuilder.AddRoute("/profiles/{**catch-all}", profileService);
        yarpBuilder.AddRoute("/stats/{**catch-all}", statService);
    })
    .WithEnvironment("ASPNETCORE_URLS", "http://*:8001")
    .WithEndpoint(port: 8001, targetPort: 8001, scheme: "http", name: "gateway", isExternal: true) 
    .WithEnvironment("VIRTUAL_HOST", "api.overflow.local") // environment variable to our services which need extrnal access. "api.overflow.local" configred in our host file in window
    .WithEnvironment("VIRTUAL_PORT", "8001") // routing our requrest coming from port 80 (nginx reverse proxy) effectively to the gateway (i.e, 8001)
    .WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

var webapp = builder.AddJavaScriptApp("webapp", "../webapp")
    .WithReference(keycloak)
    .WithHttpEndpoint(env: "PORT", port: 3000, targetPort: 4000)
    .WithEnvironment("VIRTUAL_HOST", "app.overflow.local")
    .WithEnvironment("VIRTUAL_PORT", "4000")
    .PublishAsDockerFile();

// Configuration of nginx reverse proxy in local deployment not dev
// which will proxy requests externally to the internal services such as keycloak and gateway services
if (!builder.Environment.IsDevelopment())
{
    builder.AddContainer("nginx-proxy", "nginxproxy/nginx-proxy", "1.8")
        .WithEndpoint(80, 80, "nginx", isExternal: true)
        .WithEndpoint(443, 443, "nginx-ssl", isExternal: true)
        .WithBindMount("/var/run/docker.sock", "/tmp/docker.sock", true)
        .WithBindMount("../infra/devcerts", "/etc/nginx/certs", true);

    keycloak.WithEnvironment("KC_HOSTNAME", "https://id.overflow.local")
        .WithEnvironment("KC_HOSTNAME_BACKCHANNEL_DYNAMIC", "true"); //KC_HOSTNAME_BACKCHANNEL_DYNAMIC

}

builder.Build().Run();