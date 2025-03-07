namespace API.Models.RequestsResponses
{
    public class BaseResponse
    {
        public bool Success { get; set; }
        public bool Warning { get; set; } =false;
        public string? Message { get; set; }
    }

    public class BaseRequest
    {
        
    }
}