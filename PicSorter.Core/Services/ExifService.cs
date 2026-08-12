using System;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PicSorter.Core.Models;

namespace PicSorter.Core.Services
{
    public class ExifService
    {
        public ExifInfo? ReadExifInfo(string imagePath)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                
                var exifInfo = new ExifInfo();
                
                // Find Date Taken
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (subIfdDirectory != null)
                {
                    exifInfo.DateTaken = subIfdDirectory.GetString(ExifDirectoryBase.TagDateTimeOriginal);
                }

                // Find Camera Model
                var ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (ifd0Directory != null)
                {
                    var make = ifd0Directory.GetString(ExifDirectoryBase.TagMake);
                    var model = ifd0Directory.GetString(ExifDirectoryBase.TagModel);
                    if (!string.IsNullOrWhiteSpace(make) || !string.IsNullOrWhiteSpace(model))
                    {
                        exifInfo.CameraModel = $"{make} {model}".Trim();
                    }
                }

                // Find Resolution (width and height from Exif or Jpeg)
                foreach (var directory in directories)
                {
                    if (directory.ContainsTag(ExifDirectoryBase.TagExifImageWidth) && directory.ContainsTag(ExifDirectoryBase.TagExifImageHeight))
                    {
                        var width = directory.GetString(ExifDirectoryBase.TagExifImageWidth);
                        var height = directory.GetString(ExifDirectoryBase.TagExifImageHeight);
                        exifInfo.Resolution = $"{width} x {height}";
                        break;
                    }
                    
                    // Fallback to Image Width / Height if ExifImageWidth is not present
                    if (directory.ContainsTag(ExifDirectoryBase.TagImageWidth) && directory.ContainsTag(ExifDirectoryBase.TagImageHeight))
                    {
                        var width = directory.GetString(ExifDirectoryBase.TagImageWidth);
                        var height = directory.GetString(ExifDirectoryBase.TagImageHeight);
                        exifInfo.Resolution = $"{width} x {height}";
                        break;
                    }
                }

                return exifInfo;
            }
            catch
            {
                // Return empty result safely for unsupported formats, missing EXIF, or videos
                return new ExifInfo();
            }
        }
    }
}
