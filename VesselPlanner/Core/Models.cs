using System;
using System.Collections.Generic;
using System.Linq;

namespace VesselPlanner.Core
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
        // Unique KSP GameData URL for the loaded part. Used by thumbnail lookup so
        // duplicate internal part names from different mods cannot select the wrong icon.
        public string PartUrl { get; set; }
        public string DisplayName { get; set; }
        public double MassTons { get; set; }
        public double Cost { get; set; }
        public double MaxThrustVacuumKn { get; set; }
        public double SeaLevelThrustKn { get; set; }
        public double VacuumIsp { get; set; }
        public double SeaLevelIsp { get; set; }
        // Full-throttle mass flow of the Isp-bearing propellant mixture, in metric tons/sec.
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

    // Which of an engine's two Isp figures the target delta-v is measured against.
    public enum DeltaVBasis
    {
        Vacuum,
        Atmospheric
    }

    public sealed class StageRequirements
    {
        public double PayloadDryMassTons { get; set; }
        public double OtherStageDryMassTons { get; set; }
        public double TargetDeltaV { get; set; }
        // Vacuum sizes the stage from vacuum Isp, so the design is independent of the
        // selected body and altitude. Atmospheric sizes it from the Isp at the selected
        // pressure instead, which means the altitude does redesign the stage.
        public DeltaVBasis TargetDeltaVBasis { get; set; } = DeltaVBasis.Vacuum;
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
        public bool IgnoreForIsp { get; set; }
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
        // Thrust at the currently selected planet/altitude.
        public double ThrustKn { get; set; }
        // Static full-throttle thrust at 1 atm and in vacuum for this engine-count configuration.
        public double SeaLevelThrustKn { get; set; }
        public double VacuumThrustKn { get; set; }
        public double PayloadMassTons { get; set; }
        public double OtherDryMassTons { get; set; }
        public double EngineMassTons { get; set; }
        public double TankDryMassTons { get; set; }
        public double PropellantMassTons { get; set; }
        // Wet mass of the selected stage itself: stage hardware, candidate engines,
        // tank structure and loaded stage resources/propellant. Payload/upper stages
        // are deliberately excluded from this value.
        public double StageWetMassTons { get; set; }
        // Total vehicle mass at the beginning of this stage burn. This is the m0
        // used for delta-v and initial TWR because the stage accelerates everything
        // still attached above it as well as the stage itself.
        public double StartMassTons { get; set; }
        // Backward-compatible alias retained for older internal code.
        public double WetMassTons { get { return StartMassTons; } set { StartMassTons = value; } }
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
        // Current selected-stage engine dry mass as reported by KSP's own DeltaVPartInfo data.
        // This is preferred when adjusting KSP stock stage/start/end masses for a replacement engine.
        public double StockCurrentEngineMassTons { get; set; }
        public double StageTankCapacityUnits { get; set; }
        public double StageTankVolumeLiters { get; set; }
        public double PayloadAboveStageMassTons { get; set; }
        // KSP stock delta-v simulator mass boundaries for this stage. When available,
        // these are more accurate than inferring the retained/decoupled craft mass from
        // inverseStage alone, especially with radial assemblies, fuel lines and mod parts.
        public bool HasStockStageMasses { get; set; }
        public double StockStageStartMassTons { get; set; }
        public double StockStageEndMassTons { get; set; }
        // KSP's own selected-stage mass, excluding retained payload/upper stages.
        public double StockStageMassTons { get; set; }
        public double StockStageDryMassTons { get; set; }
        public double StockStageFuelMassTons { get; set; }
        public double StockStageBurnTimeSeconds { get; set; }
        public double InferredTankDryRatio { get; set; }
        public List<ExistingResource> Resources { get; } = new List<ExistingResource>();
        public List<string> CurrentEngines { get; } = new List<string>();
        // Base AvailablePart names for each physical engine part currently assigned to this stage.
        // One entry per physical engine part; used to recognize the exact installed configuration.
        public List<string> CurrentEnginePartNames { get; } = new List<string>();
        public List<ExistingEngineInfo> CurrentEngineDetails { get; } = new List<ExistingEngineInfo>();
        public List<string> BulkheadProfiles { get; } = new List<string>();
        // Top-node sizes of engines currently assigned to this stage. These define the
        // stack interface used by the candidate-engine bulkhead-size filter.
        public List<int> TopNodeSizes { get; } = new List<int>();
    }

    public sealed class ExistingEngineInfo
    {
        // Internal KSP part name used as a fallback when a part URL is unavailable.
        public string PartName { get; set; }
        // Exact AvailablePart URL for the installed part.
        public string PartUrl { get; set; }
        public string DisplayName { get; set; }
        public double SeaLevelThrustKn { get; set; }
        public double VacuumThrustKn { get; set; }
        public double SeaLevelIsp { get; set; }
        public double VacuumIsp { get; set; }
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
