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
        public string DisplayName { get; set; }
        public double DryMassTons { get; set; }
        public double Cost { get; set; }
        // KSP part-wide bulkheadProfiles values (for example size1 or srf).
        public List<string> BulkheadProfiles { get; } = new List<string>();
        public List<TankResourceCapacity> Resources { get; } = new List<TankResourceCapacity>();
    }

    public sealed class TankSuggestion
    {
        public TankCandidate Tank { get; set; }
        public int Count { get; set; }
        public double TotalDryMassTons { get; set; }
        public double TotalCost { get; set; }
        public double ExcessFraction { get; set; }
        public List<PropellantRequirement> Provided { get; } = new List<PropellantRequirement>();

        // Kilograms of capacity for the propellants required by the selected engine, per Fund.
        // Using mass keeps the metric comparable across resources with different KSP units.
        public double CostEfficiency
        {
            get
            {
                if (TotalCost <= 0.0) return 0.0;
                return Provided.Sum(p => p.MassTons) * 1000.0 / TotalCost;
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
