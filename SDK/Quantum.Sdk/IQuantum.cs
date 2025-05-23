using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using Quantum.Sdk.Services;

namespace Quantum.Sdk;

/// <summary>
/// Quantum应用程序的主接口
/// </summary>
public interface IQuantum
{
    /// <summary>
    /// 获取主窗口实例
    /// </summary>
    BrowserWindow? Window { get; }

    /// <summary>
    /// 获取运行时服务集合
    /// </summary>
    IServiceCollection HostServices { get; }

    /// <summary>
    /// 获取模块管理器实例
    /// </summary>
    IModuleManager ModuleManager { get; }

    /// <summary>
    /// 获取注入代码管理器实例
    /// </summary>
    IInjectedCodeManager InjectedCodeManager { get; }
}
