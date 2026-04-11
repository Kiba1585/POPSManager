namespace POPSManager.Logic.Cheats
{
    public class CheatDefinition
    {
        public string Code { get; set; } = "";          // Ej: "VMODE=NTSC"
        public string Name { get; set; } = "";          // Ej: "Forzar NTSC"
        public string Description { get; set; } = "";   // Texto amigable
        public string Category { get; set; } = "";      // Video, Compatibilidad, Debug, etc.
        public bool IsOfficial { get; set; }            // True = POPStarter oficial
        public bool IsUserDefined { get; set; }         // True = creado por el usuario
    }
}
