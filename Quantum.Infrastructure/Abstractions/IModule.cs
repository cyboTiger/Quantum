using Quantum.Infrastructure.Models;

namespace Quantum.Infrastructure.Abstractions;

/// <summary>
/// 表示一个UI模块。每个子项目必须有且仅有一个实现此接口的类。
/// </summary>
public interface IModule
{
    /// <summary>
    /// 获取模块的名称
    /// </summary>
    string ModuleKey { get; }

    /// <summary>
    /// 模块的显示标题
    /// </summary>
    string ModuleTitle { get; }

    /// <summary>
    /// 模块的图标
    /// </summary>
    string? ModuleIcon { get; }

    /// <summary>
    /// 模块的默认路由
    /// </summary>
    string DefaultRoute { get; }

    /// <summary>
    /// 获取导航项
    /// </summary>
    IEnumerable<NavigationItem> GetNavigationItems();

    /// <summary>
    /// 配置模块的服务
    /// </summary>
    /// <param name="services">服务集合</param>
    void ConfigureServices(IServiceCollection services);
}
