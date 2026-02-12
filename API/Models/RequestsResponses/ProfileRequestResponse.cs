using System;

namespace API.Models.RequestsResponses
{
    public class AddProfileRequest
    {
        public required string ProfileName { get; set; }
        public required string StrategyClassName { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateProfileRequest
    {
        public required Guid Id { get; set; }
        public required string ProfileName { get; set; }
        public required string StrategyClassName { get; set; }
        public string? Description { get; set; }
    }
}
