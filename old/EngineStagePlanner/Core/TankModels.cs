using System.Collections.Generic;
using System.Linq;

namespace EngineStagePlanner.Core
{
    public sealed class TankResourceCapacity
    {
        public string ResourceName { get; set; }
        public double Units { get; set; }
        public double DensityTonsPerUnit { get; set; }
    }

    public sealed class TankCandidate
    {
        public string PartName { get; set; }
        public string DisplayName { get; set; }
        public double DryMassTons { get; set; }
        public double Cost { get; set; }
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

        public string CapacitySummary
        {
            get { return string.Join(", ", Provided.Select(p => p.ResourceName + " " + p.Units.ToString("0.###")).ToArray()); }
        }
    }
}
