using Quantum.Core.Models;

namespace Quantum.Core.Interfaces;
public interface IAuthenticateService
{
    Task<User> GetUserAsync(string username);
    Task<User> LoginAsync(string username, string password);
}
