using System;
using System.Collections.Generic;
using System.Linq;
using EngineStagePlanner.Core;

namespace EngineStagePlanner.KSP
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
                    if (!IsAvailableToPlayer(available)) continue;
                    if (available.partPrefab.FindModuleImplementing<ModuleEngines>() != null) continue;

                    var resources = available.partPrefab.Resources
                        .Cast<PartResource>()
                        .Where(r => r != null && r.info != null && r.maxAmount > 0.0)
                        .ToList();
                    if (resources.Count == 0) continue;

                    var tank = new TankCandidate
                    {
                        PartName = available.name,
                        DisplayName = available.title,
                        DryMassTons = GetDryPrefabMass(available.partPrefab),
                        Cost = available.cost
                    };

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
                    UnityEngine.Debug.LogWarning("[EngineStagePlanner] Tank scan failed for " + (available != null ? available.name : "?") + ": " + ex.Message);
                }
            }

            return results.OrderBy(t => t.DisplayName).ToList();
        }

        private static double GetDryPrefabMass(Part p)
        {
            double mass = p.mass;
            try { mass += p.GetModuleMass(p.mass, ModifierStagingSituation.CURRENT); }
            catch { }
            return Math.Max(0.0, mass);
        }

        private static bool IsAvailableToPlayer(AvailablePart part)
        {
            if (part.category == PartCategories.none) return false;
            if (HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) return true;
            if (ResearchAndDevelopment.Instance == null) return true;
            return ResearchAndDevelopment.PartModelPurchased(part);
        }
    }
}
