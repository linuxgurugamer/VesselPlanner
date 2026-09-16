using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace VesselPlanner.Core
{
    public sealed class DecouplerMassEntry
    {
        public string Profile { get; set; }
        public double DecouplerMassTons { get; set; }
        public double StackSeparatorMassTons { get; set; }
        public string Source { get; set; }
    }

    // Configurable reference masses used by Stage-By-Stage when the user asks the
    // planner to include a decoupler in the fixed dry mass of a stage.
    public static class DecouplerMasses
    {
        public const string NodeName = "VESSEL_PLANNER_DECOUPLER_MASSES";

        private static readonly List<DecouplerMassEntry> _entries = new List<DecouplerMassEntry>();
        private static bool _loaded;

        public static IList<DecouplerMassEntry> All
        {
            get
            {
                EnsureLoaded();
                return _entries;
            }
        }

        public static double GetDecouplerMassTons(IEnumerable<string> profiles)
        {
            EnsureLoaded();
            if (profiles == null) return 0.0;

            // A stage can deliberately allow more than one bulkhead profile. Use the
            // heaviest matching decoupler so the optional mass allowance never undercounts.
            double mass = 0.0;
            foreach (string profile in profiles)
            {
                if (string.IsNullOrEmpty(profile)) continue;
                foreach (DecouplerMassEntry entry in _entries)
                {
                    if (!string.Equals(entry.Profile, profile.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                    mass = Math.Max(mass, entry.DecouplerMassTons);
                    break;
                }
            }
            return mass;
        }

        public static double GetStackSeparatorMassTons(IEnumerable<string> profiles)
        {
            EnsureLoaded();
            if (profiles == null) return 0.0;

            double mass = 0.0;
            foreach (string profile in profiles)
            {
                if (string.IsNullOrEmpty(profile)) continue;
                foreach (DecouplerMassEntry entry in _entries)
                {
                    if (!string.Equals(entry.Profile, profile.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                    mass = Math.Max(mass, entry.StackSeparatorMassTons);
                    break;
                }
            }
            return mass;
        }

        public static void Reload()
        {
            _loaded = false;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _entries.Clear();

            try
            {
                ConfigNode[] nodes = GameDatabase.Instance == null ? null : GameDatabase.Instance.GetConfigNodes(NodeName);
                if (nodes != null)
                {
                    foreach (ConfigNode root in nodes)
                    {
                        if (root == null) continue;
                        foreach (ConfigNode node in root.GetNodes("MASS"))
                            ReadEntry(node);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VesselPlanner] Unable to read decoupler masses: " + ex.Message);
            }

            if (_entries.Count == 0) AddFallbackEntries();
        }

        private static void ReadEntry(ConfigNode node)
        {
            if (node == null) return;
            string profile = CommonRoutines.ReadString(node, "profile", CommonRoutines.ReadString(node, "size", ""));
            if (string.IsNullOrEmpty(profile)) return;
            profile = profile.Trim();

            double decoupler = CommonRoutines.ReadDouble(node, "decoupler", 0.0);
            double separator = CommonRoutines.ReadDouble(node, "stackSeparator", 0.0);
            string source = CommonRoutines.ReadString(node, "source", "");

            for (int i = 0; i < _entries.Count; i++)
            {
                if (!string.Equals(_entries[i].Profile, profile, StringComparison.OrdinalIgnoreCase)) continue;
                _entries[i].DecouplerMassTons = Math.Max(0.0, decoupler);
                _entries[i].StackSeparatorMassTons = Math.Max(0.0, separator);
                _entries[i].Source = source;
                return;
            }

            _entries.Add(new DecouplerMassEntry
            {
                Profile = profile,
                DecouplerMassTons = Math.Max(0.0, decoupler),
                StackSeparatorMassTons = Math.Max(0.0, separator),
                Source = source
            });
        }

        private static void AddFallbackEntries()
        {
            Add("size0", 0.010, 0.010, "Stock");
            Add("size1", 0.040, 0.050, "Stock");
            Add("size1p5", 0.090, 0.120, "Stock");
            Add("size2", 0.160, 0.210, "Stock");
            Add("size3", 0.360, 0.480, "Stock");
            Add("size4", 0.640, 0.850, "Stock");
            Add("size5", 1.500, 1.500, "SpaceY");
            Add("size6", 3.000, 3.000, "SpaceY");
        }

        private static void Add(string profile, double decoupler, double separator, string source)
        {
            _entries.Add(new DecouplerMassEntry
            {
                Profile = profile,
                DecouplerMassTons = decoupler,
                StackSeparatorMassTons = separator,
                Source = source
            });
        }
    }
}
