using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfHousePassiveClient.ViewModels;

public partial class LaundryRoomViewModel:ObservableObject
{
    public LaundryRoomViewModel()
    {
    }

    [ObservableProperty]
    public partial LightViewModel TopLight { get; set; }
}
