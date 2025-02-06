namespace zdbk.zju.edu.cn.Models;

/// <summary>
/// 表示选课界面可选择的课程
/// </summary>
public class SelectableCourse : StatefulCourse
{
    public required string Code { get; init; } // 选课课号
}