using System.Collections.Generic;

namespace PicSorter.Core.Models
{
    public class SortState
    {
        public string SourceFolder { get; set; } = "";
        public string Mode { get; set; } = "Copy";
        public List<DestinationFolderInfo> Destinations { get; set; } = new();
        public List<SortItemState> Items { get; set; } = new();
    }
}
