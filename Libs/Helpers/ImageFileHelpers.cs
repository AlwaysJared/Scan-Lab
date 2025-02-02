using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tiff;

namespace Libs.Helpers
{
    public static class ImageFileHelpers
    {
        public static void BmpToTiff(string bmpPath)
        {
            try
            {
                // Replace the file extension with .tiff
                string tiffPath = Path.ChangeExtension(bmpPath, ".tiff");

                using (Image image = Image.Load(bmpPath))
                {
                    // Save the image as TIFF
                    image.Save(tiffPath, new TiffEncoder());
                }
            }
            catch (Exception ex)
            {
                //maybe add error log logic to system (error log )
            }
        }
    }
}