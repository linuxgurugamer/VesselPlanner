using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using VesselPlanner.Core;

namespace VesselPlanner.KSP
{
    public static class TankDatabase
    {
        private const string B9PartSwitchAssemblyName = "B9PartSwitch";
        private const string B9PartSwitchModuleName = "ModuleB9PartSwitch";
        private const string B9TankTypeNodeName = "B9_TANK_TYPE";

        private sealed class B9TankTypeResource
        {
            public string Name;
            public double UnitsPerVolume;
        }

        private sealed class B9TankTypeDefinition
        {
            public string Name;
            public double TankMassPerVolume;
            public double TankCostPerVolume;
            public readonly List<B9TankTypeResource> Resources = new List<B9TankTypeResource>();
        }

        public static List<TankCandidate> ScanAvailableTanks()
        {
            var results = new List<TankCandidate>();
            if (PartLoader.LoadedPartsList == null) return results;

            bool b9Installed = IsB9PartSwitchInstalled();
            Dictionary<string, B9TankTypeDefinition> b9TankTypes = b9Installed
                ? LoadB9TankTypes()
                : new Dictionary<string, B9TankTypeDefinition>(StringComparer.OrdinalIgnoreCase);

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

                    // Keep the ordinary/default part candidate exactly as before when the
                    // prefab exposes resources directly.  B9 subtype candidates are added in
                    // addition to this entry, so the current/default configuration remains
                    // available even on switchable parts.
                    List<PartResource> resources = available.partPrefab.Resources
                        .Cast<PartResource>()
                        .Where(r => r != null && r.info != null && r.maxAmount > 0.0)
                        .ToList();
                    if (resources.Count > 0)
                        results.Add(CreateDefaultTankCandidate(available, resources));

                    // A B9 switchable tank can have a structural/default prefab with no
                    // resources at all.  Therefore subtype scanning is intentionally not
                    // gated by the prefab resource list above.
                    if (b9Installed && b9TankTypes.Count > 0)
                        AddB9SubtypeCandidates(results, available, b9TankTypes);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[VesselPlanner] Tank scan failed for " + (available != null ? available.name : "?") + ": " + ex.Message);
                }
            }

