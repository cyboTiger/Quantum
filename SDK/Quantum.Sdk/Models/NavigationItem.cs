namespace Quantum.Infrastructure.Models;

public class NavigationItem
{
    /// <summary>
    /// 菜单项的唯一标识
    /// </summary>
    public string Key { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 菜单项的显示标题
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 菜单项的图标
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// 菜单项的路由
    /// </summary>
    public required string Route { get; init; }

    /// <summary>
    /// 子菜单项
    /// </summary>
    public IEnumerable<NavigationItem> Children { get; init; } = [];
}
