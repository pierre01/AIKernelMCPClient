namespace AIKernelClient
{
    public partial class App : Application
    {
        const double newWidth = 500d;
        const double newHeight = 900d;

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {

            //app.Services
            var window = new Window(new AppShell());
            // return 

            //window.Created += Shell_Created;
            //window.Activated += Shell_Activated;
            //window.Deactivated += Shell_Deactivated;
            //window.Stopped += Shell_Stopped;
            //window.Resumed += Shell_Resumed;
            //window.Destroying += Shell_Destroying;            

            //window.SizeChanged += Window_SizeChanged;

            // Center the window.X and Y on the main display
            window.X = (DeviceDisplay.MainDisplayInfo.Width - newWidth) / 2;
            window.Y = (DeviceDisplay.MainDisplayInfo.Height - newHeight) / 2;

            // Lock the window size
            window.Width = newWidth;
            window.Height = newHeight;
            //window.MaximumHeight = newHeight;
            //window.MaximumWidth = newWidth;
            window.MinimumHeight = newHeight;
            window.MinimumWidth = newWidth;

            return window;
        }

    }
}