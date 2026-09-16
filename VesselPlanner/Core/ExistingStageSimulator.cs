using System;
using System.Collections.Generic;
using System.Linq;

namespace VesselPlanner.Core
{
    public static class ExistingStageSimulator
    {
        public static List<StageSolution> Simulate(
            ExistingStageSnapshot snap,
            IEnumerable<EngineCandidate> engines,
            double gravity,
            double atmospheres,
            double atmosphereTemperatureK,
            double atmosphereDensityKgPerM3,
            int maxEngineCount,
            double minimumTwr,
            OptimizationMode mode)
        {
            var results = new List<StageSolution>();
            if (snap == null || gravity <= 0.0) return results;

            foreach (var engine in engines)
            {
                for (int count = 1; count <= Math.Max(1, maxEngineCount); count++)
                {
                    StageSolution s = SimulateOne(snap, engine, count, gravity, atmospheres, atmosphereTemperatureK, atmosphereDensityKgPerM3);
                    if (s.IsValid && s.InitialTwr >= minimumTwr)
                    {
                        results.Add(s);
                        if (mode != OptimizationMode.HighestTwr && mode != OptimizationMode.ShortestBurn) break;
                    }
                }
            }
            return Sort(results, mode);
        }

        private static StageSolution SimulateOne(ExistingStageSnapshot snap, EngineCandidate engine, int count, double gravity, double atmospheres, double atmosphereTemperatureK, double atmosphereDensityKgPerM3)
        {
            var r = new StageSolution { Engine = engine, EngineCount = count, Isp = engine.IspAtPressure(atmospheres) };
            if (r.Isp <= 0.0) return CommonRoutines.FailStageSolution(r, "No usable ISP.");

            var allProps = engine.Propellants.Where(p => p.Ratio > 0.0).ToList();
            var props = allProps.Where(p => !p.IgnoreForIsp && p.DensityTonsPerUnit > 0.0).ToList();
            if (props.Count == 0) return CommonRoutines.FailStageSolution(r, "No mass-bearing propellants for delta-v calculation.");

            // Propellant ratios are resource-unit ratios. For existing-stage delta-v, use the
            // resource amount currently loaded in the editor, not maxAmount/capacity. This matches
            // KSP/MechJeb behavior for partially filled tanks and avoids overstating delta-v.
            double mixtureScale = double.PositiveInfinity;
            foreach (var p in props)
            {
                var have = snap.Resources.FirstOrDefault(x => string.Equals(x.Name, p.ResourceName, StringComparison.OrdinalIgnoreCase));
                if (have == null || have.Amount <= 0.0) return CommonRoutines.FailStageSolution(r, "Stage lacks usable " + p.ResourceName + ".");
                mixtureScale = Math.Min(mixtureScale, have.Amount / p.Ratio);
            }
            if (double.IsInfinity(mixtureScale) || mixtureScale <= 0.0) return CommonRoutines.FailStageSolution(r, "No usable propellant amount.");

            foreach (var p in allProps)
            {
                double units = mixtureScale * p.Ratio;
                r.Propellants.Add(new PropellantRequirement
                {
                    ResourceName = p.ResourceName,
                    Ratio = p.Ratio,
                    IgnoreForIsp = p.IgnoreForIsp,
                    Units = units,
                    MassTons = units * p.DensityTonsPerUnit,
                    VolumeLiters = p.LitersPerUnit > 0.0 ? units * p.LitersPerUnit : 0.0
                });
            }

            // Only mass-bearing resources which participate in the Isp mixture are
            // treated as rocket-equation propellant mass. Auxiliary/ignoreForIsp
            // resources are still shown with their required units and volume.
            double scannedPropellantMass = props.Sum(p => mixtureScale * p.Ratio * p.DensityTonsPerUnit);

            // For a candidate engine, burn duration must use only the propellant that this
            // engine can actually consume in its configured mixture.  DeltaVStageInfo.fuelMass
            // is the stock stage's total fuel mass for the currently installed propulsion
            // system and may include resources which a replacement candidate does not burn.
            // Using it here can overstate (or otherwise distort) candidate burn time.
            //
            // scannedPropellantMass is derived from the limiting resource amount and the
            // candidate engine's own propellant ratios, so it is the correct m in:
            //     t = m * IspVac * g0 / FVac
            // Stock start/stage/end masses remain authoritative for delta-v and TWR.
            r.PropellantMassTons = scannedPropellantMass;
            r.TankVolumeLiters = r.Propellants.Sum(x => x.VolumeLiters);
            r.PayloadMassTons = snap.PayloadAboveStageMassTons;
            r.OtherDryMassTons = snap.StageNonEngineDryMassTons;
            r.EngineMassTons = engine.MassTons * count;
            r.EngineCost = engine.Cost * count;

            // Physical wet mass of the selected stage only.  When KSP's stock stage
            // simulator is available, use DeltaVStageInfo.stageMass directly so tank dry
            // mass and tank resources cannot be counted once by KSP and again by our scanner.
            // Only the candidate-vs-installed engine mass difference is applied.
            if (snap.HasStockStageMasses && snap.StockStageMassTons > 0.0)
            {
                // Strip the currently installed engine mass out of every KSP stock mass
                // boundary first, then add the candidate engine mass exactly once.
                // StockCurrentEngineMassTons is sourced from the same DeltaVPartInfo model
                // as these stock masses, avoiding Part.mass/module-mass mismatches.
                double installedEngineMass = snap.StockCurrentEngineMassTons > 0.0
                    ? snap.StockCurrentEngineMassTons
                    : snap.CurrentEngineMassTons;

                // If this candidate is exactly the same physical engine part type and count
                // already installed in the selected stage, do not perform a remove/add
                // substitution at all. The stock masses already contain that exact engine
                // configuration, including any variant/module mass modifiers. Replacing it
                // with prefab.mass can otherwise make the current-engine row look as if the
                // engine were counted twice even though replacement-engine rows are correct.
                if (IsExactInstalledEngineConfiguration(snap, engine, count))
                {
                    r.EngineMassTons = installedEngineMass;
                    r.StageWetMassTons = snap.StockStageMassTons;
                    r.StartMassTons = snap.StockStageStartMassTons;
                    r.DryMassTons = snap.StockStageEndMassTons;
                }
                else
                {
                    double stageWithoutInstalledEngines = Math.Max(0.0, snap.StockStageMassTons - installedEngineMass);
                    double startWithoutInstalledEngines = Math.Max(0.0, snap.StockStageStartMassTons - installedEngineMass);
                    double endWithoutInstalledEngines = Math.Max(0.0, snap.StockStageEndMassTons - installedEngineMass);

                    r.StageWetMassTons = stageWithoutInstalledEngines + r.EngineMassTons;
                    r.StartMassTons = startWithoutInstalledEngines + r.EngineMassTons;
                    r.DryMassTons = endWithoutInstalledEngines + r.EngineMassTons;
                }
                r.PropellantMassTons = scannedPropellantMass;
            }
            else
            {
                // Fallback for cases where KSP's stock stage simulator is not yet ready.
                double carriedNonBurnResourceMass = Math.Max(0.0, snap.StagePropellantMassTons - r.PropellantMassTons);
                r.DryMassTons = r.PayloadMassTons + r.OtherDryMassTons + r.EngineMassTons + carriedNonBurnResourceMass;
                r.StartMassTons = r.DryMassTons + r.PropellantMassTons;
            }

            if (r.DryMassTons <= 0.0 || r.StartMassTons <= r.DryMassTons) return CommonRoutines.FailStageSolution(r, "No meaningful stage mass.");

            r.ThrustKn = engine.ThrustAtEnvironment(atmospheres, atmosphereTemperatureK, atmosphereDensityKgPerM3) * count;
            r.SeaLevelThrustKn = engine.SeaLevelThrustKn * count;
            r.VacuumThrustKn = engine.MaxThrustVacuumKn * count;
            double logMassRatio = Math.Log(r.StartMassTons / r.DryMassTons);
            r.AtmosphericDeltaV = r.Isp * StageSolver.StandardGravity * logMassRatio;
            r.VacuumDeltaV = engine.VacuumIsp * StageSolver.StandardGravity * logMassRatio;
            r.InitialTwr = r.ThrustKn / (r.StartMassTons * gravity);
            r.MaxTwr = r.VacuumThrustKn / (r.StartMassTons * gravity);
            r.FinalTwr = r.ThrustKn / (r.DryMassTons * gravity);
            // Burn rate is based on vacuum thrust/vacuum Isp and therefore does not
            // change with the selected planet or altitude.
            r.BurnTimeSeconds = StageSolver.CalculateBurnTimeSeconds(
                r.PropellantMassTons, r.VacuumThrustKn, engine.VacuumIsp);
            r.IsValid = r.ThrustKn > 0.0;
            return r;
        }


