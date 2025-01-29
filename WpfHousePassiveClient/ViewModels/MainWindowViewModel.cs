using CommunityToolkit.Mvvm.ComponentModel;
using LightsAPICommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Diagnostics;

namespace WpfHousePassiveClient.ViewModels;

class MainWindowViewModel:ObservableObject
{
        
    private const string apiUrl = "https://localhost:5042/lights";
    private static readonly HttpClient _httpClient = new HttpClient();
    private Dictionary<int, LightViewModel> _lightViewModels = new Dictionary<int, LightViewModel>();

    private bool _isPaused;
    private PeriodicTimer _fetchLightsTimer;

    JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = {new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)}
    };

    public MainWindowViewModel()
    {
        // Create a new LightViewModel for each light
        foreach (var light in House.Lights)
        {
            var lightViewModel = new LightViewModel(light);
            _lightViewModels.Add(light.Id, lightViewModel);
        }

        LivingRoom = new LivingRoomViewModel(_lightViewModels[1],_lightViewModels[19],_lightViewModels[18]);

       Start().ConfigureAwait(false);
    }

    public LivingRoomViewModel LivingRoom { get; }

    private async Task FetchData()
    {

        try
        {
            // Fetch data
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            // Get raw JSON
            string rawJson = await response.Content.ReadAsStringAsync();
            //List<Post>? posts = JsonSerializer.Deserialize<List<Post>>(rawJson);

            // Option 1: Work with a strongly-typed model
            List<Light> lights = JsonSerializer.Deserialize<List<Light>>(rawJson,_serializerOptions);
            if (lights != null)
            {
                foreach(var light in lights)
                {
                    // Display a property or the entire JSON
                    _lightViewModels[light.Id].Update(light);
                }
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine( $"An error occurred: {ex.Message}");
        }
    }

    private uint _timerTicks = 0;
    
    public async Task Start()
    {
        _isPaused = false;
        if (_fetchLightsTimer == null)
        {
            _fetchLightsTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            while (await _fetchLightsTimer.WaitForNextTickAsync())
            {
                if (!_isPaused)
                {
                    _timerTicks++;
                    //Fetch the lights every 10 ticks
                    if (_timerTicks % 10 == 0)
                    {
                        await FetchData();
                    }
                }
            }
        }
    }
}
