using System;
using System.Collections.Generic;
using System.Linq;
using EngineStagePlanner.Core;

namespace EngineStagePlanner.KSP
{
    public static class EditorStageScanner
    {
        public static int MaxStage
        {
            get
            {
                if (EditorLogic.fetch == null || EditorLogic.fetch.ship == null || EditorLogic.fetch.ship.parts == null) return 0;
                return Math.Max(0, EditorLogic.fetch.ship.parts.Where(p => p != null).Select(p => p.inverseStage).DefaultIfEmpty(0).Max());
            }
        }

        public static int ResolveStageForPart(Part clickedPart)
        {
            if (clickedPart == null || EditorLogic.fetch == null || EditorLogic.fetch.ship == null || EditorLogic.fetch.ship.parts == null)
                return -1;

            List<Part> parts = EditorLogic.fetch.ship.parts;
            if (!parts.Contains(clickedPart)) return -1;

            // Engines and decouplers have an explicit staging-display assignment; use it directly.
            if ((clickedPart.FindModuleImplementing<ModuleEngines>() != null || IsDecoupler(clickedPart)) && clickedPart.inverseStage >= 0)
                return clickedPart.inverseStage;

            // Tanks and structural parts normally do not have a useful activation stage of their own.
            // Resolve them with the same engine-to-decoupler branch rule used by Scan(), so clicking
            // any part that belongs to a propulsion stage selects that stage.
            foreach (Part enginePart in parts.Where(p => p != null && p.inverseStage >= 0 && p.FindModuleImplementing<ModuleEngines>() != null))
            {
                Part branchPart = enginePart;
                while (branchPart != null)
                {
                    if (branchPart == clickedPart) return enginePart.inverseStage;
                    if (branchPart != enginePart && IsDecoupler(branchPart)) break;
                    branchPart = branchPart.parent;
                }
            }

            // Fall back to the value KSP shows in the staging display for this part.
            return clickedPart.inverseStage;
        }

