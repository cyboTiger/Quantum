using Microsoft.Extensions.Logging;
using Quantum.Sdk;
using Quantum.Sdk.Extensions;
using Quantum.Sdk.Utilities;

namespace SearchModule;

public class SearchModule(ILogger<SearchModule> logger, IQuantum quantum) : IModule // 实现了IModule接口的类是模块加载的入口，一个程序集中只能有一个
{
    public string ModuleId => "SearchModule"; // ModuleId是模块的唯一标识符，用于区分不同的模块
    public Version Version { get; } = new(1, 0);
    public string Author => "cybotiger"; // Author是模块的作者信息
    public string Description => "聚合搜索模块";
    public Task<Result> OnAllLoadedAsync()
    {
        logger.LogInformation("SearchModule is loaded.");
        quantum.HostServices.AddEagerInitializeService<ISearchService, SearchService>();
        return Task.FromResult(Result.Success());
    }
}
