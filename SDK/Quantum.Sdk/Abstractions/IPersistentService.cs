namespace Quantum.Infrastructure.Abstractions;

public interface IPersistentService
{
    Task LoadStateAsync();
    Task SaveStateAsync();
}
