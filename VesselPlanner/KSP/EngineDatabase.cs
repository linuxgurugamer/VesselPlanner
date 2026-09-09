using System;
using System.Collections.Generic;
using System.Linq;
using VesselPlanner.Core;
using KSP.UI.Screens;

namespace VesselPlanner.KSP
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
                        // atmosphereCurve already contains the engine's configured Isp values.
                        // Do not multiply it by multIsp here: GetEngineThrust() applies the module's
                        // own engine internals, and multiplying the curve first can double-apply a
                        // runtime/module multiplier on editor instances/prefabs.
                        double vacuumIsp = atmosphereCurve != null ? atmosphereCurve.Evaluate(0f) : 0.0;
                        double seaLevelIsp = atmosphereCurve != null ? atmosphereCurve.Evaluate(1f) : 0.0;

                        // Editor candidate parts are partPrefab objects, so do not call the runtime
                        // MaxThrustOutputVac/Atm methods.  Those methods depend on live engine state.
                        // GetEngineThrust(isp, throttle) is deterministic from the ModuleEngines
                        // configuration and is the same editor-side calculation used by established
                        // KSP part-info tooling.
                        double vacuumThrust = GetConfiguredThrustKn(engine, 0.0, 1.0);
                        double seaLevelThrust = GetConfiguredThrustKn(engine, 1.0, 1.0);

                        var propellantSpecs = new List<PropellantSpec>();
                        foreach (Propellant prop in engine.propellants)
                        {
                            if (prop == null || string.IsNullOrEmpty(prop.name) || prop.ratio <= 0f) continue;
                            PartResourceDefinition def = PartResourceLibrary.Instance.GetDefinition(prop.name);
                            if (def == null) continue;
                            propellantSpecs.Add(new PropellantSpec
                            {
                                ResourceName = prop.name,
                                Ratio = prop.ratio,
                                DensityTonsPerUnit = def.density,
                                IgnoreForIsp = prop.ignoreForIsp,
                                LitersPerUnit = KspResourceVolume.GetLitersPerUnit(def)
                            });
                        }

                        bool isSolid = propellantSpecs.Any(p => string.Equals(p.ResourceName, "SolidFuel", StringComparison.OrdinalIgnoreCase));
                        bool isAirBreathing = propellantSpecs.Any(p => p.ResourceName.IndexOf("IntakeAir", StringComparison.OrdinalIgnoreCase) >= 0);
                        bool isElectric = propellantSpecs.Any(p => p.ResourceName.IndexOf("ElectricCharge", StringComparison.OrdinalIgnoreCase) >= 0);

                        var c = new EngineCandidate
                        {
                            PartName = available.name + (modules.Count > 1 ? ":" + moduleIndex : string.Empty),
                            DisplayName = available.title + (modules.Count > 1 ? " [mode " + (moduleIndex + 1) + "]" : string.Empty),
                            MassTons = prefab.mass,
                            Cost = available.cost,
                            MaxThrustVacuumKn = vacuumThrust,
                            SeaLevelThrustKn = seaLevelThrust,
                            VacuumIsp = vacuumIsp,
                            SeaLevelIsp = seaLevelIsp,
                            IspCurveEvaluator = atmosphereCurve != null
                                ? new Func<double, double>(atm => atmosphereCurve.Evaluate((float)Math.Max(0.0, atm)))
                                : null,
                            ThrustEvaluator = new Func<double, double, double, double>((atm, tempK, density) =>
                                GetConfiguredThrustKn(engine, Math.Max(0.0, atm), 1.0)),
                            TopNodeSize = GetTopNodeSize(prefab),
                            IsSolid = isSolid,
                            IsAirBreathing = isAirBreathing,
                            IsElectric = isElectric
                        };

                        c.Propellants.AddRange(propellantSpecs);
                        AddBulkheadProfiles(c.BulkheadProfiles, available.bulkheadProfiles);

                        if (c.Propellants.Any(p => !p.IgnoreForIsp && p.DensityTonsPerUnit > 0.0))
                            results.Add(c);
                        moduleIndex++;
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[VesselPlanner] Engine scan failed for " + (available != null ? available.name : "?") + ": " + ex.Message);
                }
            }
            return results.OrderBy(e => e.DisplayName).ToList();
        }

        public static double StaticThrustFromIsp(double vacuumThrustKn, double vacuumIsp, double environmentIsp)
        {
            if (vacuumThrustKn <= 0.0 || vacuumIsp <= 0.0 || environmentIsp <= 0.0) return 0.0;
            return vacuumThrustKn * environmentIsp / vacuumIsp;
        }

        /// <summary>
        /// Calculates static full-throttle thrust at a pressure in atmospheres.
        /// For conventional rocket engines, KSP part configs define maxThrust as the
        /// configured vacuum/reference thrust; atmospheric thrust follows the Isp ratio.
        /// Prefab GetEngineThrust/MaxThrustOutput calls can depend on initialized flow state,
        /// so they are only used for atmosphere/velocity-flow engines that need those curves.
        /// </summary>
        public static double GetConfiguredThrustKn(ModuleEngines engine, double atmospheres, double thrustLimitFraction)
        {
            if (engine == null || engine.atmosphereCurve == null) return 0.0;

            double atm = Math.Max(0.0, atmospheres);
            double limit = Math.Max(0.0, Math.Min(1.0, thrustLimitFraction));
            if (limit <= 0.0) return 0.0;

            double vacuumIsp = engine.atmosphereCurve.Evaluate(0f);
            double environmentIsp = engine.atmosphereCurve.Evaluate((float)atm);
            if (vacuumIsp <= 0.0 || environmentIsp <= 0.0) return 0.0;

            double configuredVacuumThrust = Math.Max(0.0, engine.maxThrust) * limit;
            if (atm <= 1e-9) return configuredVacuumThrust;

            bool flowDependsOnAtmosphereOrSpeed = engine.atmChangeFlow || engine.useAtmCurve || engine.useVelCurve;
            if (!flowDependsOnAtmosphereOrSpeed)
                return StaticThrustFromIsp(configuredVacuumThrust, vacuumIsp, environmentIsp);

            try
            {
                double value = engine.GetEngineThrust((float)environmentIsp, (float)limit);
                if (engine.useVelCurve && engine.velCurve != null)
                    value *= engine.velCurve.Evaluate(0f);

                // A prefab calculation must remain in the same order of magnitude as the
                // engine's configured reference thrust.  If it does not, use the deterministic
                // config-based value instead of accepting an uninitialized flow field.
                double fallback = StaticThrustFromIsp(configuredVacuumThrust, vacuumIsp, environmentIsp);
                double upperBound = Math.Max(configuredVacuumThrust, fallback) * 4.0 + 1e-6;
                if (!double.IsNaN(value) && !double.IsInfinity(value) && value >= 0.0 && value <= upperBound)
                    return value;
            }
            catch { }

            return StaticThrustFromIsp(configuredVacuumThrust, vacuumIsp, environmentIsp);
        }

        public static double GetVacuumThrustKn(ModuleEngines engine, double vacuumIsp)
        {
            return GetConfiguredThrustKn(engine, 0.0, 1.0);
        }

        public static double GetSeaLevelThrustKn(ModuleEngines engine, double vacuumThrustKn, double vacuumIsp, double seaLevelIsp)
        {
            return GetConfiguredThrustKn(engine, 1.0, 1.0);
        }

        /// <summary>
        /// Returns the loaded parts after KSP's editor exclusion filters have been applied.
        /// Third-party mods can add predicates to EditorPartList.ExcludeFilters, so using
        /// this list makes VesselPlanner honor parts those mods hide from the editor.
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
                UnityEngine.Debug.LogWarning("[VesselPlanner] Unable to apply editor exclusion filters: " + ex.Message);
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
