namespace PicSorter.Core.Models
{
    public class SortItemState
    {
        public string SourcePath { get; set; } = "";
        public bool IsVideo { get; set; }
        public bool Sorted { get; set; } = false;
        public string? DestFolderPath { get; set; }
        public string? LastAction { get; set; }
        public bool Committed { get; set; } = false;
    }
}
