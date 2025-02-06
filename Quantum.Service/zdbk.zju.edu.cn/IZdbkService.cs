using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Utilities;
using zdbk.zju.edu.cn.Enums;
using zdbk.zju.edu.cn.Models;

namespace zdbk.zju.edu.cn;
public interface IZdbkService : IStatefulService<ZdbkState>, IPersistentService, IInitializableService
{
    public CachedEntity<HashSet<SectionSnapshot>> SelectedSections { get; }
    public CachedEntity<HashSet<SelectableCourse>> GraduationRequirement { get; }
    public Task UpdateSectionsAsync(SelectableCourse course);
    public Task UpdateIntroductionAsync(Course course);
    public Task<Result<HashSet<SelectableCourse>>> GetAvailableCoursesAsync(CourseCategory category, int start, int end);
}
