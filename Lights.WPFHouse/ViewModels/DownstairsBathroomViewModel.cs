using CommunityToolkit.Mvvm.ComponentModel;
using Lights.Common;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;

public partial class DownstairsBathroomViewModel:ObservableObject
{
    public DownstairsBathroomViewModel()
    {
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
           
            VanityLight = new LightViewModel(new Light(3, "Vanity", 3, LightState.On, new( true, true), "FFFFFF",  100));

        }
    }

    public DownstairsBathroomViewModel(LightViewModel vanityLight)
    {
        VanityLight = vanityLight;
        VanityLight.LightSwitched += Lights_LightSwitched;
    }


    [ObservableProperty]
    public partial LightViewModel VanityLight  { get; set; }
    
    public Visibility IsRoomLighten
    {
        get
        {
            if (VanityLight.IsOn )
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Hidden;
            }
        }
    }
    private void Lights_LightSwitched(object sender, bool e)
    {
        OnPropertyChanged(nameof(IsRoomLighten));
    }
}
