using Quantum.Infrastructure.Utilities;

namespace Quantum.Infrastructure.Abstractions;

public interface IAccountService
{
    string ServiceName { get; }
    string LoginRoute { get; }
    string LoginStatus { get; }
    bool IsAuthenticated { get; }
    Task LogoutAsync();
    event Action OnLogout;
    public Task<Result<RequestClient>> GetAuthencatedClientAsync(RequestOptions? options = null);
}
