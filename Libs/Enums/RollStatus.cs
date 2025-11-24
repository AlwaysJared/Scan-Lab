using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Libs.Enums
{
    public enum RollStatus
    {
        Created,
        [Description("Scanning In Progress")]
        ScanningInProgress,
        [Description("Scanning Paused")]
        ScanningPaused,
        [Description("Scanning Complete")]
        ScanningCompleted,
        Processing,
        Processed
    }
}