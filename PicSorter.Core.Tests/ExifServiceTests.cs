using System;
using System.IO;
using PicSorter.Core.Services;
using Xunit;

namespace PicSorter.Core.Tests
{
    public class ExifServiceTests : IDisposable
    {
        private readonly string _tempFolder;

        public ExifServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "PicSorterExifTest_" + Guid.NewGuid().ToString());
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
        public void ReadExifInfo_InvalidFile_DoesNotThrow_ReturnsEmptyExif()
        {
            // Arrange
            var service = new ExifService();
            string dummyFile = Path.Combine(_tempFolder, "dummy.jpg");
            File.WriteAllText(dummyFile, "This is not a real image");

            // Act
            var result = service.ReadExifInfo(dummyFile);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.DateTaken);
            Assert.Null(result.CameraModel);
            Assert.Null(result.Resolution);
        }
        
        [Fact]
        public void ReadExifInfo_NonExistentFile_DoesNotThrow_ReturnsEmptyExif()
        {
            // Arrange
            var service = new ExifService();
            string missingFile = Path.Combine(_tempFolder, "missing.jpg");

            // Act
            var result = service.ReadExifInfo(missingFile);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.DateTaken);
            Assert.Null(result.CameraModel);
            Assert.Null(result.Resolution);
        }
    }
}
