using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Sunday.Services;

namespace Sunday.ViewModels;

public partial class HomePageViewModel : ViewModelBase, IPage
{
    private readonly MainViewModel _mainViewModel;
    private readonly WeatherService _weatherService;
    public string Title => "FORECAST";
    public PageType Type => PageType.Index;

    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _time;
    [ObservableProperty] private string? _date;
    [ObservableProperty] private ObservableCollection<ForecastModel>?  _forecast;
    
    public HomePageViewModel(MainViewModel mainViewModel, WeatherService weatherService)
    {
        _mainViewModel = mainViewModel;
        _weatherService = weatherService;
        _mainViewModel.TimeChanged += MainViewModelOnTimeChanged;
        Init();
    }

    public async Task Init()
    {
        await UpdateState();
    }

    private async Task UpdateState()
    {
        var geo = await _weatherService.GetCurrentAsync();
        var forecast = await _weatherService.GetWeeklyForecastAsync(geo.Latitude, geo.Longitude);
        Forecast = new ObservableCollection<ForecastModel>(forecast);
        City = geo.City;
        var localTime = await _weatherService.GetLocalTimeAsync(geo.Latitude, geo.Longitude);
        Time = localTime.LocalDateTime.LocalDateTime.ToShortTimeString();
        Date = localTime.LocalDateTime.LocalDateTime.ToShortDateString();
    }

    private async void MainViewModelOnTimeChanged()
    {
        await UpdateState();
    }
}