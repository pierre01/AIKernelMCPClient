using CommunityToolkit.Mvvm.ComponentModel;
using LightsAPICommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;

public partial class StairsViewModel:ObservableObject
{
    public StairsViewModel()
    {
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
           
            ChandelierLight = new LightViewModel(new Light(3, "Chandelier", 3,  LightState.On, new( true, true), "FFFFFF",  100));

        }
    }

    public StairsViewModel(LightViewModel chandelierLight)
    {
        ChandelierLight = chandelierLight;
        ChandelierLight.LightSwitched += Lights_LightSwitched;
    }


    [ObservableProperty]
    public partial LightViewModel ChandelierLight  { get; set; }
    
    public Visibility IsRoomLighten
    {
        get
        {
            if (ChandelierLight.IsOn )
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
