using CommunityToolkit.Mvvm.ComponentModel;
using LightsAPICommon;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Diagnostics;

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
        foreach (var light in House.Instance.Lights)
        {
            var lightViewModel = new LightViewModel(light);
            _lightViewModels.Add(light.LightId, lightViewModel);
        }
        // set hhtpclient user agent
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "API SoftClient");

        LivingRoom = new LivingRoomViewModel(_lightViewModels[0],_lightViewModels[1],_lightViewModels[2]);
        Office = new OfficeViewModel(_lightViewModels[8],_lightViewModels[9]);
        Stairs = new StairsViewModel(_lightViewModels[10]);
        Kitchen = new KitchenViewModel(_lightViewModels[3], _lightViewModels[4], _lightViewModels[5]);
        LaundryRoom = new LaundryRoomViewModel(_lightViewModels[7]);
        DownstairsBathroom = new DownstairsBathroomViewModel(_lightViewModels[6]);
        MasterBedroom = new MasterBedroomViewModel(_lightViewModels[11], _lightViewModels[12], _lightViewModels[13]);
        MasterBathroom = new MasterBathroomViewModel(_lightViewModels[14], _lightViewModels[15]);
        GuestBedroom = new GuestBedroomViewModel(_lightViewModels[20], _lightViewModels[21], _lightViewModels[22]);
        GuestBathroom = new GuestBathroomViewModel(_lightViewModels[16], _lightViewModels[17]);
        MasterCloset = new MasterClosetViewModel(_lightViewModels[18]);
        GuestCloset = new GuestClosetViewModel(_lightViewModels[19]);

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
                    _lightViewModels[light.LightId].Update(light);
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
