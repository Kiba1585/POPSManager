using System;
using System.IO;

namespace POPSManager.Services
{
    public class PathsService
    {
        public string PopsFolder { get; private set; }
        public string BaseElfPath { get; private set; }

        private readonly Action<string> log;

        public PathsService(Action<string> log)
        {
            this.log = log;

            // Carpeta POPS por defecto en Documentos
            PopsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "POPS"
            );

            EnsureFolder(PopsFolder);

            // Buscar POPSTARTER.ELF automáticamente
            BaseElfPath = FindPopstarterElf();
        }

        // ============================================================
        //  CREAR CARPETA SI NO EXISTE
        // ============================================================

        private void EnsureFolder(string folder)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    log($"Carpeta creada: {folder}");
                }
            }
            catch (Exception ex)
            {
                log($"ERROR creando carpeta {folder}: {ex.Message}");
            }
        }

        // ============================================================
        //  BUSCAR POPSTARTER.ELF AUTOMÁTICAMENTE
        // ============================================================

        private string FindPopstarterElf()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string[] searchPaths =
            {
                Path.Combine(baseDir, "POPSTARTER.ELF"),
                Path.Combine(baseDir, "Resources", "POPSTARTER.ELF"),
                Path.Combine(baseDir, "Installer", "POPSTARTER.ELF"),
                Path.Combine(PopsFolder, "POPSTARTER.ELF"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "POPSTARTER.ELF"
                )
            };

            foreach (var path in searchPaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        log($"POPSTARTER.ELF encontrado en: {path}");
                        return path;
                    }
                }
                catch (Exception ex)
                {
                    log($"ERROR accediendo a {path}: {ex.Message}");
                }
            }

            log("ADVERTENCIA: No se encontró POPSTARTER.ELF en ninguna ubicación conocida.");
            return "";
        }

        // ============================================================
        //  PERMITIR AL USUARIO SELECCIONAR MANUALMENTE EL ELF
        // ============================================================

        public void SetCustomElfPath(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    BaseElfPath = path;
                    log($"POPSTARTER.ELF configurado manualmente: {path}");
                }
                else
                {
                    log($"ERROR: El archivo especificado no existe: {path}");
                }
            }
            catch (Exception ex)
            {
                log($"ERROR verificando archivo personalizado: {ex.Message}");
            }
        }
    }
}
