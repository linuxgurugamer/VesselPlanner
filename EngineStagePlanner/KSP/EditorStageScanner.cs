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
                snap.CurrentEngines.Add(enginePart.partInfo != null ? enginePart.partInfo.title : enginePart.name);
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
                    if (r == null || r.info == null || r.maxAmount <= 0.0 || r.info.density <= 0.0) continue;
                    hasMassResource = true;
                    var existing = snap.Resources.FirstOrDefault(x => string.Equals(x.Name, r.resourceName, StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        existing = new ExistingResource { Name = r.resourceName, Density = r.info.density };
                        snap.Resources.Add(existing);
                    }
                    existing.Amount += r.amount;
                    existing.Capacity += r.maxAmount;
                    double liters = r.maxAmount * KspResourceVolume.GetLitersPerUnit(r.resourceName);
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

            return snap;
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
