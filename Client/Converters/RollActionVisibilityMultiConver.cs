using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data.Converters;
using Libs.Enums;

namespace Client.Converters
{
    public class RollActionVisibilityMultiConver : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                if(values[0].GetType() == typeof(UnsetValueType))
                    return false;

                var rollStatus = (RollStatus)values[0];
                var action = (string)values[1];
                
                switch (action.ToLower())
                {
                    case "start":
                        switch (rollStatus)
                        {
                            case RollStatus.Created:
                            case RollStatus.ScanningPaused:
                                return true;
                            default:
                                return false;
                        }
                    case "pause":
                        switch (rollStatus)
                        {
                            case RollStatus.ScanningInProgress:
                                return true;
                            default:
                                return false;
                        }
                    case "complete":
                        switch (rollStatus)
                        {
                            case RollStatus.ScanningInProgress:
                            case RollStatus.ScanningPaused:
                                return true;
                            default:
                                return false;
                        }
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