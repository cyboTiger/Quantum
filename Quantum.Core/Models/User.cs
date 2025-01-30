namespace Quantum.Core.Models;

public record User
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Grade { get; init; } = string.Empty;
    public string Major { get; init; } = string.Empty;
    public string AcademicYear { get; init; } = string.Empty;
    public string Semester { get; init; } = string.Empty;
    public string PasswordEncrypted { get; init; } = string.Empty;
}
