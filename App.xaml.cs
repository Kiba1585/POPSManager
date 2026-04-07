using System.Windows;
using POPSManager.Services;

namespace POPSManager
{
    public partial class App : Application
    {
        // Servicios globales accesibles desde toda la aplicación
        public static AppServices Services { get; private set; }

        public App()
        {
            // Inicialización segura y sin warnings
            Services = new AppServices();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Aquí podrías agregar inicializaciones globales si lo necesitas
            // Ejemplo: Services.Paths.EnsureFolderStructure();
        }
    }
}
