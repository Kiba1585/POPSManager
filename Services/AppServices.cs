using Microsoft.Extensions.DependencyInjection;
using POPSManager.Logic;
using POPSManager.Logic.Cheats;
using POPSManager.Services.Interfaces;
using POPSManager.Settings;

namespace POPSManager.Services
{
    /// <summary>
    /// Contenedor de Inyección de Dependencias profesional.
    /// Reemplaza el Service Locator manual por
    /// Microsoft.Extensions.DependencyInjection.
    /// </summary>
    public sealed class AppServices : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        // ============================
        // ACCESO TIPADO A SERVICIOS
        // ============================
        public ILoggingService LogService
            => _provider.GetRequiredService<ILoggingService>();
        public INotificationService Notifications
            => _provider.GetRequiredService<INotificationService>();
        public IProgressService Progress
            => _provider.GetRequiredService<IProgressService>();
        public SettingsService Settings
            => _provider.GetRequiredService<SettingsService>();
        public PathsService Paths
            => _provider.GetRequiredService<PathsService>();
        public ConverterService Converter
            => _provider.GetRequiredService<ConverterService>();
        public CheatSettingsService CheatSettings
            => _provider.GetRequiredService<CheatSettingsService>();
        public CheatManagerService CheatManager
            => _provider.GetRequiredService<CheatManagerService>();

        // GameProcessor se inicializa bajo demanda (Lazy)
        public GameProcessor GameProcessor
            => _provider.GetRequiredService<Lazy<GameProcessor>>().Value;

        public AppServices()
        {
            var services = new ServiceCollection();

            // ============================
            // LOGGING (PRIMERO SIEMPRE)
            // ============================
            services.AddSingleton<LoggingService>();
            services.AddSingleton<ILoggingService>(sp
                => sp.GetRequiredService<LoggingService>());

            // ============================
            // SETTINGS (depende de Logging)
            // ============================
            services.AddSingleton(sp =>
            {
                var log = sp.GetRequiredService<ILoggingService>();
                return new SettingsService(log.Write);
            });

            // ============================
            // PATHS (depende de Settings + Logging)
            // ============================
            services.AddSingleton(sp =>
            {
                var log = sp.GetRequiredService<ILoggingService>();
                var settings = sp.GetRequiredService<SettingsService>();
                return new PathsService(log.Write, settings);
            });

            // ============================
            // NOTIFICACIONES
            // ============================
            services.AddSingleton<NotificationService>();
            services.AddSingleton<INotificationService>(sp
                => sp.GetRequiredService<NotificationService>());

            // ============================
            // PROGRESO GLOBAL
            // ============================
            services.AddSingleton<ProgressService>();
            services.AddSingleton<IProgressService>(sp
                => sp.GetRequiredService<ProgressService>());

            // ============================
            // CHEATS (CONFIG + MANAGER)
            // ============================
            services.AddSingleton(sp =>
            {
                var paths = sp.GetRequiredService<PathsService>();
                var log = sp.GetRequiredService<ILoggingService>();
                return new CheatSettingsService(
                    paths.RootFolder, log.Write);
            });

            services.AddSingleton(sp =>
            {
                var cheatSettings =
                    sp.GetRequiredService<CheatSettingsService>();
                var log = sp.GetRequiredService<ILoggingService>();
                return new CheatManagerService(
                    cheatSettings, log.Write);
            });

            // ============================
            // CONVERSIÓN PS1 (BIN/CUE -> VCD)
            // ============================
            services.AddSingleton(sp =>
            {
                var log = sp.GetRequiredService<ILoggingService>();
                var paths = sp.GetRequiredService<PathsService>();
                var settings =
                    sp.GetRequiredService<SettingsService>();
                var notif =
                    sp.GetRequiredService<INotificationService>();
                var progress =
                    sp.GetRequiredService<IProgressService>();
                return new ConverterService(
                    log.Write,
                    paths,
                    settings,
                    (msg, type) => notif.Show(msg, type),
                    progress.SetStatus
                );
            });

            // ============================
            // GAME PROCESSOR (PS1 + PS2) - LAZY
            // ============================
            services.AddSingleton(sp =>
                new Lazy<GameProcessor>(() =>
            {
                var progress =
                    sp.GetRequiredService<IProgressService>();
                var log =
                    sp.GetRequiredService<ILoggingService>();
                var notif =
                    sp.GetRequiredService<INotificationService>();
                var paths =
                    sp.GetRequiredService<PathsService>();
                var cheatSettings =
                    sp.GetRequiredService<CheatSettingsService>();
                var cheatManager =
                    sp.GetRequiredService<CheatManagerService>();
                var settings =
                    sp.GetRequiredService<SettingsService>();
                return new GameProcessor(
                    progress, log, notif, paths,
                    cheatSettings, cheatManager,
                    settings.UseDatabase, settings.UseCovers
                );
            }));

            _provider = services.BuildServiceProvider();
        }

        // ============================
        // RESOLVE GENÉRICO (para extensibilidad)
        // ============================
        public T GetService<T>() where T : notnull
            => _provider.GetRequiredService<T>();

        // ============================
        // DISPOSE ASYNC (flush de logs)
        // ============================
        public async ValueTask DisposeAsync()
        {
            if (_provider is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else
                _provider.Dispose();
        }
    }
}
