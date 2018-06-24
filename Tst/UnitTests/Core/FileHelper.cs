using System.IO;

namespace UnitTests.Core
{
    public class FileHelper
    {
        public static void DeepCopy(DirectoryInfo src, string target)
        {
            Directory.CreateDirectory(target);
            CopyFiles(src, target);
            foreach (var dir in src.GetDirectories())
            {
                DeepCopy(dir, Path.Combine(target, dir.Name));
            }
        }

        public static void CopyFiles(DirectoryInfo src, string target)
        {
            foreach (var file in src.GetFiles())
            {
                File.Copy(file.FullName, Path.Combine(target, file.Name), true);
            }
        }
    }
}