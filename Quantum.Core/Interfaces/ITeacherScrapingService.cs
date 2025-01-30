using Quantum.Core.Models;

namespace Quantum.Core.Interfaces
{
    public interface ITeacherScrapingService
    {
        /// <summary>
        /// 初始化或更新教师数据库
        /// </summary>
        Task InitializeTeacherDataAsync();

        /// <summary>
        /// 根据教师姓名，课程名称，学院获取匹配教师
        /// </summary>
        /// <param name="instructorName">教师姓名</param>
        /// <param name="courseName">课程名称</param>
        /// <param name="college">教师所在学院</param>
        /// <returns></returns>
        Task<List<Teacher>> GetInstructorsAsync(string instructorName, string? courseName = null, string? college = null);
    }
}
