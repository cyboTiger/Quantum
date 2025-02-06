using Quantum.Infrastructure.Abstractions;
using zdbk.zju.edu.cn.Models;

namespace Quantum.UI.CourseSelectionAssistant.Services;

public interface IWishlistService : IInitializableService, IPersistentService, IAsyncDisposable
{
    event Action OnUpdated;
    void AddWish(SectionSnapshot section);
    void RemoveWish(SectionSnapshot section);

    ISet<SectionSnapshot> Wishlist { get; }
}
