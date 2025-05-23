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

        public async Task<Roll?> GetRoll(Guid rollId)
        {
            try
            {
                return await context.Rolls
                    .Include(r => r.Order)
                    .Include(r => r.Order.Scanner)
                    .FirstOrDefaultAsync(r => r.RollId == rollId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<SystemResponse> AddRoll(string orderId, int rollNumber)
        {
            try
            {
                var dbOrder = await context.Orders.Where(o => o.OrderId == orderId).FirstOrDefaultAsync();

                if (dbOrder == null)
                    return new SystemResponse { IsSuccess = true, Message = $"Order ID '{orderId}' not found" };

                var dupRoll = await context.Rolls.Where(r => r.RollNumber == rollNumber).FirstOrDefaultAsync();

                if (dupRoll != null)
                    return new SystemResponse { IsSuccess = false, Message = $"roll #{rollNumber} already exists" };

                var newRoll = new Roll
                {
                    RollId = Guid.NewGuid(),
                    RollNumber = rollNumber,
                    OrderId = dbOrder.OrderId,
                    Order = dbOrder,
                };

                context.Rolls.Add(newRoll);
                await context.SaveChangesAsync();

                return new SystemResponse { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse { IsSuccess = false, Message = ex.Message};
            }
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
                    return new SystemResponse() { IsSuccess = false, Message = $"Scanner's export directory '{roll.Order.Scanner.WatchedDir}' not found" };

                if (!Directory.Exists(roll.Order.Scanner.DestinationDir))
                    return new SystemResponse() { IsSuccess = false, Message = "Scanner's destination directory not found" };

                if (Directory.GetDirectories(roll.Order.Scanner.WatchedDir).ToList().Count == 0)
                    return new SystemResponse() { IsSuccess = false, Message = "No rolls found in scanner's watched directory" };

                // Attempt to update roll's status to 'in progress'
                var statusResp = await UpdateRollStatus(roll, RollStatus.Processing);

                if (!statusResp.IsSuccess)
                    return new SystemResponse() { IsSuccess = false, Message = statusResp.Message };

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

                bool destFolderExists = false;

                if (!Directory.Exists(rollFolderPath))
                {
                    Directory.CreateDirectory(rollFolderPath);
                }
                else
                {
                    destFolderExists = true;
                    rollFolderPath = Path.Combine(roll.Order.Scanner.DestinationDir,
                        roll.Order.OrderId,
                        "Rescans",
                        roll.RollNumber.ToString() + " @ " + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")
                    );
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
                        string fileDir = Path.GetDirectoryName(file) ?? "";

                        #region Image File Conversions & EXIF data setting
                        // If file is a .bmp file, convert it to a tiff image
                        if (extension.ToLower() == ".bmp")
                        {
                            var tiffConversionResp = await ImageFileHelpers.BmpToTiff(file);

                            if (tiffConversionResp.IsSuccess)
                                // fileName = tiffConversionResp.ReturnObject?.ToString() ?? fileName;
                                fileName = Path.ChangeExtension(fileName, ".tiff");
                            else
                            {
                                await UpdateRollStatus(roll, RollStatus.Processing);

                                return new SystemResponse { IsSuccess = false, Message = tiffConversionResp.Message };
                            }
                        }

                        // Add relevant scanner data to image exif data
                        var exifData = new ImageFileHelpers.ExifUpdateData
                        {
                            ArtistName = roll.Order.Scanner.ArtistName,
                            CameraMake = roll.Order.Scanner.Make,
                            CameraModel = roll.Order.Scanner.Model,
                        };

                        var exifUpdate = await ImageFileHelpers.UpdateExifData(Path.Combine(fileDir, fileName), exifData);

                        if (!exifUpdate.IsSuccess)
                            return new SystemResponse
                            {
                                IsSuccess = false,
                                Message = $"Error updating exif data. [ERROR]: {exifUpdate.Message}"
                            };
                        #endregion


                        // string newFileName = $"{roll.Order.CustomerInitials}-{roll.Order.OrderId}-{roll.RollNumber}-{imgCount}" + extension;
                        string newFileName = $"{roll.Order.CustomerInitials}-{roll.Order.OrderId}-{roll.RollNumber}-{imgCount}" + Path.GetExtension(fileName);

                        string newFilePath = Path.Combine(rollFolderPath, newFileName);

                        // Check if the new file name already exists
                        if (File.Exists(newFilePath))
                        {
                            continue;
                        }

                        // Move/Rename the file 
                        // (awaiting to avoid Directory.Delete conflict)
                        // File.Move(file, newFilePath);
                        await IOHelpers.MoveFileAsync(Path.Combine(fileDir, fileName), newFilePath);

                        imgCount++;
                    }
                    else
                    {
                        return new SystemResponse
                        {
                            IsSuccess = false,
                            Message = $"File '{file}' not recognized as a valid image file type"
                        };
                    }

                }

                // Delete the roll folder
                // awaited to ensure proper execution
                // Directory.Delete(latestRollDir);
                await IOHelpers.DeleteDirAsync(latestRollDir);

                statusResp = await UpdateRollStatus(roll, RollStatus.Processed);

                if (!statusResp.IsSuccess)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Error marking roll ID: {roll.RollId} as 'processed'. \n[ERROR]: {statusResp.Message}"
                    };

                return new SystemResponse() { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse() { IsSuccess = false, Message = ex.Message };
            }
        }
        public void Dispose()
        {
            context.Dispose();
        }
        public void Save()
        {
            throw new NotImplementedException();
        }
        public async Task<List<Roll>> RollsInProgress(Scanner? scnr = null)
        {
            try
            {
                if (scnr != null)
                {
                    return await context.Rolls
                    .Include(r => r.Order)
                    .Include(r => r.Order.Scanner)
                    .Where(r => r.Status == RollStatus.ScanningInProgress && r.Order.Scanner.Id == scnr.Id)
                    .ToListAsync() ?? new List<Roll>();
                }

                return await context.Rolls
                    .Include(r => r.Order)
                    .Include(r => r.Order.Scanner)
                    .Where(r => r.Status == RollStatus.ScanningInProgress)
                    .ToListAsync() ?? new List<Roll>();
            }
            catch (Exception ex)
            {
                return new List<Roll>();
            }
        }
        public async Task<SystemResponse> UpdateRollStatus(Roll roll, RollStatus status)
        {
            try
            {
                var dbRoll = await context.Rolls
                .Include(r => r.Order)
                .Include(r => r.Order.Scanner)
                .FirstOrDefaultAsync(r => r.RollId == roll.RollId);

                if (dbRoll == null)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Roll ID:{roll.RollId} not found"
                    };

                if (dbRoll.Status == status && status != RollStatus.Created && status != RollStatus.Processing)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Roll's current status already set to requested status"
                    };
                }

                if (status == RollStatus.ScanningInProgress)
                {
                    if (dbRoll.Order == null)
                        return new SystemResponse
                        {
                            IsSuccess = false,
                            Message = $"No order associated with roll ID: {dbRoll.RollId}. Assign roll to order to initiate scanning progress"
                        };

                    if (dbRoll.Order.Scanner == null)
                        return new SystemResponse
                        {
                            IsSuccess = false,
                            Message = $"No scanner associated with order ID: '{dbRoll.Order.OrderId}'. Assign order to scanner to initiate scanning progress"
                        };

                    var scnrInProgressRolls = await RollsInProgress(dbRoll.Order.Scanner);

                    if (scnrInProgressRolls.Any())
                        if (!scnrInProgressRolls.Where(r => r.RollId == dbRoll.RollId).Any())
                            return new SystemResponse
                            {
                                IsSuccess = false,
                                Message = "Other roll(s) already in progress. Complete or suspend roll(s) in progress to initiate progress of this roll."
                            };

                    dbRoll.Order.Status = OrderStatus.Processing;
                }

                if (status == RollStatus.ScanningPaused)
                {
                    dbRoll.Order.Status = OrderStatus.Created;
                }

                dbRoll.Status = status;
                dbRoll.DateUpdated = DateTime.Now;

                await context.SaveChangesAsync();

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
                    Message = ex.Message
                };
            }
        }
        public SystemResponse AllRollsProcessed(Order order)
        {
            try
            {
                var unprocessedRolls = order.Rolls.Where(r => r.Status < RollStatus.Processed).ToList();

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = !unprocessedRolls.Any()
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
        }
        public async Task<SystemResponse> DeleteRoll(Guid rollId)
        {
            try
            {
                var dbRoll = await context.Rolls.Where(r => r.RollId == rollId).FirstOrDefaultAsync();

                if (dbRoll == null)
                {
                    return new SystemResponse { IsSuccess = false, Message = $"Roll not found" };
                }

                context.Rolls.Remove(dbRoll);

                await context.SaveChangesAsync();

                return new SystemResponse { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse { IsSuccess = false, Message = ex.Message };
            }
        }
    }
}