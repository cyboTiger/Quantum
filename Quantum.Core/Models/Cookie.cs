using Microsoft.EntityFrameworkCore;

namespace Quantum.Core.Models;

[PrimaryKey(nameof(Domain), nameof(Name), nameof(Path))]
public class Cookie
{
    public string Domain { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Path { get; set; }
}
