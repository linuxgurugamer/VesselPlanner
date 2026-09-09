using System;
using System.Collections.Generic;
using System.Linq;

namespace VesselPlanner.Core
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

            // Planning geometry (fuel load, tank mass, wet/dry mass and burn time) is based
            // on the engine's vacuum performance by default. Moving the altitude slider then
            // only re-evaluates atmospheric performance rather than redesigning the stage,
            // which keeps VacuumDeltaV and BurnTimeSeconds invariant with altitude.
            //
            // On the atmospheric basis the target is instead met at the selected pressure,
            // so the sizing uses the atmospheric Isp and the altitude does change the design:
            // the same target needs more propellant the lower and thicker the air gets.
            double vacuumIsp = engine.VacuumIsp;
            double atmosphericIsp = engine.IspAtPressure(req.Atmospheres);
            double thrust = engine.ThrustAtEnvironment(
                req.Atmospheres, req.AtmosphereTemperatureK, req.AtmosphereDensityKgPerM3) * engineCount;
            if (vacuumIsp <= 0.0 || atmosphericIsp <= 0.0 || thrust <= 0.0)
                return Fail(result, "Engine has no usable thrust/ISP in this environment.");

            var allProps = engine.Propellants.Where(p => p.Ratio > 0.0).ToList();
            var ispProps = allProps.Where(p => !p.IgnoreForIsp && p.DensityTonsPerUnit > 0.0).ToList();
            if (ispProps.Count == 0)
                return Fail(result, "No mass-bearing propellants could be resolved for delta-v calculation.");

            double sizingIsp = req.TargetDeltaVBasis == DeltaVBasis.Atmospheric ? atmosphericIsp : vacuumIsp;

            double ratio = Math.Max(0.0, req.TankDryMassPerPropellantMass);
            double fixedDry = result.PayloadMassTons + result.OtherDryMassTons + result.EngineMassTons;
            double propMass = Math.Max(0.001, fixedDry * 0.25);

            for (int i = 0; i < 100; i++)
            {
                double tankDry = propMass * ratio;
                double dryMass = fixedDry + tankDry;
                // Size the stage to the requested delta-v on the selected basis. The other
                // figure is then evaluated from the same fixed mass ratio.
                double massRatio = Math.Exp(req.TargetDeltaV / (sizingIsp * StandardGravity));
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

            result.Isp = atmosphericIsp;
            result.ThrustKn = thrust;
            result.SeaLevelThrustKn = engine.SeaLevelThrustKn * engineCount;
            result.VacuumThrustKn = engine.MaxThrustVacuumKn * engineCount;
            result.PropellantMassTons = propMass;
            result.TankDryMassTons = propMass * ratio;
            result.DryMassTons = fixedDry + result.TankDryMassTons;
            // Stage wet mass excludes the payload/upper stages. Start mass includes
            // everything the selected stage must accelerate at ignition.
            result.StageWetMassTons = result.OtherDryMassTons
                + result.EngineMassTons
                + result.TankDryMassTons
                + propMass;
            result.StartMassTons = result.PayloadMassTons + result.StageWetMassTons;
            double logMassRatio = Math.Log(result.StartMassTons / result.DryMassTons);
            result.AtmosphericDeltaV = atmosphericIsp * StandardGravity * logMassRatio;
            result.VacuumDeltaV = vacuumIsp * StandardGravity * logMassRatio;
            result.InitialTwr = thrust / (result.StartMassTons * req.Gravity);
            result.MaxTwr = result.VacuumThrustKn / (result.StartMassTons * req.Gravity);
            result.FinalTwr = thrust / (result.DryMassTons * req.Gravity);

            // Burn time is intentionally independent of selected body/altitude.
            // Use the vacuum engine rating to calculate mass flow:
            //   mdot = F / (Isp * g0)
            // and therefore:
            //   t = mPropellant * Isp * g0 / F
            // With thrust in kN and mass in metric tons, the numerical units cancel
            // consistently (1 kN / g0 corresponds to metric tons/sec here).
            result.BurnTimeSeconds = CalculateBurnTimeSeconds(
                propMass, result.VacuumThrustKn, vacuumIsp);

            BuildPropellantRequirements(result, allProps, propMass);
            result.TankVolumeLiters = result.Propellants.Sum(p => p.VolumeLiters);

            if (result.InitialTwr + 1e-9 < req.MinimumTwr)
                return Fail(result, "Below minimum TWR.");

            result.IsValid = true;
            return result;
        }


        public static double CalculateBurnTimeSeconds(double propellantMassTons, double vacuumThrustKn, double vacuumIsp)
        {
            if (propellantMassTons <= 0.0 || vacuumThrustKn <= 0.0 || vacuumIsp <= 0.0)
                return 0.0;

            // mdot = F / (Isp * g0)
            double massFlowTonsPerSecond = vacuumThrustKn / (vacuumIsp * StandardGravity);
            if (massFlowTonsPerSecond <= 0.0 || double.IsNaN(massFlowTonsPerSecond) || double.IsInfinity(massFlowTonsPerSecond))
                return 0.0;

            // t = m / mdot = m * Isp * g0 / F
            return (propellantMassTons * vacuumIsp * StandardGravity) / vacuumThrustKn;
        }

        private static void BuildPropellantRequirements(StageSolution result, IList<PropellantSpec> allProps, double totalMass)
        {
            // KSP engine propellant ratios are resource-unit ratios. Resources marked
            // ignoreForIsp (for example intake/auxiliary resources) still belong to the
            // engine's required resource set, but they do not participate in the mass
            // flow used by the rocket-equation/Isp calculation.
            double massPerRatioUnit = allProps
                .Where(p => !p.IgnoreForIsp && p.DensityTonsPerUnit > 0.0)
                .Sum(p => p.Ratio * p.DensityTonsPerUnit);
            if (massPerRatioUnit <= 0.0) return;

            double scale = totalMass / massPerRatioUnit;
            foreach (var p in allProps)
            {
                double units = scale * p.Ratio;
                result.Propellants.Add(new PropellantRequirement
                {
                    ResourceName = p.ResourceName,
                    Ratio = p.Ratio,
                    IgnoreForIsp = p.IgnoreForIsp,
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
