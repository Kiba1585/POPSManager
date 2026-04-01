using System.IO;

namespace POPSManager.Logic
{
    public static class IntegrityValidator
    {
        public static bool Validate(string path)
        {
            var info = new FileInfo(path);

            if (info.Length < 100_000)
                return false;

            return true;
        }
    }
}
