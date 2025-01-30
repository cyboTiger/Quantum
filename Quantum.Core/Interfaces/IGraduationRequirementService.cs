using Quantum.Core.Models;

namespace Quantum.Core.Interfaces;

public interface IGraduationRequirementService
{
    // TODO: Implement actual graduation requirement logic
    Task<List<Course>> GetGraduationRequirementsAsync();

    //�������������Ǹ���ÿ����Ψһȷ����
    //Task<Titles> GetTitleAsync();
    //Task<List<CourseClass>> GetCourseClassAsync();

    //������������
    //Task<List<EachCourse>> GetEachCourseAsync(CourseClass courseClass, int begin, int end);
}