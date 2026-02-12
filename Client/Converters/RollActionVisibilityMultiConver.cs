using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Libs.Enums;

namespace Client.Converters
{
    public class RollActionVisibilityMultiConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                if(values[0].GetType() == typeof(UnsetValueType))
                    return false;

                var rollStatus = (RollStatus)values[0];
                var action = (string)values[1];

                // Check if scanner uses SP500Auto profile (optional 3rd binding)
                var isAuto = values.Count > 2
                    && values[2] is string strategy
                    && strategy == "SP500AutoStrategy";

                switch (action.ToLower())
                {
                    case "start":
                    case "pause":
                    case "complete":
                        // Hide manual scanning buttons for SP500Auto scanners
                        if (isAuto) return false;
                        return action.ToLower() switch
                        {
                            "start" => rollStatus == RollStatus.Created || rollStatus == RollStatus.ScanningPaused,
                            "pause" => rollStatus == RollStatus.ScanningInProgress,
                            "complete" => rollStatus == RollStatus.ScanningInProgress || rollStatus == RollStatus.ScanningPaused,
                            _ => false
                        };
                    case "startexport":
                        // Only show for SP500Auto, when roll is ready to export
                        if (!isAuto) return false;
                        return rollStatus == RollStatus.Created || rollStatus == RollStatus.ScanningPaused;
                    case "stopexport":
                        // Only show for SP500Auto, when export could be running
                        if (!isAuto) return false;
                        return rollStatus == RollStatus.Created
                            || rollStatus == RollStatus.ScanningInProgress
                            || rollStatus == RollStatus.ScanningPaused;
                    case "delete":
                        switch (rollStatus)
                        {
                            case RollStatus.Processing:
                            case RollStatus.Processed:
                            case RollStatus.ScanningCompleted:
                                return false;
                            default:
                                return true;
                        }
                    case "reset":
                        switch (rollStatus)
                        {
                            case RollStatus.Processed:
                            case RollStatus.Processing:
                                return true;
                            default:
                                return false;
                        }
                    default:
                        return false;
                }
            }
            catch
            {
                return true;
            }
        }
    }
}
