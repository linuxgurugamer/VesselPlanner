using System;
using System.Collections.Generic;
using System.Linq;

namespace VesselPlanner.Core
{
    public sealed class TankResourceCapacity
    {
        public string ResourceName { get; set; }
        public double Units { get; set; }
        public double DensityTonsPerUnit { get; set; }
        public double LitersPerUnit { get; set; }
    }

    public sealed class TankCandidate
    {
        public string PartName { get; set; }
        // Unique KSP GameData URL for exact thumbnail resolution.
        public string PartUrl { get; set; }
        public string DisplayName { get; set; }
        public double DryMassTons { get; set; }
        public double Cost { get; set; }

        // Fuel-switch subtype metadata.  Empty SubtypeMod means this is the ordinary
        // unswitched/default KSP part candidate.  The first supported switch provider is
        // B9PartSwitch; keeping the provider explicit leaves room for other fuel switchers.
        public string SubtypeMod { get; set; }
        // Identifies the individual switcher on parts which contain more than one
        // ModuleB9PartSwitch. Empty for non-switched tanks.
        public string SubtypeModuleId { get; set; }
        public string SubtypeName { get; set; }
        public string SubtypeTitle { get; set; }
        public string TankType { get; set; }
        public double AddedMass { get; set; }
        public double AddedCost { get; set; }

        // Volume used by switcher tank definitions.  For B9PartSwitch this is the module
        // baseVolume plus any subtype volumeAdded value.
        public double TankVolume { get; set; }

        // KSP part-wide bulkheadProfiles values (for example size1 or srf).
        public List<string> BulkheadProfiles { get; } = new List<string>();
        public List<TankResourceCapacity> Resources { get; } = new List<TankResourceCapacity>();

        public bool IsSubtype
        {
            get { return !string.IsNullOrEmpty(SubtypeMod) || !string.IsNullOrEmpty(SubtypeName); }
        }

        // The physical KSP PartName is shared by every switched subtype.  Use this key
        // wherever candidates must remain distinct in planning/deduplication logic.
        public string IdentityKey
        {
            get
            {
                if (!IsSubtype) return PartName ?? string.Empty;
                return (PartName ?? string.Empty) + "|" +
                       (SubtypeMod ?? string.Empty) + "|" +
                       (SubtypeModuleId ?? string.Empty) + "|" +
                       (SubtypeName ?? string.Empty) + "|" +
                       (TankType ?? string.Empty);
            }
        }

        public double WetMassTons
        {
            get
            {
                double mass = Math.Max(0.0, DryMassTons);
                foreach (TankResourceCapacity resource in Resources)
                {
                    if (resource == null || resource.Units <= 0.0 || resource.DensityTonsPerUnit <= 0.0) continue;
                    mass += resource.Units * resource.DensityTonsPerUnit;
                }
                return Math.Max(0.0, mass);
            }
        }
    }

    // One tank type in a suggested tank set. A set can contain one, two, or three
    // different tank types, but every type in the set shares the same bulkhead profile.
    public sealed class TankSuggestionPart
    {
        public TankCandidate Tank { get; set; }
        public int Count { get; set; }

        public double TotalDryMassTons
        {
            get { return Tank == null ? 0.0 : Math.Max(0.0, Tank.DryMassTons) * Math.Max(0, Count); }
        }

        public double TotalCost
        {
            get { return Tank == null ? 0.0 : Math.Max(0.0, Tank.Cost) * Math.Max(0, Count); }
        }
    }

    public sealed class TankSuggestion
    {
        public string BulkheadProfile { get; set; } = string.Empty;
        public List<TankSuggestionPart> Tanks { get; } = new List<TankSuggestionPart>();
        public double TotalDryMassTons { get; set; }
        public double TotalCost { get; set; }
        public double ExcessFraction { get; set; }
        public List<PropellantRequirement> Provided { get; } = new List<PropellantRequirement>();

        public int Count
        {
            get { return Tanks.Sum(t => t == null ? 0 : Math.Max(0, t.Count)); }
        }

        public int DifferentTankTypes
        {
            get { return Tanks.Count(t => t != null && t.Tank != null && t.Count > 0); }
        }

        public TankCandidate PrimaryTank
        {
            get
            {
                TankSuggestionPart first = Tanks.FirstOrDefault(t => t != null && t.Tank != null && t.Count > 0);
                return first == null ? null : first.Tank;
            }
        }

        public string TankSummary
        {
            get
            {
                return string.Join(" + ", Tanks
                    .Where(t => t != null && t.Tank != null && t.Count > 0)
                    .Select(t => t.Count.ToString() + "x " + (string.IsNullOrEmpty(t.Tank.DisplayName) ? t.Tank.PartName : t.Tank.DisplayName))
                    .ToArray());
            }
        }

        public string CapacitySummary
        {
            get
            {
                return string.Join(", ", Provided.Select(p =>
                    p.ResourceName + " " + p.Units.ToString("0.###") +
                    (p.VolumeLiters > 0.0 ? " (" + p.VolumeLiters.ToString("0.###") + " L)" : string.Empty)).ToArray());
            }
        }
    }
}
