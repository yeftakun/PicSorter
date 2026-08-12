using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PicSorter.Core.Services
{
    public class FileScanService
    {
        private static readonly string[] ImageExt = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".jfif" };
        private static readonly string[] VideoExt = { ".mp4", ".mov", ".avi", ".mkv", ".wmv" };

        public async IAsyncEnumerable<string> ScanFolderAsync(
            string root,
            bool recursive,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            // Poin 1 PLAN.md: Pattern async cross-platform (IAsyncEnumerable)
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = recursive
            };

            // Using Task.Run for directory traversal to keep the thread non-blocking
            var files = await Task.Run(() => Directory.GetFiles(root, "*.*", options), ct);

            var sortedFiles = files.Where(f =>
            {
                string ext = Path.GetExtension(f).ToLower();
                return ImageExt.Contains(ext) || VideoExt.Contains(ext);
            }).OrderBy(f => f).ToList();

            foreach (var file in sortedFiles)
            {
                ct.ThrowIfCancellationRequested();
                yield return file;
            }
        }

        public bool IsVideo(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return VideoExt.Contains(ext);
        }
    }
}
