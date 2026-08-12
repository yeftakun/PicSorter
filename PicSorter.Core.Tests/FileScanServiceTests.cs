using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PicSorter.Core.Models;
using PicSorter.Core.Services;
using Xunit;

namespace PicSorter.Core.Tests
{
    public class FileScanServiceTests : IDisposable
    {
        private readonly string _tempFolder;

        public FileScanServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "PicSorterScanTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempFolder);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempFolder))
            {
                try
                {
                    Directory.Delete(_tempFolder, true);
                }
                catch { }
            }
        }

        private async Task<List<ScannedFile>> ScanAsync(FileScanService service, bool recursive, SortCriteria criteria)
        {
            var results = new List<ScannedFile>();
            await foreach (var file in service.ScanFolderAsync(_tempFolder, recursive, criteria))
            {
                results.Add(file);
            }
            return results;
        }

        [Fact]
        public async Task ScanFolderAsync_RecursiveVsFlat()
        {
            // Arrange
            var service = new FileScanService();
            string rootFile = Path.Combine(_tempFolder, "root.jpg");
            string subFolder = Path.Combine(_tempFolder, "Sub");
            Directory.CreateDirectory(subFolder);
            string subFile = Path.Combine(subFolder, "sub.png");

            File.WriteAllText(rootFile, "dummy");
            File.WriteAllText(subFile, "dummy");

            // Act & Assert Flat
            var flatFiles = await ScanAsync(service, false, SortCriteria.Name);
            Assert.Single(flatFiles);
            Assert.Equal(rootFile, flatFiles[0].FullPath);
            Assert.Equal("root.jpg", flatFiles[0].RelativePath);

            // Act & Assert Recursive
            var recursiveFiles = await ScanAsync(service, true, SortCriteria.Name);
            Assert.Equal(2, recursiveFiles.Count);
            // By name: root.jpg and sub.png. In sorting by path, usually root.jpg vs Sub\sub.png
            // Depending on absolute paths, let's just check they both exist
            Assert.Contains(recursiveFiles, f => f.FullPath == rootFile);
            Assert.Contains(recursiveFiles, f => f.FullPath == subFile);
            Assert.Contains(recursiveFiles, f => f.RelativePath == "root.jpg");
            Assert.Contains(recursiveFiles, f => f.RelativePath == Path.Combine("Sub", "sub.png"));
        }

        [Fact]
        public async Task ScanFolderAsync_Sorting()
        {
            // Arrange
            var service = new FileScanService();
            
            string fileA = Path.Combine(_tempFolder, "A.jpg"); // Size 3, Created later, Modified earliest
            string fileB = Path.Combine(_tempFolder, "B.jpg"); // Size 1, Created earliest, Modified latest
            string fileC = Path.Combine(_tempFolder, "C.jpg"); // Size 2, Created middle, Modified middle

            File.WriteAllBytes(fileA, new byte[3]);
            File.WriteAllBytes(fileB, new byte[1]);
            File.WriteAllBytes(fileC, new byte[2]);

            DateTime now = DateTime.Now;

            File.SetCreationTime(fileA, now.AddDays(2));
            File.SetCreationTime(fileB, now.AddDays(0));
            File.SetCreationTime(fileC, now.AddDays(1));

            File.SetLastWriteTime(fileA, now.AddDays(0));
            File.SetLastWriteTime(fileB, now.AddDays(2));
            File.SetLastWriteTime(fileC, now.AddDays(1));

            // Act & Assert Name
            var byName = await ScanAsync(service, false, SortCriteria.Name);
            Assert.Equal(new[] { "A.jpg", "B.jpg", "C.jpg" }, byName.Select(f => f.RelativePath));

            // Act & Assert FileSize (1, 2, 3 -> B, C, A)
            var bySize = await ScanAsync(service, false, SortCriteria.FileSize);
            Assert.Equal(new[] { "B.jpg", "C.jpg", "A.jpg" }, bySize.Select(f => f.RelativePath));

            // Act & Assert DateCreated (0, 1, 2 -> B, C, A)
            var byCreated = await ScanAsync(service, false, SortCriteria.DateCreated);
            Assert.Equal(new[] { "B.jpg", "C.jpg", "A.jpg" }, byCreated.Select(f => f.RelativePath));

            // Act & Assert DateModified (0, 1, 2 -> A, C, B)
            var byModified = await ScanAsync(service, false, SortCriteria.DateModified);
            Assert.Equal(new[] { "A.jpg", "C.jpg", "B.jpg" }, byModified.Select(f => f.RelativePath));
        }
    }
}
