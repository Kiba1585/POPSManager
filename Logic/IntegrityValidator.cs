using System.IO;

namespace POPSManager.Logic
{
    public static class IntegrityValidator
    {
        public static bool Validate(string vcdPath)
        {
            var info = new FileInfo(vcdPath);

            if (info.Length < 1_000_000)
                return false;

            return true;
        }
    }
}
