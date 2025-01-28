using LightsAPICommon;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;


namespace WpfHousePassiveClient;
/// http://localhost:5042/openapi/v1/openapi.json
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
   // Reuse HttpClient if you make multiple calls, to avoid port exhaustion.
    private static readonly HttpClient _httpClient = new HttpClient();    
    
    public MainWindow()
    {
        InitializeComponent();
    }


    private async void FetchDataButton_Click(object sender, RoutedEventArgs e)
    {
        // Example endpoint: single lights from jsonplaceholder
        string apiUrl = "https://localhost:5042/lights";

        try
        {
            // Fetch data
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            // Get raw JSON
            string rawJson = await response.Content.ReadAsStringAsync();
            //List<Post>? posts = JsonSerializer.Deserialize<List<Post>>(rawJson);

            // Option 1: Work with a strongly-typed model
            List<Light>? lights = JsonSerializer.Deserialize<List<Light>>(rawJson);
            if (lights != null)
            {
                foreach(var light in lights)
                {
                    // Display a property or the entire JSON
                    ResultTextBox.Text += $"Light Id: {light.Id}\n" +
                        $"Light Name: {light.Name}\n" +
                        $"Light RoomId: {light.RoomId}\n" +
                        $"Light IsOn: {light.IsOn}\n" +
                        $"Light IsRgb: {light.IsRgb}\n" +
                        $"Light HexColor: {light.HexColor}\n" +
                        $"Light IsDimable: {light.IsDimable}\n" +
                        $"Light Brightness: {light.Brightness}\n\n";
                }
            }
            else
            {
                ResultTextBox.Text += "No valid data returned.\n";
            }
        }
        catch (Exception ex)
        {
            ResultTextBox.Text = $"An error occurred: {ex.Message}";
        }
    }


}

