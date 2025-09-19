using CommunityToolkit.Mvvm.ComponentModel;
using Lights.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;

public partial class GuestBedroomViewModel : ObservableObject
{
    public GuestBedroomViewModel()
    {
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            LightViewModel rightNightstand
                = new(new Light(1, "Front", 1,  LightState.On, new( true, true), "FFFFFF",  50));
           
            LightViewModel leftNightstand = new(new Light(19, "Back", 1, LightState.On, new(true, true), "FF0000", 100));
         
            LightViewModel wall = new(new Light(18, "Wall", 1,  LightState.On, new( true, true), "FFFFFF",  50));


            LeftNightstandLight = leftNightstand;
            RightNightstandLight = rightNightstand;
            WallLight = wall;
        }
    }

    public GuestBedroomViewModel(LightViewModel leftNightstand, LightViewModel rightNightstand, LightViewModel wall)
    {
        LeftNightstandLight = leftNightstand;
        RightNightstandLight = rightNightstand;
        WallLight = wall;
        LeftNightstandLight.LightSwitched += Lights_LightSwitched;
        RightNightstandLight.LightSwitched += Lights_LightSwitched;
        WallLight.LightSwitched += Lights_LightSwitched;
    }

    [ObservableProperty]
    public partial LightViewModel LeftNightstandLight { get; set; }

    [ObservableProperty]
    public partial LightViewModel RightNightstandLight { get; set; }

    [ObservableProperty]
    public partial LightViewModel WallLight { get; set; }


    public Visibility IsRoomLighten
    {
        get
        {
            if (LeftNightstandLight.IsOn || RightNightstandLight.IsOn || WallLight.IsOn)
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

