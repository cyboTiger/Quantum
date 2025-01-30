using Microsoft.EntityFrameworkCore;
using Quantum.Core.Repository;
using Quantum.Infrastructure.Exceptions;
using Quantum.Infrastructure.Utils;
using System.Net;

namespace Quantum.Infrastructure.Services;

public class RequestClient : HttpClient
{
    public CookieContainer CookieContainer { get; }
    private readonly QuantumDbContext _dbContext;

    private RequestClient(HttpClientHandler handler, QuantumDbContext dbContext) : base(handler)
    {
        DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.58");
        CookieContainer = handler.CookieContainer;
        _dbContext = dbContext;
    }

    public static RequestClient Create(QuantumDbContext dbContext, CookieContainer? cookieContainer = null)
    {
        cookieContainer ??= new CookieContainer();

        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = true,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        return new RequestClient(handler, dbContext);
    }

    public static RequestClient LoadFromDatabase(QuantumDbContext dbContext)
    {
        var cookieContainer = new CookieContainer();

        var cookies = dbContext.Cookies.AsNoTracking().ToList();
        foreach (var cookie in cookies)
        {
            var decryptedValue = Encryption.Decrypt(cookie.Value);
            cookieContainer.Add(new Cookie(cookie.Name, decryptedValue, cookie.Path ?? "/", cookie.Domain));
        }

        return Create(dbContext, cookieContainer);
    }

    public async Task SaveToDatabaseAsync()
    {
        var cookies = CookieContainer.GetAllCookies().Select(cookie => new Core.Models.Cookie
        {
            Name = cookie.Name,
            Value = Encryption.Encrypt(cookie.Value),
            Domain = cookie.Domain,
            Path = cookie.Path,
        }).ToList();

        // 删除旧的cookies
        _dbContext.Cookies.RemoveRange(_dbContext.Cookies);
        // 添加新的cookies
        await _dbContext.Cookies.AddRangeAsync(cookies);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ClearAllCookiesAsync()
    {
        // 清除内存中的Cookies
        var allCookies = CookieContainer.GetAllCookies().ToList();
        foreach (var cookie in allCookies)
        {
            cookie.Expired = true;  // 使Cookie过期
        }
        CookieContainer.GetAllCookies().Clear();  // 清除CookieContainer

        // 清除数据库中的Cookies
        _dbContext.Cookies.RemoveRange(_dbContext.Cookies);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsSessionValidAsync()
    {
        try
        {
            const string redirectUrl = "http://zdbk.zju.edu.cn/jwglxt/xtgl/dl_loginForward.html";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await GetAsync(redirectUrl, cts.Token);

            return response.RequestMessage is not null && response.RequestMessage.ToString().Contains("index_initMenu.html");
        }
        catch
        {
            return false;
        }
    }

    public async Task EnsureClientSessionIsValidAsync()
    {
        if (!await IsSessionValidAsync())
            throw new SessionExpiredException("Session");
    }
}
