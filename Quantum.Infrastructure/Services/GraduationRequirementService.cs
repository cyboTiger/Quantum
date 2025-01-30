using Quantum.Core.Interfaces;
using Quantum.Core.Models;
using Quantum.Infrastructure.States;
using System.Text.Json;

namespace Quantum.Infrastructure.Services;

public class GraduationRequirementService(RequestClient client, UserState userState) : IGraduationRequirementService
{
    public async Task<List<Course>> GetGraduationRequirementsAsync()
    {
        await client.EnsureClientSessionIsValidAsync();

        var userId = userState.CurrentUser?.Id ?? throw new Exception("用户未登录");
        var url = $"http://zdbk.zju.edu.cn/jwglxt/bysh/byshck_cxByshzsIndex.html?doType=query&gnmkdm=N6025&su={userId}";


        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement[]>(content);

        if (data is null)
            return [];

        return (from item in data
                where item.TryGetProperty("KCDM", out _)
                      && item.TryGetProperty("JDMC", out var courseName)
                      && courseName.GetString() != "课程名称"
                      && item.TryGetProperty("KCXF", out _)
                select new Course
                {
                    CourseCode = item.GetProperty("KCDM").GetString() ?? string.Empty,
                    Name = item.GetProperty("JDMC").GetString() ?? string.Empty,
                    Credits = decimal.Parse(item.GetProperty("KCXF").GetString() ?? "0"),
                    Status = GetCourseStatus(item),
                    SearchIndex = $"{item.GetProperty("JDMC").GetString() ?? string.Empty}{item.GetProperty("KCDM").GetString() ?? string.Empty}",
                })
                .DistinctBy(x => x.CourseCode)
                .ToList();
    }

    private static CourseStatus GetCourseStatus(JsonElement item)
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



    //������Щ�����ڱ�ҵ����������Ҫ��������������������������Ҫ
    //public async Task<Titles> GetTitleAsync()
    //{
    //    return null;
    //}

    //public async Task<List<CourseClass>> GetCourseClassAsync()
    //{
    //    return null;
    //}

    //public async Task<List<EachCourse>> GetEachCourseAsync(CourseClass courseClass, int begin, int end)
    //{
    //    return null;
    //}
}
