using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PicSorter.Core.Services;
using Xunit;

namespace PicSorter.Core.Tests
{
    public class DuplicateDetectionServiceTests : IDisposable
    {
        private readonly string _tempFolder;

        public DuplicateDetectionServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "PicSorterDuplicateTest_" + Guid.NewGuid().ToString());
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

        [Fact]
        public async Task FindDuplicatesAsync_IdenticalFiles_Detected()
        {
            var service = new DuplicateDetectionService();
            
            string file1 = Path.Combine(_tempFolder, "file1.txt");
            string file2 = Path.Combine(_tempFolder, "file2.txt");
            string file3 = Path.Combine(_tempFolder, "file3.txt"); // Not duplicate
            
            File.WriteAllText(file1, "Hello World");
            File.WriteAllText(file2, "Hello World");
            File.WriteAllText(file3, "Different Content");

            var result = await service.FindDuplicatesAsync(new[] { file1, file2, file3 }, null);

            Assert.Single(result); // One group of duplicates
            var group = result[0];
            Assert.Equal(2, group.Count);
            Assert.Contains(file1, group);
            Assert.Contains(file2, group);
        }

        [Fact]
        public async Task FindDuplicatesAsync_DifferentSize_SkippedWithoutHashing()
        {
            var service = new DuplicateDetectionService();
            
            string file1 = Path.Combine(_tempFolder, "file1.txt");
            string file2 = Path.Combine(_tempFolder, "file2.txt");
            
            File.WriteAllText(file1, "A");
            File.WriteAllText(file2, "AB"); // Different size

            var result = await service.FindDuplicatesAsync(new[] { file1, file2 }, null);

            Assert.Empty(result);
        }

        [Fact]
        public async Task FindDuplicatesAsync_SameSizeDifferentContent_NotDetected()
        {
            var service = new DuplicateDetectionService();
            
            string file1 = Path.Combine(_tempFolder, "file1.txt");
            string file2 = Path.Combine(_tempFolder, "file2.txt");
            
            File.WriteAllText(file1, "AB");
            File.WriteAllText(file2, "CD"); // Same size, different content

            var result = await service.FindDuplicatesAsync(new[] { file1, file2 }, null);

            Assert.Empty(result);
        }
    }
}
