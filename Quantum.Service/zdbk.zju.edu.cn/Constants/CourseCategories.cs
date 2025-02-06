using zdbk.zju.edu.cn.Enums;

namespace zdbk.zju.edu.cn.Constants;
public record CourseCategoryRecord(CourseCategory Id, string Name);

public static class CourseCategories
{
    public static readonly List<CourseCategoryRecord> All =
    [
        new CourseCategoryRecord(CourseCategory.MyCategory, "本专业课程"),
        new CourseCategoryRecord(CourseCategory.CompulsoryAll, "全部必修课程"),
        new CourseCategoryRecord(CourseCategory.CompulsoryIpm, "思政类/军体类"),
        new CourseCategoryRecord(CourseCategory.CompulsoryLan, "外语类"),
        new CourseCategoryRecord(CourseCategory.CompulsoryCom, "计算机类"),
        new CourseCategoryRecord(CourseCategory.CompulsoryEtp, "创新创业类"),
        new CourseCategoryRecord(CourseCategory.CompulsorySci, "自然科学通识类"),
        new CourseCategoryRecord(CourseCategory.ElectiveAll, "全部选修课程"),
        new CourseCategoryRecord(CourseCategory.ElectiveChC, "中华传统"),
        new CourseCategoryRecord(CourseCategory.ElectiveGlC, "世界文明"),
        new CourseCategoryRecord(CourseCategory.ElectiveSoc, "当代社会"),
        new CourseCategoryRecord(CourseCategory.ElectiveSci, "科技创新"),
        new CourseCategoryRecord(CourseCategory.ElectiveArt, "文艺审美"),
        new CourseCategoryRecord(CourseCategory.ElectiveBio, "生命探索"),
        new CourseCategoryRecord(CourseCategory.ElectiveTec, "博雅技艺"),
        new CourseCategoryRecord(CourseCategory.ElectiveGec, "通识核心课程"),
        new CourseCategoryRecord(CourseCategory.PhysicalEdu, "体育课程"),
        new CourseCategoryRecord(CourseCategory.MajorFundation, "专业基础课程"),
        new CourseCategoryRecord(CourseCategory.MyMajor, "本专业"),
        new CourseCategoryRecord(CourseCategory.AllMajor, "所有专业"),
        new CourseCategoryRecord(CourseCategory.AccreditedAll, "全部认定课程"),
        new CourseCategoryRecord(CourseCategory.AccreditedArt, "美育类"),
        new CourseCategoryRecord(CourseCategory.AccreditedLbr, "劳育类"),
        new CourseCategoryRecord(CourseCategory.International, "国际化课程"),
        new CourseCategoryRecord(CourseCategory.Ckc, "竺可桢学院课程"),
        new CourseCategoryRecord(CourseCategory.Honor, "荣誉课程")
    ];

    public static readonly List<CourseCategoryRecord> CompulsoryCourses = All.Where(c =>
        c.Id is CourseCategory.CompulsoryAll or
        CourseCategory.CompulsoryIpm or
        CourseCategory.CompulsoryLan or
        CourseCategory.CompulsoryCom or
        CourseCategory.CompulsoryEtp or
        CourseCategory.CompulsorySci).ToList();

    public static readonly List<CourseCategoryRecord> ElectiveCourses = All.Where(c =>
        c.Id is CourseCategory.ElectiveAll or
        CourseCategory.ElectiveChC or
        CourseCategory.ElectiveGlC or
        CourseCategory.ElectiveSoc or
        CourseCategory.ElectiveSci or
        CourseCategory.ElectiveArt or
        CourseCategory.ElectiveBio or
        CourseCategory.ElectiveTec or
        CourseCategory.ElectiveGec).ToList();

    public static readonly List<CourseCategoryRecord> MajorCourses = All.Where(c =>
        c.Id is CourseCategory.MyMajor or
        CourseCategory.AllMajor or
        CourseCategory.MajorFundation).ToList();

    public static readonly List<CourseCategoryRecord> AccreditedCourses = All.Where(c =>
        c.Id is CourseCategory.AccreditedAll or
        CourseCategory.AccreditedArt or
        CourseCategory.AccreditedLbr).ToList();

    public static readonly List<CourseCategoryRecord> SpecialCourses = All.Where(c =>
        c.Id is CourseCategory.International or
        CourseCategory.Ckc or
        CourseCategory.Honor).ToList();
}