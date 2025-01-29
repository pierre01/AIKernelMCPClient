using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfHousePassiveClient.ViewModels;

public partial class KitchenViewModel:ObservableObject
{
    public KitchenViewModel(LightViewModel bar, LightViewModel main, LightViewModel cabinet)
    {
        BarLights = bar;
        MainLights = main;
        CabinetLights = cabinet;
        BarLights.LightSwitched += Lights_LightSwitched;
        MainLights.LightSwitched += Lights_LightSwitched;
        CabinetLights.LightSwitched += Lights_LightSwitched;
    }

    public bool IsRoomDark
    {
        get
        {
            return BarLights.IsOn && MainLights.IsOn && CabinetLights.IsOn;
        }
    }

    private void Lights_LightSwitched(object sender, bool e)
    {
         OnPropertyChanged(nameof(IsRoomDark));
    }

    [ObservableProperty]
    public partial LightViewModel BarLights { get; set; }

    [ObservableProperty]
    public partial LightViewModel MainLights { get; set; }

    [ObservableProperty]
    public partial LightViewModel CabinetLights { get; set; }
}
