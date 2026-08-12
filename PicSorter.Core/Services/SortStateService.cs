using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PicSorter.Core.Models;

namespace PicSorter.Core.Services
{
    public class SortStateService
    {
        public async Task SaveStateAsync(string filePath, SortState state)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(state, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<SortState?> LoadStateAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<SortState>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
