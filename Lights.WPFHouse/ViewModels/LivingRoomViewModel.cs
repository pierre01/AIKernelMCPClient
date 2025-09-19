using CommunityToolkit.Mvvm.ComponentModel;
using Lights.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;

public partial class LivingRoomViewModel : ObservableObject
{
    public LivingRoomViewModel()
    {
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            LightViewModel front = new(new Light(1, "Front", 1,  LightState.On, new( true, true), "FFFFFF",  100));
           
            LightViewModel back = new(new Light(19, "Back", 1,  LightState.On, new( true, true), "FF0000",  100));
         
            LightViewModel wall = new(new Light(18, "Wall", 1,  LightState.On, new( true, true), "FFFFFF",  100));


            FrontLights = front;
            BackLights = back;
            WallLight = wall;
        }
    }

    public LivingRoomViewModel(LightViewModel front, LightViewModel back, LightViewModel wall)
    {
        FrontLights = front;
        BackLights = back;
        WallLight = wall;
        FrontLights.LightSwitched += Lights_LightSwitched;
        BackLights.LightSwitched += Lights_LightSwitched;
        WallLight.LightSwitched += Lights_LightSwitched;
    }

    [ObservableProperty]
    public partial LightViewModel FrontLights { get; set; }

    [ObservableProperty]
    public partial LightViewModel BackLights { get; set; }

    [ObservableProperty]
    public partial LightViewModel WallLight { get; set; }


    public Visibility IsRoomLighten
    {
        get
        {
            if (FrontLights.IsOn || BackLights.IsOn || WallLight.IsOn)
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
