using Quantum.Infrastructure.Models;
using zdbk.zju.edu.cn.Enums;

namespace zdbk.zju.edu.cn.Models;

/// <summary>
/// 表示某门课程
/// </summary>
public class Course : Entity<string>
{
    public required string Name { get; set; }
    public required decimal Credits { get; set; }
    public CourseCategory Category { get; set; }
    public string WeekTime { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Property { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
    public List<SelectableSection> Sections { get; init; } = [];
}
