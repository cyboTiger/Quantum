namespace Quantum.Infrastructure.Abstractions;

public interface IStatefulService<T>
{
    T? State { get; set; }
    event Action? OnStateChanged;
}
