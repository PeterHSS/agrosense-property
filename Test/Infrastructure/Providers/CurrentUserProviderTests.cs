using System.Security.Claims;
using Api.Common;
using Api.Infrastructure.Providers;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Test.Infrastructure.Providers;

public class CurrentUserProviderTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────────

    private static CurrentUserProvider BuildProvider(ClaimsPrincipal? user = null, bool nullContext = false)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        if (nullContext)
        {
            httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        }
        else
        {
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.User).Returns(user ?? new ClaimsPrincipal());
            httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);
        }

        return new CurrentUserProvider(httpContextAccessorMock.Object);
    }

    private static ClaimsPrincipal BuildUser(Guid? userId = null, string? email = null, bool isAdmin = false)
    {
        var claims = new List<Claim>();

        if (userId.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));

        if (email is not null)
            claims.Add(new Claim(ClaimTypes.Email, email));

        var roles = new List<string>();
        if (isAdmin) roles.Add(Roles.Admin);

        var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.NameIdentifier, ClaimTypes.Role);
        foreach (var role in roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return new ClaimsPrincipal(identity);
    }

    // ─── UserId ───────────────────────────────────────────────────────────────────

    [Fact]
    public void UserId_WhenClaimPresent_ReturnsCorrectGuid()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var provider = BuildProvider(BuildUser(userId: expected));

        // Act & Assert
        Assert.Equal(expected, provider.UserId);
    }

    [Fact]
    public void UserId_WhenClaimMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = BuildProvider(BuildUser()); // no userId claim

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.UserId);
    }

    [Fact]
    public void UserId_WhenClaimIsNotAValidGuid_ThrowsInvalidOperationException()
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "not-a-guid")]);
        var provider = BuildProvider(new ClaimsPrincipal(identity));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.UserId);
    }

    [Fact]
    public void UserId_WhenHttpContextIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = BuildProvider(nullContext: true);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.UserId);
    }

    // ─── Email ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Email_WhenClaimPresent_ReturnsCorrectEmail()
    {
        // Arrange
        var provider = BuildProvider(BuildUser(email: "user@agro.com"));

        // Act & Assert
        Assert.Equal("user@agro.com", provider.Email);
    }

    [Fact]
    public void Email_WhenClaimMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = BuildProvider(BuildUser()); // no email claim

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.Email);
    }

    [Fact]
    public void Email_WhenHttpContextIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = BuildProvider(nullContext: true);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.Email);
    }

    // ─── IsAdmin ──────────────────────────────────────────────────────────────────

    [Fact]
    public void IsAdmin_WhenUserHasAdminRole_ReturnsTrue()
    {
        // Arrange
        var provider = BuildProvider(BuildUser(isAdmin: true));

        // Act & Assert
        Assert.True(provider.IsAdmin);
    }

    [Fact]
    public void IsAdmin_WhenUserDoesNotHaveAdminRole_ReturnsFalse()
    {
        // Arrange
        var provider = BuildProvider(BuildUser(isAdmin: false));

        // Act & Assert
        Assert.False(provider.IsAdmin);
    }

    [Fact]
    public void IsAdmin_WhenHttpContextIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = BuildProvider(nullContext: true);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.IsAdmin);
    }

    // ─── All claims together ──────────────────────────────────────────────────────

    [Fact]
    public void AllProperties_WhenFullUserPresent_ReturnCorrectValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = BuildProvider(BuildUser(userId: userId, email: "admin@agro.com", isAdmin: true));

        // Act & Assert
        Assert.Equal(userId, provider.UserId);
        Assert.Equal("admin@agro.com", provider.Email);
        Assert.True(provider.IsAdmin);
    }
}