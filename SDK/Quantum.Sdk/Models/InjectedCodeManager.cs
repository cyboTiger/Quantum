using Microsoft.AspNetCore.Components;

namespace Quantum.Infrastructure.Models;

/// <summary>
/// 管理注入到App.razor的代码片段
/// </summary>
public class InjectedCodeManager
{
    private readonly List<MarkupString> _headCodes = [];
    private readonly List<MarkupString> _preBlazorCodes = [];
    private readonly List<MarkupString> _postBlazorCodes = [];

    /// <summary>
    /// 添加到head部分的代码
    /// </summary>
    public IReadOnlyList<MarkupString> HeadCodes => _headCodes;

    /// <summary>
    /// 添加到blazor.web.js之前的代码
    /// </summary>
    public IReadOnlyList<MarkupString> PreBlazorCodes => _preBlazorCodes;

    /// <summary>
    /// 添加到blazor.web.js之后的代码
    /// </summary>
    public IReadOnlyList<MarkupString> PostBlazorCodes => _postBlazorCodes;

    /// <summary>
    /// 添加代码到head部分
    /// </summary>
    /// <param name="code">HTML代码</param>
    public void AddToHead(string code) => _headCodes.Add(new MarkupString(code));

    /// <summary>
    /// 添加代码到blazor.web.js之前
    /// </summary>
    /// <param name="code">HTML代码</param>
    public void AddPreBlazor(string code) => _preBlazorCodes.Add(new MarkupString(code));

    /// <summary>
    /// 添加代码到blazor.web.js之后
    /// </summary>
    /// <param name="code">HTML代码</param>
    public void AddPostBlazor(string code) => _postBlazorCodes.Add(new MarkupString(code));
}
