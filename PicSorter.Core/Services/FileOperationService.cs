using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PicSorter.Core.Exceptions;

namespace PicSorter.Core.Services
{
    public class FileOperationService
    {
        private readonly ILogger<FileOperationService> _logger;

        public FileOperationService()
        {
            _logger = AppLogger.Factory.CreateLogger<FileOperationService>();
        }

        public Task ProcessFileAsync(string sourcePath, string destFolder, bool isMove)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("{Op} {Source} → {Dest}", isMove ? "MOVE" : "COPY", sourcePath, destFolder);

                try
                {
                    if (!Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }

                    string destPath = Path.Combine(destFolder, Path.GetFileName(sourcePath));
                    destPath = GetUniqueFilePath(destPath);

                    if (!File.Exists(sourcePath))
                    {
                        _logger.LogWarning("Source file not found, skipping: {Source}", sourcePath);
                        return;
                    }

                    if (isMove)
                        File.Move(sourcePath, destPath);
                    else
                        File.Copy(sourcePath, destPath);

                    _logger.LogInformation("OK → {Dest}", destPath);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "Permission denied for {Path}", sourcePath);
                    throw new FilePermissionDeniedException(sourcePath, ex);
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    _logger.LogError(ex, "File locked: {Path}", sourcePath);
                    throw new FileLockedException(sourcePath, ex);
                }
                catch (IOException ex) when (IsDiskFull(ex))
                {
                    string drive = Path.GetPathRoot(destFolder) ?? destFolder;
                    _logger.LogError(ex, "Disk full on {Drive}", drive);
                    throw new InsufficientSpaceException(drive, ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing {Path}", sourcePath);
                    throw; // re-throw unclassified exceptions as-is
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

        // ─── Helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Checks if an IOException is "file is being used by another process" (HResult 0x80070020 / ERROR_SHARING_VIOLATION).
        /// </summary>
        private static bool IsFileLocked(IOException ex)
        {
            const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
            const int ERROR_LOCK_VIOLATION    = unchecked((int)0x80070021);
            int hresult = ex.HResult;
            return hresult == ERROR_SHARING_VIOLATION || hresult == ERROR_LOCK_VIOLATION;
        }

        /// <summary>
        /// Checks if an IOException is "disk full" (HResult 0x80070027 / ERROR_DISK_FULL or 0x80070070).
        /// </summary>
        private static bool IsDiskFull(IOException ex)
        {
            const int ERROR_DISK_FULL          = unchecked((int)0x80070027);
            const int ERROR_HANDLE_DISK_FULL   = unchecked((int)0x80070070);
            int hresult = ex.HResult;
            return hresult == ERROR_DISK_FULL || hresult == ERROR_HANDLE_DISK_FULL;
        }
    }
}
