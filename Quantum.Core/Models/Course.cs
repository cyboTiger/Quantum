using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quantum.Core.Models;

public enum CourseStatus
{
    Unknown,
    NotSelected,
    Selected,
    Passed,
    Failed,
}

public enum CourseCategory
{
    Undefined = 0,
    MyCategory,
    CompulsoryAll,
    CompulsoryIPM,
    CompulsoryLan,
    CompulsoryCom,
    CompulsoryEtp,
    CompulsorySci,
    ElectiveAll,
    ElectiveChC,
    ElectiveGlC,
    ElectiveSoc,
    ElectiveSci,
    ElectiveArt,
    ElectiveBio,
    ElectiveTec,
    ElectiveGEC,
    PhysicalEdu,
    MajorFundation,
    MyMajor,
    AllMajor,
    AccreditedAll,
    AccreditedArt,
    AccreditedLbr,
    International,
    CKC,
    Honor
}

public record Course
{
    [Key]
    public string CourseCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Credits { get; init; }
    public string WeekTime { get; init; } = string.Empty;
    public CourseCategory Category { get; init; } = CourseCategory.Undefined;
    public string Department { get; init; } = string.Empty;
    public string SearchIndex { get; init; } = string.Empty; // 存储搜索索引（课程代码+课程名称）
    public CourseStatus Status { get; init; }
    public string Property { get; init; } = string.Empty;
    public List<CourseSection> Sections { get; set; } = [];
}

public record TimeSlot(DateTime Date, TimeOnly StartTime, TimeOnly EndTime)
{
    public static TimeSlot Parse(string timeSlotStr)
    {
        // 解析类似 "2025年06月21日(14:00-16:00)" 的格式
        var datePart = timeSlotStr[..11]; // 2025年06月21日
        var timePart = timeSlotStr[12..^1]; // 14:00-16:00

        var year = int.Parse(datePart[..4]);
        var month = int.Parse(datePart[5..7]);
        var day = int.Parse(datePart[8..10]);

        var times = timePart.Split('-');
        var startTime = TimeOnly.Parse(times[0]);
        var endTime = TimeOnly.Parse(times[1]);

        return new TimeSlot(
            new DateTime(year, month, day),
            startTime,
            endTime
        );
    }

    public override string ToString() => $"{Date:yyyy年MM月dd日}({StartTime:HH:mm}-{EndTime:HH:mm})";

    public bool OverlapsWith(TimeSlot other) => Date == other.Date && StartTime < other.EndTime && other.StartTime < EndTime;
}

public record CourseSection
{
    public static class PlotFlag
    {
        public const int SemesterMask = 0b0000_0000_0000_0110_0000_0000_0000_0000;
        public const int SemesterOffset = 17;
        public const int WeekDayMask = 0b0000_0000_0000_0001_1100_0000_0000_0000;
        public const int WeekDayOffset = 14;
        public const int WeekTypeMask = 0b0000_0000_0000_0000_0010_0000_0000_0000;
        public const int WeekTypeOffset = 13;
        public const int PlotMask = 0b0000_0000_0000_0000_0001_1111_1111_1111;
        public const int PlotOffset = 0;

        public static (int Semester, int WeekDay, int WeekType, int Plots) ParsePlotFlag(int flag)
        {
            var semester = (flag & SemesterMask) >> SemesterOffset;
            var weekDay = (flag & WeekDayMask) >> WeekDayOffset;
            var weekType = (flag & WeekTypeMask) >> WeekTypeOffset;
            var plots = (flag & PlotMask) >> PlotOffset;
            return (semester, weekDay, weekType, plots);
        }

        public static int CreatePlotFlag(int semester, int weekDay, int weekType, int plots)
            => semester << SemesterOffset
               | weekDay << WeekDayOffset
               | weekType << WeekTypeOffset
               | plots << PlotOffset;
    }

