namespace POPSManager.Logic
{
    public static class ElfOffsets
    {
        // ------------------------------------------------------------
        //  GAME ID (SCES-XXXXX)
        //  Ubicado en el header del ELF, siempre en offset 0x2C
        // ------------------------------------------------------------
        public const int GameId = 0x2C;

        // ------------------------------------------------------------
        //  VCD PATH (ruta interna que POPStarter usa)
        //  Siempre en offset 0x100 (256)
        // ------------------------------------------------------------
        public const int VcdPath = 0x100;

        // ------------------------------------------------------------
        //  TITLE (lo que OPL muestra)
        //  POPStarter usa un bloque de 48 bytes en offset 0x220
        //  Compatible con r13, r14 y builds modernas
        // ------------------------------------------------------------
        public const int Title = 0x220;
        public const int TitleMaxLength = 48;

        // ------------------------------------------------------------
        //  MULTIDISC FLAG
        //  POPStarter usa 0x01 para multidisco
        //  Offset estándar: 0x1F0
        // ------------------------------------------------------------
        public const int MultiDiscFlag = 0x1F0;

        // ------------------------------------------------------------
        //  DISC NUMBER (1, 2, 3...)
        //  Offset estándar: 0x1F1
        // ------------------------------------------------------------
        public const int DiscNumber = 0x1F1;

        // ------------------------------------------------------------
        //  REGION (NTSC-U, NTSC-J, PAL)
        //  Offset estándar: 0x1F2
        // ------------------------------------------------------------
        public const int Region = 0x1F2;

        // ------------------------------------------------------------
        //  BOOT MODE (normal, fastboot, debug)
        //  Offset estándar: 0x1F3
        // ------------------------------------------------------------
        public const int BootMode = 0x1F3;
    }
}
