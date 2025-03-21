using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Enums;
using Libs.Helpers;
using Libs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Libs.Repositories
{
    public class OrderRepository : IOrderRepository, IDisposable
    {
        private ScanLabContext context;
        public OrderRepository(ScanLabContext context)
        {
            this.context = context;
        }
        public async Task<SystemResponse> AddOrder(Order req)
        {
            try
            {
                var scnr = context.Scanners.FirstOrDefault(sc => sc.Id == req.Scanner.Id);

                if (scnr == null)
                    return new SystemResponse { IsSuccess = false, Message = "Scanner not found" };

                var newOrder = new Order
                {
                    OrderId = req.OrderId,
                    Rolls = req.Rolls,
                    Scanner = scnr,
                };
                var res = context.Orders.Add(newOrder);
                await context.SaveChangesAsync();
                return new SystemResponse()
                {
                    IsSuccess = true,
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

        public void Dispose()
        {
            context.Dispose();
        }

        public Order GetOrder(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Order>> GetOrders(string? search, OrderStatus? status)
        {
            var orders = await context.Orders
            .Include(o => o.Rolls)
            .Where(o => !String.IsNullOrWhiteSpace(search) ? (
                    o.OrderId.ToLower().Contains(search.ToLower())
                    || o.Rolls.Select(r => r.RollNumber.ToString()).ToArray().Contains(search)
                ) : true
            )
            .Where(o => status.HasValue ? (o.Status == status) : true)
            .ToListAsync();
            return orders;
        }

        public async Task<SystemResponse> ProcessOrder(string id)
        {
            var order = context.Orders
                .Include(o => o.Rolls)
                .Include(o => o.Scanner)
                .Where(o => o.OrderId.ToLower() == id.ToLower())
                .FirstOrDefault();


            if (order == null)
                return new SystemResponse() { IsSuccess = false, Message = "Order not found" };

            if (order.Rolls.Count == 0)
                return new SystemResponse() { IsSuccess = false, Message = "Order does not have any rolls associated with it." };

            if (order.Scanner == null)
                return new SystemResponse() { IsSuccess = false, Message = "Order not associated with a scanner" };

            if (!Directory.Exists(order.Scanner.WatchedDir))
                return new SystemResponse() { IsSuccess = false, Message = "Scanner's export directory not found" };

            if (!Directory.Exists(order.Scanner.DestinationDir))
                return new SystemResponse() { IsSuccess = false, Message = "Scanner's destination directory not found" };

            // Define the common image file extensions
            string[] imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };

            var rollDirsSorted = Directory.GetDirectories(order.Scanner.WatchedDir).Select(dir => new
            {
                Path = dir,
                CreationDate = Directory.GetCreationTime(dir)
            })
                .OrderBy(dir => dir.CreationDate) // Sort by creation date
                .ToList();
            var rollDirs = rollDirsSorted.Select(dir => dir.Path).ToList();

            var rollIndex = 0;
            foreach (var roll in rollDirs)
            {
                // Get all files in the directory
                string[] files = Directory.GetFiles(roll);

                var imgCount = 1;
                // Iterate through the files and check for image extensions
                foreach (var file in files)
                {
                    string extension = Path.GetExtension(file).ToLower();

                    // Check if the file is an image based on extension
                    if (Array.Exists(imageExtensions, ext => ext.Equals(extension)))
                    {
                        string fileName = Path.GetFileName(file);
                        string fileExtension = Path.GetExtension(file);
                        string newFileName = $"{order.OrderId}-{order.Rolls![rollIndex].RollNumber}-{imgCount}" + fileExtension;
                        // string newFilePath = Path.Combine(roll, newFileName);
                        string rollFolderPath = Path.Combine(order.Scanner.DestinationDir, order.OrderId, order.Rolls![rollIndex].RollNumber.ToString());
                        string newFilePath = Path.Combine(rollFolderPath, newFileName);

                        if (!Directory.Exists(rollFolderPath))
                        {
                            Directory.CreateDirectory(rollFolderPath);
                        }

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
                Directory.Delete(roll);
                rollIndex++;
            }

            order.Status = OrderStatus.Completed;
            await context.SaveChangesAsync();
            return new SystemResponse() { IsSuccess = true };
        }

        public async Task<SystemResponse> UpdateOrderStatus(Order order, OrderStatus status)
        {
            try
            {
                var dbOrder = await context.Orders
                .Include(o => o.Scanner)
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                if (dbOrder == null)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Order ID:{order.OrderId} not found"
                    };

                if (dbOrder.Status == status)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Order's current status already set to requested status"
                    };
                }

                dbOrder.Status = status;

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

        public void Save()
        {
            throw new NotImplementedException();
        }

        public Task<SystemResponse> UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }


    }
}