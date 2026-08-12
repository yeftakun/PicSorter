using System;
using System.IO;
using System.Threading.Tasks;

namespace PicSorter.Core.Services
{
    public class FileOperationService
    {
        public Task ProcessFileAsync(string sourcePath, string destFolder, bool isMove)
        {
            return Task.Run(() =>
            {
                if (!Directory.Exists(destFolder))
                {
                    Directory.CreateDirectory(destFolder);
                }

                string destPath = Path.Combine(destFolder, Path.GetFileName(sourcePath));
                destPath = GetUniqueFilePath(destPath);

                if (File.Exists(sourcePath))
                {
                    if (isMove)
                    {
                        File.Move(sourcePath, destPath);
                    }
                    else
                    {
                        File.Copy(sourcePath, destPath);
                    }
                }
            });
        }

        public string GetUniqueFilePath(string initialPath)
        {
            if (!File.Exists(initialPath))
                return initialPath;

            string? dir = Path.GetDirectoryName(initialPath);
            if (dir == null) return initialPath;

            string name = Path.GetFileNameWithoutExtension(initialPath);
            string ext = Path.GetExtension(initialPath);

            int counter = 1;
            string newPath;
            do
            {
                newPath = Path.Combine(dir, $"{name} ({counter}){ext}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }
    }
}
