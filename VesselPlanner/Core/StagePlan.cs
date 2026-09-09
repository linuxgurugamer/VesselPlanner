using System;
using System.Collections.Generic;
using System.Globalization;

namespace VesselPlanner.Core
{
    // One part chosen for a planned stage, with the number of copies the planner worked out.
    public sealed class PlannedPart
    {
        public string PartName { get; set; }
        public string DisplayName { get; set; }
        public int Quantity { get; set; }
        public bool IsEngine { get; set; }

        public PlannedPart Clone()
        {
            return new PlannedPart
            {
                PartName = PartName,
                DisplayName = DisplayName,
                Quantity = Quantity,
                IsEngine = IsEngine
            };
        }
    }

    // A stage in a stage-by-stage plan: the requirements it was solved against, and the
    // engines and tanks picked for it.
    public sealed class PlannedStage
    {
        public double TargetDeltaV { get; set; }
        public DeltaVBasis TargetDeltaVBasis { get; set; } = DeltaVBasis.Vacuum;
        public double MinimumTwr { get; set; }
        public int MaxEngineCount { get; set; } = 8;
        // Additional non-propellant dry mass carried by this stage. It is included in
        // the stage solve but is separate from the vessel payload above the stage.
        public double CargoMassTons { get; set; }
        // Optional fixed dry mass for the stage separator. The toggle is saved separately
        // so the mass can be recalculated from decouplerMasses.cfg if the bulkhead choice changes.
        public bool AddDecouplerMass { get; set; }
        public double DecouplerMassTons { get; set; }
        // KSP part bulkheadProfiles tokens the candidate engines and tanks are restricted
        // to (for example size1 or srf). Empty considers every profile: a plan is built
        // before the craft exists, so there is no stage bulkhead to match against.
        public List<string> BulkheadProfiles { get; } = new List<string>();
        public List<PlannedPart> Parts { get; } = new List<PlannedPart>();

        // Adding the same part twice accumulates rather than listing it again, so a stage
        // built up over several passes in the planner reads as one line per part.
        public void AddPart(string partName, string displayName, int quantity, bool isEngine)
        {
            if (string.IsNullOrEmpty(partName) || quantity <= 0) return;

            foreach (PlannedPart existing in Parts)
            {
                if (existing.PartName == partName && existing.IsEngine == isEngine)
                {
                    existing.Quantity += quantity;
                    return;
                }
            }

            Parts.Add(new PlannedPart
            {
                PartName = partName,
                DisplayName = string.IsNullOrEmpty(displayName) ? partName : displayName,
                Quantity = quantity,
                IsEngine = isEngine
            });
        }

