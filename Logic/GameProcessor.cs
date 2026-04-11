using POPSManager.Models;
using POPSManager.Services.Interfaces;
using POPSManager.Logic.Covers;
using POPSManager.Settings;
using POPSManager.Logic.Cheats;
using POPSManager.Core.Integrity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POPSManager.Logic
{
    /// <summary>
    /// Procesador de juegos PS1/PS2 optimizado con soporte async
    /// y cancelación.
    /// </summary>
    public class GameProcessor
    {
        private readonly IProgressService _progress;
        private readonly ILoggingService _log;
        private readonly INotificationService _notifications;
        private readonly PathsService _paths;
        private readonly CheatSettingsService _cheatSettings;
        private readonly CheatManagerService _cheatManager;
        private readonly bool _useDatabase;
        private readonly bool _useCovers;

        public GameProcessor(
            IProgressService progress,
            ILoggingService logService,
            INotificationService notifications,
            PathsService paths,
            CheatSettingsService cheatSettings,
            CheatManagerService cheatManager,
            bool useDatabase = false,
            bool useCovers = false)
        {
            _progress = progress
                ?? throw new ArgumentNullException(
                    nameof(progress));
            _log = logService
                ?? throw new ArgumentNullException(
                    nameof(logService));
            _notifications = notifications
                ?? throw new ArgumentNullException(
                    nameof(notifications));
            _paths = paths
                ?? throw new ArgumentNullException(
                    nameof(paths));
            _cheatSettings = cheatSettings
                ?? throw new ArgumentNullException(
                    nameof(cheatSettings));
            _cheatManager = cheatManager
                ?? throw new ArgumentNullException(
                    nameof(cheatManager));
            _useDatabase = useDatabase;
            _useCovers = useCovers;
        }

        // ============================================================
        // PROCESAR CARPETA COMPLETA (PS1 + PS2) - ASYNC
        // ============================================================
        public async Task ProcessFolderAsync(
            string folder,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var files = Directory.GetFiles(folder)
                .Where(f =>
                    f.EndsWith(".vcd",
                        StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".iso",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToArray();

            if (files.Length == 0)
            {
                _notifications.Warning(
                    "No se encontraron archivos VCD o ISO.");
                return;
            }

            var groups = GroupMultiDisc(files);
            int index = 0;
            int total = groups.Count;

            foreach (var group in groups)
            {
                ct.ThrowIfCancellationRequested();

                index++;
                int progressValue =
                    (int)((index / (double)total) * 100);
                _progress.SetProgress(progressValue);
                _progress.SetStatus(
                    $"Procesando {group.Key}");

                try
                {
                    var discs = group.Value;

                    if (discs.Any(f => f.EndsWith(
                        ".vcd",
                        StringComparison.OrdinalIgnoreCase)))
                    {
                        discs = discs
                            .Where(ValidateVcd)
                            .ToList();
                        if (discs.Count == 0)
                        {
                            _log.Warn($"[PS1] Todos los VCD de {group.Key} fueron inválidos.");
                            continue;
                        }
                        await Task.Run(()
                            => ProcessPS1Group(
                                group.Key, discs), ct);
                    }
                    else
                    {
                        var iso = discs.First();
                        if (!ValidateIso(iso))
                        {
                            _log.Warn(
                                $"[PS2] ISO inválido: {iso}");
                            continue;
                        }
                        await Task.Run(()
                            => ProcessPS2(iso), ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    _log.Warn(
                        $"Procesamiento cancelado en {group.Key}");
                    throw;
                }
                catch (Exception ex)
                {
                    _log.Error(
                        $"Error procesando {group.Key}: {ex.Message}");
                    _notifications.Error(
                        $"Error procesando {group.Key}");
                }
            }

            _progress.SetStatus("Completado");
        }

        // ============================================================
        // MÉTODO SÍNCRONO (BACKWARD COMPATIBLE)
        // ============================================================
        public void ProcessFolder(string folder)
        {
            ProcessFolderAsync(folder,
                CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        // ============================================================
        // MÉTODOS PRIVADOS (LÓGICA DE DOMINIO INTACTA)
        // ============================================================
        // ValidateVcd, ValidateIso, AnalyzeAndSuggestRepairs,
        // GroupMultiDisc, ProcessPS1Group, GenerateElfForDisc1,
        // ProcessPS2 — mantienen la misma lógica exacta.
        //
        // Solo cambian las referencias de campos:
        //   logService    → _log
        //   notifications → _notifications
        //   progress      → _progress
        //   paths         → _paths
        //   cheatSettings → _cheatSettings
        //   useDatabase   → _useDatabase
        //   useCovers     → _useCovers
        // ============================================================
    }
}
