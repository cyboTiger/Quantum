using Quantum.Core.Interfaces;
using Quantum.Core.Models;
using Quantum.Infrastructure.States;
using System.Text.Json;

namespace Quantum.Infrastructure.Services;

public class CourseScrapingService(RequestClient client, UserState userState, ITeacherScrapingService teacherScrapingService) : ICourseScrapingService
{
    private string UserId => userState.CurrentUser?.Id ?? throw new Exception("用户信息丢失！");
    private string SemesterYear => userState.CurrentUser?.AcademicYear ?? throw new Exception("学年信息丢失！");
    private string Semester => userState.CurrentUser?.Semester ?? throw new Exception("学期信息丢失！");

    private const string BaseUrl = "http://zdbk.zju.edu.cn/jwglxt";
    private const string CourseSelectionUrl = "/xsxk/zzxkghb_cxZzxkGhbChoosed.html";
    private const string GetCourseUrl = "/xsxk/zzxkghb_cxZzxkGhbKcList.html";
    private const string GetInstructorUrl = "/xsxk/zzxkghb_cxZzxkGhbJxbList.html";

    public async Task<List<CourseSection>> GetChosenCourseSectionsAsync()
    {
        await client.EnsureClientSessionIsValidAsync();

        // Prepare form data for course selection query
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "xn", SemesterYear },
            { "xq", Semester }
        });

        var response = await client.PostAsync($"{BaseUrl}{CourseSelectionUrl}?gnmkdm=N253530&su={UserId}", formData);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var courseData = JsonSerializer.Deserialize<List<JsonElement>>(content);

        if (courseData is null)
            return [];

        return courseData.Select(json =>
        {
            var course = new Course
            {
                CourseCode = json.GetProperty("kcdm").GetString() ?? string.Empty,
                Name = json.GetProperty("kcmc").GetString() ?? string.Empty,
                Credits = decimal.Parse(json.GetProperty("xf").GetString() ?? "0"),
                WeekTime = json.GetProperty("zxs").GetString() ?? string.Empty,
                SearchIndex =
                    $"{json.GetProperty("kcmc").GetString() ?? string.Empty}{json.GetProperty("xskcdm").GetString() ?? string.Empty}",
                Status = CourseStatus.Selected
            };

            course.Sections = [
                new CourseSection {
                Course = course,
                InstructorName = json.GetProperty("jsxm").GetString() ?? string.Empty,
                Schedule = json.GetProperty("sksj").GetString() ?? string.Empty,
                Location = json.GetProperty("skdd").GetString() ?? string.Empty,
                ExamTime = json.TryGetProperty("vkssj", out var examTime)
                            ? ParseExamTime(examTime.GetString())
                            : null,
                Semester = json.GetProperty("xxq").GetString() ?? string.Empty
            }];
            return course.Sections[0];
        }).ToList();
    }

    public async Task<List<Course>> GetAvailableCoursesAsync(CourseCategory category, int start, int end)
    {
        await client.EnsureClientSessionIsValidAsync();

        // 构造请求URL（包含查询参数）
        var requestUrl = $"{BaseUrl}{GetCourseUrl}?gnmkdm=N253530&su={UserId}";

        // 查询dl、lx、xkmc
        var (dl, lx, xkmc) = GetCourseType(category);

        // 构造POST数据（xkmc为空则不构造）

        var nj = userState.CurrentUser?.Grade ?? throw new Exception("用户未登录");
        var zydm = userState.CurrentUser?.Major ?? throw new Exception("用户未登录");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "dl", dl },
            { "lx", lx },
            { "nj", nj },
            { "xn", SemesterYear },
            { "xq", Semester },
            { "zydm", zydm },
            { "jxjhh", $"{nj}{zydm}" },
            { "xnxq", $"({SemesterYear}-{Semester})-" },
            { "kspage", start.ToString() },
            { "jspage", end.ToString() }
        }.Concat(xkmc is not null ? [new KeyValuePair<string, string>("xkmc", xkmc)] : Array.Empty<KeyValuePair<string, string>>()));

        // 发送请求
        var response = await client.PostAsync(requestUrl, formData);
        response.EnsureSuccessStatusCode();

        // 解析响应内容
        var content = await response.Content.ReadAsStringAsync();
        var courseData = JsonSerializer.Deserialize<List<JsonElement>>(content);

        if (courseData is null)
            return [];

        return courseData.Select(json =>
        {
            var kcxxParts = json.GetProperty("kcxx").GetString()?.Split('~');
            var credits = kcxxParts?.Length > 1 ? decimal.Parse(kcxxParts[1]) : 0m;
            var weekTime = kcxxParts?.Length > 2 ? kcxxParts[2] : string.Empty;

            return new Course
            {
                CourseCode = json.GetProperty("kcdm").GetString() ?? string.Empty,
                Name = json.GetProperty("kcmc").GetString() ?? string.Empty,
                Credits = credits,
                WeekTime = weekTime,
                Category = category,
                Department = json.TryGetProperty("kkxy", out var kkxy) ? kkxy.GetString() ?? string.Empty : string.Empty,
                Property = json.TryGetProperty("kcxz", out var kcxz) ? kcxz.GetString() ?? string.Empty : string.Empty,
                SearchIndex = $"{json.GetProperty("kcmc").GetString() ?? string.Empty}{json.GetProperty("xskcdm").GetString() ?? string.Empty}",
                Sections = []
            };
        }).ToList();
    }

    public async Task<List<CourseSection>> GetSectionOfACourse(Course course, bool isPeClass)
    {
        // 构造请求URL
        var requestUrl = $"{BaseUrl}{GetInstructorUrl}?gnmkdm=N253530&su={UserId}";

        var year = SemesterYear;
        var semester = Semester;
        var courseCode = course.CourseCode;
        // 构造POST数据
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "xn", year },
            { "xq", semester },
            { "xkkh", $"{(isPeClass ? "4" : "T")}({year}-{semester})-{courseCode}" }
        });

        // 发送请求
        var response = await client.PostAsync(requestUrl, formData);
        response.EnsureSuccessStatusCode();

        // 解析响应内容
        var content = await response.Content.ReadAsStringAsync();
        var sectionData = JsonSerializer.Deserialize<List<JsonElement>>(content);

        if (sectionData is null)
            return [];

        var ret = new List<CourseSection>();

        foreach (var json in sectionData)
        {
            var instructorName = json.GetProperty("jsxm").GetString() ?? string.Empty;

            // 解析容量信息
            var (availableSeats, capacity) = ParseCapacity(json.GetProperty("rs").GetString() ?? "0/0");
            var (majorWaiting, totalWaiting) = ParseWaitingCount(json.GetProperty("yxrs").GetString() ?? "0~0");

            var section = new CourseSection
            {
                Course = course,
                InstructorName = instructorName,
                Schedule = json.GetProperty("sksj").GetString() ?? string.Empty,
                Location = json.GetProperty("skdd").GetString() ?? string.Empty,
                ExamTime = json.TryGetProperty("kssj", out var examTime)
                    ? ParseExamTime(examTime.GetString() ?? string.Empty)
                    : null,
                Semester = json.GetProperty("xxq").GetString() ?? string.Empty,
                IsInternational = (json.GetProperty("gjhkc").GetString() ?? "否") == "是",
                TeachingForm = json.GetProperty("jxfs").GetString() ?? string.Empty,
                TeachingMethod = json.GetProperty("skxs").GetString() ?? string.Empty,
                AvailableSeats = availableSeats,
                Capacity = capacity,
                MajorWaitingCount = majorWaiting,
                TotalWaitingCount = totalWaiting
            };
            var teachers = (await Task.WhenAll(
                section.InstructorNames.Select(async instructor => (
                    await teacherScrapingService.GetInstructorsAsync(
                        instructor,
                        course.Name,
                        course.Department)
                ).FirstOrDefault() ?? new Teacher()))).ToList();

            section.Instructors = teachers;
            ret.Add(section);
        }

        return ret;
    }

    private static (string dl, string lx, string? xkmc) GetCourseType(CourseCategory category) => category switch
    {
        CourseCategory.MyCategory => ("xk_1", "bl", "本类(专业)选课"),
        CourseCategory.CompulsoryAll => ("xk_b", "bl", "全部课程"),
        CourseCategory.CompulsoryIPM => ("E", "zl", "思政类\\军体类"),
        CourseCategory.CompulsoryLan => ("F", "zl", "外语类"),
        CourseCategory.CompulsoryCom => ("G", "zl", "计算机类"),
        CourseCategory.CompulsoryEtp => ("P", "zl", "创新创业类"),
        CourseCategory.CompulsorySci => ("T", "zl", "自然科学通识类"),
        CourseCategory.ElectiveAll => ("xk_n", "bl", "全部课程"),
        CourseCategory.ElectiveChC => ("zhct", "zl", "中华传统"),
        CourseCategory.ElectiveGlC => ("sjwm", "zl", "世界文明"),
        CourseCategory.ElectiveSoc => ("ddsh", "zl", "当代社会"),
        CourseCategory.ElectiveSci => ("kjcx", "zl", "科技创新"),
        CourseCategory.ElectiveArt => ("wysm", "zl", "文艺审美"),
        CourseCategory.ElectiveBio => ("smts", "zl", "生命探索"),
        CourseCategory.ElectiveTec => ("byjy", "zl", "博雅技艺"),
        CourseCategory.ElectiveGEC => ("xhxk", "zl", "通识核心课程"),
        CourseCategory.PhysicalEdu => ("xk_8", "bl", "体育课程"),
        CourseCategory.MajorFundation => ("xk_zyjckc", "bl", "专业基础课程"),
        CourseCategory.MyMajor => ("zy_b", "bl", "本类(专业)"),
        CourseCategory.AllMajor => ("zy_qb", "bl", "所有类(专业)"),
        CourseCategory.AccreditedAll => ("xk_rdxkc", "bl", "qbkc"),
        CourseCategory.AccreditedArt => ("xk_rdxkc", "zl", "美育类"),
        CourseCategory.AccreditedLbr => ("xk_rdxkc", "zl", "劳育类"),
        CourseCategory.International => ("gjhkc", "zl", "国际化课程"),
        CourseCategory.CKC => ("Z", "bl", "竺可桢学院课程"),
        CourseCategory.Honor => ("R", "bl", "荣誉课程"),
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
    };

    private static (int available, int total) ParseCapacity(string capacityStr)
    {
        try
        {
            var parts = capacityStr.Split('/');
            if (parts.Length != 2)
                return (0, 0);

            var available = int.Parse(parts[0]);
            var total = int.Parse(parts[1]);
            return (available, total);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static (int major, int total) ParseWaitingCount(string waitingStr)
    {
        try
        {
            var parts = waitingStr.Split('~');
            if (parts.Length != 2)
                return (0, 0);

            var major = int.Parse(parts[0]);
            var total = int.Parse(parts[1]);
            return (major, total);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static TimeSlot? ParseExamTime(string? examTimeStr)
    {
        if (string.IsNullOrEmpty(examTimeStr))
            return null;

        try
        {
            return TimeSlot.Parse(examTimeStr);
        }
        catch
        {
            return null;
        }
    }
}