        public string Summary
        {
            get
            {
                string basis = TargetDeltaVBasis == DeltaVBasis.Atmospheric ? "atm" : "vac";
                return TargetDeltaV.ToString("0", CultureInfo.InvariantCulture) + " m/s " + basis
                    + ", TWR " + MinimumTwr.ToString("0.##", CultureInfo.InvariantCulture)
                    + ", max " + MaxEngineCount.ToString(CultureInfo.InvariantCulture) + " engines"
                    + (CargoMassTons > 0.0 ? ", cargo " + CargoMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t" : "")
                    + (AddDecouplerMass && DecouplerMassTons > 0.0 ? ", decoupler " + DecouplerMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t" : "");
            }
        }
    }

    // A whole plan: the payload it lifts, the body it starts from, and its stages in the
    // order they were added.
    public sealed class StagePlan
    {
        public const string RootNodeName = "ENGINE_STAGE_PLAN";

        public string Name { get; set; } = "Plan";
        public string VesselName { get; set; } = "";
        public double PayloadMassTons { get; set; }
        public string BodyName { get; set; } = "";
        // Finalised plans no longer take new stages; their parts can be placed instead.
        public bool Finalized { get; set; }
        public List<PlannedStage> Stages { get; } = new List<PlannedStage>();

        public ConfigNode ToConfigNode()
        {
            var root = new ConfigNode(RootNodeName);
            root.AddValue("Name", Name ?? "");
            root.AddValue("VesselName", VesselName ?? "");
            root.AddValue("PayloadMassTons", PayloadMassTons.ToString("0.####", CultureInfo.InvariantCulture));
            root.AddValue("BodyName", BodyName ?? "");
            root.AddValue("Finalized", Finalized.ToString());

            foreach (PlannedStage stage in Stages)
            {
                ConfigNode stageNode = root.AddNode("STAGE");
                stageNode.AddValue("TargetDeltaV", stage.TargetDeltaV.ToString("0.####", CultureInfo.InvariantCulture));
                stageNode.AddValue("TargetDeltaVBasis", stage.TargetDeltaVBasis.ToString());
                stageNode.AddValue("MinimumTwr", stage.MinimumTwr.ToString("0.####", CultureInfo.InvariantCulture));
                stageNode.AddValue("MaxEngineCount", stage.MaxEngineCount.ToString(CultureInfo.InvariantCulture));
                stageNode.AddValue("CargoMassTons", stage.CargoMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                stageNode.AddValue("AddDecouplerMass", stage.AddDecouplerMass.ToString());
                stageNode.AddValue("DecouplerMassTons", stage.DecouplerMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                foreach (string profile in stage.BulkheadProfiles)
                {
                    if (string.IsNullOrEmpty(profile)) continue;
                    stageNode.AddValue("BulkheadProfile", profile);

                    // Keep writing the old numeric value for sizeN profiles so a plan can
                    // still be opened by VesselPlanner 0.6.10 and earlier after a downgrade.
                    int legacySize;
                    if (VesselPlanner.Core.BulkheadProfiles.TryGetSizeForProfile(profile, out legacySize))
                        stageNode.AddValue("BulkheadSize", legacySize.ToString(CultureInfo.InvariantCulture));
                }

                foreach (PlannedPart part in stage.Parts)
                {
                    ConfigNode partNode = stageNode.AddNode("PART_ENTRY");
                    partNode.AddValue("PartName", part.PartName ?? "");
                    partNode.AddValue("DisplayName", part.DisplayName ?? "");
                    partNode.AddValue("Quantity", part.Quantity.ToString(CultureInfo.InvariantCulture));
                    partNode.AddValue("IsEngine", part.IsEngine.ToString());
                }
            }

            return root;
        }

        public static StagePlan FromConfigNode(ConfigNode root)
        {
            if (root == null) return null;

            var plan = new StagePlan
            {
                Name = ReadString(root, "Name", "Plan"),
                VesselName = ReadString(root, "VesselName", ""),
                PayloadMassTons = ReadDouble(root, "PayloadMassTons", 0.0),
                BodyName = ReadString(root, "BodyName", ""),
                Finalized = ReadBool(root, "Finalized", false)
            };

            foreach (ConfigNode stageNode in root.GetNodes("STAGE"))
            {
                var stage = new PlannedStage
                {
                    TargetDeltaV = ReadDouble(stageNode, "TargetDeltaV", 0.0),
                    TargetDeltaVBasis = string.Equals(ReadString(stageNode, "TargetDeltaVBasis", "Vacuum"), "Atmospheric", StringComparison.OrdinalIgnoreCase)
                        ? DeltaVBasis.Atmospheric
                        : DeltaVBasis.Vacuum,
                    MinimumTwr = ReadDouble(stageNode, "MinimumTwr", 0.0),
                    MaxEngineCount = (int)ReadDouble(stageNode, "MaxEngineCount", 8.0),
                    CargoMassTons = Math.Max(0.0, ReadDouble(stageNode, "CargoMassTons", 0.0)),
                    AddDecouplerMass = ReadBool(stageNode, "AddDecouplerMass", false),
                    DecouplerMassTons = Math.Max(0.0, ReadDouble(stageNode, "DecouplerMassTons", 0.0))
                };

                if (stageNode.HasValue("BulkheadProfile"))
                {
                    foreach (string value in stageNode.GetValues("BulkheadProfile"))
                        AddProfile(stage.BulkheadProfiles, value);
                }

                // Plans saved before 0.6.11 stored only integer stack sizes. Translate
                // those entries to the corresponding KSP bulkheadProfiles token.
                if (stageNode.HasValue("BulkheadSize"))
                {
                    foreach (string value in stageNode.GetValues("BulkheadSize"))
                    {
                        int size;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out size) && size >= 0)
                            AddProfile(stage.BulkheadProfiles, VesselPlanner.Core.BulkheadProfiles.ProfileForSize(size));
                    }
                }

                if (stage.AddDecouplerMass && stage.DecouplerMassTons <= 0.0)
                    stage.DecouplerMassTons = DecouplerMasses.GetDecouplerMassTons(stage.BulkheadProfiles);

                foreach (ConfigNode partNode in stageNode.GetNodes("PART_ENTRY"))
                {
                    stage.Parts.Add(new PlannedPart
                    {
                        PartName = ReadString(partNode, "PartName", ""),
                        DisplayName = ReadString(partNode, "DisplayName", ""),
                        Quantity = (int)ReadDouble(partNode, "Quantity", 1.0),
                        IsEngine = ReadBool(partNode, "IsEngine", false)
                    });
                }

                plan.Stages.Add(stage);
            }

            return plan;
        }

        private static void AddProfile(List<string> profiles, string value)
        {
            if (profiles == null || string.IsNullOrEmpty(value)) return;
            string profile = value.Trim();
            if (profile.Length == 0) return;
            foreach (string existing in profiles)
                if (string.Equals(existing, profile, StringComparison.OrdinalIgnoreCase)) return;
            profiles.Add(profile);
        }

        private static string ReadString(ConfigNode node, string key, string defaultValue)
        {
            return node != null && node.HasValue(key) ? node.GetValue(key) : defaultValue;
        }

        private static double ReadDouble(ConfigNode node, string key, double defaultValue)
        {
            if (node == null || !node.HasValue(key)) return defaultValue;
            double value;
            if (!double.TryParse(node.GetValue(key), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return defaultValue;
            return double.IsNaN(value) || double.IsInfinity(value) ? defaultValue : value;
        }

        private static bool ReadBool(ConfigNode node, string key, bool defaultValue)
        {
            if (node == null || !node.HasValue(key)) return defaultValue;
            bool value;
            return bool.TryParse(node.GetValue(key), out value) ? value : defaultValue;
        }
    }
}
