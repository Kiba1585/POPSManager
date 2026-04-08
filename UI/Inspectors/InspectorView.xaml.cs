using POPSManager.Logic.Inspectors;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace POPSManager.UI.Inspectors
{
    public partial class InspectorView : UserControl
    {
        public InspectorView(string filePath)
        {
            InitializeComponent();
            DataContext = new InspectorViewModel(filePath);
        }
    }

    public class InspectorViewModel
    {
        // ============================
        //  PROPIEDADES BÁSICAS
        // ============================
        public string FilePath { get; }
        public string FileSize { get; }
        public string FileType { get; }

        public string GameId { get; }
        public string Region { get; }

        public string SystemCnf { get; }
        public string PvdInfo { get; }

        public ObservableCollection<InternalFileInfo> InternalFiles { get; } = new();

        // ============================
        //  CONSTRUCTOR
        // ============================
        public InspectorViewModel(string filePath)
        {
            FilePath = filePath;
            FileSize = FormatSize(new FileInfo(filePath).Length);
            FileType = DetectFileType(filePath);

            try
            {
                if (FileType == "PS1 VCD")
                {
                    var info = VcdInspector.Inspect(filePath);

                    GameId = info.GameId ?? "No detectado";
                    Region = info.Region ?? "Desconocida";

                    SystemCnf = info.SystemCnf != null
                        ? SafeDecode(info.SystemCnf)
                        : "No encontrado";

                    PvdInfo = info.Pvd != null
                        ? FormatPvd(info.Pvd)
                        : "PVD no disponible";

                    if (info.Files != null)
                    {
                        foreach (var f in info.Files.OrderBy(f => f.Key))
                            InternalFiles.Add(new InternalFileInfo(f.Key, f.Value.lba, f.Value.size));
                    }
                }
                else
                {
                    var info = IsoInspector.Inspect(filePath);

                    GameId = info.GameId ?? "No detectado";
                    Region = info.Region ?? "Desconocida";

                    SystemCnf = info.SystemCnf != null
                        ? SafeDecode(info.SystemCnf)
                        : "No encontrado";

                    PvdInfo = info.Pvd != null
                        ? FormatPvd(info.Pvd)
                        : "PVD no disponible";

                    if (info.Files != null)
                    {
                        foreach (var f in info.Files.OrderBy(f => f.Key))
                            InternalFiles.Add(new InternalFileInfo(f.Key, f.Value.lba, f.Value.size));
                    }
                }
            }
            catch (Exception ex)
            {
                SystemCnf = $"Error leyendo archivo:\n{ex.Message}";
                Region = "Desconocida";
                GameId = "Error";
                PvdInfo = "Error";
            }
        }

        // ============================
        //  HELPERS
        // ============================

        private string DetectFileType(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".vcd" ? "PS1 VCD" : "ISO";
        }

        private string FormatSize(long bytes)
        {
            double mb = bytes / 1024d / 1024d;
            return $"{mb:F2} MB";
        }

        private string SafeDecode(byte[] data)
        {
            try
            {
                return Encoding.ASCII.GetString(data);
            }
            catch
            {
                return "(No se pudo decodificar SYSTEM.CNF)";
            }
        }

        private string FormatPvd(PvdInfo pvd)
        {
            return
                $"Identificador: {pvd.Identifier}\n" +
                $"Volume Name: {pvd.VolumeName}\n" +
                $"System ID: {pvd.SystemId}";
        }
    }

    public class InternalFileInfo
    {
        public string Name { get; }
        public int Lba { get; }
        public int Size { get; }

        public InternalFileInfo(string name, int lba, int size)
        {
            Name = name;
            Lba = lba;
            Size = size;
        }
    }
}
