namespace Quantum.Core.Models;

public record GraduationRequirement
{
    public string CategoryName { get; init; } = string.Empty;
    public decimal RequiredCredits { get; init; }
    public List<string> RequiredCourseIds { get; init; } = [];
    public List<string> OptionalCourseIds { get; init; } = [];
    public bool IsMandatory { get; init; }

    // TODO: Add additional fields based on actual graduation requirements structure
}


//下面这些是我的代码
public enum Results
{
    Passed,
    Failed,
    Learning
}

public enum RequiredOrOptional
{
    Optional,
    Required
}


//标题
public record Titles
{
    public string StudentID { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string CollegeName { get; init; } = string.Empty;
    public string Major { get; init; } = string.Empty;
    public int Year { get; init; }
    public Results GeneralResult { get; init; }
}
// 基类：CourseClass
public record CourseClass
{

    public List<EachCourse> Courses { get; init; } = [];
    public List<CourseClass> SubClasses { get; init; } = [];
}
// 派生类：MajorCourseClass
public record MajorCourseClass : CourseClass
{
    public string ClassName { get; init; } = string.Empty;
    public string CreditRequirement { get; init; } = string.Empty;
    public string GainedCredit { get; init; } = string.Empty;
    public Results AduitResult { get; init; }
}
// 派生类：MinorCourseClass
public record MinorCourseClass : CourseClass
{
    public string ClassName { get; init; } = string.Empty;
    public int RequiredAmount { get; init; }
    public int CompletedAmount { get; init; }
    public int GainedCredit { get; init; }
    public Results AduitResult { get; init; }
}
//课程具体内容
public record EachCourse
{
    public string CourseName { get; init; } = string.Empty;
    public string CreditID { get; init; } = string.Empty;
    public RequiredOrOptional RoO { get; init; }
    public string ProfesionalDirection { get; init; } = string.Empty;
    public string Alternativeourse { get; init; } = string.Empty;
    public int Credit { get; init; }
    public int Score { get; init; }
    public Results CourseResult { get; init; }

}