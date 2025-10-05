using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using Sunday.Models;
using Sunday.Services;

namespace Sunday.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private Timer? _timer;
    private readonly WeatherService  _weatherService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] IPage? _currentPage;
    [ObservableProperty] SideMenuViewModel? _sideMenu;

    public event Action? TimeChanged;

    public MainViewModel(WeatherService weatherService, NavigationService navigationService, SideMenuViewModel? sideMenu)
    {
        _weatherService = weatherService;
        _navigationService = navigationService;
        _sideMenu = sideMenu;
    }

    public void Initialize()
    {
        _weatherService.Init();
        _navigationService.OnNavigated += NavigationServiceOnOnNavigated;
        SideMenu?.Init();
        _timer = new Timer(60000);
        _timer.Elapsed += (s, e) => TimeChanged?.Invoke();
        //_timer.Start();
    }

    private void NavigationServiceOnOnNavigated()
    {
        CurrentPage = _navigationService.Current;
    }
}