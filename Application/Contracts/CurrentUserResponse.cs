namespace QuizGamePlatform.Backend.Application.Contracts
{
    public record CurrentUserResponse(
        string KeycloakSubject,
        string? DisplayName,
        string? Email);
}
