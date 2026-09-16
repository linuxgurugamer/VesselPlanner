using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
// File: PlanetPackHeuristics.cs

using static VesselPlanner.ToolbarRegistration;

namespace DeltaVEditor
{
    public enum PlanetPackKind
    {
        Unknown = 0,
        Stock,
        JNSQ,
        GPP,
        KSRSS,
        RSS,
        BeyondHome,
        OPM,
        GEP,             // Grannus Expansion Pack
        WhirligigWorld,
        CustomSinglePack, // Some other pack folder we don't explicitly know
        MultiPack          // More than one pack detected
    }

    public sealed class PlanetPackInfo
    {
        public PlanetPackKind Kind;
        public string FolderName;   // e.g. "JNSQ", "GPP", "KSRSS"
        public string DisplayName;  // e.g. "JNSQ", "Galileo's Planet Pack"
        public double ScaleFactor;  // 1.0 for stock, 2.7 for JNSQ, etc. (0.0 = unknown)
        public string Source;       // Brief description of how it was detected

        public override string ToString()
        {
            return $"Kind={Kind}, Folder='{FolderName}', Display='{DisplayName}', Scale={ScaleFactor}, Source={Source}";
        }
    }

    public static class PlanetPackHeuristics
    {
        private static bool _initialized;
        private static PlanetPackInfo _cachedInfo;

        /// <summary>
        /// High-level entry point.
        /// Returns a PlanetPackInfo with Kind, FolderName, DisplayName, ScaleFactor, Source.
        /// </summary>
        public static PlanetPackInfo GetPlanetPackInfo()
        {
            if (_initialized && _cachedInfo != null)
                return _cachedInfo;

            _cachedInfo = DetectPlanetPackInternal();
            _initialized = true;

            Debug.Log($"[PlanetPackHeuristics] Detected planet pack: {_cachedInfo}");
            return _cachedInfo;
        }

        public static void Reset()
        {
            _initialized = false;
            _cachedInfo = null;
        }

        // ========================
        // Internal detection logic
        // ========================

        private static PlanetPackInfo DetectPlanetPackInternal()
        {
            Log.Info("DetectPlanetPackInternal");

            // 1) Look at Kopernicus cache files under GameData/*/Cache/*.bin
            var cacheCandidates = DetectCandidatesFromCacheDirectories();
            foreach (var s in cacheCandidates)
                Log.Info("DetectCandidatesFromCacheDirectories: " + s);

            // 2) Look at body / homeworld names from FlightGlobals/Planetarium
            var bodyHints = DetectHintsFromBodies(out CelestialBody homeBody);

            // 3) Try to reconcile everything into a single PlanetPackInfo
            var info = CombineSignals(cacheCandidates, bodyHints, homeBody);

            // Final fallback: if still unknown, assume stock
            if (info.Kind == PlanetPackKind.Unknown)
            {
                info.Kind = PlanetPackKind.Stock;
                info.DisplayName = "Stock";
                if (info.ScaleFactor <= 0.0)
                    info.ScaleFactor = 1.0;
                if (string.IsNullOrEmpty(info.Source))
                    info.Source = "Fallback (no non-core Kopernicus caches or hints found)";
            }

            return info;
        }

        /// <summary>
        /// Scan GameData for *.bin in folders like &quot;GameData/JNSQ/Cache/...&quot; and
        /// collect the first directory after GameData as a candidate.
        /// Ignores core folders like Squad, Kopernicus, ModuleManager, etc.
        /// </summary>
        private static HashSet<string> DetectCandidatesFromCacheDirectories()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string gameDataRoot = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
                if (!Directory.Exists(gameDataRoot))
                    return result;

                string[] cacheFiles = Directory.GetFiles(gameDataRoot, "*.bin", SearchOption.AllDirectories);

                foreach (var file in cacheFiles)
                {
                    if (file.IndexOf("Cache", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    int idx = file.IndexOf("GameData", StringComparison.OrdinalIgnoreCase);
                    if (idx < 0)
                        continue;

                    string relative = file.Substring(idx + "GameData".Length + 1); // after "GameData/"
                    string[] parts = relative.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    string folder = parts[0];

                    if (IsCoreFolder(folder))
                        continue;

                    result.Add(folder);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PlanetPackHeuristics] DetectCandidatesFromCacheDirectories failed: " + ex);
            }

            return result;
        }

        private static bool IsCoreFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return true;

