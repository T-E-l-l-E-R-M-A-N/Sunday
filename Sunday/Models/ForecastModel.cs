namespace Sunday.Services;

public class ForecastModel
{
    public int Temperature { get; set; }
    public int MaxTemperature { get; set; }
    public int MinTemperature { get; set; }
    public string Time { get; set; }
    public string Date { get; set; }
    public string Text { get; set; }
    
    public string WeatherImage { get; set; }
}