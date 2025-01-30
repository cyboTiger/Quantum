using Microsoft.EntityFrameworkCore;
using Quantum.Core.Models;
using Quantum.Core.Repository;

namespace Quantum.Infrastructure.States;

public class UserState(QuantumDbContext dbContext)
{
    public bool IsLoggedIn => CurrentUser is not null;
    public User? CurrentUser { get; private set; }
    public List<CourseSection>? ChosenCourseSections { get; set; }

    public event Action? OnUserStateChanged;

    public async void Login(User user)
    {
        CurrentUser = user;
        await SaveToDatabaseAsync();
        NotifyStateChanged();
    }

    public void Logout()
    {
        CurrentUser = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnUserStateChanged?.Invoke();

    public static UserState LoadFromDatabase(QuantumDbContext dbContext)
    {
        var userState = new UserState(dbContext);

        var user = dbContext.UserInfo.AsNoTracking().FirstOrDefault();
        if (user is not null)
        {
            userState.Login(user);
        }

        return userState;
    }

    public async Task SaveToDatabaseAsync()
    {
        if (CurrentUser is null)
            return;

        dbContext.UserInfo.RemoveRange(dbContext.UserInfo);
        await dbContext.SaveChangesAsync();
        await dbContext.UserInfo.AddAsync(CurrentUser);
        await dbContext.SaveChangesAsync();
    }
}
