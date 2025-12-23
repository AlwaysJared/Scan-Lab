namespace Libs.Enums
{
    public enum CompletionDetectionMode
    {
        Manual,           // SP-500/SP-3000 - scan tech clicks Complete Roll
        TimeBasedDelay,   // HS-1800 - automatic after delay
        ExitFile          // Future - watch for specific completion file
    }
}
