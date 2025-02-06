using zdbk.zju.edu.cn.Enums;

namespace zdbk.zju.edu.cn.Models;
public class StatefulCourse : Course
{
    public required CourseStatus Status { get; set; }
}
