using System;
using System.Collections.Generic;
using System.Linq;

namespace EngineStagePlanner.Core
{
    public static class ExistingStageSimulator
    {
        public static List<StageSolution> Simulate(
            ExistingStageSnapshot snap,
            IEnumerable<EngineCandidate> engines,
            double gravity,
            double atmospheres,
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
                    StageSolution s = SimulateOne(snap, engine, count, gravity, atmospheres);
                    if (s.IsValid && s.InitialTwr >= minimumTwr)
                    {
                        results.Add(s);
                        if (mode != OptimizationMode.HighestTwr && mode != OptimizationMode.ShortestBurn) break;
                    }
                }
            }
            return Sort(results, mode);
        }

        private static StageSolution SimulateOne(ExistingStageSnapshot snap, EngineCandidate engine, int count, double gravity, double atmospheres)
        {
            var r = new StageSolution { Engine = engine, EngineCount = count, Isp = engine.IspAtPressure(atmospheres) };
            if (r.Isp <= 0.0) return Fail(r, "No usable ISP.");

            var props = engine.Propellants.Where(p => !p.IgnoreForIsp && p.Ratio > 0.0 && p.DensityTonsPerUnit > 0.0).ToList();
            if (props.Count == 0) return Fail(r, "No mass-bearing propellants.");

            // Propellant ratios are resource-unit ratios. Existing tank capacities place an upper
            // bound on the common mixture scale. If a required resource is absent, the engine is incompatible.
            double mixtureScale = double.PositiveInfinity;
            foreach (var p in props)
            {
                var have = snap.Resources.FirstOrDefault(x => string.Equals(x.Name, p.ResourceName, StringComparison.OrdinalIgnoreCase));
                if (have == null || have.Capacity <= 0.0) return Fail(r, "Stage lacks " + p.ResourceName + ".");
                mixtureScale = Math.Min(mixtureScale, have.Capacity / p.Ratio);
            }
            if (double.IsInfinity(mixtureScale) || mixtureScale <= 0.0) return Fail(r, "No usable propellant capacity.");

            foreach (var p in props)
            {
                double units = mixtureScale * p.Ratio;
                r.Propellants.Add(new PropellantRequirement
                {
                    ResourceName = p.ResourceName,
                    Ratio = p.Ratio,
                    Units = units,
                    MassTons = units * p.DensityTonsPerUnit,
                    VolumeLiters = p.LitersPerUnit > 0.0 ? units * p.LitersPerUnit : 0.0
                });
            }

            r.PropellantMassTons = r.Propellants.Sum(x => x.MassTons);
            r.TankVolumeLiters = r.Propellants.Sum(x => x.VolumeLiters);
            r.PayloadMassTons = snap.PayloadAboveStageMassTons;
            r.OtherDryMassTons = snap.StageNonEngineDryMassTons;
            r.EngineMassTons = engine.MassTons * count;
            r.EngineCost = engine.Cost * count;
            r.DryMassTons = r.PayloadMassTons + r.OtherDryMassTons + r.EngineMassTons;
            r.WetMassTons = r.DryMassTons + r.PropellantMassTons;
            if (r.DryMassTons <= 0.0 || r.WetMassTons <= r.DryMassTons) return Fail(r, "No meaningful stage mass.");

            r.ThrustKn = engine.ThrustAtPressure(atmospheres) * count;
            r.DeltaV = r.Isp * StageSolver.StandardGravity * Math.Log(r.WetMassTons / r.DryMassTons);
            r.InitialTwr = r.ThrustKn / (r.WetMassTons * gravity);
            r.FinalTwr = r.ThrustKn / (r.DryMassTons * gravity);
            double flow = r.ThrustKn / (r.Isp * StageSolver.StandardGravity);
            r.BurnTimeSeconds = flow > 0.0 ? r.PropellantMassTons / flow : 0.0;
            r.IsValid = r.ThrustKn > 0.0;
            return r;
        }

        private static StageSolution Fail(StageSolution r, string why) { r.IsValid = false; r.FailureReason = why; return r; }

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
