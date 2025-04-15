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
    public HttpClient _httpClient { get; set; } = new HttpClient();

    public ZDBKRetriever()
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<IEnumerable<(int id, string publishTime, string Url, string title)>> RetrieveSourcesAsync(int numPosts = 10, string keyWord = "")
    {
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
            response.EnsureSuccessStatusCode();

            // 读取响应内容并解析为字符串
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(responseBody);

            // 检查 items 是否存在且非空
            var items = jsonObject["items"]?.ToObject<List<JObject>>();
            if (items == null || items.Count == 0)
            {
                Console.WriteLine("未找到任何数据项。");
                return Enumerable.Empty<(int id, string publishTime, string Url, string title)>();
            }

            var result = new List<(int id, string publishTime, string Url, string title)>();

            for (int i = 0; i < items.Count; i++)
            {
                result.Add((
                    id: i + 1,
                    publishTime: items[i]["fbsj"]?.ToString() ?? "unknown",
                    Url: items[i]["xwbh"]?.ToString() ?? "unknown",
                    title: items[i]["xwbt"]?.ToString() ?? "unknown"
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

