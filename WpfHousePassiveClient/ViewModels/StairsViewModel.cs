using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfHousePassiveClient.ViewModels;

public partial class StairsViewModel:ObservableObject
{
    public StairsViewModel()
    {
    }

    [ObservableProperty]
    public partial LightViewModel ChandelierLight { get; set; }
}
