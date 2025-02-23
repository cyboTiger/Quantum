namespace Quantum.Infrastructure.Abstractions;
public interface IInitializableService
{
    bool IsInitialized { get; }
    Task InitializeTask { get; }
}
