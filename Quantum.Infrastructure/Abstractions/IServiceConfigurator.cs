namespace Quantum.Infrastructure.Abstractions;

/// <summary>
/// 服务配置器接口。每个程序集最多只能有一个实现了该接口的类。
/// </summary>
public interface IServiceConfigurator
{
    /// <summary>
    /// 配置服务
    /// </summary>
    /// <param name="services">服务集合</param>
    void ConfigureServices(IServiceCollection services);
}
