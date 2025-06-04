using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quantum.Sdk;
using Quantum.Sdk.Utilities;
using SearchModule;

namespace SearchUiModule;

public class SearchUiModule(ILogger<SearchUiModule> logger, IQuantum quantum) : IUiModule // 实现了IUiModule接口的类是UI模块加载的入口，一个程序集中只能有一个
{
    public string ModuleId => "WheelchairSearchModule"; // ModuleId是模块的唯一标识符，用于区分不同的模块
    public Version Version { get; } = new(1, 0);
    public string Author => "WCSteam"; // Author是模块的作者信息
    public string Description => "AI聚合搜索平台"; // Description是模块的描述信息
    public Task<Result> OnAllLoadedAsync()// OnAllLoadedAsync方法是所有模块加载完成后的回调方法
                                          // 通常用于检查依赖项是否已加载，注册服务等

    {
        quantum.InjectedCodeManager.AddPreBlazor("<script>console.log('hello, world');</script>");
        logger.LogInformation("WheelchairSearch is Loaded");
        quantum.HostServices.AddScoped<SearchService>();

        return Task.FromResult<Result>(quantum.ModuleManager.GetModule("WheelchairSearchModule", new Version(1, 0)));
    }

    public string ModuleTitle => "WheelchairSearch"; // ModuleTitle是模块的标题，用于显示在上方导航栏上
    public string ModuleIcon => "search"; // ModuleIcon是模块的图标，用于显示在上方导航栏上，具体图标样式请参考 https://antblazor.com/zh-CN/components/icon
    public string DefaultRoute => "/wc-search"; // DefaultComponent是模块的默认页面组件

    public IEnumerable<NavigationItem> GetNavigationItems() // GetNavigationItems方法用于获取模块的导航项列表，用于显示在左侧菜单栏上
        =>
        [
            // new NavigationItem
            // {
            //     Title = "WheelchairSearch",
            //     Icon = "search",
            //     Route = "/wc-search",
            // }
        ];
}
