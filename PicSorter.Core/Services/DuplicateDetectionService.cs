using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Threading.Tasks;

namespace PicSorter.Core.Services
{
    public class DuplicateDetectionService
    {
        public async Task<List<List<string>>> FindDuplicatesAsync(IEnumerable<string> filePaths, IProgress<int>? progress)
        {
            var files = filePaths.ToList();
            var identicalGroups = new List<List<string>>();
            
            if (files.Count < 2) return identicalGroups;

            // 1. Group by file size to quickly skip non-duplicates
            var sizeGroups = files
                .Select(path => new { Path = path, Size = new FileInfo(path).Length })
                .GroupBy(x => x.Size)
                .Where(g => g.Count() > 1)
                .ToList();

            int totalFilesToHash = sizeGroups.Sum(g => g.Count());
            int processedCount = 0;

            // 2. Hash files with the same size
            foreach (var sizeGroup in sizeGroups)
            {
                var hashGroups = new Dictionary<ulong, List<string>>();

                foreach (var fileInfo in sizeGroup)
                {
                    try
                    {
                        ulong hash = await ComputeXxHash64Async(fileInfo.Path);

                        if (!hashGroups.ContainsKey(hash))
                        {
                            hashGroups[hash] = new List<string>();
                        }
                        hashGroups[hash].Add(fileInfo.Path);
                    }
                    catch
                    {
                        // Ignore files that cannot be read
                    }
                    
                    processedCount++;
                    progress?.Report(processedCount * 100 / (totalFilesToHash > 0 ? totalFilesToHash : 1));
                }

                // Collect identical sub-groups
                identicalGroups.AddRange(hashGroups.Values.Where(g => g.Count > 1));
            }

            return identicalGroups;
        }

        private async Task<ulong> ComputeXxHash64Async(string filePath)
        {
            const int bufferSize = 81920; // 80 KB chunks
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous);
            var hasher = new XxHash64();
            await hasher.AppendAsync(fs);
            return hasher.GetCurrentHashAsUInt64();
        }
    }
}
