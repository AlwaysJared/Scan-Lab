using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Enums;
using Libs.Helpers;
using Libs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Libs.Repositories
{
    public class RollRepository : IRollRepository, IDisposable
    {

        private ScanLabContext context;
        public RollRepository(ScanLabContext context)
        {
            this.context = context;
        }

        public async Task<SystemResponse> ProcessRoll(Guid rollId)
        {
            try
            {
                var roll = await context.Rolls
                    .Include(r => r.Order)
                    .Include(r => r.Order.Scanner)
                    .Where(r => r.RollId == rollId)
                    .FirstOrDefaultAsync();

                if (roll == null)
                    return new SystemResponse() { IsSuccess = false, Message = "Roll not found" };

                if (roll.Order == null)
                    return new SystemResponse() { IsSuccess = false, Message = "Order not found" };

                if (roll.Order.Scanner == null)
                    return new SystemResponse() { IsSuccess = false, Message = "Order not associated with a scanner" };

                if (!Directory.Exists(roll.Order.Scanner.WatchedDir))
                    return new SystemResponse() { IsSuccess = false, Message = "Scanner's export directory not found" };

                if (!Directory.Exists(roll.Order.Scanner.DestinationDir))
                    return new SystemResponse() { IsSuccess = false, Message = "Scanner's destination directory not found" };

                
                //Check that roll requesting processing is part of an order in progress
                

                
                // Define the common image file extensions
                string[] imageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp"];

                var rollDirsSorted = Directory.GetDirectories(roll.Order.Scanner.WatchedDir).Select(dir => new
                {
                    Path = dir,
                    CreationDate = Directory.GetCreationTime(dir)
                })
                .OrderBy(dir => dir.CreationDate) // Sort by creation date
                .ToList();

                var latestRollDir = rollDirsSorted.Select(dir => dir.Path).ToList()[0];

                string rollFolderPath = Path.Combine(roll.Order.Scanner.DestinationDir, roll.Order.OrderId, roll.RollNumber.ToString());

                if (!Directory.Exists(rollFolderPath))
                {
                    Directory.CreateDirectory(rollFolderPath);
                }

                // Get all files in the directory
                string[] files = Directory.GetFiles(latestRollDir);

                var imgCount = 1;
                // Iterate through the files and check for image extensions
                foreach (var file in files)
                {
                    string extension = Path.GetExtension(file).ToLower();

                    // Check if the file is an image based on extension
                    if (Array.Exists(imageExtensions, ext => ext.Equals(extension)))
                    {
                        string fileName = Path.GetFileName(file);
                        
                        if (extension.ToLower() == ".bmp")
                        {
                            ImageFileHelpers.BmpToTiff(file);
                            // Replace the file extension with .tiff
                            fileName = Path.ChangeExtension(fileName, ".tiff");
                        }

                        string newFileName = $"{roll.Order.OrderId}-{roll.RollNumber}-{imgCount}" + extension;

                        string newFilePath = Path.Combine(rollFolderPath, newFileName);

                        // Check if the new file name already exists
                        if (File.Exists(newFilePath))
                        {
                            continue;
                        }

                        // Rename the file
                        File.Move(file, newFilePath);

                        imgCount++;
                    }

                }
                Directory.Delete(latestRollDir);
                

                return new SystemResponse() { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse() { IsSuccess = false, Message = ex.Message };
            }
        }
        
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        public void Save()
        {
            throw new NotImplementedException();
        }

        public async Task<List<Roll>?> RollsInProgress(Roll roll)
        {
            try
            {
                var rollInProgress = context.Rolls
                    .Where(r => r.Status == RollStatus.ScanningInProgress)
                    .ToList();

                if (rollInProgress.Count == 0)
                    return null;

                return rollInProgress;
            }
            catch (Exception ex)
            {
                
                return null;
            }
        }
    }
}