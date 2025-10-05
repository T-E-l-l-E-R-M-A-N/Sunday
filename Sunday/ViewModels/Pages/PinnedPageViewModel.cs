using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sunday.Models;
using Sunday.Services;

namespace Sunday.ViewModels;

public partial class PinnedPageViewModel : ViewModelBase, IPage
{
    private readonly WeatherService _weatherService;
    private readonly NavigationService _navigationService;
    public string Title => "PLACES";
    public PageType Type => PageType.Pinned;
    [ObservableProperty] private ObservableCollection<CityModel>?  _cities;

    public PinnedPageViewModel(WeatherService weatherService, NavigationService navigationService)
    {
        _weatherService = weatherService;
        _navigationService = navigationService;
        Init();
    }

    public async Task Init()
    {
        var cities = _weatherService.GetPinned();
        foreach (var cityModel in cities)
        {
            var forecast = await _weatherService.GetForecast(cityModel.Id);
            cityModel.Forecast = new ObservableCollection<ForecastModel>(forecast);
        }
        Cities = new ObservableCollection<CityModel>(cities);
    }

    [RelayCommand]
    private void Add()
    {
        _navigationService.NavigateToSearch();
    }

    [RelayCommand]
    private void Return()
    {
        _navigationService.NavigateToHome();
    }

    [RelayCommand]
    private async Task Unpin(int id)
    {
        _weatherService.Unpin(id);
        var cities = _weatherService.GetPinned();
        if (cities != null || cities.Count == 0)
        {
            foreach (var cityModel in cities)
            {
                var forecast = await _weatherService.GetForecast(cityModel.Id);
                cityModel.Forecast = new ObservableCollection<ForecastModel>(forecast);
            }
        }
        Cities = new ObservableCollection<CityModel>(cities);
    }
}