using System.Collections.Generic;

namespace POPSManager.Models
{
    /// <summary>
    /// Información extraída de un archivo .CUE.
    /// </summary>
    public class CueInfo
    {
        /// <summary>Ruta al archivo .CUE.</summary>
        public string CuePath { get; set; } = "";

        /// <summary>Lista de archivos referenciados en el CUE.</summary>
        public List<CueFile> Files { get; set; } = new();

        /// <summary>Lista de pistas (tracks) del disco.</summary>
        public List<CueTrack> Tracks { get; set; } = new();

        /// <summary>Ruta al archivo BIN principal (TRACK 01 INDEX 01).</summary>
        public string BinPath { get; set; } = "";
    }

    /// <summary>
    /// Archivo referenciado en un CUE.
    /// </summary>
    public class CueFile
    {
        /// <summary>Ruta al archivo.</summary>
        public string Path { get; set; } = "";

        /// <summary>Tipo de archivo (BINARY, WAVE, etc.).</summary>
        public string Type { get; set; } = "";
    }

    /// <summary>
    /// Pista de un disco definida en un CUE.
    /// </summary>
    public class CueTrack
    {
        /// <summary>Número de pista (1, 2, ...).</summary>
        public int Number { get; set; }

        /// <summary>Modo de la pista (MODE1/2352, MODE2/2352, AUDIO).</summary>
        public string Mode { get; set; } = "";

        /// <summary>Índices de la pista.</summary>
        public List<CueIndex> Indexes { get; set; } = new();

        /// <summary>Archivo al que pertenece esta pista.</summary>
        public CueFile? File { get; set; }
    }

    /// <summary>
    /// Índice dentro de una pista CUE.
    /// </summary>
    public class CueIndex
    {
        /// <summary>Número de índice (00, 01, ...).</summary>
        public int Number { get; set; }

        /// <summary>Minuto.</summary>
        public int Minute { get; set; }

        /// <summary>Segundo.</summary>
        public int Second { get; set; }

        /// <summary>Frame (1/75 de segundo).</summary>
        public int Frame { get; set; }

        /// <summary>Convierte el tiempo a número de sector (2KB por sector).</summary>
        public int ToSector() => (Minute * 60 * 75) + (Second * 75) + Frame;
    }
}