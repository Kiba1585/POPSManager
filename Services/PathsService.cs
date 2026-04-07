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

            // Carpeta POPS por defecto
            PopsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "POPS");

            if (!Directory.Exists(PopsFolder))
                Directory.CreateDirectory(PopsFolder);

            BaseElfPath = FindPopstarterElf();
        }

        // ============================================================
        //  BUSCAR POPSTARTER.ELF AUTOMÁTICAMENTE
        // ============================================================

        private string FindPopstarterElf()
        {
            string[] searchPaths =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "POPSTARTER.ELF"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "POPSTARTER.ELF"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Installer", "POPSTARTER.ELF"),
                Path.Combine(PopsFolder, "POPSTARTER.ELF"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "POPSTARTER.ELF")
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    log($"POPSTARTER.ELF encontrado en: {path}");
                    return path;
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
    }
}
