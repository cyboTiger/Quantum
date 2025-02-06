namespace Quantum.Infrastructure.Models;

public abstract class Entity<T>
{
    public required T Id { get; init; }

    public override bool Equals(object? other)
    {
        if (other is Entity<T> entity)
        {
            return EqualityComparer<T>.Default.Equals(Id, entity.Id);
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Id);
}
