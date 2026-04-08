using POPSManager.Logic.Inspectors;
using System.Collections.ObjectModel;
using System.IO;
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
        public string FilePath { get; }
        public string FileSize { get; }
        public string FileType { get; }

        public string? GameId { get; }
        public string Region { get; }

        public string? SystemCnf { get; }
        public string PvdInfo { get; }

        public ObservableCollection<InternalFileInfo> InternalFiles { get; } = new();

        public InspectorViewModel(string filePath)
        {
            FilePath = filePath;
            FileSize = $"{new FileInfo(filePath).Length / 1024 / 1024} MB";
            FileType = filePath.EndsWith(".vcd", System.StringComparison.OrdinalIgnoreCase) ? "PS1 VCD" : "ISO";

            if (FileType == "PS1 VCD")
            {
                var info = VcdInspector.Inspect(filePath);

                GameId = info.GameId;
                Region = info.Region;
                SystemCnf = info.SystemCnf != null ? System.Text.Encoding.ASCII.GetString(info.SystemCnf) : "No encontrado";

                PvdInfo = $"ID: {info.Pvd.Identifier}\n" +
                          $"Volume: {info.Pvd.VolumeName}\n" +
                          $"System: {info.Pvd.SystemId}";

                foreach (var f in info.Files)
                    InternalFiles.Add(new InternalFileInfo(f.Key, f.Value.lba, f.Value.size));
            }
            else
            {
                var info = IsoInspector.Inspect(filePath);

                GameId = info.GameId;
                Region = info.Region;
                SystemCnf = info.SystemCnf != null ? System.Text.Encoding.ASCII.GetString(info.SystemCnf) : "No encontrado";

                PvdInfo = $"ID: {info.Pvd.Identifier}\n" +
                          $"Volume: {info.Pvd.VolumeName}\n" +
                          $"System: {info.Pvd.SystemId}";

                foreach (var f in info.Files)
                    InternalFiles.Add(new InternalFileInfo(f.Key, f.Value.lba, f.Value.size));
            }
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
