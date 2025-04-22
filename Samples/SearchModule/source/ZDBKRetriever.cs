using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SearchModule;

public class ZDBKRetriever : ISourceRetriever
{
    public string SourceId { get; } = "ZDBK";
    public string SourceName { get; } = "浙江大学教务管理网";
    public string SourceUrl { get; } = "http://zdbk.zju.edu.cn/jwglxt/xtgl/xwck_cxMoreLoginNews.html";
    private readonly HttpClient _httpClient;
    public ZDBKRetriever(IHttpClientFactory httpClientFactory) {
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<List<(string title, string url, string date, int number)>> RetrieveSourcesAsync(int numPosts = 10, string keyWord = "")
    {
        Console.WriteLine($"开始从 {SourceName} 检索数据...");
        Console.WriteLine($"检索关键词: {keyWord}");
        var paramsDict = new Dictionary<string, string>
        {
            { "doType", "query" },
            { "xwbt", keyWord },
            { "_search", "false" },
            { "queryModel.showCount", numPosts.ToString() },
            { "queryModel.currentPage", "1" },
            { "queryModel.sortName", "sfzd desc,fbsj" },
            { "queryModel.sortOrder", "desc" }
        };

        var content = new StringContent(JsonConvert.SerializeObject(paramsDict), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(SourceUrl, content);
            Console.WriteLine($"请求地址: {SourceUrl}");
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"请求成功，状态码: {response.StatusCode}");

            // 读取响应内容并解析为字符串
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(responseBody);

            // 检查 items 是否存在且非空
            var items = jsonObject["items"]?.ToObject<List<JObject>>();
            if (items == null || items.Count == 0)
            {
                Console.WriteLine("未找到任何数据项。");
                return [];
            }

            var result = new List<(string title, string url, string date, int number)>();

            for (int i = 0; i < items.Count; i++)
            {
                result.Add((
                    title: items[i]["xwbt"]?.ToString() ?? "unknown",
                    url: items[i]["xwbh"]?.ToString() ?? "unknown",
                    date: items[i]["fbsj"]?.ToString() ?? "unknown",
                    number: i + 1
                ));
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"请求失败: {ex.Message}");
            return [];
        }
    }
}

