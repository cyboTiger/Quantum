using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Http; // 添加必要的命名空间
using System.Net.Http;

namespace SearchModule;
public class SearchService : ISearchService
{
    private readonly ILogger<SearchService> _logger;
    private readonly Dictionary<string, ISourceRetriever> _sourceMap;
    private Dictionary<string, bool> _sourceRetrieve;
    private readonly IHttpClientFactory _httpClientFactory;

    public SearchService(ILogger<SearchService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        InitializeTask = InitializeAsync();
        _sourceMap = new Dictionary<string, ISourceRetriever>
        {
            { "ZDBK", new ZDBKRetriever(_httpClientFactory) },
            { "CSPO", new CSPORetriever(_httpClientFactory) }
        };
        _sourceRetrieve = new Dictionary<string, bool>();
        foreach (var key in _sourceMap.Keys)
        {
            _sourceRetrieve[key] = true;
        }
        setEnableSource(1, false);
    }

    public bool IsInitialized { get; private set; }
    public Task InitializeTask { get; }
    public Task InitializeAsync()
    {
        _logger.LogInformation("SearchService is initialized.");
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public async Task<List<(string title, string url, string date, int number)>> getSearchResult(string searchText) {
        var tasks = _sourceMap
            .Where(source => _sourceRetrieve[source.Key])
            .Select(async source =>
            {
                var sourceResult = await source.Value.RetrieveSourcesAsync(keyWord: searchText);
                Console.WriteLine($"从 {source.Value.SourceName} 检索到 {sourceResult.Count} 条数据");
                return sourceResult;
            });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }
    
    public void setEnableAI(bool enableAI) { }

    public int sourceCnt()
    {
        return _sourceMap.Count;
    }

    public int pingSource(int source)
    {
        // 假设 source 是 _sourceMap 中的索引
        if (source < 0 || source >= _sourceMap.Count)
        {
            _logger.LogWarning("Invalid source index.");
            return -1;
        }

        var sourceKey = _sourceMap.Keys.ElementAt(source);
        var sourceRetriever = _sourceMap[sourceKey];

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
        if (source < 0 || source >= _sourceMap.Count)
        {
            _logger.LogWarning("Invalid source index.");
            return;
        }

        var sourceKey = _sourceMap.Keys.ElementAt(source);
        _sourceRetrieve[sourceKey] = enable;
        _logger.LogInformation($"Source {sourceKey} is now {(enable ? "enabled" : "disabled")}.");
    }
}
