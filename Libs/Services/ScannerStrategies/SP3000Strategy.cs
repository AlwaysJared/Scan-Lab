using Libs.Data.Models;
using Libs.Enums;

namespace Libs.Services.ScannerStrategies
{
    /// <summary>
    /// SP-3000 strategy - currently identical to SP-500 (manual processing)
    /// Separated for future customization if needed
    /// </summary>
    public class SP3000Strategy : SP500Strategy
    {
        // Inherits all behavior from SP500Strategy
        // Can override specific methods if SP-3000 differs in the future
    }
}
