using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Sunday.ViewModels;

namespace Sunday.Views;

public partial class MainView : UserControl
{
    private Point _swipeStartPoint;
    private bool _swiping;
    public MainView()
    {
        InitializeComponent();

        ContentView.PointerPressed += ContentViewOnPointerPressed;
        ContentView.PointerMoved += ContentViewOnPointerMoved;
        ContentView.PointerReleased += ContentViewOnPointerReleased;
        
        SideMenu.PointerPressed += ContentViewOnPointerPressed;
        SideMenu.PointerMoved += ContentViewOnPointerMoved;
        SideMenu.PointerReleased += ContentViewOnPointerReleased;
    }

    private void ContentViewOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _swiping = false;
    }

    private void ContentViewOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _swiping = true;
        _swipeStartPoint = e.GetPosition(this);
    }

    private void ContentViewOnPointerMoved(object? sender, PointerEventArgs e)
    {
        var children = this.GetVisualDescendants();
        var scrollViewers = children.OfType<ScrollViewer>();
        var scroll = scrollViewers.FirstOrDefault(x=>x.Name == "ScrollViewer");
        if (_swiping)
        {
            if (scroll == null)
            {
                var swipe = e.GetPosition(this);
                if (_swipeStartPoint != null && swipe.X > (_swipeStartPoint.X + 10))
                {
                    (this.DataContext as MainViewModel).SideMenu.IsPresented = true;
                }
                if (_swipeStartPoint != null && swipe.X < (_swipeStartPoint.X - 10))
                {
                    (this.DataContext as MainViewModel).SideMenu.IsPresented = false;
                }
            }
            else
            {
                if (scroll.Offset.X == 0)
                {
                    if (_swiping)
                    {
                        var swipe = e.GetPosition(this);
                        if (_swipeStartPoint != null && swipe.X > (_swipeStartPoint.X + 10))
                        {
                            (this.DataContext as MainViewModel).SideMenu.IsPresented = true;
                        }
                        if (_swipeStartPoint != null && swipe.X < (_swipeStartPoint.X - 10))
                        {
                            (this.DataContext as MainViewModel).SideMenu.IsPresented = false;
                        }
                    }
                }
            }
        }
    }
}