    public int Id { get; init; }
    public Course Course { get; init; } = null!;
    public string InstructorName { get; init; } = string.Empty;
    [NotMapped]
    public List<string> InstructorNames => InstructorName.Split("<br>").ToList();
    public List<Teacher> Instructors { get; set; } = [];
    public string Semester { get; init; } = string.Empty;
    public string Schedule { get; init; } = string.Empty;
    [NotMapped]
    public List<string> Schedules => Schedule.Split("<br>").ToList();
    [NotMapped]
    public List<int> PlotFlags =>
        Schedules.SelectMany(schedule =>
        {
            var flags = new List<int>();

            var plotFlag = 0;

            // 解析节数 (0..12位) 
            var plotMatch = System.Text.RegularExpressions.Regex.Match(schedule, @"第([\d,]+)节");
            if (!plotMatch.Success)
                return flags;

            foreach (var plot in plotMatch.Groups[1].Value.Split(','))
            {
                if (int.TryParse(plot, out var plotNum) && plotNum is >= 1 and <= 13)
                {
                    plotFlag |= 1 << (plotNum - 1);
                }
            }

            // 解析单双周 (13位)
            var weekTypeFlags = new List<int>();
            var isSingleWeek = schedule.Contains("{单周}");
            var isDoubleWeek = schedule.Contains("{双周}");

            // 如果没有特别指定单双周，则两种都添加
            if (!isSingleWeek && !isDoubleWeek)
            {
                weekTypeFlags.AddRange([0, 1]);
            }
            else
            {
                if (isSingleWeek)
                    weekTypeFlags.Add(0);
                if (isDoubleWeek)
                    weekTypeFlags.Add(1);
            }

            // 解析周几 (14..16位)
            var weekDayMap = new Dictionary<string, int>
            {
                {"一", 0}, {"二", 1}, {"三", 2}, {"四", 3},
                {"五", 4}, {"六", 5}, {"日", 6}
            };

            var weekDayMatch = System.Text.RegularExpressions.Regex.Match(schedule, @"周(.)");
            if (!weekDayMatch.Success)
                return flags;

            var weekDayFlag = weekDayMap[weekDayMatch.Groups[1].Value];

            var semesterFlags = new List<int>();
            // 解析学期信息 (17..18位)
            if (Semester.Contains("春"))
                semesterFlags.Add(0);
            if (Semester.Contains("夏"))
                semesterFlags.Add(1);
            if (Semester.Contains("秋"))
                semesterFlags.Add(2);
            if (Semester.Contains("冬"))
                semesterFlags.Add(3);

            flags.AddRange(
                from semesterFlag in semesterFlags
                from weekTypeFlag in weekTypeFlags
                select semesterFlag << PlotFlag.SemesterOffset
                       | weekDayFlag << PlotFlag.WeekDayOffset
                       | weekTypeFlag << PlotFlag.WeekTypeOffset
                       | plotFlag << PlotFlag.PlotOffset);

            return flags;
        }).Order().ToList();
    public string Location { get; init; } = string.Empty;
    [NotMapped]
    public List<string> Locations => Location.Split("<br>").ToList();
    public string GetLocationByPlotFlag(int plotFlag) => Locations[Array.IndexOf([.. PlotFlags], plotFlag) % Locations.Count];
    public TimeSlot? ExamTime { get; init; }
    public string TeachingForm { get; init; } = string.Empty;
    public string TeachingMethod { get; init; } = string.Empty;
    public int AvailableSeats { get; init; }
    public int MajorWaitingCount { get; init; }
    public int TotalWaitingCount { get; init; }
    public int Capacity { get; init; }  // 格式：余量/总容量
    public bool IsInternational { get; init; }

    [NotMapped]
    public decimal SelectionProbability =>
        AvailableSeats <= 0 ? 0.00m :
        TotalWaitingCount > 0 && TotalWaitingCount > AvailableSeats ?
        decimal.Round((decimal)AvailableSeats / TotalWaitingCount, 2) :
        1.00m;

    public bool IsConflictWith(CourseSection section)
    {
        foreach (var flagl in PlotFlags)
        {
            foreach (var flagr in section.PlotFlags)
            {
                var (semesterl, weekDayl, weekTypel, slotsl) = PlotFlag.ParsePlotFlag(flagl);
                var (semesterr, weekDayr, weekTyper, slotsr) = PlotFlag.ParsePlotFlag(flagr);

                if ((ExamTime is not null
                    && section.ExamTime is not null
                    && ExamTime.OverlapsWith(section.ExamTime))
                    || (semesterl == semesterr
                        && weekDayl == weekDayr
                        && weekTypel == weekTyper
                        && (slotsl & slotsr) != 0))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public virtual bool Equals(CourseSection? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Course.CourseCode == other.Course.CourseCode &&
               InstructorName == other.InstructorName &&
               Semester == other.Semester &&
               Schedule == other.Schedule &&
               Location == other.Location;
    }

    public override int GetHashCode() => HashCode.Combine(Course.CourseCode, InstructorName, Semester, Schedule, Location);
}
