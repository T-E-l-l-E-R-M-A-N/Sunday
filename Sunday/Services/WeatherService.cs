using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using OpenWeatherMap.Standard;
using OpenWeatherMap.Standard.Models;
using Sunday.Models;

namespace Sunday.Services;

public record GeoPoint(double Latitude, double Longitude, string? City, string? Timezone);

public record LocalTimeResult(
    DateTimeOffset LocalDateTime,
    string Timezone,                 // IANA, напр. "Europe/Moscow"
    string TimezoneAbbreviation,     // напр. "MSK"
    TimeSpan UtcOffset               // смещение от UTC
);
public record GeoCity(
    string Name,
    double Latitude,
    double Longitude,
    string CountryCode,
    string? Admin1,
    string Timezone,
    int? Population,
    double? Elevation
);


public class WeatherService
{
    private readonly string _apiKey = "1ff7268e73ce387a3b19d0156dca18b3";
    private string _cachedImagesFolder;
    private Current _current;
    private readonly HttpClient _httpClient = new();
    private string? _jsonFilePath;

    public void Init()
    {
        _current = new Current(_apiKey);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _jsonFilePath = Environment.CurrentDirectory + "/weatherdata.json";
        else
        {
            _jsonFilePath = FileSystem.AppDataDirectory + "/weatherdata.json";
        }
        if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _cachedImagesFolder = Environment.CurrentDirectory + "/cache";
        else
        {
            _cachedImagesFolder = FileSystem.AppDataDirectory + "/cache";
        }

        PopularCities();
    }
    
    public async Task<List<GeoCity>> SearchCitiesAsync(
        string name,
        string? countryCode = null,
        int count = 5,
        string language = "ru")
    {
        if (string.IsNullOrWhiteSpace(name))
            return new();

        var url =
            $"https://geocoding-api.open-meteo.com/v1/search" +
            $"?name={Uri.EscapeDataString(name)}" +
            $"&count={count}" +
            $"&language={Uri.EscapeDataString(language)}";

        if (!string.IsNullOrWhiteSpace(countryCode))
            url += $"&country={Uri.EscapeDataString(countryCode)}"; // ISO-2 код страны

        var json = await _httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var list = new List<GeoCity>();
        if (!root.TryGetProperty("results", out var results))
            return list; // ничего не найдено

        foreach (var r in results.EnumerateArray())
        {
            list.Add(new GeoCity(
                Name: r.GetProperty("name").GetString()!,
                Latitude: r.GetProperty("latitude").GetDouble(),
                Longitude: r.GetProperty("longitude").GetDouble(),
                CountryCode: r.GetProperty("country_code").GetString()!,
                Admin1: r.TryGetProperty("admin1", out var a1) ? a1.GetString() : null,
                Timezone: r.TryGetProperty("timezone", out var tz) ? tz.GetString()! : "UTC",
                Population: r.TryGetProperty("population", out var pop) && pop.ValueKind is not JsonValueKind.Null
                            ? pop.GetInt32() : null,
                Elevation: r.TryGetProperty("elevation", out var elv) && elv.ValueKind is not JsonValueKind.Null
                           ? elv.GetDouble() : null
            ));
        }

        return list;
    }

    /// <summary>
    /// Возвращает "лучшее" совпадение (по наибольшему населению, если есть).
    /// </summary>
    public async Task<GeoCity?> GetBestMatchAsync(string name, string? countryCode = null, string language = "ru")
    {
        var list = await SearchCitiesAsync(name, countryCode, count: 10, language: language);
        return list
            .OrderByDescending(c => c.Population ?? 0)
            .ThenBy(c => c.Name.Length) // небольшой приоритет более точному имени
            .FirstOrDefault();
    }
    
    public async Task<LocalTimeResult> GetLocalTimeAsync(double lat, double lon)
    {
        // timezone=auto — вернёт корректную таймзону для этих координат
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString().Replace(',', '.')}&longitude={lon.ToString().Replace(',', '.')}&current_weather=true&timezone=auto";

        var json = await _httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tz  = root.GetProperty("timezone").GetString()!;
        var abbr = root.GetProperty("timezone_abbreviation").GetString()!;
        var offsetSeconds = root.GetProperty("utc_offset_seconds").GetInt32();

