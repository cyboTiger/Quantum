namespace zdbk.zju.edu.cn.Constants;
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