using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Models;
using Quantum.Infrastructure.Utilities;

namespace zdbk.zju.edu.cn.Models;
/// <summary>
/// 表示某门课程的教学班(Id为选课课号)
/// </summary>
public class Section : Entity<string>, ISnapshotable<SectionSnapshot>
{
    public required Course Course { get; init; }
    public HashSet<string> Instructors { get; set; } = [];
    public string Semesters { get; set; } = string.Empty;
    public HashSet<(string Schedule, string Location)> ScheduleAndLocations { get; set; } = [];
    public string TeachingForm { get; set; } = string.Empty;
    public string TeachingMethod { get; set; } = string.Empty;
    public TimeSlot? ExamTime { get; set; }
    public bool IsInternational { get; set; }

    public virtual SectionSnapshot CreateSnapshot() => new()
    {
        CourseCredits = Course.Credits,
        CourseId = Course.Id,
        CourseName = Course.Name,
        Id = Id,
        Instructors = Instructors,
        ScheduleAndLocations = ScheduleAndLocations,
        ExamTime = ExamTime,
        Semesters = Semesters,
        IsInternational = IsInternational,
        TeachingForm = TeachingForm,
        TeachingMethod = TeachingMethod
    };
}
