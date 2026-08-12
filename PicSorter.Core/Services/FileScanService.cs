using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PicSorter.Core.Services
{
    public enum SortCriteria { Name, DateModified, DateCreated, FileSize }

    public class FileScanService
    {
        private static readonly string[] ImageExt = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".jfif" };
        private static readonly string[] VideoExt = { ".mp4", ".mov", ".avi", ".mkv", ".wmv" };

        public async IAsyncEnumerable<PicSorter.Core.Models.ScannedFile> ScanFolderAsync(
            string root,
            bool recursive,
            SortCriteria sortCriteria,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = recursive
            };

            var files = await Task.Run(() => Directory.GetFiles(root, "*.*", options), ct);

            var filteredFiles = files.Where(f =>
            {
                string ext = Path.GetExtension(f).ToLower();
                return ImageExt.Contains(ext) || VideoExt.Contains(ext);
            });

            IEnumerable<string> sortedFiles = sortCriteria switch
            {
                SortCriteria.DateModified => filteredFiles.OrderBy(f => File.GetLastWriteTime(f)),
                SortCriteria.DateCreated => filteredFiles.OrderBy(f => File.GetCreationTime(f)),
                SortCriteria.FileSize => filteredFiles.OrderBy(f => new FileInfo(f).Length),
                _ => filteredFiles.OrderBy(f => f)
            };

            var resultFiles = sortedFiles.ToList();

            foreach (var file in resultFiles)
            {
                ct.ThrowIfCancellationRequested();
                yield return new PicSorter.Core.Models.ScannedFile
                {
                    FullPath = file,
                    RelativePath = Path.GetRelativePath(root, file)
                };
            }
        }

        public bool IsVideo(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return VideoExt.Contains(ext);
        }
    }
}
