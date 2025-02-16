using CommunityToolkit.Mvvm.ComponentModel;
using LightsAPICommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;

public partial class GuestBathroomViewModel : ObservableObject
{
    public GuestBathroomViewModel()
    {
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            LightViewModel top = new(new Light(1, "Front", 1, LightState.On, new(true, true), "FFFFFF", 100));
           
            LightViewModel vanity = new(new Light(19, "Back", 1,  LightState.On, new( true, true), "FF0000",  100));
         

            TopLights = top;
            VanityLights = vanity;
        }
    }

    public GuestBathroomViewModel(LightViewModel top, LightViewModel vanity)
    {
        TopLights = top;
        VanityLights = vanity;
        TopLights.LightSwitched += Lights_LightSwitched;
        VanityLights.LightSwitched += Lights_LightSwitched;
    }

    [ObservableProperty]
    public partial LightViewModel TopLights { get; set; }

    [ObservableProperty]
    public partial LightViewModel VanityLights { get; set; }



    public Visibility IsRoomLighten
    {
        get
        {
            if (TopLights.IsOn || VanityLights.IsOn )
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
