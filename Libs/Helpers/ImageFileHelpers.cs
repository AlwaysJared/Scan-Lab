using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// using ImageMagick;
using Libs.Classes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace Libs.Helpers
{
    public static class ImageFileHelpers
    {
        public static async Task<SystemResponse> BmpToTiff(string bmpPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(bmpPath))
                    {
                        // Replace the file extension with .tiff
                        string tiffPath = Path.ChangeExtension(bmpPath, ".tiff");

                        using (Image image = Image.Load(bmpPath))
                        {
                            // Save the image as TIFF
                            image.Save(tiffPath, new TiffEncoder());
                        }

                        File.Delete(bmpPath);

                        return new SystemResponse { IsSuccess = true, ReturnObject = tiffPath };
                    }
                    else
                        return new SystemResponse
                        {
                            IsSuccess = false,
                            Message = "Path of .bmp file requested for .tiff conversion not found"
                        };
                }
                catch (Exception ex)
                {
                    return new SystemResponse { IsSuccess = false, Message = ex.Message };
                }
            });
        }

        public class ExifUpdateData
        {
            public string? CameraMake { get; set; }
            public string? CameraModel { get; set; }
            public string? ArtistName { get; set; }
        }

        public static async Task<SystemResponse> UpdateExifData(string filePath, ExifUpdateData exifData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Load existing image
                    using var image = Image.Load(filePath);

                    // Get or create the EXIF profile
                    var exifProfile = image.Metadata.ExifProfile ?? new ExifProfile();

                    // Modify or add EXIF tags
                    exifProfile.SetValue(ExifTag.Artist, exifData.ArtistName ?? "");
                    exifProfile.SetValue(ExifTag.Make, exifData.CameraMake ?? "");
                    exifProfile.SetValue(ExifTag.Model, exifData.CameraModel ?? "");

                    // Assign back the profile (if new)
                    image.Metadata.ExifProfile = exifProfile;

                    switch (Path.GetExtension(filePath).ToLower())
                    {
                        case ".jpeg":
                        case ".jpg":
                            image.Save(filePath, new JpegEncoder());
                            break;
                        case ".tiff":
                            image.Save(filePath, new TiffEncoder());
                            break;
                    }

                    return new SystemResponse
                            {
                                IsSuccess = true
                            };
                }
                catch (Exception ex)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                    };
                }
            });
        }
    }
}