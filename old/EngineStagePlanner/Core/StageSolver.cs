using System;
using System.Collections.Generic;
using System.Linq;

namespace EngineStagePlanner.Core
{
    public static class StageSolver
    {
        public const double StandardGravity = 9.80665;

        public static StageSolution Solve(StageRequirements req, EngineCandidate engine, int engineCount)
        {
            var result = new StageSolution
            {
                Engine = engine,
                EngineCount = engineCount,
                PayloadMassTons = Math.Max(0.0, req.PayloadDryMassTons),
                OtherDryMassTons = Math.Max(0.0, req.OtherStageDryMassTons),
                EngineMassTons = Math.Max(0.0, engine.MassTons * engineCount),
                EngineCost = Math.Max(0.0, engine.Cost * engineCount)
            };

            if (engineCount <= 0 || req.TargetDeltaV <= 0.0 || req.Gravity <= 0.0)
                return Fail(result, "Invalid planning inputs.");

            double isp = engine.IspAtPressure(req.Atmospheres);
            double thrust = engine.ThrustAtEnvironment(
                req.Atmospheres, req.AtmosphereTemperatureK, req.AtmosphereDensityKgPerM3) * engineCount;
            if (isp <= 0.0 || thrust <= 0.0)
                return Fail(result, "Engine has no usable thrust/ISP in this environment.");

            var ispProps = engine.Propellants.Where(p => !p.IgnoreForIsp && p.Ratio > 0.0 && p.DensityTonsPerUnit > 0.0).ToList();
            if (ispProps.Count == 0)
                return Fail(result, "No mass-bearing propellants could be resolved.");

            double ratio = Math.Max(0.0, req.TankDryMassPerPropellantMass);
            double fixedDry = result.PayloadMassTons + result.OtherDryMassTons + result.EngineMassTons;
            double propMass = Math.Max(0.001, fixedDry * 0.25);

            for (int i = 0; i < 100; i++)
            {
                double tankDry = propMass * ratio;
                double dryMass = fixedDry + tankDry;
                double massRatio = Math.Exp(req.TargetDeltaV / (isp * StandardGravity));
                double nextPropMass = dryMass * (massRatio - 1.0);

                if (double.IsNaN(nextPropMass) || double.IsInfinity(nextPropMass) || nextPropMass > 1e9)
                    return Fail(result, "No finite solution for the selected engine and target delta-v.");

                if (Math.Abs(nextPropMass - propMass) < 1e-8)
                {
                    propMass = nextPropMass;
                    break;
                }
                propMass = nextPropMass;
            }

            result.Isp = isp;
            result.ThrustKn = thrust;
            result.PropellantMassTons = propMass;
            result.TankDryMassTons = propMass * ratio;
            result.DryMassTons = fixedDry + result.TankDryMassTons;
            result.WetMassTons = result.DryMassTons + propMass;
            double logMassRatio = Math.Log(result.WetMassTons / result.DryMassTons);
            result.AtmosphericDeltaV = isp * StandardGravity * logMassRatio;
            result.VacuumDeltaV = engine.VacuumIsp * StandardGravity * logMassRatio;
            result.InitialTwr = thrust / (result.WetMassTons * req.Gravity);
            double vacuumThrust = engine.ThrustAtEnvironment(0.0, 288.15, 0.0) * engineCount;
            result.MaxTwr = vacuumThrust / (result.WetMassTons * req.Gravity);
            result.FinalTwr = thrust / (result.DryMassTons * req.Gravity);

            // thrust kN / (Isp * g0) = metric tons/sec because 1 kN = 1 t*m/s^2.
            double massFlowTonsPerSecond = thrust / (isp * StandardGravity);
            result.BurnTimeSeconds = massFlowTonsPerSecond > 0.0 ? propMass / massFlowTonsPerSecond : 0.0;

            BuildPropellantRequirements(result, ispProps, propMass);
            result.TankVolumeLiters = result.Propellants.Sum(p => p.VolumeLiters);

            if (result.InitialTwr + 1e-9 < req.MinimumTwr)
                return Fail(result, "Below minimum TWR.");

            result.IsValid = true;
            return result;
        }

        private static void BuildPropellantRequirements(StageSolution result, IList<PropellantSpec> props, double totalMass)
        {
            // KSP propellant ratios are volumetric/resource-unit ratios, not mass ratios.
            // Find scale s where sum(s * ratio_i * density_i) = requested propellant mass.
            double massPerRatioUnit = props.Sum(p => p.Ratio * p.DensityTonsPerUnit);
            if (massPerRatioUnit <= 0.0) return;
            double scale = totalMass / massPerRatioUnit;

            foreach (var p in props)
            {
                double units = scale * p.Ratio;
                result.Propellants.Add(new PropellantRequirement
                {
                    ResourceName = p.ResourceName,
                    Ratio = p.Ratio,
                    Units = units,
                    MassTons = units * p.DensityTonsPerUnit,
                    VolumeLiters = p.LitersPerUnit > 0.0 ? units * p.LitersPerUnit : 0.0
                });
            }
        }

        private static StageSolution Fail(StageSolution result, string reason)
        {
            result.IsValid = false;
            result.FailureReason = reason;
            return result;
        }
    }
}
