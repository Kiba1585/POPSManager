using System.Windows;
using POPSManager.Services;

namespace POPSManager
{
    public partial class App : Application
    {
        public AppServices Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inicializar todos los servicios de la aplicación
            Services = new AppServices();
        }
    }
}
