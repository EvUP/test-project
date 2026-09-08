using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuizGamePlatform.Backend.Api.Swagger;
using QuizGamePlatform.Backend.Application.Options;
using QuizGamePlatform.Backend.DataAccess;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QuizGamePlatform.Backend.Application.Extensions
{
    public static class ServiceExtensions
    {
        private static string? GetConnection(string connectionName, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(connectionName)
               ?? throw new InvalidOperationException($"Connection string {connectionName} not found.");

            return connectionString;
        }

        public static IServiceCollection AddApplicationDbContext(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = GetConnection("DefaultConnection", configuration);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
            });

            return services;
        }

        public static IServiceCollection AddRedisCache(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = GetConnection("Redis", configuration);

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "quiz-platform-redis";
            });

            return services;
        }

        /// <summary>Настраивает проверку JWT от Keycloak.</summary>
        public static IServiceCollection AddKeycloakAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<KeycloakOptions>()
                .Bind(configuration.GetSection(KeycloakOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(options => Uri.TryCreate(options.Authority, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                    "Keycloak:Authority must be an absolute HTTP or HTTPS URI.")
                .ValidateOnStart();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<KeycloakOptions>>((options, keycloakOptions) =>
                {
                    var keycloak = keycloakOptions.Value;
                    options.Authority = keycloak.Authority;
                    options.Audience = keycloak.Audience;
                    options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;

                    // Сохраняем исходные имена клеймов, включая sub.
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = keycloak.Authority,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ClockSkew = TimeSpan.FromSeconds(keycloak.ClockSkewSeconds),
                        NameClaimType = "preferred_username",
                    };
                });

            services.AddAuthorization();

            return services;
        }

        public static IServiceCollection AddSwaggerWithKeycloak(
            this IServiceCollection services)
        {
            services.AddSwaggerGen();
            services.AddOptions<SwaggerGenOptions>()
                .Configure<IOptions<KeycloakOptions>>((options, keycloakOptions) =>
                {
                    var keycloak = keycloakOptions.Value;
                    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Quiz Game API", Version = "v1" });

                    options.AddSecurityDefinition("Keycloak", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri(keycloak.AuthorizationUrl),
                                TokenUrl = new Uri(keycloak.TokenUrl),
                                Scopes = new Dictionary<string, string>
                                {
                                    { "openid", "OpenID Connect Scope" },
                                    { "profile", "User Profile Scope" },
                                },
                            },
                        },
                    });

                    options.OperationFilter<AuthorizeOperationFilter>();
                });

            return services;
        }
    }
}
