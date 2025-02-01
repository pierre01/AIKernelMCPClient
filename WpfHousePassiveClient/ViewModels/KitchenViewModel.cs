using CommunityToolkit.Mvvm.ComponentModel;
using LightsAPICommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;

public partial class KitchenViewModel : ObservableObject
{

    public KitchenViewModel()
    {
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {

            BarLights = new LightViewModel(new Light(9, "BarLights", 3, true, true, "FFFFFF", true, 100));
            MainLights = new LightViewModel(new Light(2, "MainLights", 3, true, true, "FFFFFF", true, 100));
            CabinetLights = new LightViewModel(new Light(10, "CabinetLights", 3, true, true, "FFFFFF", true, 100));

        }
    }
    public KitchenViewModel(LightViewModel bar, LightViewModel main, LightViewModel cabinet)
    {
        BarLights = bar;
        MainLights = main;
        CabinetLights = cabinet;
        BarLights.LightSwitched += Lights_LightSwitched;
        MainLights.LightSwitched += Lights_LightSwitched;
        CabinetLights.LightSwitched += Lights_LightSwitched;
    }


    public Visibility IsRoomLighten
    {
        get
        {
            if (BarLights.IsOn || MainLights.IsOn  || CabinetLights.IsOn )
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

    [ObservableProperty]
    public partial LightViewModel BarLights { get; set; }

    [ObservableProperty]
    public partial LightViewModel MainLights { get; set; }

    [ObservableProperty]
    public partial LightViewModel CabinetLights { get; set; }
}
