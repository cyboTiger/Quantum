using Microsoft.AspNetCore.Components;

namespace Quantum.Infrastructure.Models;

public class NavigationItem
{
    /// <summary>
    /// 菜单项的唯一标识
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// 菜单项的显示标题
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// 菜单项的图标
    /// </summary>
    public required string Icon { get; set; }

    /// <summary>
    /// 菜单项的路由
    /// </summary>
    public required string Route { get; set; }

    /// <summary>
    /// 自定义标题内容
    /// </summary>
    public RenderFragment? CustomTitle { get; set; }

    /// <summary>
    /// 子菜单项
    /// </summary>
    public List<NavigationItem> Children { get; set; } = [];
}
