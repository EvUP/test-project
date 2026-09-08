using Microsoft.Extensions.Configuration;

namespace QuizGamePlatform.Backend.Tests;

internal static class KeycloakTestConfiguration
{
    // Test issuer identifier; JWT metadata and keys are supplied in memory.
    public const string Issuer = "https://auth.example.test/realms/quizgame";
    public const string Audience = "quiz-backend";

    public static IConfiguration Create(string? key = null, string? value = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = Issuer,
            ["Keycloak:Audience"] = Audience,
            ["Keycloak:ClientId"] = "swagger-client",
            ["Keycloak:RequireHttpsMetadata"] = "false",
            ["Keycloak:ClockSkewSeconds"] = "30",
        };
        if (key is not null)
        {
            values[$"Keycloak:{key}"] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
