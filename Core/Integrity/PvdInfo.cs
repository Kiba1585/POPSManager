namespace POPSManager.Core.Integrity
{
    /// <summary>
    /// Primary Volume Descriptor de un ISO/VCD.
    /// </summary>
    public class PvdInfo
    {
        public string Identifier { get; set; } = "";
        public string VolumeName { get; set; } = "";
        public string SystemId { get; set; } = "";
    }
}