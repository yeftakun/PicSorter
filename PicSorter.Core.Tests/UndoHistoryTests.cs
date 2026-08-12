using System;
using System.IO;
using System.Threading.Tasks;
using PicSorter.Core.Models;
using PicSorter.Core.ViewModels;
using Xunit;

namespace PicSorter.Core.Tests
{
    public class UndoHistoryTests : IDisposable
    {
        private readonly string _tempFolder;

        public UndoHistoryTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "PicSorterTest_" + Guid.NewGuid().ToString());
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
        public async Task TestFullUndoHistory()
        {
            // Arrange: Buat 3 dummy files
            string file1 = Path.Combine(_tempFolder, "A.jpg");
            string file2 = Path.Combine(_tempFolder, "B.jpg");
            string file3 = Path.Combine(_tempFolder, "C.jpg");
            File.WriteAllText(file1, "dummy");
            File.WriteAllText(file2, "dummy");
            File.WriteAllText(file3, "dummy");

            string destFolder = Path.Combine(_tempFolder, "Dest");
            Directory.CreateDirectory(destFolder);

            var vm = new MainViewModel();
            vm.ShowMessage = msg => { }; // ignore messages

            vm.SourceFolder = _tempFolder;
            vm.Destinations.Add(new DestinationFolderInfo { Shortcut = "1", FolderPath = destFolder });

            // Act: Mulai sorting
            await vm.StartSortingCommand.ExecuteAsync(null);

            // Assert: Harus di file A
            Assert.Equal("File: A.jpg", vm.FileName);

            // Assign A
            await vm.TryGetCommandForKey("1");
            Assert.Equal("File: B.jpg", vm.FileName);
            Assert.Equal(1, vm.ProgressValue);

            // Assign B
            await vm.TryGetCommandForKey("1");
            Assert.Equal("File: C.jpg", vm.FileName);
            Assert.Equal(2, vm.ProgressValue);

            // Assign C
            await vm.TryGetCommandForKey("1");
            Assert.Equal("File: -", vm.FileName); // Habis
            Assert.Equal(3, vm.ProgressValue);

            // Undo C
            await vm.TryGetCommandForKey("Back");
            Assert.Equal("File: C.jpg", vm.FileName);
            Assert.Equal(2, vm.ProgressValue);

            // Undo B
            await vm.TryGetCommandForKey("Back");
            Assert.Equal("File: B.jpg", vm.FileName);
            Assert.Equal(1, vm.ProgressValue);

            // Undo A
            await vm.TryGetCommandForKey("Back");
            Assert.Equal("File: A.jpg", vm.FileName);
            Assert.Equal(0, vm.ProgressValue);

            // Coba undo lagi saat kosong (seharusnya aman)
            await vm.TryGetCommandForKey("Back");
            Assert.Equal("File: A.jpg", vm.FileName);
            Assert.Equal(0, vm.ProgressValue);
        }
    }
}
