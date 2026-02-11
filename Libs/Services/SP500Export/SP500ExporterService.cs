using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Enums;
using Libs.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libs.Services.SP500Export
{
    /// <summary>
    /// Singleton service that orchestrates SP-500 auto-export sessions.
    /// Uses IServiceScopeFactory to access scoped services (DbContext, repositories).
    /// </summary>
    public class SP500ExporterService : IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SP500ExporterService> _logger;
        private readonly ConcurrentDictionary<Guid, SP500ExportSession> _activeSessions = new();

        public SP500ExporterService(IServiceScopeFactory scopeFactory, ILogger<SP500ExporterService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Starts an auto-export session for a roll.
        /// Loads scanner config, validates paths, and begins polling the Frontier share.
        /// </summary>
        public async Task<SystemResponse> StartExport(Guid rollId, Guid? staffId = null)
        {
            // Check if session already exists
            if (_activeSessions.ContainsKey(rollId))
                return new SystemResponse { IsSuccess = false, Message = "Export session already active for this roll" };

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ScanLabContext>();

            // Load roll with order, scanner, and profile
            var roll = await context.Rolls
                .Include(r => r.Order)
                .Include(r => r.Order.Scanner)
                .Include(r => r.Order.Scanner.Profile)
                .FirstOrDefaultAsync(r => r.RollId == rollId);

            if (roll == null)
                return new SystemResponse { IsSuccess = false, Message = "Roll not found" };

            if (roll.Order?.Scanner?.Profile == null)
                return new SystemResponse { IsSuccess = false, Message = "Roll has no scanner profile configured" };

            var scanner = roll.Order.Scanner;
            var profile = scanner.Profile;

            // Validate strategy is SP500Auto
            if (profile.StrategyClassName != "SP500AutoStrategy")
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = $"Scanner profile '{profile.ProfileName}' is not an SP-500 Auto profile"
                };

            // Check for existing session on same scanner
            if (_activeSessions.Values.Any(s => s.ScannerId == scanner.Id))
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = $"Scanner '{scanner.ScannerName}' already has an active export session"
                };

            // Load profile configurations
            var configs = await context.ProfileConfigurations
                .Where(pc => pc.ProfileId == profile.Id)
                .ToDictionaryAsync(pc => pc.ConfigKey, pc => pc.ConfigValue);

            // Get FrontierSharePath
            if (!configs.TryGetValue("FrontierSharePath", out var frontierSharePath) ||
                string.IsNullOrWhiteSpace(frontierSharePath))
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = "FrontierSharePath not configured for this profile"
                };

            if (!Directory.Exists(frontierSharePath))
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = $"Frontier share path does not exist: {frontierSharePath}"
                };

            if (!Directory.Exists(scanner.WatchedDir))
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = $"Scanner staging directory does not exist: {scanner.WatchedDir}"
                };

            // Parse config values with defaults
            var endOfExportFileName = configs.GetValueOrDefault("EndOfExportFileName", "CdOrder.INF");
            int.TryParse(configs.GetValueOrDefault("TimeoutMinutes", "90"), out var timeoutMinutes);
            int.TryParse(configs.GetValueOrDefault("OuterPollerSensitivity", "3"), out var sensitivity);
            int.TryParse(configs.GetValueOrDefault("CopierMaxRetries", "72"), out var maxRetries);

            // Create staging subdirectory path inside WatchedDir
            var sessionStagingPath = Path.Combine(scanner.WatchedDir, rollId.ToString());

            // Create session
            var session = new SP500ExportSession(
                rollId,
                scanner.Id,
                frontierSharePath,
                sessionStagingPath,
                endOfExportFileName,
                sensitivity,
                maxRetries,
                timeoutMinutes,
                _logger);

            // Wire up completion handler
            session.OnCompleted += async (completedSession) =>
            {
                await OnSessionCompleted(completedSession, staffId);
            };

            session.OnFailed += async (failedSession) =>
            {
                _logger.LogWarning("Export session {SessionId} failed with status {Status}",
                    failedSession.SessionId, failedSession.Status);
                _activeSessions.TryRemove(failedSession.RollId, out _);
            };

            // Register and start
            _activeSessions[rollId] = session;
            await session.StartAsync();

            _logger.LogInformation("Started SP500 export session for roll {RollId} (Session: {SessionId})",
                rollId, session.SessionId);

            return new SystemResponse { IsSuccess = true, Message = session.SessionId.ToString() };
        }

        /// <summary>
        /// Stops an active export session for a roll.
        /// </summary>
        public SystemResponse StopExport(Guid rollId)
        {
            if (!_activeSessions.TryRemove(rollId, out var session))
                return new SystemResponse { IsSuccess = false, Message = "No active export session found for this roll" };

            session.Cancel();
            session.Dispose();

            _logger.LogInformation("Stopped SP500 export session for roll {RollId}", rollId);
            return new SystemResponse { IsSuccess = true };
        }

        /// <summary>
        /// Gets the status of an active export session.
        /// </summary>
        public SP500ExportStatusDto? GetSessionStatus(Guid rollId)
        {
            if (!_activeSessions.TryGetValue(rollId, out var session))
                return null;

            return MapToDto(session);
        }

        /// <summary>
        /// Gets all active export sessions.
        /// </summary>
        public List<SP500ExportStatusDto> GetActiveSessions()
        {
            return _activeSessions.Values.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Called when an export session completes successfully.
        /// Triggers ProcessRoll and order completion check.
        /// </summary>
        private async Task OnSessionCompleted(SP500ExportSession session, Guid? staffId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var rollRepo = scope.ServiceProvider.GetRequiredService<RollRepository>();
                var orderRepo = scope.ServiceProvider.GetRequiredService<OrderRepository>();

                _logger.LogInformation("Auto-triggering ProcessRoll for roll {RollId}", session.RollId);

                // Trigger ProcessRoll — this moves/renames images from staging to destination
                var processResult = await rollRepo.ProcessRoll(session.RollId, staffId);

                if (!processResult.IsSuccess)
                {
                    _logger.LogError("ProcessRoll failed for roll {RollId}: {Message}",
                        session.RollId, processResult.Message);
                    return;
                }

                _logger.LogInformation("ProcessRoll completed for roll {RollId}", session.RollId);

                // Check if all rolls in order are processed
                var roll = await rollRepo.GetRoll(session.RollId);
                if (roll?.Order != null)
                {
                    var allProcessed = await rollRepo.AllRollsProcessed(roll.Order);
                    if (allProcessed.IsSuccess && allProcessed.ReturnObject is bool orderComplete && orderComplete)
                    {
                        var completeResult = await orderRepo.UpdateOrderStatus(
                            roll.Order, OrderStatus.Completed, staffId);

                        if (completeResult.IsSuccess)
                            _logger.LogInformation("Order {OrderId} marked as completed", roll.Order.OrderId);
                        else
                            _logger.LogError("Failed to mark order {OrderId} as completed: {Message}",
                                roll.Order.OrderId, completeResult.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in post-export processing for roll {RollId}", session.RollId);
            }
            finally
            {
                _activeSessions.TryRemove(session.RollId, out _);
            }
        }

        private static SP500ExportStatusDto MapToDto(SP500ExportSession session)
        {
            return new SP500ExportStatusDto
            {
                SessionId = session.SessionId,
                RollId = session.RollId,
                ScannerId = session.ScannerId,
                Status = session.Status.ToString(),
                DetectedRollNumber = session.DetectedRollNumber,
                ImagesExported = session.ImagesExported,
                StartedAt = session.StartedAt
            };
        }

        public void Dispose()
        {
            foreach (var session in _activeSessions.Values)
            {
                session.Cancel();
                session.Dispose();
            }
            _activeSessions.Clear();
            _logger.LogInformation("SP500ExporterService disposed");
        }
    }

    /// <summary>
    /// DTO for exposing session status via API.
    /// </summary>
    public class SP500ExportStatusDto
    {
        public Guid SessionId { get; set; }
        public Guid RollId { get; set; }
        public Guid ScannerId { get; set; }
        public required string Status { get; set; }
        public string? DetectedRollNumber { get; set; }
        public int ImagesExported { get; set; }
        public DateTime StartedAt { get; set; }
    }
}
