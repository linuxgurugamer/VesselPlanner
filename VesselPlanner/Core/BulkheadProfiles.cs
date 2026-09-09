using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace VesselPlanner.Core
{
    // Describes one selectable KSP bulkheadProfiles token. Stack profiles can also carry
    // the attach-node size used by the existing-engine Bulkhead column/filter; non-stack
    // profiles such as "srf" leave Size at -1.
    public sealed class BulkheadProfile
    {
        public string Profile { get; set; }
        public int Size { get; set; } = -1;
        public string Name { get; set; }
        public string Description { get; set; }

        public string Label
        {
            get
            {
                string name = string.IsNullOrEmpty(Name) ? Profile : Name;
                if (string.IsNullOrEmpty(name)) name = Size >= 0 ? "size" + Size.ToString(CultureInfo.InvariantCulture) : "profile";
                return string.IsNullOrEmpty(Description) ? name : name + " (" + Description + ")";
            }
        }
    }

    // The Stage-By-Stage selector uses the actual KSP bulkheadProfiles tokens (size0,
    // size1, srf, ...). Stack-size descriptions still come from BulkheadProfiles.cfg so
    // the existing engine Bulkhead column can display friendly diameter names.
    public static class BulkheadProfiles
    {
        public const string NodeName = "ENGINE_STAGE_PLANNER_BULKHEADS";

        private static readonly List<BulkheadProfile> _profiles = new List<BulkheadProfile>();
        private static bool _loaded;

        public static IList<BulkheadProfile> All
        {
            get
            {
                EnsureLoaded();
                return _profiles;
            }
        }

        public static void Reload()
        {
            _loaded = false;
            EnsureLoaded();
        }

        public static string LabelFor(int size)
        {
            if (size < 0) return "none";
            EnsureLoaded();
            foreach (BulkheadProfile profile in _profiles)
                if (profile.Size == size && !string.IsNullOrEmpty(profile.Name)) return profile.Name;
            return "size" + size.ToString(CultureInfo.InvariantCulture);
        }

        public static string DescriptionFor(int size)
        {
            if (size < 0) return "no top node";
            EnsureLoaded();
            foreach (BulkheadProfile profile in _profiles)
                if (profile.Size == size) return profile.Label;
            return "size" + size.ToString(CultureInfo.InvariantCulture);
        }

        public static string ProfileForSize(int size)
        {
            if (size < 0) return null;
            EnsureLoaded();
            foreach (BulkheadProfile profile in _profiles)
                if (profile.Size == size && !string.IsNullOrEmpty(profile.Profile)) return profile.Profile;
            return "size" + size.ToString(CultureInfo.InvariantCulture);
        }

        public static string NameForProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName)) return "none";
            EnsureLoaded();
            foreach (BulkheadProfile profile in _profiles)
                if (string.Equals(profile.Profile, profileName, StringComparison.OrdinalIgnoreCase))
                    return string.IsNullOrEmpty(profile.Name) ? profileName : profile.Name;
            return profileName;
        }

        public static bool TryGetSizeForProfile(string profileName, out int size)
        {
            size = -1;
            if (string.IsNullOrEmpty(profileName)) return false;
            EnsureLoaded();
            foreach (BulkheadProfile profile in _profiles)
            {
                if (!string.Equals(profile.Profile, profileName, StringComparison.OrdinalIgnoreCase)) continue;
                if (profile.Size < 0) return false;
                size = profile.Size;
                return true;
            }

            if (!profileName.StartsWith("size", StringComparison.OrdinalIgnoreCase)) return false;
            return int.TryParse(profileName.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out size) && size >= 0;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _profiles.Clear();

            try
            {
                ConfigNode[] nodes = GameDatabase.Instance == null ? null : GameDatabase.Instance.GetConfigNodes(NodeName);
                if (nodes != null)
                {
                    foreach (ConfigNode node in nodes) ReadProfiles(node);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VesselPlanner] Unable to read bulkhead profiles: " + ex.Message);
            }

            if (_profiles.Count == 0) AddFallbackProfiles();
            EnsureSurfaceProfile();
            _profiles.Sort(CompareProfiles);
        }

        private static void ReadProfiles(ConfigNode node)
        {
            if (node == null) return;

            foreach (ConfigNode profileNode in node.GetNodes("PROFILE"))
            {
                int size = -1;
                if (profileNode.HasValue("size"))
                {
                    int parsedSize;
                    if (int.TryParse(profileNode.GetValue("size"), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedSize) && parsedSize >= 0)
                        size = parsedSize;
                }

                string profileName = profileNode.HasValue("profile") ? profileNode.GetValue("profile") : null;
                if (string.IsNullOrEmpty(profileName) && size >= 0)
                    profileName = "size" + size.ToString(CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(profileName)) continue;

                string name = profileNode.HasValue("name") ? profileNode.GetValue("name") : null;
                string description = profileNode.HasValue("description") ? profileNode.GetValue("description") : null;

                bool replaced = false;
                for (int i = 0; i < _profiles.Count; i++)
                {
                    bool sameProfile = string.Equals(_profiles[i].Profile, profileName, StringComparison.OrdinalIgnoreCase);
                    bool sameStackSize = size >= 0 && _profiles[i].Size == size;
                    if (!sameProfile && !sameStackSize) continue;
                    _profiles[i].Profile = profileName;
                    _profiles[i].Size = size;
                    _profiles[i].Name = name;
                    _profiles[i].Description = description;
                    replaced = true;
                    break;
                }

                if (!replaced)
                    _profiles.Add(new BulkheadProfile { Profile = profileName, Size = size, Name = name, Description = description });
            }
        }

        private static void EnsureSurfaceProfile()
        {
            foreach (BulkheadProfile profile in _profiles)
                if (string.Equals(profile.Profile, "srf", StringComparison.OrdinalIgnoreCase)) return;
            _profiles.Add(new BulkheadProfile { Profile = "srf", Size = -1, Name = "srf", Description = "Surface attach" });
        }

        private static int CompareProfiles(BulkheadProfile a, BulkheadProfile b)
        {
            // Surface attach is the first choice in the Stage-By-Stage selector. Keep
            // the stack profiles in numeric size order after it, followed by any other
            // mod-defined non-stack profiles.
            bool aSurface = a != null && string.Equals(a.Profile, "srf", StringComparison.OrdinalIgnoreCase);
            bool bSurface = b != null && string.Equals(b.Profile, "srf", StringComparison.OrdinalIgnoreCase);
            if (aSurface && !bSurface) return -1;
            if (!aSurface && bSurface) return 1;

            double aOrder, bOrder;
            bool aStack = TryGetProfileOrder(a, out aOrder);
            bool bStack = TryGetProfileOrder(b, out bOrder);
            if (aStack && bStack) return aOrder.CompareTo(bOrder);
            if (aStack) return -1;
            if (bStack) return 1;
            return string.Compare(a == null ? null : a.Profile, b == null ? null : b.Profile, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetProfileOrder(BulkheadProfile profile, out double order)
        {
            order = 0.0;
            if (profile == null) return false;
            if (profile.Size >= 0)
            {
                order = profile.Size;
                return true;
            }
            if (string.IsNullOrEmpty(profile.Profile) || !profile.Profile.StartsWith("size", StringComparison.OrdinalIgnoreCase))
                return false;

            string suffix = profile.Profile.Substring(4).Replace("p", ".").Replace("P", ".");
            return double.TryParse(suffix, NumberStyles.Float, CultureInfo.InvariantCulture, out order);
        }

        private static void AddFallbackProfiles()
        {
            AddStack(0, "0.625 m", "Tiny");
            AddStack(1, "1.25 m", "Small");
            _profiles.Add(new BulkheadProfile { Profile = "size1p5", Size = -1, Name = "1.875 m", Description = "Medium" });
            AddStack(2, "2.5 m", "Large");
            AddStack(3, "3.75 m", "Extra large");
            AddStack(4, "5 m", "Huge");
            AddStack(5, "7.5 m", "Oversize, part pack defined");
            AddStack(6, "10 m", "Oversize, part pack defined");
            AddStack(7, "12.5 m", "Oversize, part pack defined");
            AddStack(8, "15 m", "Oversize, part pack defined");
            AddStack(9, "17.5 m", "Oversize, part pack defined");
            AddStack(10, "20 m", "Oversize, part pack defined");
            _profiles.Add(new BulkheadProfile { Profile = "srf", Size = -1, Name = "srf", Description = "Surface attach" });
        }

        private static void AddStack(int size, string name, string description)
        {
            _profiles.Add(new BulkheadProfile
            {
                Profile = "size" + size.ToString(CultureInfo.InvariantCulture),
                Size = size,
                Name = name,
                Description = description
            });
        }
    }
}
