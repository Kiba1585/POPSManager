using POPSManager.Services;

namespace POPSManager
{
    public partial class App : Application
    {
        public static AppServices Services { get; private set; }

        public App()
        {
            Services = new AppServices();
        }
    }
}
