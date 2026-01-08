using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Common;

public static class AuthExtensions
{
    public static IServiceCollection AddKeyCloakAuthentication(this IServiceCollection services)
    {
        // it will make connection with keycloak service with configuration.
        services.AddAuthentication()
            .AddKeycloakJwtBearer(serviceName: "keycloak", realm: "overflow", options =>
            {
                options.RequireHttpsMetadata = false;
                options.Audience = "overflow";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuers = 
                    [
                        "http://localhost:6001/realms/overflow", // local environment
                        "http://keycloak/realms/overflow", // this is for deployed environment (inside docker container)
                        "http://id.overflow.local/realms/overflow", // this is for full deployment inside docker compose option
                    ]
                };
            });
        
        return services;
    }
}