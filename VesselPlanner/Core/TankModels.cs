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
        // KSP part-wide bulkheadProfiles values (for example size1 or srf).
        public List<string> BulkheadProfiles { get; } = new List<string>();
        public List<TankResourceCapacity> Resources { get; } = new List<TankResourceCapacity>();
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
