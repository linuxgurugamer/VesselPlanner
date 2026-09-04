using System.Collections.Generic;
using System.Linq;

namespace EngineStagePlanner.Core
{
    public static class PlannerEngine
    {
        public static List<StageSolution> Calculate(IEnumerable<EngineCandidate> engines, StageRequirements req, OptimizationMode mode)
        {
            var results = new List<StageSolution>();
            foreach (var engine in engines)
            {
                for (int count = 1; count <= req.MaxEngineCount; count++)
                {
                    var solution = StageSolver.Solve(req, engine, count);
                    if (solution.IsValid)
                    {
                        results.Add(solution);
                        // For a given engine, more engines generally only add mass once min TWR is met.
                        // Keep testing only for HighestTwr/ShortestBurn; otherwise retain the lightest count.
                        if (mode != OptimizationMode.HighestTwr && mode != OptimizationMode.ShortestBurn)
                            break;
                    }
                }
            }

            switch (mode)
            {
                case OptimizationMode.LowestPropellantMass: return results.OrderBy(r => r.PropellantMassTons).ThenBy(r => r.WetMassTons).ToList();
                case OptimizationMode.LowestCost: return results.OrderBy(r => r.EngineCost).ThenBy(r => r.WetMassTons).ToList();
                case OptimizationMode.ShortestBurn: return results.OrderBy(r => r.BurnTimeSeconds).ThenBy(r => r.WetMassTons).ToList();
                case OptimizationMode.HighestTwr: return results.OrderByDescending(r => r.InitialTwr).ThenBy(r => r.WetMassTons).ToList();
                case OptimizationMode.HighestIsp: return results.OrderByDescending(r => r.Isp).ThenBy(r => r.WetMassTons).ToList();
                default: return results.OrderBy(r => r.WetMassTons).ThenBy(r => r.EngineCount).ToList();
            }
        }
    }
}
