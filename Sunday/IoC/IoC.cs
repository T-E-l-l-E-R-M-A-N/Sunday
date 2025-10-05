using Autofac;
using Sunday.Services;
using Sunday.ViewModels;

namespace Sunday;

public static class IoC
{
    private static IContainer? _container;

    public static void Build()
    {
        var services = new ContainerBuilder();

        services.RegisterType<MainViewModel>().SingleInstance();
        services.RegisterType<SideMenuViewModel>().SingleInstance();
        services.RegisterType<HomePageViewModel>().As<IPage>().SingleInstance();
        services.RegisterType<PinnedPageViewModel>().As<IPage>().SingleInstance();
        services.RegisterType<SearchPageViewModel>().As<IPage>().SingleInstance();

        services.RegisterType<WeatherService>().SingleInstance();
        services.RegisterType<NavigationService>().SingleInstance();
        
        _container = services.Build();
    }
    public static T Resolve<T>() => _container.Resolve<T>();
}