            string f = folder.Trim();
            return string.Equals(f, "Squad", StringComparison.OrdinalIgnoreCase)
                || string.Equals(f, "SquadExpansion", StringComparison.OrdinalIgnoreCase)
                || string.Equals(f, "Kopernicus", StringComparison.OrdinalIgnoreCase)
                || string.Equals(f, "ModuleManager", StringComparison.OrdinalIgnoreCase)
                || string.Equals(f, "000_Harmony", StringComparison.OrdinalIgnoreCase)
                || string.Equals(f, "000_ClickThroughBlocker", StringComparison.OrdinalIgnoreCase)
                || string.Equals(f, "000_Toolbar", StringComparison.OrdinalIgnoreCase)
                || string.Equals(f, "000_ToolbarControl", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a set of hints from bodies (home world name, presence of particular bodies).
        /// Also returns the homeBody as an out parameter.
        /// </summary>
        private static HashSet<string> DetectHintsFromBodies(out CelestialBody homeBody)
        {
            var hints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            homeBody = null;

            try
            {
                if (Planetarium.fetch != null)
                    homeBody = Planetarium.fetch.Home;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PlanetPackHeuristics] Could not get Planetarium.fetch.Home: " + ex);
            }

            if (homeBody != null)
            {
                hints.Add("HOME:" + homeBody.name);

                // Quick check on radius for some rough scale/rescale hints
                double r = homeBody.Radius;
                hints.Add("HOME_RADIUS:" + r.ToString("F0"));
            }

            try
            {
                var bodies = FlightGlobals.Bodies;
                if (bodies != null)
                {
                    foreach (var b in bodies)
                    {
                        if (b == null) continue;
                        hints.Add("BODY:" + b.name);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PlanetPackHeuristics] Could not iterate FlightGlobals.Bodies: " + ex);
            }

            return hints;
        }

        private static PlanetPackInfo CombineSignals(
            HashSet<string> cacheCandidates,
            HashSet<string> bodyHints,
            CelestialBody homeBody)
        {
            var info = new PlanetPackInfo
            {
                Kind = PlanetPackKind.Unknown,
                FolderName = "",
                DisplayName = "",
                ScaleFactor = 0.0,
                Source = ""
            };

            // 1) If we got more than one distinct candidate folder from caches → MultiPack
            if (cacheCandidates != null && cacheCandidates.Count > 1)
            {
                info.Kind = PlanetPackKind.MultiPack;
                info.FolderName = string.Join(",", new List<string>(cacheCandidates).ToArray());
                info.DisplayName = info.FolderName;
                info.Source = "Multiple non-core Kopernicus cache folders detected";
                return info;
            }

            // 2) Single folder candidate from cache
            string cacheFolder = null;
            if (cacheCandidates != null && cacheCandidates.Count == 1)
            {
                foreach (var f in cacheCandidates) cacheFolder = f;
            }

            // 3) Map known folder names first
            if (!string.IsNullOrEmpty(cacheFolder))
            {
                var mapped = MapFolderNameToPack(cacheFolder);
                if (mapped.Kind != PlanetPackKind.Unknown)
                    return mapped;

                // Unknown folder -> treat as custom single pack
                info.Kind = PlanetPackKind.CustomSinglePack;
                info.FolderName = cacheFolder;
                info.DisplayName = cacheFolder;
                info.ScaleFactor = 0.0;
                info.Source = "Single non-core Kopernicus cache folder";
                return info;
            }

            // 4) No cache candidates; use body/home hints to guess
            if (bodyHints != null && bodyHints.Count > 0)
            {
                var fromHints = GuessPackFromBodyHints(bodyHints, homeBody);
                if (fromHints.Kind != PlanetPackKind.Unknown)
                    return fromHints;
            }

            // Nothing conclusive
            return info;
        }

        // ========================
        // Mapping / heuristics
        // ========================

        private static PlanetPackInfo MapFolderNameToPack(string folder)
        {
            var info = new PlanetPackInfo
            {
                Kind = PlanetPackKind.Unknown,
                FolderName = folder,
                DisplayName = folder,
                ScaleFactor = 0.0,
                Source = "Folder-based mapping"
            };

            string f = folder.Trim();

            if (f.Equals("JNSQ", StringComparison.OrdinalIgnoreCase))
            {
                info.Kind = PlanetPackKind.JNSQ;
                info.DisplayName = "JNSQ";
                info.ScaleFactor = 2.7; // commonly used
                return info;
            }

            if (f.Equals("GPP", StringComparison.OrdinalIgnoreCase) ||
                f.Equals("Galileos-Planet-Pack", StringComparison.OrdinalIgnoreCase))
            {
                info.Kind = PlanetPackKind.GPP;
                info.DisplayName = "Galileo's Planet Pack";
                info.ScaleFactor = 1.0; // roughly stock-ish scale
                return info;
            }

            if (f.Equals("KSRSS", StringComparison.OrdinalIgnoreCase))
            {
                info.Kind = PlanetPackKind.KSRSS;
                info.DisplayName = "KSRSS";
                info.ScaleFactor = 2.5; // commonly used rescale
                return info;
            }

            if (f.Equals("RealSolarSystem", StringComparison.OrdinalIgnoreCase) ||
                f.Equals("RSS", StringComparison.OrdinalIgnoreCase))
            {
                info.Kind = PlanetPackKind.RSS;
                info.DisplayName = "Real Solar System";
                info.ScaleFactor = 10.0; // classic RSS scale
                return info;
            }

            if (f.Equals("BeyondHome", StringComparison.OrdinalIgnoreCase))
            {
                info.Kind = PlanetPackKind.BeyondHome;
                info.DisplayName = "Beyond Home";
                info.ScaleFactor = 1.0; // custom system, not strictly uniform
                return info;
            }

            if (f.Equals("OPM", StringComparison.OrdinalIgnoreCase) ||
                f.Equals("OuterPlanetsMod", StringComparison.OrdinalIgnoreCase))
            {
                info.Kind = PlanetPackKind.OPM;
                info.DisplayName = "Outer Planets Mod";
                info.ScaleFactor = 1.0;
                return info;
            }

            if (f.Equals("GEP", StringComparison.OrdinalIgnoreCase) ||
                f.Equals("GrannusExpansionPack", StringComparison.OrdinalIgnoreCase))
            {
                info.Kind = PlanetPackKind.GEP;
                info.DisplayName = "Grannus Expansion Pack";
                info.ScaleFactor = 1.0;
                return info;
            }

            if (f.Equals("WhirligigWorld", StringComparison.OrdinalIgnoreCase))
            {
                info.Kind = PlanetPackKind.WhirligigWorld;
                info.DisplayName = "Whirligig World";
                info.ScaleFactor = 1.0;
                return info;
            }

            return info;
        }

        private static PlanetPackInfo GuessPackFromBodyHints(HashSet<string> hints, CelestialBody homeBody)
        {
            var info = new PlanetPackInfo
            {
                Kind = PlanetPackKind.Unknown,
                FolderName = "",
                DisplayName = "",
                ScaleFactor = 0.0,
                Source = "Body-name heuristics"
            };

            // Convert hints to something quick to query
            bool Has(string prefix, string value)
            {
                string key = prefix + value;
                return hints.Contains(key);
            }

            // Quick helpers
            bool HasBody(string name) => Has("BODY:", name);
            bool HasHomeNamed(string n) => Has("HOME:", n);

            // ==========================
            // Homeworld-based detection
            // ==========================

            if (HasHomeNamed("Gael"))
            {
                info.Kind = PlanetPackKind.GPP;
                info.FolderName = "GPP(? guessed from Gael)";
                info.DisplayName = "Galileo's Planet Pack";
                info.ScaleFactor = 1.0;
                return info;
            }

            if (HasHomeNamed("Rhode"))
            {
                info.Kind = PlanetPackKind.BeyondHome;
                info.FolderName = "BeyondHome(? guessed from Rhode)";
                info.DisplayName = "Beyond Home";
                info.ScaleFactor = 1.0;
                return info;
            }

            if (HasHomeNamed("Mesbin"))
            {
                info.Kind = PlanetPackKind.WhirligigWorld;
                info.FolderName = "WhirligigWorld(? guessed from Mesbin)";
                info.DisplayName = "Whirligig World";
                info.ScaleFactor = 1.0;
                return info;
            }

            if (HasHomeNamed("Earth"))
            {
                // Could be RSS or KSRSS (or another Earth-based pack)
                // Rough heuristic using radius
                double radius = homeBody != null ? homeBody.Radius : 0.0;

                if (radius > 5_000_000) // ~ Earth radius → RSS-like
                {
                    info.Kind = PlanetPackKind.RSS;
                    info.FolderName = "RealSolarSystem(? guessed from Earth radius)";
                    info.DisplayName = "Real Solar System";
                    info.ScaleFactor = 10.0;
                    return info;
                }

                // Otherwise assume KSRSS-like smaller scale
                info.Kind = PlanetPackKind.KSRSS;
                info.FolderName = "KSRSS(? guessed from Earth)";
                info.DisplayName = "KSRSS";
                info.ScaleFactor = 2.5;
                return info;
            }

            // ==========================
            // Extra-body-based detection
            // ==========================

            // OPM: presence of Sarnus / Urlum / Neidon / Plock on top of stock
            if (HasBody("Sarnus") || HasBody("Urlum") || HasBody("Neidon") || HasBody("Plock"))
            {
                info.Kind = PlanetPackKind.OPM;
                info.FolderName = "OPM(? guessed from Sarnus/Urlum/Neidon/Plock)";
                info.DisplayName = "Outer Planets Mod";
                info.ScaleFactor = 1.0;
                return info;
            }

            // GEP: presence of Grannus as an extra star
            if (HasBody("Grannus"))
            {
                info.Kind = PlanetPackKind.GEP;
                info.FolderName = "GEP(? guessed from Grannus)";
                info.DisplayName = "Grannus Expansion Pack";
                info.ScaleFactor = 1.0;
                return info;
            }

            // ==========================
            // Kerbin-based / rescale hints
            // ==========================

            if (HasHomeNamed("Kerbin") && homeBody != null)
            {
                double r = homeBody.Radius; // stock Kerbin radius ~ 600000
                double scale = r / 600000.0;

                // Rough detection for JNSQ (2.5–3.0x stock)
                if (scale > 2.5 && scale < 3.0)
                {
                    info.Kind = PlanetPackKind.JNSQ;
                    info.FolderName = "JNSQ(? guessed from Kerbin radius)";
                    info.DisplayName = "JNSQ";
                    info.ScaleFactor = 2.7;
                    return info;
                }
            }

            // Nothing matched
            return info;
        }
    }
}