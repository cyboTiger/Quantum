using Quantum.Infrastructure.Utilities;
using System.Text.Json;
using zdbk.zju.edu.cn.Models;

namespace Quantum.UI.CourseSelectionAssistant.Services;

public class WishlistService(ILogger<WishlistService> logger) : IWishlistService
{
    public ISet<SectionSnapshot> Wishlist { get; } = new HashSet<SectionSnapshot>();
    private static readonly string WishSectionsDataPath = "csa_wishlist.json".ToDataPath();
    public bool IsInitialized { get; private set; }
    public Task InitializeTask { get; private set; } = Task.CompletedTask;
    public event Action? OnUpdated;

    public void AddWish(SectionSnapshot section)
    {
        if (!Wishlist.Add(section))
        { return; }

        OnUpdated?.Invoke();
        _ = SaveStateAsync();
    }

    public void RemoveWish(SectionSnapshot section)
    {
        if (!Wishlist.Remove(section))
        { return; }

        OnUpdated?.Invoke();
        _ = SaveStateAsync();
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        { return; }

        InitializeTask = LoadStateAsync()
            .ContinueWith(_ => { IsInitialized = true; });
        await InitializeTask;
    }

    public async Task LoadStateAsync()
    {
        // 加载心愿单
        try
        {
            if (File.Exists(WishSectionsDataPath))
            {
                var json = await File.ReadAllTextAsync(WishSectionsDataPath);
                var snapshots = JsonSerializer.Deserialize<HashSet<SectionSnapshot>>(json.Decrypt());

                if (snapshots is not null)
                {
                    Wishlist.Clear();
                    Wishlist.UnionWith(snapshots);
                    OnUpdated?.Invoke();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load wish sections");
            Wishlist.Clear();
        }
    }

    public async Task SaveStateAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Wishlist);
            var directory = Path.GetDirectoryName(WishSectionsDataPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(WishSectionsDataPath, json.Encrypt());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save wish sections");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await SaveStateAsync();
        GC.SuppressFinalize(this);
    }
}

internal class Messages
{
    public const string NoNeedToUpdateSections = "No need to update sections.";
}