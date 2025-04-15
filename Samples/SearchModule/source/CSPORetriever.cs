using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SearchModule;

public class CSPORetriever : ISourceRetriever
{
    public string SourceId { get; } = "CSPO";
    public string SourceName { get; } = "浙江大学计算机学院网";
    public string SourceUrl { get; } = "http://cspo.zju.edu.cn/_web/_search/api/searchCon/create.rst?_p=YXM9NDg0JnQ9MTIxNCZkPTQ5ODUmcD0xJm09U04m";

    public HttpClient _httpClient { get; set;} = new HttpClient();

    public CSPORetriever()
    {
        _httpClient.DefaultRequestHeaders.Add("Referer", SourceUrl);
        _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
    }

    public static string searchInfo_payload(string keyword, int pageIndex = 1)
    {
        var searchInfo = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "field", "pageIndex" }, { "value", pageIndex } },
            new Dictionary<string, object> { { "field", "group" }, { "value", 0 } },
            new Dictionary<string, object> { { "field", "searchType" }, { "value", "" } },
            new Dictionary<string, object> { { "field", "keyword" }, { "value", keyword } },
            new Dictionary<string, object> { { "field", "recommend" }, { "value", "1" } },
            new Dictionary<string, object> { { "field", 4 }, { "value", "" } },
            new Dictionary<string, object> { { "field", 5 }, { "value", "" } },
            new Dictionary<string, object> { { "field", 6 }, { "value", "" } },
            new Dictionary<string, object> { { "field", 7 }, { "value", "" } }
        };

        var options = new JsonSerializerOptions { WriteIndented = false };
        string searchInfoJson = JsonSerializer.Serialize(searchInfo, options);
        byte[] bytes = Encoding.UTF8.GetBytes(searchInfoJson);
        return Convert.ToBase64String(bytes);
    }
    
    public async Task<IEnumerable<(int id, string publishTime, string Url, string title)>> RetrieveSourcesAsync(int numPosts = 10, string keyWord = "")
    {
        try
        {
            var targetInfos = new List<(int id, string publishTime, string Url, string title)>();
            int numPages = (int)Math.Ceiling((double)numPosts / 10);

            for (int i = 0; i < numPages; i++)
            {
                string payload = $"searchInfo={searchInfo_payload(keyWord, i + 1)}";
                var content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");
                
                HttpResponseMessage response = await _httpClient.PostAsync(SourceUrl, content);
                response.EnsureSuccessStatusCode(); // 检查请求是否成功
                string responseBody = await response.Content.ReadAsStringAsync();

                string urlPattern = "<a href='([^']+)'";
                string captionPattern = ">([^<]+)<\\\\/a>";
                string timePattern = "发布时间:(\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2})";

                MatchCollection urlMatches = Regex.Matches(responseBody, urlPattern);
                MatchCollection captionMatches = Regex.Matches(responseBody, captionPattern);
                MatchCollection timeMatches = Regex.Matches(responseBody, timePattern);

                int minLength = Math.Min(urlMatches.Count, Math.Min(captionMatches.Count, timeMatches.Count));
                for (int j = 0; j < minLength; j++)
                {
                    // 假设无法直接获取到 id，这里暂时设置为 0 或者根据 URL 尝试解析
                    int id = j;
                    targetInfos.Add((id, timeMatches[j].Groups[1].Value, urlMatches[j].Groups[1].Value, captionMatches[j].Groups[1].Value));
                }
            }

            return targetInfos.Take(numPosts); // 限制返回数量
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error fetching: {e.Message}");
            return [];
        }
    }
}