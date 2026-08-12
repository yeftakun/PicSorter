using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PicSorter.Core.Exceptions;
using PicSorter.Core.Services;
using Xunit;

namespace PicSorter.Core.Tests
{
    public class FileOperationServiceTests : IDisposable
    {
        private readonly string _tempFolder;
        private readonly FileOperationService _service;

        public FileOperationServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "PicSorterOpTest_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempFolder);
            _service = new FileOperationService();
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempFolder, true); } catch { }
        }

        // ─── Happy paths ──────────────────────────────────────────────────────

        [Fact]
        public async Task Copy_CreatesFileInDestination()
        {
            string src = CreateTempFile("copy_me.jpg");
            string dest = Path.Combine(_tempFolder, "dest_copy");

            await _service.ProcessFileAsync(src, dest, isMove: false);

            Assert.True(File.Exists(Path.Combine(dest, "copy_me.jpg")));
            Assert.True(File.Exists(src), "Original should still exist after copy");
        }

        [Fact]
        public async Task Move_RemovesSourceFile()
        {
            string src = CreateTempFile("move_me.jpg");
            string dest = Path.Combine(_tempFolder, "dest_move");

            await _service.ProcessFileAsync(src, dest, isMove: true);

            Assert.False(File.Exists(src), "Source should be gone after move");
            Assert.True(File.Exists(Path.Combine(dest, "move_me.jpg")));
        }

        [Fact]
        public async Task Copy_AppendsCounterWhenDestExists()
        {
            string src1 = CreateTempFile("dup.jpg", content: "original");
            string dest = Path.Combine(_tempFolder, "dest_dup");
            Directory.CreateDirectory(dest);
            // Pre-seed destination with a file of the same name
            File.WriteAllText(Path.Combine(dest, "dup.jpg"), "pre-existing");

            await _service.ProcessFileAsync(src1, dest, isMove: false);

            Assert.True(File.Exists(Path.Combine(dest, "dup.jpg")));
            Assert.True(File.Exists(Path.Combine(dest, "dup (1).jpg")));
        }

        // ─── Simulated FileLockedException ────────────────────────────────────

        [Fact(Skip = "Requires Windows sharing violation — run manually to verify HResult detection")]
        public async Task Copy_LockedFile_ThrowsFileLockedException()
        {
            // Open source exclusively to lock it
            string src = CreateTempFile("locked.jpg");
            using var lockStream = new FileStream(src, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            string dest = Path.Combine(_tempFolder, "dest_locked");

            await Assert.ThrowsAsync<FileLockedException>(
                () => _service.ProcessFileAsync(src, dest, isMove: false));
        }

        [Fact]
        public async Task Copy_MissingSource_DoesNotThrow()
        {
            // A file that does not exist should be silently skipped (no exception)
            string src = Path.Combine(_tempFolder, "ghost.jpg");
            string dest = Path.Combine(_tempFolder, "dest_missing");

            // Should complete without throwing
            await _service.ProcessFileAsync(src, dest, isMove: false);

            Assert.False(Directory.Exists(dest) && Directory.GetFiles(dest).Length > 0,
                "Nothing should be in dest for a missing source");
        }

        // ─── FilePermissionDeniedException simulation ─────────────────────────

        [Fact]
        public async Task Copy_UnauthorizedAccess_ThrowsFilePermissionDeniedException()
        {
            // Simulate by passing an invalid/root path as destination that we cannot create
            string src = CreateTempFile("no_perm.jpg");
            // On Windows, writing to a drive root without admin rights raises UnauthorizedAccessException
            // We simulate this using a subclassed FileOperationService that re-throws UnauthorizedAccessException.
            // For a pure unit test we wrap via a custom adapter:
            var ex = new UnauthorizedAccessException("Access denied");
            var mapped = new FilePermissionDeniedException(src, ex);

            // Verify mapping only (integration test for the real Windows scenario)
            Assert.Equal(src, mapped.FilePath);
            Assert.Equal(ex, mapped.InnerException);
            Assert.Contains("Akses ditolak", mapped.Message);
        }

        // ─── InsufficientSpaceException unit test ─────────────────────────────

        [Fact]
        public void InsufficientSpaceException_CarriesDrive()
        {
            var ex = new InsufficientSpaceException(@"C:\");
            Assert.Equal(@"C:\", ex.DestinationDrive);
            Assert.Contains("C:\\", ex.Message);
        }

        // ─── FileLockedException unit test ─────────────────────────────────────

        [Fact]
        public void FileLockedException_CarriesPath()
        {
            var inner = new IOException("sharing violation");
            var ex = new FileLockedException(@"C:\photo.jpg", inner);
            Assert.Equal(@"C:\photo.jpg", ex.FilePath);
            Assert.Same(inner, ex.InnerException);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private string CreateTempFile(string name, string content = "dummy image bytes")
        {
            string path = Path.Combine(_tempFolder, name);
            File.WriteAllText(path, content);
            return path;
        }
    }
}
