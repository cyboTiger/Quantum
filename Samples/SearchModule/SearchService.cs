using Microsoft.Extensions.Logging;

namespace SearchModule;
public class SearchService : ISearchService
{
    private readonly ILogger<SearchService> _logger;
    private readonly Dictionary<string, ISourceRetriever> _sourceRetrieve;

    public SearchService(ILogger<SearchService> logger)
    {
        _logger = logger;
        InitializeTask = InitializeAsync();
        _sourceRetrieve = [];
    }

    public bool IsInitialized { get; private set; }
    public Task InitializeTask { get; }
    public Task InitializeAsync()
    {
        _logger.LogInformation("SearchService is initialized.");
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public string Search(string query)
    {
        // 这里可以实现搜索逻辑
        return $"Search result for '{query}'";
    }
}
