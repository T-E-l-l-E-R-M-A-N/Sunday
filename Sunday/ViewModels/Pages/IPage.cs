namespace Sunday.ViewModels;

public interface IPage
{
    string Title { get; }
    PageType Type { get; }
    
}

public enum PageType
{
    Index,
    Pinned,
    Search,
    Settings,
    About,
    Forecast
}