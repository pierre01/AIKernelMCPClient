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
using WpfHousePassiveClient.Views;

namespace WpfHousePassiveClient.ViewModels;

class MainWindowViewModel:ObservableObject
{
        
    private const string apiUrl = "https://localhost:5042/lights";
    //private const string apiUrl = "https://rh8xzzh8-5042.usw3.devtunnels.ms/lights";
    private static readonly HttpClient _httpClient = new();
    private readonly Dictionary<int, LightViewModel> _lightViewModels = [];

    private bool _isPaused;
    private PeriodicTimer _fetchLightsTimer;

    private readonly JsonSerializerOptions _serializerOptions = new()
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
        // set hhtpclient user agent
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "API SoftClient");

        LivingRoom = new LivingRoomViewModel(_lightViewModels[1],_lightViewModels[2],_lightViewModels[3]);
        Office = new OfficeViewModel(_lightViewModels[9],_lightViewModels[10]);
        Stairs = new StairsViewModel(_lightViewModels[11]);
        Kitchen = new KitchenViewModel(_lightViewModels[4], _lightViewModels[5], _lightViewModels[6]);
        LaundryRoom = new LaundryRoomViewModel(_lightViewModels[8]);
        DownstairsBathroom = new DownstairsBathroomViewModel(_lightViewModels[7]);
        MasterBedroom = new MasterBedroomViewModel(_lightViewModels[12], _lightViewModels[13], _lightViewModels[14]);
        MasterBathroom = new MasterBathroomViewModel(_lightViewModels[15], _lightViewModels[16]);
        GuestBedroom = new GuestBedroomViewModel(_lightViewModels[21], _lightViewModels[22], _lightViewModels[23]);
        GuestBathroom = new GuestBathroomViewModel(_lightViewModels[17], _lightViewModels[18]);
        MasterCloset = new MasterClosetViewModel(_lightViewModels[19]);
        GuestCloset = new GuestClosetViewModel(_lightViewModels[20]);

        Start().ConfigureAwait(false);
    }

    // All the rooms (areas with light) in the House
    public DownstairsBathroomViewModel DownstairsBathroom { get; }
    public GuestBathroomViewModel GuestBathroom { get; }
    public GuestBedroomViewModel GuestBedroom { get; }
    public GuestClosetViewModel GuestCloset { get; }
    public KitchenViewModel Kitchen { get; }
    public LaundryRoomViewModel LaundryRoom { get; }
    public LivingRoomViewModel LivingRoom { get; }
    public MasterBathroomViewModel MasterBathroom { get; }
    public MasterBedroomViewModel MasterBedroom { get; }
    public MasterClosetViewModel MasterCloset { get; }
    public OfficeViewModel Office { get; }
    public StairsViewModel Stairs { get; }

    private async Task FetchData()
    {

        try
        {
            // Fetch data
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            // Get raw JSON
            string rawJson = await response.Content.ReadAsStringAsync();

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
