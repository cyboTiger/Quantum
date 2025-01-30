using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Quantum.Core.Interfaces;
using Quantum.Core.Models;
using Quantum.Core.Repository;
using System.Data;
using System.Text.Json;

namespace Quantum.Infrastructure.Services;

public class TeacherScrapingService(QuantumDbContext dbContext) : ITeacherScrapingService
{
    private readonly HttpClient _httpClient = new();
    private const string BaseUrl = "https://chalaoshi.click";
    private const int BatchSize = 100;
    private const int MaxFailCount = 10;
    private static bool _isInited;

    private async Task<int> GetLastTeacherIdAsync()
    {
        var lastTeacherIdStr = await dbContext.Configs
            .Where(c => c.Key == DatabaseConfig.ConfigKeys.LastTeacherId)
            .Select(c => c.Value)
            .FirstOrDefaultAsync() ?? "0";

        return int.Parse(lastTeacherIdStr);
    }

    private async Task UpdateLastTeacherIdAsync(int lastTeacherId)
    {
        var existing = await dbContext.Configs.FirstOrDefaultAsync(c => c.Key == DatabaseConfig.ConfigKeys.LastTeacherId);
        if (existing is not null)
        {
            existing.Value = lastTeacherId.ToString();
        }
        else
        {
            await dbContext.Configs.AddAsync(new DatabaseConfig
            {
                Key = DatabaseConfig.ConfigKeys.LastTeacherId,
                Value = lastTeacherId.ToString()
            });
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task InitializeTeacherDataAsync()
    {
        if (_isInited)
            return;
        _isInited = true;

        if (dbContext.Configs.FirstOrDefault(cfg => cfg.Key == DatabaseConfig.ConfigKeys.TeacherDataInitialized) is null)
        {
            // 先尝试从JSON文件初始化
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "teachers.json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(jsonPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var teachers = JsonSerializer.Deserialize<List<Teacher>>(json, options);
                    if (teachers?.Count > 0)
                    {
                        foreach (var teacher in teachers)
                        {
                            var existingTeacher = await dbContext.Teachers
                                .FirstOrDefaultAsync(t => t.Id == teacher.Id);

                            if (existingTeacher is not null)
                            {
                                dbContext.Entry(existingTeacher).CurrentValues.SetValues(teacher);
                            }
                            else
                            {
                                await dbContext.Teachers.AddAsync(teacher);
                            }
                        }
                        dbContext.Configs.Add(new DatabaseConfig
                        {
                            Key = DatabaseConfig.ConfigKeys.TeacherDataInitialized,
                            Value = DatabaseConfig.ConfigKeys.TeacherDataInitialized
                        });
                        await UpdateLastTeacherIdAsync(teachers.Max(t => t.Id));
                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"从JSON文件成功导入 {teachers.Count} 位教师");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"从JSON文件导入失败：{ex.Message}");
                }
            }
        }

        var lastTeacherId = await GetLastTeacherIdAsync();
        var lastSuccessTeacherId = lastTeacherId;
        var teacherId = lastTeacherId + 1;
        var batchCount = 0;
        var failCount = 0;

        while (true)
        {
            try
            {
                var teacher = await ScrapeTeacherDataAsync(teacherId);
                if (teacher is null)
                {
                    failCount++;
                    if (failCount >= MaxFailCount)
                        break;
                    teacherId++;
                    continue;
                }

                failCount = 0;
                lastSuccessTeacherId = teacherId;

                var existingTeacher = await dbContext.Teachers
                    .FirstOrDefaultAsync(t => t.Id == teacherId);

                teacher.Id = teacherId;
                teacher.LastUpdatedOn = DateTime.Now;

                if (existingTeacher is not null)
                {
                    dbContext.Entry(existingTeacher).CurrentValues.SetValues(teacher);
                }
                else
                {
                    await dbContext.Teachers.AddAsync(teacher);
                }

                await dbContext.SaveChangesAsync();

                Console.WriteLine($"{teacher.Id} {teacher.Name} {teacher.Rating} {teacher.Courses}");

                batchCount++;
                if (batchCount >= BatchSize)
                {
                    await UpdateLastTeacherIdAsync(lastSuccessTeacherId);
                    batchCount = 0;
                }

                teacherId++;
            }
            catch (HttpRequestException)
            {
                await UpdateLastTeacherIdAsync(lastSuccessTeacherId);
                break;
            }
        }

        await UpdateLastTeacherIdAsync(lastSuccessTeacherId);
    }

    private async Task<Teacher?> ScrapeTeacherDataAsync(int teacherId)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/t/{teacherId}/");
        if (!response.IsSuccessStatusCode)
            return null;

        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nameNode = doc.DocumentNode.SelectSingleNode("//div[@class='teacher']//h3");
        if (nameNode == null)
            return null;

        var ratingStr = doc.DocumentNode.SelectSingleNode("//div[@class='teacher']//div[@class='right']//h2")?.InnerText.Trim() ?? "0";
        if (ratingStr == "N/A")
            ratingStr = "0";

        var teacher = new Teacher
        {
            Name = nameNode.InnerText.Trim(),
            College = doc.DocumentNode.SelectSingleNode("//div[@class='teacher']//p[2]")?.InnerText.Trim() ?? string.Empty,
            Rating = decimal.Parse(ratingStr) // 设置更新时间为当前时间
        };

        var courseNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'course-list')]//div[contains(@class, 'row') and position()>1]");

        if (courseNodes is not null)
        {
            foreach (var courseNode in courseNodes)
            {
                var courseNameElement = courseNode.SelectSingleNode(".//p[@class='course_name']");

                if (courseNameElement is null)
                    continue;

                var courseName = courseNameElement.InnerText.Trim();
                if (!string.IsNullOrEmpty(courseName))
                {
                    teacher.CoursesList.Add(courseName);
                }
            }
        }

        return teacher;
    }

    public async Task<List<Teacher>> GetInstructorsAsync(string instructorName, string? courseName, string? college)
    {
        await InitializeTeacherDataAsync();

        // 获取所有同名教师
        var teachers = await dbContext.Teachers.Where(t => t.Name == instructorName).ToListAsync();

        if (!teachers.Any() || (courseName == null && college == null))
        {
            return teachers;
        }

        // 更新教师信息，只更新三天前的数据
        var thresholdDate = DateTime.Now.AddDays(-3);
        foreach (var teacher in teachers.Where(teacher => teacher.LastUpdatedOn <= thresholdDate))
        {
            var latestTeacher = await ScrapeTeacherDataAsync(teacher.Id);
            if (latestTeacher is null)
            {
                continue;
            }

            latestTeacher.Id = teacher.Id;
            dbContext.Entry(teacher).CurrentValues.SetValues(latestTeacher);
            await dbContext.SaveChangesAsync();
        }

        // 按课程和学院进行排序
        return teachers.OrderByDescending(t =>
        {
            var score = 0;

            if (courseName is not null && t.CoursesList.Any(c => c.Contains(courseName, StringComparison.OrdinalIgnoreCase)))
            {
                score += 2;
            }

            if (college is not null && t.College.Contains(college, StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
            }

            return score;
        }).ToList();
    }
}
