using AIKernelClient.ViewModels;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using System.Globalization;

namespace AIKernelClient
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;


        public MainPage( MainPageViewModel viewModel)
        {
            _viewModel = viewModel;
            BindingContext = _viewModel;
            InitializeComponent();
        }

    }

}
