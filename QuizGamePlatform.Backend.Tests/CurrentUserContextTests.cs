using Microsoft.AspNetCore.Http;
using Moq;
using QuizGamePlatform.Backend.Application.Services;
using System.Security.Claims;

namespace QuizGamePlatform.Backend.Tests
{
    public class CurrentUserContextTests
    {
        private readonly Mock<IHttpContextAccessor> _accessor = new();
        private readonly CurrentUserContext _sut;

        public CurrentUserContextTests()
        {
            _sut = new CurrentUserContext(_accessor.Object);
        }

        private void SetUser(ClaimsPrincipal? principal, bool withHttpContext = true)
        {
            if (!withHttpContext)
            {
                _accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
                return;
            }

            var context = new DefaultHttpContext();

            if (principal is not null)
            {
                context.User = principal;
            }

            _accessor.Setup(a => a.HttpContext).Returns(context);
        }

        private static ClaimsPrincipal AuthenticatedUser(params Claim[] claims) =>
            new(new ClaimsIdentity(claims, authenticationType: "Bearer"));

        [Fact]
        public void KeycloakSubject_ReturnsSubClaim_WhenAuthenticated()
        {
            var sub = Guid.NewGuid().ToString();
            SetUser(AuthenticatedUser(
                new Claim("sub", sub),
                new Claim("preferred_username", "player_one"),
                new Claim("email", "player@local.test")));

            Assert.True(_sut.IsAuthenticated);
            Assert.Equal(sub, _sut.KeycloakSubject);
            Assert.Equal("player_one", _sut.DisplayName);
            Assert.Equal("player@local.test", _sut.Email);
        }

        [Fact]
        public void ReturnsNothing_WhenIdentityIsNotAuthenticated()
        {
            SetUser(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "spoofed") })));

            Assert.False(_sut.IsAuthenticated);
            Assert.Null(_sut.KeycloakSubject);
            Assert.Null(_sut.DisplayName);
            Assert.Null(_sut.Email);
        }

        [Fact]
        public void ReturnsNothing_WhenHttpContextIsMissing()
        {
            SetUser(principal: null, withHttpContext: false);

            Assert.False(_sut.IsAuthenticated);
            Assert.Null(_sut.KeycloakSubject);
        }

        [Fact]
        public void KeycloakSubject_IsNull_WhenSubClaimIsAbsent()
        {
            SetUser(AuthenticatedUser(new Claim("preferred_username", "player_one")));

            Assert.True(_sut.IsAuthenticated);
            Assert.Null(_sut.KeycloakSubject);
            Assert.Equal("player_one", _sut.DisplayName);
        }

        [Fact]
        public void EmptyClaimIsTreatedAsMissing()
        {
            SetUser(AuthenticatedUser(
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("preferred_username", "   ")));

            Assert.Null(_sut.DisplayName);
        }
    }
}
