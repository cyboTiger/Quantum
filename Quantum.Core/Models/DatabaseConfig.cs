using System.ComponentModel.DataAnnotations;

namespace Quantum.Core.Models;

public class DatabaseConfig
{
    [Key]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public static class ConfigKeys
    {
        public const string LastTeacherId = "LastTeacherId";
        public const string TeacherDataInitialized = "TeacherDataInitialized";
    }
}

