using System;
using System.IO;

namespace POPSManager.Core.Integrity
{
    public sealed class VcdInspector
    {
        public string Path { get; }

        public VcdInspector(string vcdPath)
        {
            Path = vcdPath ?? throw new ArgumentNullException(nameof(vcdPath));
        }

        public IntegrityReport InspectBasic()
        {
            var report = new IntegrityReport();

            if (!File.Exists(Path))
            {
                report.AddError("VCD_NOT_FOUND", $"El archivo VCD no existe: {Path}");
                return report;
            }

            var info = new FileInfo(Path);
            if (info.Length < 1024 * 1024)
                report.AddWarning("VCD_TOO_SMALL", $"El VCD parece demasiado pequeño ({info.Length} bytes).");

            if (info.Length % 2048 != 0)
                report.AddWarning("VCD_ALIGNMENT", "El tamaño del VCD no es múltiplo de 2048 bytes (posible desalineación).");

            // Aquí podrías leer el header POPStarter, offsets, etc.
            // Por ahora solo dejamos hooks para no romper nada.

            report.AddInfo("VCD_OK_BASIC", "Validación básica del VCD completada.");
            return report;
        }
    }
}
