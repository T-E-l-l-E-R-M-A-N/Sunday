using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sunday.ViewModels;

namespace Sunday.Services;

public class NavigationService
{
    public event Action OnNavigated;
    public IPage Current {get; private set;}

    public void Navigate(IPage page)
    {
        Current = page;
        OnNavigated?.Invoke();
    }

    public void NavigateToHome()
    {
        var page =  IoC.Resolve<IEnumerable<IPage>>().FirstOrDefault(x=>x.Type == PageType.Index);
        Navigate(page);
    }

    public void NavigateToSearch()
    {
        var page =  IoC.Resolve<IEnumerable<IPage>>().FirstOrDefault(x => x.Type == PageType.Search);
        Navigate(page);
    }
}