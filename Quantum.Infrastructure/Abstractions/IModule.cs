using Quantum.Infrastructure.Utilities;

namespace Quantum.Infrastructure.Abstractions;

/// <summary>
/// 表示一个基础模块接口
/// </summary>
public interface IModule
{
    /// <summary>
    /// 获取模块的唯一标识符
    /// </summary>
    string ModuleId { get; }

    /// <summary>
    /// 获取模块的版本号
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// 获取模块的作者
    /// </summary>
    string Author { get; }

    /// <summary>
    /// 获取模块的介绍
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 当所有模块加载完成时执行的异步逻辑
    /// </summary>
    /// <param name="modules">所有已加载的模块</param>
    /// <returns>表示异步操作结果的任务，包含一个布尔值，指示模块是否已就绪</returns>
    Task<Result> OnAllLoadedAsync(IEnumerable<IModule> modules);
}
