namespace Libs.Classes
{
    public sealed class SystemResponse
    {
        public bool IsSuccess { get; set; }
        public bool IsError { get; set; } = false;
        public string? Message { get; set; }
        public object? ReturnObject { get; set; }
    }
}