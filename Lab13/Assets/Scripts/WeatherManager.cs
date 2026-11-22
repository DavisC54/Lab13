using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Talks to OpenWeatherMap to get the current weather for the selected city
/// </summary>
public class WeatherManager
{
    private const string BaseUrl = "http://api.openweathermap.org/data/2.5/weather";

    private string apiKey;

    public WeatherManager()
    {
        // Key lives in Assets/Resources/ApiKey.txt so it isn't sitting in the code
        TextAsset keyFile = Resources.Load<TextAsset>("ApiKey");
        if (!keyFile)
        {
            Debug.LogError("ApiKey.txt missing from Resources");
            return;
        }

        // Trim so a trailing newline in the file doesn't break the URL
        apiKey = keyFile.text.Trim();
    }

    private IEnumerator CallAPI(string url, Action<string> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"network problem: {request.error}");
            }
            else if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"response error: {request.responseCode}");
            }
            else
            {
                callback(request.downloadHandler.text);
            }
        }
    }

    // city is "Name,countrycode" like "Orlando,us"
    public IEnumerator GetWeatherXML(string city, Action<string> callback)
    {
        string url = $"{BaseUrl}?q={city}&mode=xml&appid={apiKey}";
        return CallAPI(url, callback);
    }
}