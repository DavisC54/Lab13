using System;
using System.Globalization;
using System.Xml;
using UnityEngine;

public enum City
{
    Orlando,
    London,
    Tokyo,
    Sydney,
    Reykjavik
}

/// <summary>
/// Grabs the weather for the selected city and sets the skybox and sun to match
/// Changing the city in the Inspector during Play mode reloads everything
/// </summary>
public class WeatherController : MonoBehaviour
{
    // Determines how close to sunrise or sunset actually counts as sunrise or sunset
    private const double TwilightPeriod = 45;

    [SerializeField] private City city;
    [SerializeField] private Light sun;

    [Header("Skyboxes")]
    [SerializeField] private Material daySky;
    [SerializeField] private Material sunnySky;
    [SerializeField] private Material sunlessSky;
    [SerializeField] private Material rainySky;
    [SerializeField] private Material snowySky;
    [SerializeField] private Material sunriseSky;
    [SerializeField] private Material sunsetSky;
    [SerializeField] private Material nightSky;

    private WeatherManager weatherManager;
    private City loadedCity;

    private void Awake()
    {
        weatherManager = new WeatherManager();
    }

    private void Start()
    {
        LoadWeather();
    }

    // watching for the enum to change in the Inspector
    private void Update()
    {
        if (city != loadedCity)
        {
            LoadWeather();
        }
    }

    private void LoadWeather()
    {
        loadedCity = city;
        StartCoroutine(weatherManager.GetWeatherXML(GetQuery(city), OnXMLDataLoaded));
    }

    private string GetQuery(City target)
    {
        return target switch
        {
            City.Orlando => "Orlando,us",
            City.London => "London,gb",
            City.Tokyo => "Tokyo,jp",
            City.Sydney => "Sydney,au",
            City.Reykjavik => "Reykjavik,is",
            _ => "Orlando,us"
        };
    }

    private void OnXMLDataLoaded(string data)
    {
        int code;
        DateTime sunrise;
        DateTime sunset;

        try
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            XmlNode root = doc.SelectSingleNode("current");

            XmlNode weather = root.SelectSingleNode("weather");
            code = int.Parse(weather.Attributes["number"].Value);
            string cityName = root.SelectSingleNode("city").Attributes["name"].Value;

            // timezone is the city's UTC offset in seconds
            int offsetSeconds = int.Parse(root.SelectSingleNode("city/timezone").InnerText);
            DateTime cityTime = DateTime.UtcNow.AddSeconds(offsetSeconds);
            double hoursFromHere = (cityTime - DateTime.Now).TotalHours;

            // rise and set are UTC but have no Z on the end, so force UTC or they get read as PC local time
            XmlNode sunNode = root.SelectSingleNode("city/sun");
            sunrise = ParseUtc(sunNode.Attributes["rise"].Value);
            sunset = ParseUtc(sunNode.Attributes["set"].Value);

            Debug.Log($"{cityName}: {weather.Attributes["value"].Value} ({code}), local {cityTime:HH:mm} ({hoursFromHere:+0.#;-0.#;0}h)");
        }
        catch (Exception e)
        {
            // Bad XML or a missing node, bail instead of half-applying the weather
            Debug.LogError($"weather parse failed: {e.Message}");
            return;
        }

        ApplyWeather(code, DateTime.UtcNow, sunrise, sunset);
    }

    private DateTime ParseUtc(string value)
    {
        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    // Picks the sky and sun from the OpenWeatherMap condition code
    // Weather codes: 2xx-5xx rain, 6xx snow, 7xx fog, 800 clear, 801+ clouds
    private void ApplyWeather(int code, DateTime nowUtc, DateTime sunrise, DateTime sunset)
    {
        bool nearSunrise = Math.Abs((nowUtc - sunrise).TotalMinutes) <= TwilightPeriod;
        bool nearSunset = Math.Abs((nowUtc - sunset).TotalMinutes) <= TwilightPeriod;
        bool isNight = !nearSunrise && !nearSunset && (nowUtc < sunrise || nowUtc > sunset);

        if (isNight)
        {
            SetSky(nightSky, 0.15f, new Color(0.45f, 0.55f, 0.85f));
            return;
        }

        if (code >= 600 && code < 700)
        {
            SetSky(snowySky, 0.6f, new Color(0.85f, 0.9f, 1f));
            return;
        }

        if (code < 600)
        {
            SetSky(rainySky, 0.45f, new Color(0.6f, 0.65f, 0.75f));
            return;
        }

        if (nearSunrise)
        {
            SetSky(sunriseSky, 0.8f, new Color(1f, 0.7f, 0.45f));
            return;
        }

        if (nearSunset)
        {
            SetSky(sunsetSky, 0.8f, new Color(1f, 0.55f, 0.3f));
            return;
        }

        if (code == 800)
        {
            SetSky(sunnySky, 1.3f, new Color(1f, 0.96f, 0.85f));
        }
        else if (code >= 801 && code <= 802)
        {
            SetSky(daySky, 1f, Color.white);
        }
        else
        {
            SetSky(sunlessSky, 0.6f, new Color(0.75f, 0.78f, 0.82f));
        }
    }

    private void SetSky(Material sky, float intensity, Color color)
    {
        RenderSettings.skybox = sky;

        // Ambient lighting comes from the skybox so it won't change until this gets called
        DynamicGI.UpdateEnvironment();

        sun.intensity = intensity;
        sun.color = color;
    }
}