using Quantum.Core.Models;

namespace Quantum.Core.Interfaces;

public interface ICourseScrapingService
{
    Task<List<CourseSection>> GetChosenCourseSectionsAsync();

    // 获取课程
    // 需要的参数： 课程大类， 开始序号， 结束序号（允许越界）
    // 可选参数： 教学计划号 & 学年学期标识， 用于查询本类专业课程。
    Task<List<Course>> GetAvailableCoursesAsync(CourseCategory category, int start, int end);

    // 针对特定课程获取具体教学班
    // 前端返回的参数：Course
    // 抓包需要的参数： 课程大类， 学年， 学期(1春秋 2春夏)， 课程号
    Task<List<CourseSection>> GetSectionOfACourse(Course course, bool isPeClass = false);
}
