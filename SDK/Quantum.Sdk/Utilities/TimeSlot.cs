namespace Quantum.Infrastructure.Utilities;
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
