using System;
using System.Collections.Generic;
using System.Linq;

namespace EngineStagePlanner.Core
{
    public enum OptimizationMode
    {
        LowestStageMass,
        LowestPropellantMass,
        LowestCost,
        ShortestBurn,
        HighestTwr,
        HighestIsp
    }

    public sealed class PropellantSpec
    {
        public string ResourceName { get; set; }
        public double Ratio { get; set; }
        public double DensityTonsPerUnit { get; set; }
        public bool IgnoreForIsp { get; set; }
        public double LitersPerUnit { get; set; }
    }

    public sealed class EngineCandidate
    {
        public string PartName { get; set; }
        public string DisplayName { get; set; }
        public double MassTons { get; set; }
        public double Cost { get; set; }
        public double MaxThrustVacuumKn { get; set; }
        public double VacuumIsp { get; set; }
        public double SeaLevelIsp { get; set; }
        public bool IsSolid { get; set; }
        public bool IsAirBreathing { get; set; }
        public bool IsElectric { get; set; }
        public List<string> BulkheadProfiles { get; } = new List<string>();
        public List<PropellantSpec> Propellants { get; } = new List<PropellantSpec>();

        public double IspAtPressure(double atmospheres)
        {
            atmospheres = Math.Max(0.0, atmospheres);
            if (atmospheres <= 0.0) return VacuumIsp;
            if (atmospheres >= 1.0) return SeaLevelIsp;
            return VacuumIsp + (SeaLevelIsp - VacuumIsp) * atmospheres;
        }

        public double ThrustAtPressure(double atmospheres)
        {
            if (VacuumIsp <= 0.0) return 0.0;
            return MaxThrustVacuumKn * IspAtPressure(atmospheres) / VacuumIsp;
        }
    }

    public sealed class StageRequirements
    {
        public double PayloadDryMassTons { get; set; }
        public double OtherStageDryMassTons { get; set; }
        public double TargetDeltaV { get; set; }
        public double MinimumTwr { get; set; }
        public double Gravity { get; set; } = StageSolver.StandardGravity;
        public double Atmospheres { get; set; }
        public int MaxEngineCount { get; set; } = 8;
        public double TankDryMassPerPropellantMass { get; set; } = 0.125;
    }

    public sealed class PropellantRequirement
    {
        public string ResourceName { get; set; }
        public double Ratio { get; set; }
        public double Units { get; set; }
        public double MassTons { get; set; }
        public double VolumeLiters { get; set; }
    }

    public sealed class StageSolution
    {
        public EngineCandidate Engine { get; set; }
        public int EngineCount { get; set; }
        public bool IsValid { get; set; }
        public string FailureReason { get; set; }
        public double Isp { get; set; }
        public double ThrustKn { get; set; }
        public double PayloadMassTons { get; set; }
        public double OtherDryMassTons { get; set; }
        public double EngineMassTons { get; set; }
        public double TankDryMassTons { get; set; }
        public double PropellantMassTons { get; set; }
        public double WetMassTons { get; set; }
        public double DryMassTons { get; set; }
        public double DeltaV { get; set; }
        public double InitialTwr { get; set; }
        public double FinalTwr { get; set; }
        public double BurnTimeSeconds { get; set; }
        public double EngineCost { get; set; }
        public double TankVolumeLiters { get; set; }
        public List<PropellantRequirement> Propellants { get; } = new List<PropellantRequirement>();

        public string PropellantSummary
        {
            get { return string.Join(" + ", Propellants.Select(p => p.ResourceName).ToArray()); }
        }
    }

    public sealed class ExistingStageSnapshot
    {
        public int StageNumber { get; set; }
        public double VesselWetMassTons { get; set; }
        public double VesselDryMassTons { get; set; }
        public double StagePropellantMassTons { get; set; }
        public double StagePropellantCapacityMassTons { get; set; }
        public double StageNonEngineDryMassTons { get; set; }
        public double CurrentEngineMassTons { get; set; }
        public double StageTankCapacityUnits { get; set; }
        public double StageTankVolumeLiters { get; set; }
        public double PayloadAboveStageMassTons { get; set; }
        public double InferredTankDryRatio { get; set; }
        public List<ExistingResource> Resources { get; } = new List<ExistingResource>();
        public List<string> CurrentEngines { get; } = new List<string>();
        public List<string> BulkheadProfiles { get; } = new List<string>();
    }

    public sealed class ExistingResource
    {
        public string Name { get; set; }
        public double Amount { get; set; }
        public double Capacity { get; set; }
        public double Density { get; set; }
        public double VolumeLiters { get; set; }
    }
}
