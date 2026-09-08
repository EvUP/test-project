namespace QuizGamePlatform.Backend.Application.Abstractions
{
    /// <summary>Данные пользователя текущего запроса.</summary>
    public interface ICurrentUserContext
    {
        bool IsAuthenticated { get; }

        /// <summary>Идентификатор пользователя в Keycloak (sub).</summary>
        string? KeycloakSubject { get; }

        /// <summary>Имя учётной записи (preferred_username).</summary>
        string? DisplayName { get; }

        string? Email { get; }
    }
}
