namespace POPSManager.Logic
{
    public static class ElfOffsets
    {
        // ------------------------------------------------------------
        // GAME ID (SCES-XXXXX)
        // POPStarter lo almacena en offset 0x2C
        // ------------------------------------------------------------
        public const int GameId = 0x2C;
        public const int GameIdMaxLength = 11; // SCES-XXXXX (11 chars)

        // ------------------------------------------------------------
        // VCD PATH (ruta interna del VCD)
        // POPStarter lo almacena en offset 0x100
        // ------------------------------------------------------------
        public const int VcdPath = 0x100;
        public const int VcdPathMaxLength = 128;

        // ------------------------------------------------------------
        // TITLE (lo que OPL muestra)
        // POPStarter usa un bloque de 48 bytes en offset 0x220
        // ------------------------------------------------------------
        public const int Title = 0x220;
        public const int TitleMaxLength = 48;
    }
}
