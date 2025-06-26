using LightsAPICommon;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using WpfHousePassiveClient.ViewModels;


namespace WpfHousePassiveClient;
/// http://localhost:5042/openapi/v1/openapi.json
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }


    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        //// Set the window size to 500x900
        //this.Width = 500;
        //this.Height = 900;
        //// Center the window on the screen
        //this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}


