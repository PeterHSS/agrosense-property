namespace Api.Infrastructure.Providers;

public interface ICurrentUserProvider
{
    Guid UserId { get; }
    string Email { get; }
    bool IsAdmin { get; }
}
