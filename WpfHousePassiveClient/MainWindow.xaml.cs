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
        string apiUrl = "http://localhost:5042/lights";

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
                // Display a property or the entire JSON
                //ResultTextBlock.Text = $"Post Title: {lights.Title}\n" +
                //                       $"Post Body: {lights.Body}";
            }
            else
            {
                //ResultTextBlock.Text = "No valid data returned.";
            }
        }
        catch (Exception ex)
        {
            //ResultTextBlock.Text = $"An error occurred: {ex.Message}";
        }
    }
}

