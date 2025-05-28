using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Net.NetworkInformation;
using System.Text; // 添加必要的命名空间
using System.Text.Json;

namespace SearchModule;
public class SearchService : ISearchService
{
    private readonly ILogger<SearchService> _logger;
    private Dictionary<string, bool> _sourceRetrieve;
    private readonly IHttpClientFactory _httpClientFactory;
    public bool enableAI { get; set; } 
    public Dictionary<string, ISourceRetriever> sourceMap { get; set; }
    public string aiApiUrl { get; set; } // 大先生 "https://chat.zju.edu.cn/api/ai/v1/chat/completions"
    public string aiApiKey { get; set; }

    private class AiResponse
    {
        public Choice[] choices { get; set; }

        public class Choice
        {
            public Message message { get; set; }
        }

        public class Message
        {
            public string content { get; set; }
        }
    }

    private class KeywordResponse
    {
        public List<string> keyword { get; set; }
    }

    public SearchService(ILogger<SearchService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        InitializeTask = InitializeAsync();
        sourceMap = new Dictionary<string, ISourceRetriever>
        {
            { "ZDBK", new ZDBKRetriever(_httpClientFactory) },          // TODO 硬编码
            { "CSPO", new CSPORetriever(_httpClientFactory) }
        };
        _sourceRetrieve = new Dictionary<string, bool>();
        foreach (var key in sourceMap.Keys)
        {
            _sourceRetrieve[key] = true;
        }
        setEnableSource(0, false);
    }

    public bool IsInitialized { get; private set; }
    public Task InitializeTask { get; }
    public Task InitializeAsync()
    {
        _logger.LogInformation("SearchService is initialized.");
        IsInitialized = true;
        return Task.CompletedTask;
    }

    private class UrlEqualityComparer : IEqualityComparer<(string title, string url, string date, int number, string source)>
    {
        public bool Equals((string title, string url, string date, int number, string source) x, 
            (string title, string url, string date, int number, string source) y)
        {
            return x.url == y.url;
        }

        public int GetHashCode((string title, string url, string date, int number, string source) obj)
        {
            return obj.url?.GetHashCode() ?? 0;
        }
    }

    public async Task<List<(string title, string url, string date, int number, string source)>> getSearchResult(string searchText)
    {
        if (enableAI)
        {
            try
            {
                var keywords = await GetAIKeywordsAsync(searchText);
                var allResults = new List<(string title, string url, string date, int number, string source)>();

                foreach (var keyword in keywords)
                {
                    var keywordResults = await SearchWithSourcesAsync(keyword);
                    allResults.AddRange(keywordResults);
                }

                return allResults.Distinct(new UrlEqualityComparer()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI search failed, falling back to normal search");
                return await SearchWithSourcesAsync(searchText);
            }
        }

        return await SearchWithSourcesAsync(searchText);
    }

    private async Task<List<string>> GetAIKeywordsAsync(string searchText)
    {
        if (string.IsNullOrEmpty(aiApiKey))
        {
            throw new InvalidOperationException("AI API key is not configured");
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {aiApiKey}");

        var requestBody = new
        {
            model = "deepseek-v3",
            messages = new[]
            {
                new { role = "system", content = "现在有一个搜索词条，现在请你通过这个词条提取出一个最简短，但准确的搜索词，并将这个词以不同的说法表示，总共不超过十个，便于进一步的检索。你需要以json格式进行回答，不要回答其它内容，不要带前缀和后缀。格式为{keyword:[]}" },
                new { role = "user", content = searchText }
            },
            stream = false
        };

        var response = await client.PostAsync(
            aiApiUrl,
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        );

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var aiResponse = JsonSerializer.Deserialize<AiResponse>(responseContent);

        // 提取JSON字符串并解析
        var contentStr = aiResponse?.choices[0].message.content;

        // 尝试找到JSON内容的开始和结束位置
        int jsonStart = contentStr.IndexOf('{');
        int jsonEnd = contentStr.LastIndexOf('}');

        if (jsonStart == -1 || jsonEnd == -1 || jsonEnd <= jsonStart)
        {
            _logger.LogWarning($"Invalid AI response format: {contentStr}");
            throw new InvalidOperationException("AI response does not contain valid JSON");
        }

        // 提取JSON部分
        contentStr = contentStr.Substring(jsonStart, jsonEnd - jsonStart + 1);

        try
        {
            var keywordResponse = JsonSerializer.Deserialize<KeywordResponse>(contentStr);
            return keywordResponse.keyword ?? new List<string> { searchText };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"Failed to parse AI response: {contentStr}");
            throw;
        }
    }

    private async Task<List<(string title, string url, string date, int number, string source)>> SearchWithSourcesAsync(string searchText)
    {
        var tasks = sourceMap
            .Where(source => _sourceRetrieve[source.Key])
            .Select(async source =>
            {
                var sourceResult = await source.Value.RetrieveSourcesAsync(keyWord: searchText);
                Console.WriteLine($"从 {source.Value.SourceName} 检索到 {sourceResult.Count} 条数据");
                return sourceResult.Select(item => (
                    item.title,
                    item.url,
                    item.date,
                    item.number,
                    source.Key
                )).ToList();
            });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    public int sourceCnt()
    {
        return sourceMap.Count;
    }

    public int pingSource(int source)
    {
        // 假设 source 是 _sourceMap 中的索引
        if (source < 0 || source >= sourceMap.Count)
        {
            _logger.LogWarning("Invalid source index.");
            return -1;
        }

        var sourceKey = sourceMap.Keys.ElementAt(source);
        var sourceRetriever = sourceMap[sourceKey];

        if (!_sourceRetrieve[sourceKey])
        {
            _logger.LogWarning($"Source {sourceKey} is disabled.");
            return -1;
        }

        try
        {
            using (Ping pingSender = new Ping())
            {
                PingReply reply = pingSender.Send(sourceRetriever.SourceUrl);
                if (reply.Status == IPStatus.Success)
                {
                    _logger.LogInformation($"Ping to {sourceRetriever.SourceUrl} succeeded. Roundtrip time: {reply.RoundtripTime} ms");
                    return (int)reply.RoundtripTime;
                }
                else
                {
                    _logger.LogWarning($"Ping to {sourceRetriever.SourceUrl} failed. Status: {reply.Status}");
                    return -1;
                }
            }
        }
        catch (PingException e)
        {
            _logger.LogError($"Ping exception: {e.Message}");
            return -1;
        }
    }

    public void setEnableSource(int source, bool enable)
    {
        if (source < 0 || source >= sourceMap.Count)
        {
            _logger.LogWarning("Invalid source index.");
            return;
        }

        var sourceKey = sourceMap.Keys.ElementAt(source);
        _sourceRetrieve[sourceKey] = enable;
        _logger.LogInformation($"Source {sourceKey} is now {(enable ? "enabled" : "disabled")}.");
    }
}
