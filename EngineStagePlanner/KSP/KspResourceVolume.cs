using System;

namespace EngineStagePlanner.KSP
{
    internal static class KspResourceVolume
    {
        /// <summary>
        /// Returns the physical volume, in liters, represented by one KSP resource unit.
        /// KSP 1.12 exposes this directly on PartResourceDefinition.volume, so no
        /// stock-resource name table or mod-specific fallback is required.
        /// </summary>
        public static double GetLitersPerUnit(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName) || PartResourceLibrary.Instance == null) return 0.0;
            try
            {
                return GetLitersPerUnit(PartResourceLibrary.Instance.GetDefinition(resourceName));
            }
            catch
            {
                return 0.0;
            }
        }

        public static double GetLitersPerUnit(PartResourceDefinition definition)
        {
            if (definition == null) return 0.0;
            try
            {
                double volume = definition.volume;
                return double.IsNaN(volume) || double.IsInfinity(volume) || volume <= 0.0 ? 0.0 : volume;
            }
            catch
            {
                return 0.0;
            }
        }
    }
}
