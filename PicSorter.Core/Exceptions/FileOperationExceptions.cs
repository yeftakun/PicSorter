using System;

namespace PicSorter.Core.Exceptions
{
    /// <summary>
    /// Raised when a file cannot be accessed because it is locked by another process.
    /// </summary>
    public class FileLockedException : IOException
    {
        public string FilePath { get; }

        public FileLockedException(string filePath, Exception? inner = null)
            : base($"File sedang digunakan proses lain: {filePath}", inner)
        {
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Raised when a copy/move operation fails because the target disk has insufficient free space.
    /// </summary>
    public class InsufficientSpaceException : IOException
    {
        public string DestinationDrive { get; }

        public InsufficientSpaceException(string destinationDrive, Exception? inner = null)
            : base($"Ruang disk tidak cukup di {destinationDrive}", inner)
        {
            DestinationDrive = destinationDrive;
        }
    }

    /// <summary>
    /// Raised when the process lacks permission to read the source or write to the destination.
    /// </summary>
    public class FilePermissionDeniedException : UnauthorizedAccessException
    {
        public string FilePath { get; }

        public FilePermissionDeniedException(string filePath, Exception? inner = null)
            : base($"Akses ditolak: {filePath}", inner)
        {
            FilePath = filePath;
        }
    }
}
