using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Extensions;
using Quantum.Infrastructure.Models;
using Quantum.UI.CourseSelectionAssistant.Services;

namespace Quantum.UI.CourseSelectionAssistant;

public class CsaModule : IModule
{
    public string ModuleKey => "csa";
    public string ModuleTitle => "选课助手";
    public string ModuleIcon => "book";
    public string DefaultRoute => "/csa";

    public IEnumerable<NavigationItem> GetNavigationItems()
    {
        return
        [
            new NavigationItem
            {
                Key = "dashboard",
                Title = "选课状态",
                Icon = "dashboard",
                Route = "/csa"
            },

            new NavigationItem
            {
                Key = "courses",
                Title = "课程列表",
                Icon = "unordered-list",
                Route = "/csa/courses"
            },

            new NavigationItem
            {
                Key = "graduation",
                Title = "毕业自审",
                Icon = "audit",
                Route = "/csa/graduation"
            },

            new NavigationItem
            {
                Key = "wishlist",
                Title = "我的心愿",
                Icon = "heart",
                Route = "/csa/wishlist"
            },
        ];
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddInitializableService<IWishlistService, WishlistService>();
    }
}
