using Microsoft.EntityFrameworkCore;
using Quantum.Core.Repository;
using Quantum.Infrastructure.Services;
using System.Text.Json;

namespace Quantum.ConsoleTest;

public class ExportTeachers
{
    public static async Task Export()
    {
        Console.WriteLine("开始导出教师数据...");
        var context = new QuantumDbContext();
        var service = new TeacherScrapingService(context);

        await context.Database.EnsureCreatedAsync();
        await service.InitializeTeacherDataAsync();

        var teachers = await context.Teachers
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .ToListAsync();

        Console.WriteLine($"找到 {teachers.Count} 位教师");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(teachers, options);
        var exportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "teachers.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);
        await File.WriteAllTextAsync(exportPath, json, System.Text.Encoding.UTF8);

        Console.WriteLine($"教师数据已导出到: {exportPath}");
        Console.WriteLine("导出完成！");
    }
}
