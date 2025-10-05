using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sunday.Services;

namespace Sunday.ViewModels;

public partial class SideMenuViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;
    [ObservableProperty] private bool _isPresented;
    [ObservableProperty] private ObservableCollection<IPage>? _pages;

    public SideMenuViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void Init()
    {
        var pages = IoC.Resolve<IEnumerable<IPage>>();
        Pages  = new ObservableCollection<IPage>(pages.Skip(0));
        _navigationService.NavigateToHome();
    }

    [RelayCommand]
    private void TogglePresented()
    {
        IsPresented = !IsPresented;
    }

    [RelayCommand]
    private void OpenPage(IPage page)
    {
        _navigationService.Navigate(page);
        IsPresented = false;
    }
}