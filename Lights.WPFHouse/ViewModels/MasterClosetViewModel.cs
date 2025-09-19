using CommunityToolkit.Mvvm.ComponentModel;
using Lights.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;


public partial class MasterClosetViewModel:ObservableObject
{
    public MasterClosetViewModel()
    {
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
           
            CeilingLight = new LightViewModel(new Light(3, "ceiling", 3,  LightState.On, new( true, true), "FFFFFF",  100));

        }
    }

    public MasterClosetViewModel(LightViewModel ceillingLight)
    {
        CeilingLight = ceillingLight;
        CeilingLight.LightSwitched += Lights_LightSwitched;
    }


    [ObservableProperty]
    public partial LightViewModel CeilingLight  { get; set; }
    
    public Visibility IsRoomLighten
    {
        get
        {
            if (CeilingLight.IsOn )
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



