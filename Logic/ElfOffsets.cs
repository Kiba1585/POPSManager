namespace POPSManager.Logic
{
    public static class ElfOffsets
    {
        // ------------------------------------------------------------
        // GAME ID (SCES_XXXXX)
        // POPStarter lo almacena en offset 0x2C
        // Longitud real: 10 bytes (4 letras + '_' + 5 números)
        // ------------------------------------------------------------
        public const int GameId = 0x2C;
        public const int GameIdMaxLength = 10;

        // ------------------------------------------------------------
        // VCD PATH (ruta interna del VCD)
        // POPStarter lo almacena en offset 0x100
        // Longitud real: 128 bytes
        // ------------------------------------------------------------
        public const int VcdPath = 0x100;
        public const int VcdPathMaxLength = 128;

        // ------------------------------------------------------------
        // TITLE (lo que OPL muestra)
        // POPStarter usa un bloque de 48 bytes en offset 0x220
        // 47 caracteres + 0x00 final
        // ------------------------------------------------------------
        public const int Title = 0x220;
        public const int TitleMaxLength = 48;

        // ------------------------------------------------------------
        // Tamaño mínimo del ELF base para validar integridad
        // POPSTARTER.ELF real mide ~700 KB, pero 64 KB es seguro
        // ------------------------------------------------------------
        public const int MinimumElfSize = 0x10000; // 64 KB
    }
}
