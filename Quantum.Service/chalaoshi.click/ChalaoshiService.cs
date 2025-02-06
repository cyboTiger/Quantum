using chalaoshi.click.Models;
using Microsoft.Extensions.Logging;
using Quantum.Infrastructure.Utilities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace chalaoshi.click;

public class ChalaoshiService(ILogger<ChalaoshiService> logger) : IChalaoshiService
{
    private static readonly string DataFilePath = "chalaoshi.json".ToDataPath();
    private const string DataUrl = "https://chalaoshi.click/static/json/search.json";
    private HashSet<Teacher>? _data;
    private readonly HttpClient _httpClient = new();
    public Task InitializeTask { get; private set; } = Task.CompletedTask;
    public bool IsInitialized { get; private set; }

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await SaveStateAsync();
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        { return; }

        InitializeTask = LoadStateAsync().ContinueWith(_ => IsInitialized = true);
        await InitializeTask;
    }

    public async Task LoadStateAsync()
    {
        if (File.Exists(DataFilePath))
        {
            var fileInfo = new FileInfo(DataFilePath);
            if (DateTime.Now - fileInfo.LastWriteTime <= TimeSpan.FromDays(3))
            {
                var json = await File.ReadAllTextAsync(DataFilePath);
                _data = JsonSerializer.Deserialize<HashSet<Teacher>>(json.Decrypt());
                return;
            }
        }

        try
        {
            var json = await _httpClient.GetStringAsync(DataUrl);
            var jsonNode = Assertion.NotNull(JsonNode.Parse(json));
            var colleges = Assertion.NotNull(jsonNode["colleges"]).AsArray()
                .ToDictionary(c => (int)c!["id"]!, c => (string)c!["name"]!);

            _data = new HashSet<Teacher>();

            foreach (var teacher in jsonNode["teachers"]!.AsArray())
            {
                var collegeId = (int)teacher!["xy"]!;
                var rating = decimal.TryParse((string?)teacher["rate"], out var r) ? r : 0m;

                _data.Add(new Teacher
                {
                    Id = (int)teacher["id"]!,
                    Name = (string)teacher["name"]!,
                    College = colleges.TryGetValue(collegeId, out var value) ? value : string.Empty,
                    Rating = rating
                });
            }

            await SaveStateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download or process teacher data from {Url}", DataUrl);
            throw;
        }
    }

    public async Task SaveStateAsync()
    {
        if (_data is null)
        { return; }

        var directory = Path.GetDirectoryName(DataFilePath);
        if (!string.IsNullOrEmpty(directory))
        { Directory.CreateDirectory(directory); }

        var json = JsonSerializer.Serialize(_data);
        await File.WriteAllTextAsync(DataFilePath, json.Encrypt());
    }

    public async Task<Teacher?> GetTeacherByNameAsync(string name, string? college)
    {
        await InitializeTask;
        Assertion.NotNull(_data);

        var matchingTeachers = _data!.Where(t => t.Name == name).ToList();
        if (matchingTeachers.Count is 0)
        { return null; }

        if (string.IsNullOrEmpty(college))
        { return matchingTeachers[0]; }

        var teacherInCollege = matchingTeachers
            .FirstOrDefault(t => t.College.Contains(college, StringComparison.OrdinalIgnoreCase));

        return teacherInCollege ?? matchingTeachers[0];
    }
}
