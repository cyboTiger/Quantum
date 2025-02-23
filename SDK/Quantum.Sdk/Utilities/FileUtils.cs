namespace Quantum.Infrastructure.Utilities;

public static class FileUtils
{
    public static readonly string DataFileFolder = Path.Combine(".", "data");
    public static string ToDataPath(this string filename) => Path.Combine(DataFileFolder, filename);
}
