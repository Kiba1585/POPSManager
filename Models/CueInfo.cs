using System.Collections.Generic;

namespace POPSManager.Models
{
    public class CueInfo
    {
        public string CuePath { get; set; } = "";
        public List<CueFile> Files { get; set; } = new();
        public List<CueTrack> Tracks { get; set; } = new();

        // Archivo BIN principal (TRACK 01 INDEX 01)
        public string BinPath { get; set; } = "";
    }

    public class CueFile
    {
        public string Path { get; set; } = "";
        public string Type { get; set; } = ""; // BINARY, WAVE, etc.
    }

    public class CueTrack
    {
        public int Number { get; set; }
        public string Mode { get; set; } = ""; // MODE1/2352, MODE2/2352, AUDIO
        public List<CueIndex> Indexes { get; set; } = new();
        public CueFile? File { get; set; }
    }

    public class CueIndex
    {
        public int Number { get; set; } // 00, 01, 02...
        public int Minute { get; set; }
        public int Second { get; set; }
        public int Frame { get; set; }

        public int ToSector() => (Minute * 60 * 75) + (Second * 75) + Frame;
    }
}
