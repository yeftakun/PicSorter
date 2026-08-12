using System.Collections.Generic;

namespace PicSorter.Core.Models
{
    public class AppSettings
    {
        public List<DestinationFolderInfo> FavoriteDestinations { get; set; } = new();
        public string LastUsedMode { get; set; } = "Copy";
        public double WindowWidth { get; set; } = 1100;
        public double WindowHeight { get; set; } = 740;
        public double WindowTop { get; set; } = double.NaN;
        public double WindowLeft { get; set; } = double.NaN;
        public string ThemePreference { get; set; } = "Auto"; // Light, Dark, Auto
    }
}