            return results
                .OrderBy(t => t.DisplayName)
                .ThenBy(t => t.IdentityKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static TankCandidate CreateDefaultTankCandidate(AvailablePart available, IEnumerable<PartResource> resources)
        {
            var tank = new TankCandidate
            {
                PartName = available.name,
                PartUrl = available.partUrl,
                DisplayName = LocalizedPartTitle(available),
                DryMassTons = CommonRoutines.GetDryPartMass(available.partPrefab),
                Cost = available.cost,
                SubtypeMod = string.Empty,
                SubtypeModuleId = string.Empty,
                SubtypeName = string.Empty,
                SubtypeTitle = string.Empty,
                TankType = string.Empty,
                AddedMass = 0.0,
                AddedCost = 0.0,
                TankVolume = 0.0
            };
            CommonRoutines.AddBulkheadProfiles(tank.BulkheadProfiles, available.bulkheadProfiles);

            foreach (PartResource resource in resources)
            {
                if (resource == null || resource.info == null || resource.maxAmount <= 0.0) continue;
                tank.Resources.Add(new TankResourceCapacity
                {
                    ResourceName = resource.resourceName,
                    Units = resource.maxAmount,
                    DensityTonsPerUnit = resource.info.density,
                    LitersPerUnit = KspResourceVolume.GetLitersPerUnit(resource.info)
                });
            }
            return tank;
        }

        private static void AddB9SubtypeCandidates(List<TankCandidate> results, AvailablePart available,
            IDictionary<string, B9TankTypeDefinition> tankTypes)
        {
            if (results == null || available == null || available.partPrefab == null || available.partConfig == null) return;

            string baseTitle = LocalizedPartTitle(available);
            foreach (ConfigNode moduleNode in available.partConfig.GetNodes("MODULE"))
            {
                if (moduleNode == null) continue;
                if (!string.Equals(CommonRoutines.ReadString(moduleNode, "name", string.Empty),
                    B9PartSwitchModuleName, StringComparison.OrdinalIgnoreCase))
                    continue;

                string moduleId = CommonRoutines.ReadString(moduleNode, "moduleID", string.Empty).Trim();
                double baseVolume = Math.Max(0.0, CommonRoutines.ReadDouble(moduleNode, "baseVolume", 0.0));
                foreach (ConfigNode subtypeNode in moduleNode.GetNodes("SUBTYPE"))
                {
                    if (subtypeNode == null) continue;

                    string subtypeName = CommonRoutines.ReadString(subtypeNode, "name", string.Empty).Trim();
                    if (subtypeName.Length == 0) continue;

                    // B9PartSwitch uses its hard-coded Structural tank type when tankType is
                    // omitted.  Supporting that here also allows SUBTYPE-level RESOURCE nodes
                    // to define useful storage without a named B9_TANK_TYPE.
                    string tankTypeName = CommonRoutines.ReadString(subtypeNode, "tankType", "Structural").Trim();
                    if (tankTypeName.Length == 0) tankTypeName = "Structural";

                    B9TankTypeDefinition tankType;
                    if (!tankTypes.TryGetValue(tankTypeName, out tankType) || tankType == null)
                    {
                        if (string.Equals(tankTypeName, "Structural", StringComparison.OrdinalIgnoreCase))
                        {
                            tankType = new B9TankTypeDefinition
                            {
                                Name = "Structural",
                                TankMassPerVolume = 0.0,
                                TankCostPerVolume = 0.0
                            };
                        }
                        else
                        {
                            continue;
                        }
                    }

                    double volumeMultiplier = CommonRoutines.ReadDouble(subtypeNode, "volumeMultiplier", 1.0);
                    double volumeAdded = CommonRoutines.ReadDouble(subtypeNode, "volumeAdded", 0.0);
                    double volume = Math.Max(0.0, baseVolume * volumeMultiplier + volumeAdded);
                    if (volume <= 0.0) continue;

                    string rawSubtypeTitle = CommonRoutines.ReadString(subtypeNode, "title", subtypeName);
                    string subtypeTitle = Localize(rawSubtypeTitle, subtypeName);
                    double addedMass = CommonRoutines.ReadDouble(subtypeNode, "addedMass", 0.0);
                    double addedCost = CommonRoutines.ReadDouble(subtypeNode, "addedCost", 0.0);

                    var tank = new TankCandidate
                    {
                        PartName = available.name,
                        PartUrl = available.partUrl,
                        DisplayName = baseTitle + " [B9PartSwitch: " + subtypeTitle + "]",
                        // B9PartSwitch tank mass/cost values are expressed per unit of tank
                        // volume. Subtype addedMass/addedCost are applied on top of the base
                        // part values. Resource mass is kept separate in Resources.
                        DryMassTons = Math.Max(0.0,
                            available.partPrefab.mass + addedMass + tankType.TankMassPerVolume * volume),
                        Cost = Math.Max(0.0,
                            available.cost + addedCost + tankType.TankCostPerVolume * volume),
                        SubtypeMod = B9PartSwitchAssemblyName,
                        SubtypeModuleId = moduleId,
                        SubtypeName = subtypeName,
                        SubtypeTitle = subtypeTitle,
                        TankType = tankTypeName,
                        AddedMass = addedMass,
                        AddedCost = addedCost,
                        TankVolume = volume
                    };
                    CommonRoutines.AddBulkheadProfiles(tank.BulkheadProfiles, available.bulkheadProfiles);

                    foreach (B9TankTypeResource resource in tankType.Resources)
                        SetB9ResourceCapacity(tank, resource == null ? null : resource.Name,
                            resource == null ? 0.0 : resource.UnitsPerVolume, volume);

                    // B9PartSwitch SUBTYPE-level RESOURCE nodes override the matching
                    // B9_TANK_TYPE resource and add new resources when no match exists.
                    foreach (ConfigNode resourceNode in subtypeNode.GetNodes("RESOURCE"))
                    {
                        if (resourceNode == null) continue;
                        string resourceName = CommonRoutines.ReadString(resourceNode, "name", string.Empty).Trim();
                        if (resourceName.Length == 0 || !resourceNode.HasValue("unitsPerVolume")) continue;
                        double unitsPerVolume = CommonRoutines.ReadDouble(resourceNode, "unitsPerVolume", 0.0);
                        SetB9ResourceCapacity(tank, resourceName, unitsPerVolume, volume);
                    }

                    if (tank.Resources.Count > 0)
                        results.Add(tank);
                }
            }
        }

        private static void SetB9ResourceCapacity(TankCandidate tank, string resourceName,
            double unitsPerVolume, double volume)
        {
            if (tank == null || string.IsNullOrWhiteSpace(resourceName) || volume <= 0.0) return;

            TankResourceCapacity existing = tank.Resources.FirstOrDefault(r => r != null &&
                string.Equals(r.ResourceName, resourceName, StringComparison.OrdinalIgnoreCase));

            // An explicit zero unitsPerVolume on a subtype removes/zeros that inherited
            // resource rather than adding another copy of it.
            if (unitsPerVolume <= 0.0)
            {
                if (existing != null) tank.Resources.Remove(existing);
                return;
            }

            PartResourceDefinition definition = null;
            try
            {
                if (PartResourceLibrary.Instance != null)
                    definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            }
            catch { }
            if (definition == null) return;

            double units = unitsPerVolume * volume;
            if (units <= 0.0) return;

            if (existing != null)
            {
                existing.Units = units;
                existing.DensityTonsPerUnit = definition.density;
                existing.LitersPerUnit = KspResourceVolume.GetLitersPerUnit(definition);
                return;
            }

            tank.Resources.Add(new TankResourceCapacity
            {
                ResourceName = resourceName,
                Units = units,
                DensityTonsPerUnit = definition.density,
                LitersPerUnit = KspResourceVolume.GetLitersPerUnit(definition)
            });
        }

        private static Dictionary<string, B9TankTypeDefinition> LoadB9TankTypes()
        {
            var result = new Dictionary<string, B9TankTypeDefinition>(StringComparer.OrdinalIgnoreCase);
            try
            {
                ConfigNode[] nodes = GameDatabase.Instance == null
                    ? null
                    : GameDatabase.Instance.GetConfigNodes(B9TankTypeNodeName);
                if (nodes == null) return result;

                foreach (ConfigNode node in nodes)
                {
                    if (node == null) continue;
                    string name = CommonRoutines.ReadString(node, "name", string.Empty).Trim();
                    if (name.Length == 0) continue;

                    var definition = new B9TankTypeDefinition
                    {
                        Name = name,
                        TankMassPerVolume = CommonRoutines.ReadDouble(node, "tankMass", 0.0),
                        TankCostPerVolume = CommonRoutines.ReadDouble(node, "tankCost", 0.0)
                    };

                    foreach (ConfigNode resourceNode in node.GetNodes("RESOURCE"))
                    {
                        if (resourceNode == null) continue;
                        string resourceName = CommonRoutines.ReadString(resourceNode, "name", string.Empty).Trim();
                        double unitsPerVolume = CommonRoutines.ReadDouble(resourceNode, "unitsPerVolume", 0.0);
                        if (resourceName.Length == 0 || unitsPerVolume <= 0.0) continue;
                        definition.Resources.Add(new B9TankTypeResource
                        {
                            Name = resourceName,
                            UnitsPerVolume = unitsPerVolume
                        });
                    }

                    // B9PartSwitch treats tank type names as unique. Keep the first
                    // loaded definition if a malformed install leaves a duplicate behind.
                    if (!result.ContainsKey(name)) result.Add(name, definition);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[VesselPlanner] Unable to read B9PartSwitch tank types: " + ex.Message);
            }
            return result;
        }

        private static bool IsB9PartSwitchInstalled()
        {
            try
            {
                return AssemblyLoader.loadedAssemblies != null &&
                    AssemblyLoader.loadedAssemblies.Any(a => a != null && a.assembly != null &&
                        string.Equals(a.assembly.GetName().Name, B9PartSwitchAssemblyName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        private static string LocalizedPartTitle(AvailablePart available)
        {
            if (available == null) return string.Empty;
            string title = available.title ?? string.Empty;
            return Localize(title, string.IsNullOrEmpty(title) ? (available.name ?? string.Empty) : title);
        }

        private static string Localize(string text, string fallback)
        {
            string value = string.IsNullOrEmpty(text) ? (fallback ?? string.Empty) : text;
            try
            {
                string localized = Localizer.Format(value);
                if (!string.IsNullOrEmpty(localized)) return localized;
            }
            catch { }
            return value;
        }
    }
}
