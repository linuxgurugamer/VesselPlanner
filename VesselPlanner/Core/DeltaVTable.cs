using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using VesselPlanner;

namespace VesselPlanner.Core
{
    public sealed class DeltaV
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public float dV_to_low_orbit { get; set; }
        public float ejection_dV { get; set; }
        public float capture_dV { get; set; }
        public float transfer_to_low_orbit_dV { get; set; }
        public float total_capture_dV { get; set; }
        public float dV_low_orbit_to_surface { get; set; }
        public float ascent_dV { get; set; }
        public float plane_change_dV { get; set; }
        public string parent { get; set; }
        public bool isMoon { get; set; }
        public int sortOrder { get; set; }
    }

    public static class DeltaVTable
    {
        public const string SAVE_MOD_FOLDER = "VesselPlanner/PluginData";
        public const string DELTA_V_FOLDER = SAVE_MOD_FOLDER + "/DeltaVTables";
        public static string planetPack = "Stock";

        internal static bool deltaVloaded = false;
        internal static string loadedDeltaVTable = "";
        internal static readonly List<DeltaV> DeltaVDict = new List<DeltaV>();

        public static string LoadedTable { get { return loadedDeltaVTable; } }
        public static bool IsLoaded { get { return deltaVloaded && DeltaVDict.Count > 0; } }
        public static int Count { get { return DeltaVDict.Count; } }

        public static PlanetPackInfo DetectPlanetPack()
        {
            PlanetPackInfo packInfo = PlanetPackHeuristics.GetPlanetPackInfo();

            Debug.Log("[VesselPlanner] Planet pack detected: " + packInfo);

            string packName;
            if (packInfo.Kind == PlanetPackKind.CustomSinglePack)
                packName = packInfo.FolderName;
            else
                packName = packInfo.Kind.ToString();

            if (string.IsNullOrWhiteSpace(packName))
                packName = PlanetPackKind.Stock.ToString();

            if (!string.Equals(planetPack, packName, StringComparison.OrdinalIgnoreCase))
            {
                planetPack = packName;
                deltaVloaded = false;
                loadedDeltaVTable = "";
                DeltaVDict.Clear();
            }
            else
            {
                planetPack = packName;
            }

            return packInfo;
        }

        private static string GetDeltaVFileAbsolute(string pack)
        {
            return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", DELTA_V_FOLDER, pack + ".csv");
        }

        public static string GetDeltaVFilePath(string pack)
        {
            return GetDeltaVFileAbsolute(pack);
        }

        public static void LoadDeltaV(string pack)
        {
            if (deltaVloaded && loadedDeltaVTable == pack)
                return;

            deltaVloaded = true;
            loadedDeltaVTable = pack;
            DeltaVDict.Clear();

            string csvPath = GetDeltaVFileAbsolute(pack);
            if (!File.Exists(csvPath))
            {
                Debug.LogError("[VesselPlanner] DeltaV CSV not found: " + csvPath);
                return;
            }

            try
            {
                using (var reader = new StreamReader(csvPath))
                {
                    bool firstLine = true;
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue;

                        if (firstLine)
                        {
                            firstLine = false;
                            continue;
                        }

                        string[] cols = line.Split(',');
                        if (cols.Length < 13)
                        {
                            Debug.LogWarning("[VesselPlanner] Invalid DeltaV CSV row (expected 13 columns): " + line);
                            continue;
                        }

                        try
                        {
                            var dv = new DeltaV
                            {
                                Origin = cols[0].Trim(),
                                Destination = cols[1].Trim(),
                                dV_to_low_orbit = Math.Max(0f, ParseFloat(cols[2])),
                                ejection_dV = Math.Max(0f, ParseFloat(cols[3])),
                                capture_dV = Math.Max(0f, ParseFloat(cols[4])),
                                transfer_to_low_orbit_dV = Math.Max(0f, ParseFloat(cols[5])),
                                total_capture_dV = Math.Max(0f, ParseFloat(cols[6])),
                                dV_low_orbit_to_surface = Math.Max(0f, ParseFloat(cols[7])),
                                ascent_dV = Math.Max(0f, ParseFloat(cols[8])),
                                plane_change_dV = Math.Max(0f, ParseFloat(cols[9])),
                                parent = cols[10].Trim(),
                                isMoon = ParseBool(cols[11]),
                                sortOrder = ParseInt(cols[12])
                            };

                            if (!string.IsNullOrEmpty(dv.Origin) || !string.IsNullOrEmpty(dv.Destination))
                                DeltaVDict.Add(dv);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("[VesselPlanner] Error parsing DeltaV row: " + line + "\n" + ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[VesselPlanner] Unable to load DeltaV CSV " + csvPath + ": " + ex);
                DeltaVDict.Clear();
            }
        }

        public static void Reload()
        {
            deltaVloaded = false;
            LoadDeltaV(string.IsNullOrEmpty(loadedDeltaVTable) ? planetPack : loadedDeltaVTable);
        }

        public static string[] GetBodies(bool moonsOnly = false)
        {
            var names = new Dictionary<string, Tuple<int, bool>>(StringComparer.OrdinalIgnoreCase);
            foreach (DeltaV dv in DeltaVDict)
            {
                AddBody(names, dv.Destination, dv.sortOrder, dv.isMoon);
                // Self rows are the authoritative metadata rows; accepting Origin also makes
                // user-authored pair-only tables useful without requiring separate body rows.
                if (string.Equals(dv.Origin, dv.Destination, StringComparison.OrdinalIgnoreCase))
                    AddBody(names, dv.Origin, dv.sortOrder, dv.isMoon);
            }

            return names
                .Where(kv => !moonsOnly || kv.Value.Item2)
                .OrderBy(kv => kv.Value.Item1)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => kv.Key)
                .ToArray();
        }

        private static void AddBody(Dictionary<string, Tuple<int, bool>> names, string name, int order, bool isMoon)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            Tuple<int, bool> old;
            if (!names.TryGetValue(name.Trim(), out old) || order < old.Item1)
                names[name.Trim()] = Tuple.Create(order, isMoon);
        }

        public static bool TryGetLaunchDeltaV(string body, out double value)
        {
            DeltaV row = FindBodyRow(body);
            value = row != null ? row.dV_to_low_orbit : 0.0;
            return row != null && value > 0.0;
        }

        public static bool TryGetLandingDeltaV(string body, out double value)
        {
            DeltaV row = FindBodyRow(body);
            value = row != null ? row.dV_low_orbit_to_surface : 0.0;
            return row != null && value > 0.0;
        }

        public static bool TryGetMoonReturnDeltaV(string moon, out double value)
        {
            value = 0.0;
            DeltaV moonRow = FindBodyRow(moon);
            if (moonRow == null || !moonRow.isMoon) return false;

            double ascent = moonRow.ascent_dV > 0f ? moonRow.ascent_dV : moonRow.dV_to_low_orbit;
            if (ascent <= 0.0) return false;

            // The parent -> moon transfer row's capture burn is the reverse-direction
            // moon-orbit escape burn used when returning from the moon to its parent.
            double escapeToParent = 0.0;
            if (!string.IsNullOrWhiteSpace(moonRow.parent))
            {
                DeltaV parentToMoon = DeltaVDict.FirstOrDefault(d =>
                    string.Equals(d.Origin, moonRow.parent, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(d.Destination, moon, StringComparison.OrdinalIgnoreCase));
                if (parentToMoon != null)
                    escapeToParent = Math.Max(0f, parentToMoon.capture_dV);
            }

            value = ascent + escapeToParent;
            return value > 0.0;
        }

        public static bool TryGetTransferDeltaV(string origin, string destination, out double value)
        {
            DeltaV row = DeltaVDict.FirstOrDefault(d =>
                string.Equals(d.Origin, origin, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(d.Destination, destination, StringComparison.OrdinalIgnoreCase));
            value = row != null ? row.total_capture_dV : 0.0;
            return row != null && value > 0.0;
        }

        private static DeltaV FindBodyRow(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            DeltaV self = DeltaVDict.FirstOrDefault(d =>
                string.Equals(d.Origin, body, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(d.Destination, body, StringComparison.OrdinalIgnoreCase));
            if (self != null) return self;
            return DeltaVDict.FirstOrDefault(d => string.Equals(d.Destination, body, StringComparison.OrdinalIgnoreCase));
        }

        private static float ParseFloat(string value)
        {
            float result;
            return float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : 0f;
        }

        private static int ParseInt(string value)
        {
            int result;
            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : int.MaxValue;
        }

        private static bool ParseBool(string value)
        {
            bool result;
            return bool.TryParse(value.Trim(), out result) && result;
        }
    }
}
