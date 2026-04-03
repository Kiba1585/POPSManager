using System.IO;

namespace POPSManager.Services
{
    public class ConverterService
    {
        /// <summary>
        /// Convierte todos los archivos BIN/CUE/ISO de una carpeta a VCD.
        /// Esta es una versión placeholder que solo copia los archivos.
        /// Puedes reemplazar la lógica interna por tu conversión real.
        /// </summary>
        public void ConvertFolder(string sourceFolder, string outputFolder)
        {
            if (!Directory.Exists(sourceFolder) || !Directory.Exists(outputFolder))
                return;

            var files = Directory.GetFiles(sourceFolder);

            foreach (var file in files)
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();

                // Solo convertir formatos válidos
                if (ext != ".bin" && ext != ".cue" && ext != ".iso")
                    continue;

                string name = Path.GetFileNameWithoutExtension(file);
                string dest = Path.Combine(outputFolder, $"{name}.vcd");

                // Placeholder: copia el archivo como .vcd
                File.Copy(file, dest, true);
            }
        }
    }
}
