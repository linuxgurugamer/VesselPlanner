using System;
using System.Collections.Generic;
using System.Linq;
using EngineStagePlanner.Core;

namespace EngineStagePlanner.KSP
{
    public static class EngineDatabase
    {
        public static List<EngineCandidate> ScanAvailableEngines()
        {
            var results = new List<EngineCandidate>();
            if (PartLoader.LoadedPartsList == null) return results;

            foreach (AvailablePart available in PartLoader.LoadedPartsList)
            {
                try
                {
                    if (available == null || available.partPrefab == null) continue;
                    if (!IsAvailableToPlayer(available)) continue;

                    Part prefab = available.partPrefab;
                    var modules = prefab.FindModulesImplementing<ModuleEngines>();
                    if (modules == null || modules.Count == 0) continue;

                    // Multi-mode parts can have multiple engine modules. Expose each distinct module as a candidate.
                    int moduleIndex = 0;
                    foreach (ModuleEngines engine in modules)
                    {
                        if (engine == null || engine.maxThrust <= 0f) { moduleIndex++; continue; }

                        var c = new EngineCandidate
                        {
                            PartName = available.name + (modules.Count > 1 ? ":" + moduleIndex : string.Empty),
                            DisplayName = available.title + (modules.Count > 1 ? " [mode " + (moduleIndex + 1) + "]" : string.Empty),
                            MassTons = prefab.mass,
                            Cost = available.cost,
                            MaxThrustVacuumKn = engine.maxThrust,
                            VacuumIsp = engine.atmosphereCurve != null ? engine.atmosphereCurve.Evaluate(0f) : 0f,
                            SeaLevelIsp = engine.atmosphereCurve != null ? engine.atmosphereCurve.Evaluate(1f) : 0f
                        };

                        AddBulkheadProfiles(c.BulkheadProfiles, available.bulkheadProfiles);

                        foreach (Propellant prop in engine.propellants)
                        {
                            if (prop == null || string.IsNullOrEmpty(prop.name) || prop.ratio <= 0f) continue;
                            PartResourceDefinition def = PartResourceLibrary.Instance.GetDefinition(prop.name);
                            if (def == null) continue;
                            c.Propellants.Add(new PropellantSpec
                            {
                                ResourceName = prop.name,
                                Ratio = prop.ratio,
                                DensityTonsPerUnit = def.density,
                                IgnoreForIsp = prop.ignoreForIsp,
                                LitersPerUnit = KspResourceVolume.GetLitersPerUnit(prop.name)
                            });
                        }

                        c.IsSolid = c.Propellants.Any(p => string.Equals(p.ResourceName, "SolidFuel", StringComparison.OrdinalIgnoreCase));
                        c.IsAirBreathing = c.Propellants.Any(p => p.ResourceName.IndexOf("IntakeAir", StringComparison.OrdinalIgnoreCase) >= 0);
                        c.IsElectric = c.Propellants.Any(p => p.ResourceName.IndexOf("ElectricCharge", StringComparison.OrdinalIgnoreCase) >= 0);

                        if (c.Propellants.Any(p => !p.IgnoreForIsp && p.DensityTonsPerUnit > 0.0))
                            results.Add(c);
                        moduleIndex++;
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[EngineStagePlanner] Engine scan failed for " + (available != null ? available.name : "?") + ": " + ex.Message);
                }
            }
            return results.OrderBy(e => e.DisplayName).ToList();
        }

        private static void AddBulkheadProfiles(List<string> output, string profiles)
        {
            if (output == null || string.IsNullOrEmpty(profiles)) return;
            foreach (string raw in profiles.Split(','))
            {
                string profile = raw.Trim();
                if (profile.Length == 0) continue;
                if (!output.Any(x => string.Equals(x, profile, StringComparison.OrdinalIgnoreCase)))
                    output.Add(profile);
            }
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
