using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sunday.Models;
using Sunday.Services;

namespace Sunday.ViewModels;

public partial class SearchPageViewModel : ViewModelBase, IPage
{
    private readonly WeatherService _weatherService;
    private readonly NavigationService _navigationService;
    public string Title => "EXPLORE";
    public PageType Type => PageType.Search;
    [ObservableProperty] string? _searchText;
    [ObservableProperty] ObservableCollection<CityModel>? _searchResults;
    [ObservableProperty] bool _isSearching;

    public SearchPageViewModel(WeatherService weatherService, NavigationService navigationService)
    {
        _weatherService = weatherService;
        _navigationService = navigationService;
        SearchResults = [];
        Init();
    }

    public async Task Init()
    {
        var cities = _weatherService.PopularCities();
        foreach (var cityModel in cities)
        {
            var forecast = await _weatherService.GetForecast(cityModel.Id);
            cityModel.Forecast = new ObservableCollection<ForecastModel>(forecast);
            cityModel.CurrentWeather = forecast[0];
            SearchResults.Add(cityModel);
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        IsSearching = true;
        try
        {
            var geoCity = await _weatherService.GetBestMatchAsync(_searchText);
            if (geoCity != null)
            {
                var weather =
                    await _weatherService.GetWeatherDataByCoordinatesAsync(geoCity.Latitude, geoCity.Longitude);

                var cityModel = new CityModel()
                {
                    Id = weather.City.Id,
                    Name = weather.City.Name,
                };

                var forecast = await _weatherService.GetWeeklyForecastAsync(geoCity.Latitude, geoCity.Longitude);
                cityModel.Forecast = new ObservableCollection<ForecastModel>(forecast);
                cityModel.CurrentWeather = forecast[0];
                
                SearchResults.Insert(0, cityModel);
            }
            IsSearching = false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
    
    [RelayCommand]
    private void Return()
    {
        _navigationService.NavigateToHome();
    }

    [RelayCommand]
    private async Task Pin(CityModel cityModel)
    {
        _weatherService.Pin(cityModel);
        var pinned = IoC.Resolve<IEnumerable<IPage>>().FirstOrDefault(x => x.Type == PageType.Pinned);
        await(pinned as PinnedPageViewModel).Init();
        _navigationService.Navigate(pinned);
    }
}