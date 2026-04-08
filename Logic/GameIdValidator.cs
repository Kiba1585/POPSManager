namespace POPSManager.Logic
{
    public static class GameIdValidator
    {
        public static bool IsPs1(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            return id.StartsWith("SCES") ||
                   id.StartsWith("SLES") ||
                   id.StartsWith("SCUS") ||
                   id.StartsWith("SLUS") ||
                   id.StartsWith("SLPS") ||
                   id.StartsWith("SLPM") ||
                   id.StartsWith("SCPS");
        }

        public static bool IsPs2(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            return id.StartsWith("SLES") ||
                   id.StartsWith("SLUS") ||
                   id.StartsWith("SCUS") ||
                   id.StartsWith("SCES") ||
                   id.StartsWith("SLPM") ||
                   id.StartsWith("SLPS");
        }
    }
}