        // В current_weather.time приходит локальное время без смещения, ISO-8601, напр. "2025-10-05T21:30"
        var localNaive = DateTime.Parse(
            root.GetProperty("current_weather").GetProperty("time").GetString()!,
            CultureInfo.InvariantCulture);

        var offset = TimeSpan.FromSeconds(offsetSeconds);
        var local = new DateTimeOffset(localNaive, offset);

        return new LocalTimeResult(local, tz, abbr, offset);
    }
    
    public async Task<GeoPoint> GetCurrentAsync()
    {
        // Основной провайдер
        try
        {
            var json = await _httpClient.GetStringAsync("https://ipapi.co/json/");
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            return new GeoPoint(
                r.GetProperty("latitude").GetDouble(),
                r.GetProperty("longitude").GetDouble(),
                r.TryGetProperty("city", out var c) ? c.GetString() : null,
                r.TryGetProperty("timezone", out var tz) ? tz.GetString() : null
            );
        }
        catch
        {
            // Фолбэк (ipinfo.io) — тоже без ключа
            var json = await _httpClient.GetStringAsync("https://ipinfo.io/json");
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            var loc = r.GetProperty("loc").GetString()!; // "lat,lon"
            var parts = loc.Split(',');
            return new GeoPoint(
                double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                r.TryGetProperty("city", out var c) ? c.GetString() : null,
                r.TryGetProperty("timezone", out var tz) ? tz.GetString() : null
            );
        }
    }
    public async Task<List<ForecastModel>> GetWeeklyForecastAsync(double lat, double lon, string lang = "ru", string units = "metric")
    {
        string url =
            $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString().Replace(',', '.')}&longitude={lon.ToString().Replace(',', '.')}&daily=temperature_2m_max,temperature_2m_min,weathercode&timezone=auto";

        var json = await _httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement.GetProperty("daily");
        var times = root.GetProperty("time").EnumerateArray();
        var tempsMax = root.GetProperty("temperature_2m_max").EnumerateArray();
        var tempsMin = root.GetProperty("temperature_2m_min").EnumerateArray();
        var codes = root.GetProperty("weathercode").EnumerateArray();

        var forecasts = new List<ForecastModel>();
        var culture = new CultureInfo("ru-RU");

        while (times.MoveNext() && tempsMax.MoveNext() && tempsMin.MoveNext() && codes.MoveNext())
        {
            var date = DateTime.Parse(times.Current.GetString()!, culture);
            var max = (int)Math.Round(tempsMax.Current.GetDouble());
            var min = (int)Math.Round(tempsMin.Current.GetDouble());
            var code = codes.Current.GetInt32();

            var forecastModel = await CreateForecastModel(times, max, min, code);
            forecasts.Add(forecastModel);
            
        }

        return forecasts;
    }

    private async Task<ForecastModel> CreateForecastModel(JsonElement.ArrayEnumerator times, int max, int min, int code)
    {
        var forecastModel = new ForecastModel
        {
            Date = times.Current.GetDateTime().ToShortDateString(),
            Time = times.Current.GetDateTime().ToShortTimeString(),
            Temperature = (max + min) / 2,
            MaxTemperature = max,
            MinTemperature = min,
            Text = GetWeatherDescription(code)
        };
        forecastModel.WeatherImage = forecastModel.Text.ToLower() switch
        {
            "ясно" => await GetWeatherImage("clear", "https://i.pinimg.com/originals/fb/37/ae/fb37ae6b9b9442f432a896073db3cf8e.jpg"),
            "переменная облачность" => await GetWeatherImage("clouds", "https://inbusiness.kz/uploads/2024-7/vsIemgfj.jpg"),
            "туман" => await GetWeatherImage("fog", "https://osken-onir.kz/uploads/posts/2023-01/1673242787_1642754499_1-phonoteka-org-p-fon-tuman-1.jpg"),
            "мелкий дождь" => await GetWeatherImage("smallrain", "https://get.pxhere.com/photo/water-nature-snow-winter-sunlight-morning-rain-leaf-wildlife-stream-reflection-autumn-weather-season-freezing-atmospheric-phenomenon-21606.jpg"),
            "дождь" => await GetWeatherImage("rain", "https://avatars.mds.yandex.net/i?id=2d5e50761acd87dea612ad51de576513_l-8132087-images-thumbs&n=13"),
            "снег" => await GetWeatherImage("snow", "https://avatars.mds.yandex.net/get-znatoki-cover/1357594/2a0000017d8f310dfaa83fb7e128363c1f43/orig"),
            "ливень" => await GetWeatherImage("rainfall", "https://i.ytimg.com/vi/PXnPx_cMsdw/maxresdefault.jpg?sqp=-oaymwEmCIAKENAF8quKqQMa8AEB-AH-CYAC0AWKAgwIABABGFMgWChlMA8=&rs=AOn4CLDBV_B6FUy41L-RcFIobbGIn8ecTQ"),
            "гроза" => await GetWeatherImage("thunderstorm", "https://caliber.az/media/photos/original/f76bcaa2390055cd6328346b14e76693.webp"),
            _ => ""
        };
        return forecastModel;
    }

    private async Task<string> GetWeatherImage(string weather, string urlPath)
    {
        if(!Directory.Exists(_cachedImagesFolder))
            Directory.CreateDirectory(_cachedImagesFolder);

        switch (weather)
        {
            case "fog":
                if (!File.Exists(Path.Combine(_cachedImagesFolder, "fog.jpg")))
                {
                    using var wc = new WebClient();
                    await wc.DownloadFileTaskAsync(new  Uri(urlPath), Path.Combine(_cachedImagesFolder, "fog.jpg"));
                }
                
                return Path.Combine(_cachedImagesFolder, "fog.jpg");
            case "clear":
                if (!File.Exists(Path.Combine(_cachedImagesFolder, "clear.jpg")))
                {
                    using var wc = new WebClient();
                    await wc.DownloadFileTaskAsync(new  Uri(urlPath), Path.Combine(_cachedImagesFolder, "clear.jpg"));
                }
                return Path.Combine(_cachedImagesFolder, "clear.jpg");
            case "rain":
                if (!File.Exists(Path.Combine(_cachedImagesFolder, "rain.jpg")))
                {
                    using var wc = new WebClient();
                    await wc.DownloadFileTaskAsync(new  Uri(urlPath), Path.Combine(_cachedImagesFolder, "rain.jpg"));
                }
                return Path.Combine(_cachedImagesFolder, "rain.jpg");
            case "snow":
                if (!File.Exists(Path.Combine(_cachedImagesFolder, "snow.jpg")))
                {
                    using var wc = new WebClient();
                    await wc.DownloadFileTaskAsync(new  Uri(urlPath), Path.Combine(_cachedImagesFolder, "snow.jpg"));
                }
                return Path.Combine(_cachedImagesFolder, "snow.jpg");
            case "rainfall":
                if (!File.Exists(Path.Combine(_cachedImagesFolder, "rainfall.jpg")))
                {
                    using var wc = new WebClient();
                    await wc.DownloadFileTaskAsync(new  Uri(urlPath), Path.Combine(_cachedImagesFolder, "rainfall.jpg"));
                }
                return Path.Combine(_cachedImagesFolder, "rainfall.jpg");
            case "smallrain":
                if (!File.Exists(Path.Combine(_cachedImagesFolder, "smallrain.jpg")))
                {
                    using var wc = new WebClient();
                    await wc.DownloadFileTaskAsync(new  Uri(urlPath), Path.Combine(_cachedImagesFolder, "smallrain.jpg"));
                }
                return Path.Combine(_cachedImagesFolder, "smallrain.jpg");
            case "clouds":
                if (!File.Exists(Path.Combine(_cachedImagesFolder, "clouds.jpg")))
                {
                    using var wc = new WebClient();
                    await wc.DownloadFileTaskAsync(new  Uri(urlPath), Path.Combine(_cachedImagesFolder, "clouds.jpg"));
                }
                return Path.Combine(_cachedImagesFolder, "clouds.jpg");
            case "thunderstorm":
                if (!File.Exists(Path.Combine(_cachedImagesFolder, "thunderstorm.jpg")))
                {
                    using var wc = new WebClient();
                    await wc.DownloadFileTaskAsync(new  Uri(urlPath), Path.Combine(_cachedImagesFolder, "thunderstorm.jpg"));
                }
                return Path.Combine(_cachedImagesFolder, "thunderstorm.jpg");
        }

        return "";
    }
    
    private static string GetWeatherDescription(int code) => code switch
    {
        0 => "Ясно",
        1 or 2 or 3 =>      "Переменная облачность",
        45 or 48 =>         "Туман",
        51 or 53 or 55 =>   "Мелкий дождь",
        61 or 63 or 65 =>   "Дождь",
        71 or 73 or 75 =>   "Снег",
        80 or 81 or 82 =>   "Ливень",
        95 or 96 or 99 =>   "Гроза",
        _ => "Неизвестно"
    };

    public async Task<List<ForecastModel>> GetForecast(int cityId)
    {
        var city = _current.GetForecastDataByCityId(cityId);
        var lat = city.City.Coordinates.Latitude;
        var lon = city.City.Coordinates.Longitude;
        var forecast = await GetWeeklyForecastAsync(lat, lon);
        
        return forecast;
    }

    private static ForecastModel CreateForecastModel(WeatherData weatherData)
    {
        var temperature = weatherData.WeatherDayInfo.Temperature;
        var minTemp = weatherData.WeatherDayInfo.MinimumTemperature;
        var maxTemp = weatherData.WeatherDayInfo.MaximumTemperature;
        var localTime = weatherData.AcquisitionDateTime.ToUniversalTime().ToShortTimeString();
        var localDate = weatherData.AcquisitionDateTime.ToUniversalTime().ToShortDateString();
        var text = weatherData.Weathers.FirstOrDefault().Description;
        var forecastModel = new ForecastModel()
        {
            Temperature = Convert.ToInt32(temperature),
            Time = localTime,
            Date = localDate,
            Text = text,
            MaxTemperature = Convert.ToInt32(maxTemp),
            MinTemperature = Convert.ToInt32(minTemp),
        };
        
        return forecastModel;
    }

    public async Task<ForecastData> GetWeatherDataByCoordinatesAsync(double latitude, double longitude)
    {
        var weatherData = await _current.GetForecastDataByCoordinatesAsync(latitude, longitude);
        return weatherData;
    }

    public List<CityModel> PopularCities()
    {
        string[] cities = [ 
            "новороссийск", 
            "ярославль", 
            "сочи", 
            "краснодар",
            "тюмень",
            "санкт-петербург",
            "казань",
            "севастополь",
            "смоленск",
            "москва"
        ];
        List<CityModel> cityModels = new List<CityModel>();
        foreach (var city in cities)
        {
            var forecastData = _current?.GetForecastDataByCityName(city);

            var cityModel = CreateCityModel(forecastData);
            cityModels.Add(cityModel);
        }
        
        return cityModels;
    }

    public CityModel GetCity(string cityName)
    {
        var forecastData = _current?.GetForecastDataByCityName(cityName);
        var cityModel = CreateCityModel(forecastData);
        return cityModel;
    }
    public List<CityModel> GetPinned()
    {
        if (File.Exists(_jsonFilePath))
        {
            var json = File.ReadAllText(_jsonFilePath);
            var cities = JsonConvert.DeserializeObject<List<CityModel>>(json);
            return cities;
        }
        
        return [];
    }

    public void Pin(CityModel cityModel)
    {
        var pinned =  GetPinned();
        if(pinned.Find(x=>x.Id == cityModel.Id) == null)
            pinned.Insert(0,cityModel);
        SerializeCities(pinned);
    }

    public void Unpin(int id)
    {
        var pinned = GetPinned();
        var index = pinned.IndexOf(pinned.FirstOrDefault(x=>x.Id == id));
        if (index < 0)
            return;
        
        pinned.RemoveAt(index);
        SerializeCities(pinned);
    }
    private void SerializeCities(List<CityModel> pinned)
    {
        var serialized = JsonConvert.SerializeObject(pinned);
        File.WriteAllText(_jsonFilePath, serialized);
    }
    private CityModel CreateCityModel(ForecastData? forecastData)
    {
        var description = forecastData.WeatherData.FirstOrDefault().Weathers.FirstOrDefault().Description;
        var feels_like = Math.Round(forecastData.WeatherData.FirstOrDefault().WeatherDayInfo.FeelsLike);
        var message = $"Feels like: {Convert.ToInt32(feels_like)}°C. {description}";
        var cityModel = new CityModel()
        {
            Id = forecastData.City.Id,
            Name = forecastData.City.Name,
            Message = message
        };
        
        cityModel.CurrentWeather = CreateForecastModel(forecastData.WeatherData.FirstOrDefault());
        return cityModel;
    }
}