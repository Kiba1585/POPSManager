using System.Windows;
using POPSManager.Services;

namespace POPSManager
{
    public partial class App : Application
    {
        // Servicios globales accesibles desde toda la aplicación
        public static AppServices? Services { get; private set; }

        public App()
        {
            // Inicialización temprana de servicios globales
            Services = new AppServices();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inicializaciones globales opcionales
            // Services.Paths.EnsureFolderStructure();
        }
    }
}
