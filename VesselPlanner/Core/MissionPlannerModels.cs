using System;
using System.Collections.Generic;
using System.Globalization;

namespace VesselPlanner.Core
{
    public enum MissionStepKind
    {
        EnginesAndTanks,
        Subassemblies
    }

    public enum Maneuver
    {
        None,
        Launch,
        SubOrbitalLaunch,
        Orbit,
        Reentry,
        Landing,
        ResourceTransfer,
        Splashdown,
        ImpactAsteroid,
        TransferToAnotherPlanet,
        ChangeApoapsis,
        ChangeBothPeAndAp,
        ChangeInclination,
        ChangePeriapsis,
        ChangeSemiMajorAxis,
        FineTuneClosestApproach,
        InterceptAsteroid,
        InterceptVessel,
        MatchPlanesWithVessel,
        MatchVelocitiesWithVessel,
        ReturnFromAMoon
    }

    public sealed class MissionManeuver
    {
        public MissionStepKind StepKind { get; set; } = MissionStepKind.EnginesAndTanks;
        public Maneuver Kind { get; set; }
        public string Body { get; set; } = string.Empty;
        public string SourceBody { get; set; } = string.Empty;
        public string DestinationBody { get; set; } = string.Empty;
        public double DeltaV { get; set; }
        public DeltaVBasis DeltaVBasis { get; set; } = DeltaVBasis.Vacuum;

        // Saved KSP subassemblies carried by this mission step. Each entry stores its
        // count and optional per-copy decoupler mass so Stage-By-Stage remains
        // deterministic if the source .craft file later changes or is unavailable.
        public List<PlannedSubassembly> Assemblies { get; } = new List<PlannedSubassembly>();

        // 0.7.20/0.7.21 mission files stored a single assembly in these values. They are
        // retained for backward compatibility and are converted to Assemblies when loaded.
        public string AssemblyFile { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public double AssemblyMassTons { get; set; }

        public bool HasAssembly
        {
            get
            {
                return Assemblies.Count > 0 ||
                    !string.IsNullOrEmpty(AssemblyFile) || !string.IsNullOrEmpty(AssemblyName);
            }
        }

        public double AssemblyTotalMassTons
        {
            get
            {
                if (Assemblies.Count > 0)
                {
                    double total = 0.0;
                    foreach (PlannedSubassembly item in Assemblies)
                        if (item != null) total += item.TotalMassTons;
                    return Math.Max(0.0, total);
                }
                return Math.Max(0.0, AssemblyMassTons);
            }
        }

        public int AssemblyCopyCount
        {
            get
            {
                if (Assemblies.Count == 0) return HasAssembly ? 1 : 0;
                int count = 0;
                foreach (PlannedSubassembly item in Assemblies)
                    if (item != null) count += Math.Max(0, item.Quantity);
                return count;
            }
        }

        public string AssemblySummary
        {
            get
            {
                if (!HasAssembly) return string.Empty;
                if (Assemblies.Count == 1)
                {
                    PlannedSubassembly item = Assemblies[0];
                    if (item != null)
                    {
                        int count = Math.Max(1, item.Quantity);
                        return count.ToString(CultureInfo.InvariantCulture) + " x " + item.DisplayName +
                            " (" + item.TotalMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t)";
                    }
                }
                if (Assemblies.Count > 1)
                {
                    return AssemblyCopyCount.ToString(CultureInfo.InvariantCulture) + " subassemblies / " +
                        Assemblies.Count.ToString(CultureInfo.InvariantCulture) + " types (" +
                        AssemblyTotalMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t)";
                }
                string name = string.IsNullOrEmpty(AssemblyName) ? AssemblyFile : AssemblyName;
                return AssemblyMassTons > 0.0
                    ? name + " (" + AssemblyMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t)"
                    : name;
            }
        }

        public string LocationSummary
        {
            get
            {
                if (Kind == VesselPlanner.Core.Maneuver.TransferToAnotherPlanet)
                    return (SourceBody ?? string.Empty) + " -> " + (DestinationBody ?? string.Empty);
                return Body ?? string.Empty;
            }
        }
    }
}
