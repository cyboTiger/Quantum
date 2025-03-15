using AntDesign;
using Microsoft.AspNetCore.Components;
using OneOf;

namespace Quantum.Sdk.Extensions;

/// <summary>
/// 消息服务扩展方法类
/// </summary>
public static class MessageServiceExtension
{
    /// <summary>
    /// 在执行异步操作时显示加载消息
    /// </summary>
    /// <typeparam name="T">异步操作的返回类型</typeparam>
    /// <param name="messageService">消息服务</param>
    /// <param name="asyncFunc">要执行的异步操作</param>
    /// <param name="content">加载过程中显示的消息</param>
    /// <param name="onClose">加载消息关闭时执行的操作</param>
    /// <returns>异步操作的结果</returns>
    public static async Task<T> LoadingWhen<T>(
        this IMessageService messageService,
        Func<Task<T>> asyncFunc,
        OneOf<string, RenderFragment, MessageConfig> content,
        Action? onClose = null
    )
    {
        if (content.IsT2)
        {
            content.AsT2.Duration = 0;
        }
        var task = messageService.Loading(content, 0, onClose);
        try
        {
            return await asyncFunc();
        }
        finally
        {
            task.Start();
        }
    }

    /// <summary>
    /// 在执行异步操作时显示加载消息（无返回值版本）
    /// </summary>
    /// <param name="messageService">消息服务</param>
    /// <param name="asyncFunc">要执行的异步操作</param>
    /// <param name="content">加载过程中显示的消息</param>
    /// <param name="onClose">加载消息关闭时执行的操作</param>
    public static async Task LoadingWhen(
        this IMessageService messageService,
        Func<Task> asyncFunc,
        OneOf<string, RenderFragment, MessageConfig> content,
        Action? onClose = null
    )
    {
        if (content.IsT2)
        {
            content.AsT2.Duration = 0;
        }
        var task = messageService.Loading(content, 0, onClose);
        try
        {
            await asyncFunc();
        }
        finally
        {
            task.Start();
        }
    }
}
