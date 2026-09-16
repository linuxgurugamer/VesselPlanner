using System;
using System.Collections.Generic;
using System.Linq;
using VesselPlanner.Core;

namespace VesselPlanner.KSP
{
    public static class TankDatabase
    {
        public static List<TankCandidate> ScanAvailableTanks()
        {
            var results = new List<TankCandidate>();
            if (PartLoader.LoadedPartsList == null) return results;

            foreach (AvailablePart available in PartLoader.LoadedPartsList)
            {
                try
                {
                    if (available == null || available.partPrefab == null) continue;
                    if (!CommonRoutines.IsAvailableToPlayer(available)) continue;
                    if (available.partPrefab.FindModuleImplementing<ModuleEngines>() != null) continue;

                    // Only offer stackable tanks that can connect on both ends. Radial tanks
                    // and other storage parts missing either node are not suitable for the
                    // Stage-by-Stage tank-selection workflow.
                    if (available.partPrefab.FindAttachNode("top") == null ||
                        available.partPrefab.FindAttachNode("bottom") == null)
                        continue;

                    var resources = available.partPrefab.Resources
                        .Cast<PartResource>()
                        .Where(r => r != null && r.info != null && r.maxAmount > 0.0)
                        .ToList();
                    if (resources.Count == 0) continue;

                    var tank = new TankCandidate
                    {
                        PartName = available.name,
                        PartUrl = available.partUrl,
                        DisplayName = available.title,
                        DryMassTons = CommonRoutines.GetDryPartMass(available.partPrefab),
                        Cost = available.cost
                    };
                    CommonRoutines.AddBulkheadProfiles(tank.BulkheadProfiles, available.bulkheadProfiles);

                    foreach (PartResource r in resources)
                    {
                        tank.Resources.Add(new TankResourceCapacity
                        {
                            ResourceName = r.resourceName,
                            Units = r.maxAmount,
                            DensityTonsPerUnit = r.info.density,
                            LitersPerUnit = KspResourceVolume.GetLitersPerUnit(r.info)
                        });
                    }
                    results.Add(tank);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[VesselPlanner] Tank scan failed for " + (available != null ? available.name : "?") + ": " + ex.Message);
                }
            }

            return results.OrderBy(t => t.DisplayName).ToList();
        }
    }
}
