using Quantum.Infrastructure.Models;

namespace Quantum.Infrastructure.Abstractions;

/// <summary>
/// 表示一个UI模块接口
/// </summary>
public interface IUiModule : IModule
{
    /// <summary>
    /// 获取模块标题
    /// </summary>
    string ModuleTitle { get; }

    /// <summary>
    /// 获取模块图标
    /// </summary>
    string? ModuleIcon { get; }

    /// <summary>
    /// 获取默认页面
    /// </summary>
    string DefaultRoute { get; }

    /// <summary>
    /// 获取导航项列表
    /// </summary>
    /// <returns>有序的导航项列表</returns>
    IEnumerable<NavigationItem> GetNavigationItems();
}
