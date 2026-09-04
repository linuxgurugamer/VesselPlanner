using System;
using System.Collections.Generic;
using System.Linq;
using EngineStagePlanner.Core;
using KSP.UI.Screens;

namespace EngineStagePlanner.KSP
{
    public static class EngineDatabase
    {
        public static List<EngineCandidate> ScanAvailableEngines()
        {
            var results = new List<EngineCandidate>();
            List<AvailablePart> availableParts = GetEditorFilteredParts();
            if (availableParts.Count == 0) return results;

            foreach (AvailablePart available in availableParts)
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

                        FloatCurve atmosphereCurve = engine.atmosphereCurve;
                        double ispMultiplier = engine.multIsp > 0f ? engine.multIsp : 1.0;
                        double vacuumThrust = engine.maxThrust;
                        try
                        {
                            double kspVacuumThrust = engine.MaxThrustOutputVac(false);
                            if (!double.IsNaN(kspVacuumThrust) && !double.IsInfinity(kspVacuumThrust) && kspVacuumThrust > 0.0)
                                vacuumThrust = kspVacuumThrust;
                        }
                        catch { }

                        var c = new EngineCandidate
                        {
                            PartName = available.name + (modules.Count > 1 ? ":" + moduleIndex : string.Empty),
                            DisplayName = available.title + (modules.Count > 1 ? " [mode " + (moduleIndex + 1) + "]" : string.Empty),
                            MassTons = prefab.mass,
                            Cost = available.cost,
                            MaxThrustVacuumKn = vacuumThrust,
                            VacuumIsp = atmosphereCurve != null ? atmosphereCurve.Evaluate(0f) * ispMultiplier : 0f,
                            SeaLevelIsp = atmosphereCurve != null ? atmosphereCurve.Evaluate(1f) * ispMultiplier : 0f,
                            IspCurveEvaluator = atmosphereCurve != null
                                ? new Func<double, double>(atm => atmosphereCurve.Evaluate((float)Math.Max(0.0, atm)) * ispMultiplier)
                                : null,
                            ThrustEvaluator = new Func<double, double, double, double>((atm, tempK, density) =>
                            {
                                try
                                {
                                    if (atm <= 1e-9)
                                        return engine.MaxThrustOutputVac(false);

                                    return engine.MaxThrustOutputAtm(
                                        false, false, (float)Math.Max(0.0, atm), tempK, Math.Max(0.0, density));
                                }
                                catch
                                {
                                    double vacIsp = atmosphereCurve != null ? atmosphereCurve.Evaluate(0f) * ispMultiplier : 0.0;
                                    double envIsp = atmosphereCurve != null ? atmosphereCurve.Evaluate((float)Math.Max(0.0, atm)) * ispMultiplier : 0.0;
                                    return vacIsp > 0.0 ? vacuumThrust * envIsp / vacIsp : vacuumThrust;
                                }
                            }),
                            TopNodeSize = GetTopNodeSize(prefab)
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


        /// <summary>
        /// Returns the loaded parts after KSP's editor exclusion filters have been applied.
        /// Third-party mods can add predicates to EditorPartList.ExcludeFilters, so using
        /// this list makes Engine Stage Planner honor parts those mods hide from the editor.
        ///
        /// CategorizerFilters are intentionally not applied here: those represent the
        /// category currently selected in the stock parts palette and should not restrict
        /// the planner to whichever category happens to be open.
        /// </summary>
        private static List<AvailablePart> GetEditorFilteredParts()
        {
            var parts = PartLoader.LoadedPartsList == null
                ? new List<AvailablePart>()
                : PartLoader.LoadedPartsList.Where(p => p != null).ToList();

            try
            {
                EditorPartList editorPartList = EditorPartList.Instance;
                if (editorPartList != null && editorPartList.ExcludeFilters != null)
                {
                    parts = editorPartList.ExcludeFilters.GetFilteredList(parts);
                }
            }
            catch (Exception ex)
            {
                // Failing open is preferable to losing the entire candidate list if a
                // third-party editor filter throws an exception.
                UnityEngine.Debug.LogWarning("[EngineStagePlanner] Unable to apply editor exclusion filters: " + ex.Message);
            }

            return parts;
        }

        private static int GetTopNodeSize(Part part)
        {
            if (part == null) return -1;
            try
            {
                AttachNode top = part.FindAttachNode("top");
                return top != null ? top.size : -1;
            }
            catch
            {
                return -1;
            }
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
