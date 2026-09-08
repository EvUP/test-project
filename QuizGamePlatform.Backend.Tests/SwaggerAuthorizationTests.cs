using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using QuizGamePlatform.Backend.Api.Controllers;
using QuizGamePlatform.Backend.Application.Extensions;
using Swashbuckle.AspNetCore.Swagger;

namespace QuizGamePlatform.Backend.Tests;

public class SwaggerAuthorizationTests
{
    [Fact]
    public async Task ApiDocument_RequiresAuthenticationOnlyForMe()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddKeycloakAuthentication(KeycloakTestConfiguration.Create());
        builder.Services.AddSwaggerWithKeycloak();
        await using var app = builder.Build();
        app.MapControllers();

        var document = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        Assert.Empty(document.SecurityRequirements);
        var protectedOperations = document.Paths
            .SelectMany(path => path.Value.Operations.Values.Select(operation => (path.Key, operation)))
            .Where(item => item.operation.Security.Count > 0)
            .ToList();
        var protectedOperation = Assert.Single(protectedOperations);
        Assert.Equal("/api/Auth/me", protectedOperation.Key);
        Assert.Equal("Keycloak", Assert.Single(Assert.Single(protectedOperation.operation.Security).Keys).Reference.Id);
        Assert.True(document.Paths.Count > 1);
        Assert.Equal(
            $"{KeycloakTestConfiguration.Issuer}/protocol/openid-connect/auth",
            document.Components.SecuritySchemes["Keycloak"].Flows.AuthorizationCode.AuthorizationUrl.AbsoluteUri);
    }

    [Fact]
    public async Task ControllerAuthorization_IsInherited_AndAllowAnonymousOverridesIt()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(SwaggerProtectedController).Assembly);
        builder.Services.AddKeycloakAuthentication(KeycloakTestConfiguration.Create());
        builder.Services.AddSwaggerWithKeycloak();
        await using var app = builder.Build();
        app.MapControllers();

        var document = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        Assert.Single(document.Paths["/swagger-test/protected"].Operations.Values.Single().Security);
        Assert.Empty(document.Paths["/swagger-test/public"].Operations.Values.Single().Security);
    }
}

[ApiController]
[Route("swagger-test")]
[Authorize]
public class SwaggerProtectedController : ControllerBase
{
    [HttpGet("protected")]
    public IActionResult Protected() => Ok();

    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public() => Ok();
}
