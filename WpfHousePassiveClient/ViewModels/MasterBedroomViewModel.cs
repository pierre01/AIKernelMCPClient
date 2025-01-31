using CommunityToolkit.Mvvm.ComponentModel;
using LightsAPICommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;

public partial class MasterBedroomViewModel : ObservableObject
{
    public MasterBedroomViewModel()
    {
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            LightViewModel rightNightstand
                = new LightViewModel(new Light(1, "Front", 1, true, true, "FFFFFF", true, 30));
           
            LightViewModel leftNightstand = new LightViewModel(new Light(19, "Back", 1, true, true, "FF0000", true, 100));
         
            LightViewModel wall = new LightViewModel(new Light(18, "Wall", 1, true, false, "FFFFFF", true, 100));


            LeftNightstandLight = leftNightstand;
            RightNightstandLight = rightNightstand;
            WallLight = wall;
        }
    }

    public MasterBedroomViewModel(LightViewModel leftNightstand, LightViewModel rightNightstand, LightViewModel wall)
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
