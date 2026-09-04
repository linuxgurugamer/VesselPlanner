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
        public Func<double, double> IspCurveEvaluator { get; set; }
        // Evaluates full-throttle engine thrust for the requested environment.
        // Arguments: pressure in atmospheres, temperature in K, density in kg/m^3.
        public Func<double, double, double, double> ThrustEvaluator { get; set; }
        public bool IsSolid { get; set; }
        public bool IsAirBreathing { get; set; }
        public bool IsElectric { get; set; }
        public List<string> BulkheadProfiles { get; } = new List<string>();
        // KSP stack-node size of the engine's top attachment node. -1 means no top node.
        public int TopNodeSize { get; set; } = -1;
        public List<PropellantSpec> Propellants { get; } = new List<PropellantSpec>();

        public double IspAtPressure(double atmospheres)
        {
            atmospheres = Math.Max(0.0, atmospheres);
            if (IspCurveEvaluator != null)
            {
                try { return Math.Max(0.0, IspCurveEvaluator(atmospheres)); }
                catch { }
            }

            if (atmospheres <= 0.0) return VacuumIsp;
            if (atmospheres >= 1.0) return SeaLevelIsp;
            return VacuumIsp + (SeaLevelIsp - VacuumIsp) * atmospheres;
        }

        public double ThrustAtEnvironment(double atmospheres, double temperatureK, double densityKgPerM3)
        {
            atmospheres = Math.Max(0.0, atmospheres);
            if (ThrustEvaluator != null)
            {
                try
                {
                    double value = ThrustEvaluator(atmospheres, temperatureK, densityKgPerM3);
                    if (!double.IsNaN(value) && !double.IsInfinity(value) && value >= 0.0)
                        return value;
                }
                catch { }
            }

            // Fallback for an engine/module that cannot be evaluated through KSP's thrust API.
            if (VacuumIsp <= 0.0) return 0.0;
            return MaxThrustVacuumKn * IspAtPressure(atmospheres) / VacuumIsp;
        }

        public double ThrustAtPressure(double atmospheres)
        {
            return ThrustAtEnvironment(atmospheres, 288.15, 0.0);
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
        public double AtmosphereTemperatureK { get; set; } = 288.15;
        public double AtmosphereDensityKgPerM3 { get; set; }
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
        // AtmosphericDeltaV is calculated at the selected body's altitude/pressure.
        // VacuumDeltaV uses the same stage mass ratio with vacuum Isp.
        public double AtmosphericDeltaV { get; set; }
        public double VacuumDeltaV { get; set; }
        // Backward-compatible alias used by optimization code; atmospheric delta-v is the planning basis.
        public double DeltaV { get { return AtmosphericDeltaV; } set { AtmosphericDeltaV = value; } }
        public double InitialTwr { get; set; }
        // Vacuum-thrust TWR using the same wet mass and selected body gravity.
        public double MaxTwr { get; set; }
        public double FinalTwr { get; set; }
        public double BurnTimeSeconds { get; set; }
        public double EngineCost { get; set; }
        // Atmospheric delta-v at the selected body/altitude per Fund of total engine cost.
        public double CostEfficiency { get { return EngineCost > 0.0 ? AtmosphericDeltaV / EngineCost : 0.0; } }
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
        // Top-node sizes of engines currently assigned to this stage. These define the
        // stack interface used by the candidate-engine bulkhead-size filter.
        public List<int> TopNodeSizes { get; } = new List<int>();
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
