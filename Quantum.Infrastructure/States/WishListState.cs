using Quantum.Core.Models;

namespace Quantum.Infrastructure.States;
public class WishListState
{
    public List<CourseSection> WishList { get; } = [];
    public List<CourseSection> OptimizedSchedule { get; set; } = [];
    public decimal MaxCredits;

    public event Action? OnWishListStateChanged;
    public void AddSection(CourseSection section)
    {
        WishList.Add(section);
        NotifyStateChanged();
    }
    public void RemoveSection(CourseSection section)
    {
        WishList.Remove(section);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnWishListStateChanged?.Invoke();
}
