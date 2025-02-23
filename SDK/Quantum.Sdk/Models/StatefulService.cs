using Quantum.Infrastructure.Abstractions;

namespace Quantum.Infrastructure.Models;

public abstract class StatefulService<T> : IStatefulService<T>
{
    private T? _state;
    public virtual T? State
    {
        get => _state;
        set
        {
            _state = value;
            NotifyStateChanged();
        }
    }

    public event Action? OnStateChanged = delegate { };

    protected virtual void NotifyStateChanged() => OnStateChanged?.Invoke();
}