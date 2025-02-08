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
    /// 模块初始化
    /// </summary>
    void Load(IServiceCollection services);
}
