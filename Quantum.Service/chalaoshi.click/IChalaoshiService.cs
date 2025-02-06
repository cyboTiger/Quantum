using chalaoshi.click.Models;
using Quantum.Infrastructure.Abstractions;

namespace chalaoshi.click;
public interface IChalaoshiService : IPersistentService, IInitializableService
{
    Task<Teacher?> GetTeacherByNameAsync(string name, string? college = null);
}
