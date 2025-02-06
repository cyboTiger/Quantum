using Quantum.Infrastructure.Models;

namespace chalaoshi.click.Models;
public class Teacher : Entity<int>
{
    public required string Name { get; init; }
    public required string College { get; init; }
    public required decimal Rating { get; init; }
}
