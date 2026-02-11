using Libs.Data.Models;
using System;
using System.Collections.Generic;

namespace Libs.Services.ScannerStrategies
{
    public static class ScannerStrategyFactory
    {
        /// <summary>
        /// Registry of available strategy classes.
        /// IMPORTANT: When adding new strategies, register them here.
        /// </summary>
        public static readonly Dictionary<string, Type> StrategyRegistry = new()
        {
            { "NoritsuControllerStrategy", typeof(NoritsuControllerStrategy) },
            { "SP500Strategy", typeof(SP500Strategy) },
            { "SP3000Strategy", typeof(SP3000Strategy) },
            { "SP500AutoStrategy", typeof(SP500AutoStrategy) }
        };

        /// <summary>
        /// Validates if a strategy class name is registered
        /// </summary>
        public static bool IsValidStrategy(string className)
        {
            return StrategyRegistry.ContainsKey(className);
        }

        /// <summary>
        /// Gets list of all available strategy class names (for Admin UI)
        /// </summary>
        public static List<string> GetAvailableStrategies()
        {
            return new List<string>(StrategyRegistry.Keys);
        }

        /// <summary>
        /// Creates a strategy instance from a scanner's profile
        /// </summary>
        public static IScannerStrategy? CreateStrategy(Scanner scanner)
        {
            if (scanner.Profile == null)
                return null;

            return CreateStrategy(scanner.Profile.StrategyClassName);
        }

        /// <summary>
        /// Creates a strategy instance from a class name
        /// </summary>
        public static IScannerStrategy? CreateStrategy(string strategyClassName)
        {
            if (!StrategyRegistry.TryGetValue(strategyClassName, out var strategyType))
                return null;

            return (IScannerStrategy?)Activator.CreateInstance(strategyType);
        }
    }
}
