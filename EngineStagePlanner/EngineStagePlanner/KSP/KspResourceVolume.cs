using System;
using System.Collections.Generic;

namespace EngineStagePlanner.KSP
{
    internal static class KspResourceVolume
    {
        // Stock LF/O tanks are conventionally treated as 5 L per resource unit.
        // Unknown resources deliberately report 0 L; the UI still shows exact KSP units.
        private static readonly Dictionary<string, double> LitersPerUnit = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "LiquidFuel", 5.0 },
            { "Oxidizer", 5.0 },
            { "SolidFuel", 5.0 },
            { "MonoPropellant", 4.0 },
            { "XenonGas", 0.1 }
        };

        public static double GetLitersPerUnit(string resourceName)
        {
            double value;
            return LitersPerUnit.TryGetValue(resourceName ?? string.Empty, out value) ? value : 0.0;
        }
    }
}
