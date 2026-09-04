using System;
using System.Collections.Generic;
using System.Linq;

namespace EngineStagePlanner.Core
{
    public static class TankPlanner
    {
        public static List<TankSuggestion> Suggest(IEnumerable<TankCandidate> tanks, StageSolution solution)
        {
            var results = new List<TankSuggestion>();
            if (solution == null || solution.Propellants.Count == 0) return results;

            foreach (TankCandidate tank in tanks)
            {
                int count = 1;
                bool compatible = true;

                foreach (PropellantRequirement req in solution.Propellants)
                {
                    TankResourceCapacity cap = tank.Resources.FirstOrDefault(r =>
                        string.Equals(r.ResourceName, req.ResourceName, StringComparison.OrdinalIgnoreCase));
                    if (cap == null || cap.Units <= 0.0)
                    {
                        compatible = false;
                        break;
                    }
                    count = Math.Max(count, (int)Math.Ceiling(req.Units / cap.Units));
                }
                if (!compatible || count <= 0) continue;

                var suggestion = new TankSuggestion
                {
                    Tank = tank,
                    Count = count,
                    TotalDryMassTons = tank.DryMassTons * count,
                    TotalCost = tank.Cost * count
                };

                double requestedTotal = 0.0;
                double excessTotal = 0.0;
                foreach (PropellantRequirement req in solution.Propellants)
                {
                    TankResourceCapacity cap = tank.Resources.First(r =>
                        string.Equals(r.ResourceName, req.ResourceName, StringComparison.OrdinalIgnoreCase));
                    double providedUnits = cap.Units * count;
                    double providedMass = providedUnits * cap.DensityTonsPerUnit;
                    requestedTotal += req.Units;
                    excessTotal += Math.Max(0.0, providedUnits - req.Units);
                    suggestion.Provided.Add(new PropellantRequirement
                    {
                        ResourceName = req.ResourceName,
                        Units = providedUnits,
                        MassTons = providedMass,
                        VolumeLiters = 0.0
                    });
                }
                suggestion.ExcessFraction = requestedTotal > 0.0 ? excessTotal / requestedTotal : 0.0;
                results.Add(suggestion);
            }

            return results
                .OrderBy(s => s.TotalDryMassTons)
                .ThenBy(s => s.ExcessFraction)
                .ThenBy(s => s.Count)
                .ToList();
        }
    }
}
