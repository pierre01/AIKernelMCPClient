using CommunityToolkit.Mvvm.ComponentModel;
using LightsAPICommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHousePassiveClient.ViewModels;

    public partial class GuestClosetViewModel:ObservableObject
    {
        public GuestClosetViewModel()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
           
                CeilingLight = new LightViewModel(new Light(3, "ceiling", 3, true, true, "FFFFFF", true, 100));

            }
        }

        public GuestClosetViewModel(LightViewModel ceillingLight)
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


