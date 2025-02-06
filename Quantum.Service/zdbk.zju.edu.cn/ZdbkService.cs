using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Quantum.Infrastructure.Models;
using Quantum.Infrastructure.Utilities;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using zdbk.zju.edu.cn.Enums;
using zdbk.zju.edu.cn.Models;
using zjuam.zju.edu.cn;
using Result = Quantum.Infrastructure.Utilities.Result;

namespace zdbk.zju.edu.cn;

public partial class ZdbkService : PersistentStatefulService<ZdbkState>, IZdbkService
{
    private const string SsoLoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string BaseUrl = "http://zdbk.zju.edu.cn/jwglxt";
    private const string GetInfoUrl = "/xsxk/zzxkghb_cxZzxkGhbIndex.html";
    private const string SsoRedirectUrl = "/xtgl/login_ssologin.html";
    private const string RedirectUrl = "/xtgl/dl_loginForward.html";
    private const string SectionSelectedUrl = "/xsxk/zzxkghb_cxZzxkGhbChoosed.html";
    private const string GetCourseUrl = "/xsxk/zzxkghb_cxZzxkGhbKcList.html";
    private const string GetSectionUrl = "/xsxk/zzxkghb_cxZzxkGhbJxbList.html";
    private const string GetGraduationRequirementUrl = "/bysh/byshck_cxByshzsIndex.html";
    private const string GetIntroductionUrl = "/xkjjsc/kcjjck_cxXkjjPage.html";

    private readonly IZjuamService _zjuamService;
    private readonly ILogger<ZdbkService> _logger;
    public CachedEntity<HashSet<SectionSnapshot>> SelectedSections { get; }
    public CachedEntity<HashSet<SelectableCourse>> GraduationRequirement { get; }

    public ZdbkService(IZjuamService zjuamService, ILogger<ZdbkService> logger)
        : base("zdbk.json".ToDataPath(),
            async state => await Task.Run(() => ZdbkState.Serialize(state)),
            async data => await Task.Run(() => ZdbkState.Deserialize(data)))
    {
        _zjuamService = zjuamService;
        _zjuamService.OnLogout += async () => await LogoutAsync();
        _logger = logger;
        SelectedSections = new CachedEntity<HashSet<SectionSnapshot>>(
            GetSelectedSectionsAsync,
            []
        );
        GraduationRequirement = new CachedEntity<HashSet<SelectableCourse>>(
            GetGraduationRequirementsAsync,
            []
        );
    }

    public async Task<Result<HashSet<SelectableCourse>>> GetAvailableCoursesAsync(CourseCategory category, int start, int end)
    {
        await InitializeTask;

        if (!IsLoggedIn)
        {
            var tokenResult = await ValidOrRefreshTokenAsync();
            if (!tokenResult.IsSuccess)
            { return Result.Failure(Messages.NotLoggedIn); }
        }

        using var client = RequestClient.Create(new RequestOptions
        {
            Cookies = [State!.JSessionId, State!.Route]
        });

        // 构造请求URL（包含查询参数）
        var requestUrl = $"{BaseUrl}{GetCourseUrl}?gnmkdm=N253530&su={State.Id}";

        // 查询dl、lx、xkmc
        var (dl, lx, xkmc) = GetCourseType(category);

        // 构造POST数据（xkmc为空则不构造）
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "dl", dl },
            { "lx", lx },
            { "nj", State.Grade },
            { "xn", State.AcademicYear },
            { "xq", State.Semester },
            { "zydm", State.Major },
            { "jxjhh", $"{State.Grade}{State.Major}" },
            { "xnxq", $"({State.AcademicYear}-{State.Semester})-" },
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
        {
            return Result.Failure(Messages.UnknownError);
        }

