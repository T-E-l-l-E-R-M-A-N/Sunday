using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Sunday.Services;
using Sunday.ViewModels;

namespace Sunday.Models;

public partial class CityModel : ViewModelBase
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Message { get; set; }
    public bool IsPinned { get; set; }
    public ForecastModel CurrentWeather { get; set; }

    [ObservableProperty] private ObservableCollection<ForecastModel> _forecast;
}