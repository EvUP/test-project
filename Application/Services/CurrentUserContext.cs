using QuizGamePlatform.Backend.Application.Abstractions;
using System.Security.Claims;

namespace QuizGamePlatform.Backend.Application.Services
{
    public class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
    {
        private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public string? KeycloakSubject => GetClaim("sub");

        public string? DisplayName => GetClaim("preferred_username");

        public string? Email => GetClaim("email");

        private string? GetClaim(string claimType)
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            var value = User?.FindFirstValue(claimType);

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