        return Result.Success(
            courseData.Select(json =>
            {
                var kcxxParts = json.GetProperty("kcxx").GetString()?.Split('~');
                var credits = kcxxParts?.Length > 1 ? decimal.Parse(kcxxParts[1]) : 0m;
                var weekTime = kcxxParts?.Length > 2 ? kcxxParts[2] : string.Empty;

                var course = new SelectableCourse
                {
                    Id = json.GetProperty("kcdm").GetString() ?? string.Empty,
                    Code = json.GetProperty("xkkh").GetString() ?? string.Empty,
                    Name = json.GetProperty("kcmc").GetString() ?? string.Empty,
                    Credits = credits,
                    WeekTime = weekTime,
                    Category = category,
                    Department = json.TryGetProperty("kkxy", out var kkxy) ? kkxy.GetString() ?? string.Empty : string.Empty,
                    Property = json.TryGetProperty("kcxz", out var kcxz) ? kcxz.GetString() ?? string.Empty : string.Empty,
                    Status = int.Parse(json.GetProperty("kcxzzt").GetString() ?? "0") is 1 ? CourseStatus.Selected : CourseStatus.NotSelected,
                };
                return course;
            }).ToHashSet());

        (string dl, string lx, string? xkmc) GetCourseType(CourseCategory cate) => cate switch
        {
            CourseCategory.MyCategory => ("xk_1", "bl", "本类(专业)选课"),
            CourseCategory.CompulsoryAll => ("xk_b", "bl", "全部课程"),
            CourseCategory.CompulsoryIpm => ("E", "zl", "思政类\\军体类"),
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
            CourseCategory.ElectiveGec => ("xhxk", "zl", "通识核心课程"),
            CourseCategory.PhysicalEdu => ("xk_8", "bl", "体育课程"),
            CourseCategory.MajorFundation => ("xk_zyjckc", "bl", "专业基础课程"),
            CourseCategory.MyMajor => ("zy_b", "bl", "本类(专业)"),
            CourseCategory.AllMajor => ("zy_qb", "bl", "所有类(专业)"),
            CourseCategory.AccreditedAll => ("xk_rdxkc", "bl", "qbkc"),
            CourseCategory.AccreditedArt => ("xk_rdxkc", "zl", "美育类"),
            CourseCategory.AccreditedLbr => ("xk_rdxkc", "zl", "劳育类"),
            CourseCategory.International => ("gjhkc", "zl", "国际化课程"),
            CourseCategory.Ckc => ("Z", "bl", "竺可桢学院课程"),
            CourseCategory.Honor => ("R", "bl", "荣誉课程"),
            CourseCategory.Undefined => default!,
            _ => throw new ArgumentOutOfRangeException(nameof(cate), cate, null)
        };
    }

    public bool IsLoggedIn => State is not null;

    public async Task<bool> IsTokenValidAsync()
    {
        if (!IsLoggedIn)
        { return false; }
        try
        {
            using var client = RequestClient.Create(new RequestOptions
            {
                Cookies = [State!.JSessionId, State!.Route]
            });

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await client.GetAsync($"{BaseUrl}{RedirectUrl}", cts.Token);

            return response.RequestMessage is not null && response.RequestMessage.ToString().Contains("index_initMenu.html");
        }
        catch
        { return false; }

    }

    public async Task<Result> ValidOrRefreshTokenAsync()
    {
        if (await IsTokenValidAsync())
        { return Result.Success(Messages.LoginSuccessful); } // Cookie 仍然有效

        return _zjuamService.IsAuthenticated
            ? await LoginAsync(_zjuamService.State!.Id)
            : Result.Failure(Messages.NotLoggedIn);
    }

    public async Task LogoutAsync()
    {
        State = null;
        await SaveStateAsync();
    }

    public async Task UpdateIntroductionAsync(Course course)
    {
        await InitializeTask;

        if (!IsLoggedIn)
        {
            var tokenResult = await ValidOrRefreshTokenAsync();
            if (!tokenResult.IsSuccess)
            { return; }
        }

        using var client = RequestClient.Create(new RequestOptions
        {
            Cookies = [State!.JSessionId, State!.Route]
        });


        var response = await client.GetAsync($"{BaseUrl}{GetIntroductionUrl}?xkjjid={course.Id}&htmlType=kcjj");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // 从 input 标签中提取 HTML 内容
        var match = ExtractCourseIntroductionRegex().Match(content);
        if (match.Success)
        { course.Introduction = HttpUtility.HtmlDecode(match.Groups[1].Value); }
    }

    public async Task UpdateSectionsAsync(SelectableCourse course)
    {
        await InitializeTask;

        if (!IsLoggedIn)
        {
            var tokenResult = await ValidOrRefreshTokenAsync();
            if (!tokenResult.IsSuccess)
            { return; }
        }

        using var client = RequestClient.Create(new RequestOptions
        {
            Cookies = [State!.JSessionId, State.Route]
        });
        // 构造请求URL
        var requestUrl = $"{BaseUrl}{GetSectionUrl}?gnmkdm=N253530&su={State.Id}";
        // 构造POST数据
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "xn", State.AcademicYear },
            { "xq", State.Semester},
            { "xkkh", course.Code }
        });

        // 发送请求
        var response = await client.PostAsync(requestUrl, formData);
        response.EnsureSuccessStatusCode();

        // 解析响应内容
        var content = await response.Content.ReadAsStringAsync();
        var sectionData = JsonSerializer.Deserialize<List<JsonElement>>(content);

        if (sectionData is null)
        { return; }

        var ret = new HashSet<SelectableSection>();

        foreach (var json in sectionData)
        {
            // 解析容量信息
            var (availableSeats, capacity) = ParseCapacity(json.GetProperty("rs").GetString() ?? "0/0");
            var (majorWaiting, totalWaiting) = ParseWaitingCount(json.GetProperty("yxrs").GetString() ?? "0~0");

            TimeSlot? examTime;

            try
            {
                examTime = TimeSlot.Parse(json.GetProperty("kssj").GetString()!);
            }
            catch (Exception)
            {
                examTime = null;
            }


            var section = new SelectableSection
            {
                Id = Assertion.NotNullNorEmpty(json.GetProperty("xkkh").GetString()),
                Course = course,
                Instructors = [.. Assertion.NotNullNorEmpty(json.GetProperty("jsxm").GetString()).Split("<br>")],
                ScheduleAndLocations = Assertion.NotNullNorEmpty(json.GetProperty("sksj").GetString())
                                        .Split("<br>")
                                        .Zip(Assertion.NotNullNorEmpty(json.GetProperty("skdd").GetString()).Split("<br>"))
                                        .Select(pair => (pair.First, pair.Second))
                                        .ToHashSet(),
                ExamTime = examTime,
                Semesters = Assertion.NotNullNorEmpty(json.GetProperty("xxq").GetString()),
                IsInternational = (json.GetProperty("gjhkc").GetString() ?? "否") == "是",
                TeachingForm = Assertion.NotNullNorEmpty(json.GetProperty("jxfs").GetString()),
                TeachingMethod = Assertion.NotNullNorEmpty(json.GetProperty("skxs").GetString()),
                AvailableSeats = availableSeats,
                Capacity = capacity,
                MajorWaitingCount = majorWaiting,
                TotalWaitingCount = totalWaiting
            };
            ret.Add(section);
        }

        course.Sections.Clear();
        course.Sections.AddRange(ret);

        return;

        (int Available, int Total) ParseCapacity(string capacityStr)
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

        (int Major, int Total) ParseWaitingCount(string waitingStr)
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
    }

    private async Task<Result> LoginAsync(string username)
    {
        var zjuamResult = await _zjuamService.ValidOrRefreshTokenAsync();
        return zjuamResult.IsSuccess
            ? await LoginOnce()
            : Result.Failure(zjuamResult.Message);

        async Task<Result> LoginOnce()
        {
            var clientResult = await _zjuamService.GetAuthencatedClientAsync(new RequestOptions
            {
                AllowRedirects = true
            });
            if (!clientResult.IsSuccess)
            { return Result.Failure(clientResult.Message); }// 统一身份认证失败

            using var client = clientResult.Value!; // 已登录到统一身份认证的 RequestClient，Cookie 不一定还有效

            var ssoResponse = await client.GetAsync($"{SsoLoginUrl}?service={Uri.EscapeDataString($"{BaseUrl}{SsoRedirectUrl}")}");

            // ReSharper disable once InvertIf
            if (ssoResponse.Headers.Location is not null) // 重定向成功
            {
                var redirectResponse = await client.GetAsync(ssoResponse.Headers.Location);
                if (!redirectResponse.IsSuccessStatusCode)
                { return Result.Failure(Messages.NotLoggedIn); }
                var cookies = client.CookieContainer.GetCookies(new Uri(BaseUrl));
                var jSessionId = Assertion.NotNull(cookies["JSESSIONID"]);
                var route = Assertion.NotNull(cookies["route"]);

                var response = await client.GetAsync($"{BaseUrl}{GetInfoUrl}?gnmkdm=N253530&su={username}");
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 提取信息
                var name = Assertion.NotNullNorEmpty(doc.DocumentNode.SelectSingleNode("//*[@id='chi']/div/div[1]/h5/span[1]/font/b")?.InnerText);
                var studentId = ExtractStudentIdRegex()
                    .Match(Assertion.NotNullNorEmpty(doc.DocumentNode.SelectSingleNode("//*[@id='chi']/div/div[1]/h5/span[1]/text()")?.InnerText))
                    .Groups[1].Value;
                var academicYear = Assertion.NotNullNorEmpty(doc.DocumentNode.SelectSingleNode("//*[@id='xkxn']")?.InnerText);
                var grade = Assertion.NotNullNorEmpty(doc.DocumentNode.SelectSingleNode("//*[@id='nj']")?.GetAttributeValue("value", ""));
                var majorCode = Assertion.NotNullNorEmpty(doc.DocumentNode.SelectSingleNode("//*[@id='zydm']")?.GetAttributeValue("value", ""));
                var semester = Assertion.NotNullNorEmpty(doc.DocumentNode.SelectSingleNode("//*[@id='xq']")?.GetAttributeValue("value", ""));

                _logger.LogInformation($"Name: {name}\n" +
                                      $"Student ID: {studentId}\n" +
                                      $"Academic Year: {academicYear}\n" +
                                      $"Grade: {grade}\n" +
                                      $"Major Code: {majorCode}\n" +
                                      $"Semester: {semester}\n");

                State = new ZdbkState(jSessionId, route, studentId, name, grade, majorCode, academicYear, semester);
                await SaveStateAsync();
                return Result.Success(Messages.LoginSuccessful);
            }

            return Result.Failure(Messages.NotLoggedIn);
        }
    }

    private async Task<Result<HashSet<SectionSnapshot>>> GetSelectedSectionsAsync()
    {
        await InitializeTask;

        if (!IsLoggedIn)
        {
            var tokenResult = await ValidOrRefreshTokenAsync();
            if (!tokenResult.IsSuccess)
            { return Result.Failure(Messages.NotLoggedIn); }
        }

        using var client = RequestClient.Create(new RequestOptions
        {
            Cookies = [State!.JSessionId, State!.Route]
        });

        // Prepare form data for course selection query
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "xn", State.AcademicYear },
            { "xq", State.Semester }
        });

        var response = await client.PostAsync($"{BaseUrl}{SectionSelectedUrl}?gnmkdm=N253530&su={State.Id}", formData);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        List<JsonElement>? courseData;
        try
        {
            courseData = JsonSerializer.Deserialize<List<JsonElement>>(content); // 如果Session过期，则这里会抛异常
        }
        catch (JsonException)
        {
            await ValidOrRefreshTokenAsync();
            courseData = JsonSerializer.Deserialize<List<JsonElement>>(content); // 再抛异常的话，就让他冒到顶上
        }

        if (courseData is null)
        { return Result.Failure(Messages.FailedToGetSelectedCourses); }

        return Result.Success(courseData.Select(json =>
        {
            var course = new SelectableCourse
            {
                Id = Assertion.NotNullNorEmpty(json.GetProperty("kcdm").GetString()),
                Code = json.GetProperty("xkkh").GetString() ?? string.Empty,
                Name = json.GetProperty("kcmc").GetString() ?? string.Empty,
                Credits = decimal.Parse(json.GetProperty("xf").GetString() ?? "0"),
                WeekTime = json.GetProperty("zxs").GetString() ?? string.Empty,
                Status = CourseStatus.Selected
            };

            TimeSlot? examTime;

            if (!json.TryGetProperty("vkssj", out var examTimeElement) || string.IsNullOrEmpty(examTimeElement.GetString()))
            {
                examTime = null;
            }
            else
            {
                examTime = TimeSlot.Parse(examTimeElement.GetString()!);
            }

            var section = new SelectableSection
            {
                Id = Assertion.NotNullNorEmpty(json.GetProperty("xkkh").GetString()),
                Course = course,
                Instructors = [.. Assertion.NotNullNorEmpty(json.GetProperty("jsxm").GetString()).Split("<br>")],
                ScheduleAndLocations = Assertion.NotNullNorEmpty(json.GetProperty("sksj").GetString())
                    .Split("<br>")
                    .Zip(Assertion.NotNullNorEmpty(json.GetProperty("skdd").GetString()).Split("<br>"))
                    .Select(pair => (pair.First, pair.Second))
                    .ToHashSet(),
                Semesters = Assertion.NotNullNorEmpty(json.GetProperty("xxq").GetString()),
                ExamTime = examTime,
            };

            return section.CreateSnapshot();
        }).ToHashSet());
    }

    private async Task<Result<HashSet<SelectableCourse>>> GetGraduationRequirementsAsync()
    {
        await InitializeTask;

        if (!IsLoggedIn)
        {
            var tokenResult = await ValidOrRefreshTokenAsync();
            if (!tokenResult.IsSuccess)
            { return Result.Failure(Messages.NotLoggedIn); }
        }

        using var client = RequestClient.Create(new RequestOptions
        {
            Cookies = [State!.JSessionId, State!.Route]
        });
        var url = $"{BaseUrl}{GetGraduationRequirementUrl}?doType=query&gnmkdm=N6025&su={State.Id}";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement[]>(content);

        if (data is null)
        { return Result.Failure(Messages.UnknownError); }

        var ret = new HashSet<SelectableCourse>();

        foreach (var item in data)
        {
            if (!item.TryGetProperty("KCDM", out _)
                || !item.TryGetProperty("JDMC", out var courseName)
                || courseName.GetString() == "课程名称"
                || !item.TryGetProperty("KCXF", out _))
                continue;

            var id = Assertion.NotNullNorEmpty(item.GetProperty("KCDM").GetString());
            var code = $"T({State.AcademicYear}-{State.Semester})-{id}";

            var course = new SelectableCourse
            {
                Id = id,
                Code = code,
                Name = Assertion.NotNullNorEmpty(item.GetProperty("JDMC").GetString()),
                Credits = decimal.Parse(item.GetProperty("KCXF").GetString() ?? "0"),
                Status = GetCourseStatus(item)
            };
            ret.Add(course);
        }

        return Result.Success(ret);

        static CourseStatus GetCourseStatus(JsonElement item)
        {
            if (item.TryGetProperty("SFTG", out var value))
            {
                var status = value.GetString();
                return status switch
                {
                    "通过" => CourseStatus.Passed,
                    "不通过" => CourseStatus.Failed,
                    _ => CourseStatus.Unknown
                };
            }

            if (item.TryGetProperty("KCBZ", out var kcbz) && kcbz.GetString() == "在修")
            {
                return CourseStatus.Selected;
            }

            return CourseStatus.NotSelected;
        }

    }

    [GeneratedRegex(@"\((\d+)\)", RegexOptions.Singleline)]
    private static partial Regex ExtractStudentIdRegex();

    public bool IsInitialized { get; private set; }
    public Task InitializeTask { get; private set; } = Task.CompletedTask;
    public async Task InitializeAsync()
    {
        if (IsInitialized)
        { return; }

        InitializeTask = LoadStateAsync().ContinueWith(_ => IsInitialized = true);
        await InitializeTask;
    }

    [GeneratedRegex("""
                    <input[^>]*name="xkjjHtml"[^>]*value="([\s\S]*?)"
                    """, RegexOptions.Singleline)]
    private static partial Regex ExtractCourseIntroductionRegex();
}

internal class Messages
{
    public const string UnknownError = "Unknown error occurred.";
    public const string NotLoggedIn = "User is not logged in.";
    public const string LoginSuccessful = "Login successful.";
    public const string FailedToGetSelectedCourses = "Failed to get selected courses.";
    public const string NoNeedToUpdateSections = "No need to update sections.";
    public const string FailedToGetCourseIntroduction = "Failed to get course introduction";
}