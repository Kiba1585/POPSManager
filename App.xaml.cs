using System.Windows;
using POPSManager.Services;

namespace POPSManager
{
    public partial class App : Application
    {
        // Inicialización segura y sin warnings
        public AppServices Services { get; } = new AppServices();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ya no es necesario inicializar aquí
            // Services = new AppServices();
        }
    }
}
