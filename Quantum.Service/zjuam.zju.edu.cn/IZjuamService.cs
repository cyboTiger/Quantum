using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Utilities;

namespace zjuam.zju.edu.cn;
public interface IZjuamService : IStatefulService<ZjuamState>, IPersistentService, IInitializableService, IAccountService
{
    public Task<Result> ValidOrRefreshTokenAsync();
    public Task<Result> LoginAsync(string username, string password);
}