        private static bool IsExactInstalledEngineConfiguration(ExistingStageSnapshot snap, EngineCandidate engine, int count)
        {
            if (snap == null || engine == null || count <= 0 || snap.CurrentEnginePartNames == null) return false;
            if (snap.CurrentEnginePartNames.Count != count || count == 0) return false;

            string candidatePartName = engine.PartName ?? string.Empty;
            int colon = candidatePartName.IndexOf(':');
            if (colon >= 0) candidatePartName = candidatePartName.Substring(0, colon);
            if (candidatePartName.Length == 0) return false;

            return snap.CurrentEnginePartNames.All(name =>
                !string.IsNullOrEmpty(name) &&
                string.Equals(name, candidatePartName, StringComparison.OrdinalIgnoreCase));
        }

        private static List<StageSolution> Sort(List<StageSolution> results, OptimizationMode mode)
        {
            switch (mode)
            {
                case OptimizationMode.LowestPropellantMass: return results.OrderBy(x => x.PropellantMassTons).ThenByDescending(x => x.DeltaV).ToList();
                case OptimizationMode.LowestCost: return results.OrderBy(x => x.EngineCost).ThenByDescending(x => x.DeltaV).ToList();
                case OptimizationMode.ShortestBurn: return results.OrderBy(x => x.BurnTimeSeconds).ToList();
                case OptimizationMode.HighestTwr: return results.OrderByDescending(x => x.InitialTwr).ToList();
                case OptimizationMode.HighestIsp: return results.OrderByDescending(x => x.Isp).ThenByDescending(x => x.DeltaV).ToList();
                default: return results.OrderBy(x => x.WetMassTons).ThenByDescending(x => x.DeltaV).ToList();
            }
        }
    }
}
