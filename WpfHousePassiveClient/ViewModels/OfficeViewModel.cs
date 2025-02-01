using CommunityToolkit.Mvvm.ComponentModel;
using LightsAPICommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;

public partial class OfficeViewModel : ObservableObject
{
    public OfficeViewModel()
    {
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            LightViewModel deskLight = new LightViewModel(new Light(1, "desk", 1, true, true, "ffc58f", true, 70));
           
            LightViewModel floorLight = new LightViewModel(new Light(19, "floorLamp", 1, true, false, "FFFFFF", false, 60));
         

            DeskLight = deskLight;
            FloorLight = floorLight;
        }
    }

    public OfficeViewModel(LightViewModel deskLight, LightViewModel floorLight)
    {
        DeskLight = deskLight;
        FloorLight = floorLight;
        DeskLight.LightSwitched += Lights_LightSwitched;
        FloorLight.LightSwitched += Lights_LightSwitched;
    }

    [ObservableProperty]
    public partial LightViewModel DeskLight { get; set; }

    [ObservableProperty]
    public partial LightViewModel FloorLight { get; set; }



    public Visibility IsRoomLighten
    {
        get
        {
            if (DeskLight.IsOn || FloorLight.IsOn )
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

