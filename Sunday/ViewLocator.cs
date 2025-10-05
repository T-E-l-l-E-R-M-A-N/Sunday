using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Sunday.Models;
using Sunday.ViewModels;

namespace Sunday;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().Name!.Replace("ViewModel", "");
        var type = this.GetType().Assembly.DefinedTypes.FirstOrDefault(x=>x.Name == name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase & data is not CityModel;
    }
}