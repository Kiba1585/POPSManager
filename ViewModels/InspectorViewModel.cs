using POPSManager.Core.Integrity;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace POPSManager.ViewModels
{
    public class InspectorViewModel : ViewModelBase
    {
        private string _filePath;
        private string _fileSize;
        private string _fileType;
        private string _gameId;
        private string _region;
        private string _systemCnf;
        private string _pvdInfo;
        private ObservableCollection<InternalFileInfo> _internalFiles = new();

        public InspectorViewModel() { }

        public InspectorViewModel(string filePath)
        {
            LoadFile(filePath);
        }

        public void LoadFile(string filePath)
        {
            FilePath = filePath;
            FileSize = FormatSize(new FileInfo(filePath).Length);
            FileType = DetectFileType(filePath);

            try
            {
                if (FileType == "PS1 VCD")
                {
                    var inspector = new VcdInspector(filePath);
                    var info = inspector.Inspect();

                    GameId = info.GameId ?? "No detectado";
                    Region = info.Region ?? "Desconocida";
                    SystemCnf = info.SystemCnf != null ? SafeDecode(info.SystemCnf) : "No encontrado";
                    PvdInfo = info.Pvd != null ? FormatPvd(info.Pvd) : "PVD no disponible";

                    InternalFiles.Clear();
                    if (info.Files != null)
                    {
                        foreach (var f in info.Files.OrderBy(f => f.Key))
                            InternalFiles.Add(new InternalFileInfo(f.Key, f.Value.lba, f.Value.size));
                    }
                }
                else
                {
                    GameId = "No detectado";
                    Region = "Desconocida";
                    SystemCnf = "No implementado";
                    PvdInfo = "No implementado";
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

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public string FileSize
        {
            get => _fileSize;
            set { _fileSize = value; OnPropertyChanged(); }
        }

        public string FileType
        {
            get => _fileType;
            set { _fileType = value; OnPropertyChanged(); }
        }

        public string GameId
        {
            get => _gameId;
            set { _gameId = value; OnPropertyChanged(); }
        }

        public string Region
        {
            get => _region;
            set { _region = value; OnPropertyChanged(); }
        }

        public string SystemCnf
        {
            get => _systemCnf;
            set { _systemCnf = value; OnPropertyChanged(); }
        }

        public string PvdInfo
        {
            get => _pvdInfo;
            set { _pvdInfo = value; OnPropertyChanged(); }
        }

        public ObservableCollection<InternalFileInfo> InternalFiles
        {
            get => _internalFiles;
            set { _internalFiles = value; OnPropertyChanged(); }
        }

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