        public static ExistingStageSnapshot Scan(int stage)
        {
            var snap = new ExistingStageSnapshot { StageNumber = stage, InferredTankDryRatio = 0.125 };
            if (EditorLogic.fetch == null || EditorLogic.fetch.ship == null) return snap;
            List<Part> parts = EditorLogic.fetch.ship.parts;
            if (parts == null || parts.Count == 0) return snap;

            // Full editor craft masses.
            foreach (Part p in parts)
            {
                if (p == null) continue;
                double dry = GetDryPartMass(p);
                double res = GetResourceMass(p);
                snap.VesselDryMassTons += dry;
                snap.VesselWetMassTons += dry + res;
            }

            // KSP does not assign tanks to activation stages directly. We therefore identify the
            // stage's propulsion resources by starting from engines activated in the selected
            // inverseStage and walking their parent branches up to the next decoupler boundary.
            var stageEngines = parts.Where(p => p != null && p.inverseStage == stage && p.FindModuleImplementing<ModuleEngines>() != null).ToList();
            var stageParts = new HashSet<Part>();
            foreach (Part enginePart in stageEngines)
            {
                string engineTitle = enginePart.partInfo != null ? enginePart.partInfo.title : enginePart.name;
                string enginePartName = enginePart.partInfo != null ? enginePart.partInfo.name : enginePart.name;
                snap.CurrentEngines.Add(engineTitle);
                if (!string.IsNullOrEmpty(enginePartName))
                    snap.CurrentEnginePartNames.Add(enginePartName);
                AddCurrentEngineDetails(snap, enginePart, engineTitle);
                AddTopNodeSize(snap.TopNodeSizes, enginePart);
                AddBranchUntilDecoupler(enginePart, stageParts);
            }

            // Fallback for unusual staging/modded craft: use parts whose inverseStage matches.
            if (stageParts.Count == 0)
                foreach (Part p in parts.Where(p => p != null && p.inverseStage == stage)) stageParts.Add(p);

            double tankDry = 0.0;
            foreach (Part p in stageParts)
            {
                if (p.partInfo != null)
                    AddBulkheadProfiles(snap.BulkheadProfiles, p.partInfo.bulkheadProfiles);

                bool hasMassResource = false;
                foreach (PartResource r in p.Resources)
                {
                    if (r == null || r.info == null || r.maxAmount <= 0.0) continue;
                    if (r.info.density > 0.0) hasMassResource = true;
                    var existing = snap.Resources.FirstOrDefault(x => string.Equals(x.Name, r.resourceName, StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        existing = new ExistingResource { Name = r.resourceName, Density = r.info.density };
                        snap.Resources.Add(existing);
                    }
                    existing.Amount += r.amount;
                    existing.Capacity += r.maxAmount;
                    double liters = r.maxAmount * KspResourceVolume.GetLitersPerUnit(r.info);
                    existing.VolumeLiters += liters;
                    snap.StageTankCapacityUnits += r.maxAmount;
                    snap.StageTankVolumeLiters += liters;
                    snap.StagePropellantMassTons += r.amount * r.info.density;
                    snap.StagePropellantCapacityMassTons += r.maxAmount * r.info.density;
                }
                double partDry = GetDryPartMass(p);
                if (p.FindModuleImplementing<ModuleEngines>() != null)
                    snap.CurrentEngineMassTons += partDry;
                else
                    snap.StageNonEngineDryMassTons += partDry;
                if (hasMassResource) tankDry += partDry;
            }

            // Tank structural ratio is a property of tank capacity, not of how full the tank
            // happens to be in the editor. Using current loaded resource mass made Planning
            // overestimate tank dry mass for partially filled stages, which also depressed TWR.
            if (snap.StagePropellantCapacityMassTons > 1e-9 && tankDry > 0.0)
                snap.InferredTankDryRatio = Math.Max(0.01, Math.Min(1.0, tankDry / snap.StagePropellantCapacityMassTons));

            // Payload above this stage: parts with lower inverseStage values are normally retained
            // after this stage burns. This is an editor heuristic and is shown as such in the UI.
            // Do not count parts already included in stageParts again. Tank/structural parts often
            // have inverseStage 0 even when they belong physically to this stage; the old test
            // double-counted those parts (and their fuel), which inflated m0/m1 and produced
            // delta-v values that could differ dramatically from KSP/MechJeb.
            snap.PayloadAboveStageMassTons = parts.Where(p => p != null && !stageParts.Contains(p) && p.inverseStage < stage)
                .Sum(p => GetDryPartMass(p) + GetResourceMass(p));

            // Prefer KSP's own stage mass boundaries when the stock delta-v simulator can
            // provide them.  Its part/fuel-flow staging model correctly accounts for retained
            // payload, radial assemblies, fuel lines/crossfeed and separation behavior that
            // cannot be reconstructed reliably from inverseStage alone.
            TryApplyStockStageMasses(snap, EditorLogic.fetch.ship, stage, stageEngines);

            return snap;
        }

        private static void TryApplyStockStageMasses(ExistingStageSnapshot snap, ShipConstruct ship, int stage, IEnumerable<Part> selectedEngineParts)
        {
            if (snap == null || ship == null) return;
            try
            {
                // The editor ShipConstruct normally already owns the stock VesselDeltaV
                // instance. Reuse it so we read the same completed simulation that drives
                // KSP's staging delta-v display; only create one if the ship does not have it.
                VesselDeltaV vesselDeltaV = ship.vesselDeltaV ?? VesselDeltaV.Create(ship);
                if (vesselDeltaV == null) return;

                DeltaVStageInfo info = vesselDeltaV.GetStage(stage);
                if (info == null) return;

                double start = info.startMass;
                double end = info.endMass;
                double stageMass = info.stageMass;
                double stageDryMass = info.dryMass;
                double stageFuelMass = info.fuelMass;

                if (start <= 0.0 || end <= 0.0 || start <= end) return;

                // Remove the engines from the SAME KSP stage model that supplied
                // startMass/stageMass/endMass.  Using the planner's independently scanned
                // engine set here can disagree with DeltaVStageInfo and leave the installed
                // engine inside the stock mass before the candidate engine is added.
                // DeltaVStageInfo.enginesInStage is authoritative for this stage.
                // Count each physical engine part only once (important for multimode engines).
                double stockCurrentEngineMass = 0.0;
                var countedStockEngineParts = new HashSet<Part>();
                if (info.enginesInStage != null)
                {
                    foreach (DeltaVEngineInfo engineInfo in info.enginesInStage)
                    {
                        if (engineInfo == null || engineInfo.partInfo == null || engineInfo.partInfo.part == null) continue;
                        Part enginePart = engineInfo.partInfo.part;
                        if (!countedStockEngineParts.Add(enginePart)) continue;
                        stockCurrentEngineMass += Math.Max(0.0, engineInfo.partInfo.dryMass);
                    }
                }

                // If KSP has not populated enginesInStage yet, fall back to matching the
                // scanner's selected engine parts against KSP's DeltaVPartInfo list.
                var selectedEngineSet = new HashSet<Part>((selectedEngineParts ?? Enumerable.Empty<Part>()).Where(p => p != null));
                if (stockCurrentEngineMass <= 0.0 && selectedEngineSet.Count > 0 && vesselDeltaV.PartInfo != null)
                {
                    var countedEngineParts = new HashSet<Part>();
                    foreach (DeltaVPartInfo partInfo in vesselDeltaV.PartInfo)
                    {
                        if (partInfo == null || partInfo.part == null) continue;
                        if (!selectedEngineSet.Contains(partInfo.part) || !countedEngineParts.Add(partInfo.part)) continue;
                        stockCurrentEngineMass += Math.Max(0.0, partInfo.dryMass);
                    }
                }

                // Last-resort fallback if the stock DeltaV part data is not ready yet.
                if (stockCurrentEngineMass <= 0.0 && selectedEngineSet.Count > 0)
                    stockCurrentEngineMass = selectedEngineSet.Sum(GetDryPartMass);

                snap.StockCurrentEngineMassTons = Math.Max(0.0, stockCurrentEngineMass);
                if (snap.StockCurrentEngineMassTons > 0.0)
                    snap.CurrentEngineMassTons = snap.StockCurrentEngineMassTons;

                // DeltaVStageInfo already exposes the stage's own mass separately from the
                // full vehicle start/end masses.  Use those values directly instead of
                // reconstructing stage mass from the branch scanner.  The scanner still
                // supplies resource names/capacities for candidate compatibility, but it
                // no longer participates in the authoritative mass totals.
                if (stageMass <= 0.0 && stageDryMass >= 0.0 && stageFuelMass >= 0.0)
                    stageMass = stageDryMass + stageFuelMass;
                if (stageDryMass <= 0.0 && stageMass > 0.0 && stageFuelMass >= 0.0)
                    stageDryMass = Math.Max(0.0, stageMass - stageFuelMass);

                snap.HasStockStageMasses = true;
                snap.StockStageStartMassTons = start;
                snap.StockStageEndMassTons = end;
                snap.StockStageMassTons = Math.Max(0.0, stageMass);
                snap.StockStageDryMassTons = Math.Max(0.0, stageDryMass);
                snap.StockStageFuelMassTons = Math.Max(0.0, stageFuelMass);
                snap.StockStageBurnTimeSeconds = Math.Max(0.0, info.stageBurnTime);

                // Retained payload/upper-stage mass is the full start mass minus KSP's
                // selected-stage mass.  This assigns each kilogram to exactly one side
                // of the boundary and prevents tank structure/resources from appearing
                // in both the stage and payload totals.
                if (snap.StockStageMassTons > 0.0)
                {
                    double stockPayload = start - snap.StockStageMassTons;
                    if (stockPayload >= 0.0 && !double.IsNaN(stockPayload) && !double.IsInfinity(stockPayload))
                        snap.PayloadAboveStageMassTons = stockPayload;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[EngineStagePlanner] Stock stage-mass lookup failed; using fallback scanner: " + ex.Message);
            }
        }

        private static void AddCurrentEngineDetails(ExistingStageSnapshot snap, Part enginePart, string engineTitle)
        {
            if (snap == null || enginePart == null) return;
            try
            {
                var modules = enginePart.FindModulesImplementing<ModuleEngines>();
                if (modules == null || modules.Count == 0) return;

                int moduleIndex = 0;
                foreach (ModuleEngines engine in modules)
                {
                    if (engine == null || engine.maxThrust <= 0f) { moduleIndex++; continue; }
                    double vacuumIsp = engine.atmosphereCurve != null ? engine.atmosphereCurve.Evaluate(0f) : 0.0;
                    double seaLevelIsp = engine.atmosphereCurve != null ? engine.atmosphereCurve.Evaluate(1f) : 0.0;
                    double thrustLimit = Math.Max(0.0, Math.Min(1.0, engine.thrustPercentage / 100.0));
                    double vacuumThrust = EngineDatabase.GetConfiguredThrustKn(engine, 0.0, thrustLimit);
                    double seaLevelThrust = EngineDatabase.GetConfiguredThrustKn(engine, 1.0, thrustLimit);

                    snap.CurrentEngineDetails.Add(new ExistingEngineInfo
                    {
                        DisplayName = engineTitle + (modules.Count > 1 ? " [mode " + (moduleIndex + 1) + "]" : string.Empty),
                        SeaLevelThrustKn = seaLevelThrust,
                        VacuumThrustKn = vacuumThrust,
                        SeaLevelIsp = seaLevelIsp,
                        VacuumIsp = vacuumIsp
                    });
                    moduleIndex++;
                }
            }
            catch { }
        }

        private static void AddTopNodeSize(List<int> output, Part part)
        {
            if (output == null || part == null) return;
            try
            {
                AttachNode top = part.FindAttachNode("top");
                if (top != null && top.size >= 0 && !output.Contains(top.size))
                    output.Add(top.size);
            }
            catch { }
        }

        private static void AddBulkheadProfiles(List<string> output, string profiles)
        {
            if (output == null || string.IsNullOrEmpty(profiles)) return;
            foreach (string raw in profiles.Split(','))
            {
                string profile = raw.Trim();
                if (profile.Length == 0 || string.Equals(profile, "srf", StringComparison.OrdinalIgnoreCase)) continue;
                if (!output.Any(x => string.Equals(x, profile, StringComparison.OrdinalIgnoreCase)))
                    output.Add(profile);
            }
        }

        private static void AddBranchUntilDecoupler(Part start, HashSet<Part> output)
        {
            Part p = start;
            while (p != null && output.Add(p))
            {
                if (p != start && IsDecoupler(p)) break;
                p = p.parent;
            }
        }

        private static bool IsDecoupler(Part p)
        {
            return p.FindModuleImplementing<ModuleDecouple>() != null || p.FindModuleImplementing<ModuleAnchoredDecoupler>() != null;
        }

        private static double GetResourceMass(Part p)
        {
            double mass = 0.0;
            foreach (PartResource r in p.Resources)
                if (r != null && r.info != null) mass += r.amount * r.info.density;
            return mass;
        }

        private static double GetDryPartMass(Part p)
        {
            // GetModuleMass is used by KSP for modules that alter part mass (e.g. variants).
            double mass = p.mass;
            try { mass += p.GetModuleMass(p.mass, ModifierStagingSituation.CURRENT); }
            catch { }
            return Math.Max(0.0, mass);
        }
    }
}
