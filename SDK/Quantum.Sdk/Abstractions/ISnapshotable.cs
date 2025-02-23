namespace Quantum.Infrastructure.Abstractions;

public interface ISnapshotable<out T>
{
    T CreateSnapshot();
}
