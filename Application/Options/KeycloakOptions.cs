using System.ComponentModel.DataAnnotations;

namespace QuizGamePlatform.Backend.Application.Options
{
    public class KeycloakOptions
    {
        public const string SectionName = "Keycloak";

        /// <summary>URL realm в Keycloak.</summary>
        [Required]
        public string Authority { get; set; } = string.Empty;

        /// <summary>Ожидаемое значение aud в токене.</summary>
        [Required]
        public string Audience { get; set; } = string.Empty;

        /// <summary>ID клиента для входа через Swagger.</summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        public bool RequireHttpsMetadata { get; set; } = true;

        /// <summary>Допуск при проверке времени действия токена, в секундах.</summary>
        [Range(0, int.MaxValue)]
        public int ClockSkewSeconds { get; set; } = 30;

        public string AuthorizationUrl => $"{Authority.TrimEnd('/')}/protocol/openid-connect/auth";

        public string TokenUrl => $"{Authority.TrimEnd('/')}/protocol/openid-connect/token";
    }
}
