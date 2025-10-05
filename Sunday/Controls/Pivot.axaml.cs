using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Sunday.ViewModels;

namespace Sunday.Controls;

public partial class Pivot : SelectingItemsControl
{
    private Point _swipeStartPoint;
    private bool _swiping;
    
    public static readonly StyledProperty<IDataTemplate> HeaderTemplateProperty = AvaloniaProperty.Register<Pivot, IDataTemplate>(
        nameof(HeaderTemplate));

    public IDataTemplate HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }
    public Pivot()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var ContentView = e.NameScope.Find<ContentControl>("PART_ContentView");

        if (ContentView != null)
        {
            ContentView.PointerPressed += ContentViewOnPointerPressed;
            ContentView.PointerMoved += ContentViewOnPointerMoved;
            ContentView.PointerReleased += ContentViewOnPointerReleased;
        }

        SelectedIndex = 0;
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
        if (_swiping)
        {
            var swipe = e.GetPosition(this);
            if (_swipeStartPoint != null && swipe.X > (_swipeStartPoint.X + 100))
            {
                if (SelectedIndex != 0) SelectedIndex--;
                _swiping  = false;
            }
            if (_swipeStartPoint != null && swipe.X < (_swipeStartPoint.X - 100))
            {
                if (SelectedIndex != Items.Count - 1) SelectedIndex++;
                _swiping  = false;
            }
        }
    }
}