using System.Text.RegularExpressions;
using zdbk.zju.edu.cn.Constants;
using zdbk.zju.edu.cn.Models;

namespace zdbk.zju.edu.cn.Utilities;
public static partial class SectionSnapshotUtils
{
    public static IEnumerable<(int PlotFlag, string Location)> GetPlotFlagAndLocations(this SectionSnapshot section) =>
        section.ScheduleAndLocations.SelectMany(scheduleAndLocation =>
        {
            var schedule = scheduleAndLocation.Schedule;
            var ret = new List<(int, string)>();

            var plotFlag = 0;

            // 解析节数 (0..12位) 
            var plotMatch = ParsePlotRegex().Match(schedule);
            if (!plotMatch.Success)
                return ret;

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
                weekTypeFlags.AddRange(new[] { 0, 1 });
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

            var weekDayMatch = ParseWeekDayRegex().Match(schedule);
            if (!weekDayMatch.Success)
                return ret;

            var weekDayFlag = weekDayMap[weekDayMatch.Groups[1].Value];

            var semesterFlags = new List<int>();
            // 解析学期信息 (17..18位)
            if (section.Semesters.Contains("春"))
                semesterFlags.Add(0);
            if (section.Semesters.Contains("夏"))
                semesterFlags.Add(1);
            if (section.Semesters.Contains("秋"))
                semesterFlags.Add(2);
            if (section.Semesters.Contains("冬"))
                semesterFlags.Add(3);


            ret.AddRange(
                from semesterFlag in semesterFlags
                from weekTypeFlag in weekTypeFlags
                select (semesterFlag << PlotFlag.SemesterOffset
                       | weekDayFlag << PlotFlag.WeekDayOffset
                       | weekTypeFlag << PlotFlag.WeekTypeOffset
                       | plotFlag << PlotFlag.PlotOffset, scheduleAndLocation.Location));

            return ret;
        });

    public static bool IsConflictWith(this SectionSnapshot lhs, SectionSnapshot rhs)
    {
        if (lhs.ExamTime is null || rhs.ExamTime is null || lhs.ExamTime.OverlapsWith(rhs.ExamTime))
        { return false; }

        foreach (var flagl in lhs.GetPlotFlagAndLocations().Select(pair => pair.PlotFlag))
        {
            foreach (var flagr in rhs.GetPlotFlagAndLocations().Select(pair => pair.PlotFlag))
            {
                var (semesterl, weekDayl, weekTypel, slotsl) = PlotFlag.ParsePlotFlag(flagl);
                var (semesterr, weekDayr, weekTyper, slotsr) = PlotFlag.ParsePlotFlag(flagr);

                if (semesterl == semesterr
                    && weekDayl == weekDayr
                    && weekTypel == weekTyper
                    && (slotsl & slotsr) != 0)
                { return true; }
            }
        }

        return false;
    }

    [GeneratedRegex(@"第([\d,]+)节")]
    private static partial Regex ParsePlotRegex();
    [GeneratedRegex("周(.)")]
    private static partial Regex ParseWeekDayRegex();
}
