using System;
using System.Collections.Generic;
using System.Globalization;

namespace VesselPlanner.Core
{
    public enum PlannedStageKind
    {
        EnginesAndTanks,
        Subassemblies
    }

    // One part chosen for a planned engine/tank stage, with the number of copies the planner worked out.
    public sealed class PlannedPart
    {
        public string PartName { get; set; }
        // Unique KSP GameData URL for the loaded part, same as EngineCandidate.PartUrl /
        // TankCandidate.PartUrl - carried through here so the Stage-By-Stage window can
        // resolve and cache its thumbnail by URL instead of PartName alone. Without it,
        // any mod pack that ships a duplicate/replacement part sharing another part's
        // internal name causes the wrong thumbnail to be reused for one of them.
        public string PartUrl { get; set; }
        public string DisplayName { get; set; }
        public int Quantity { get; set; }
        public bool IsEngine { get; set; }

        public PlannedPart Clone()
        {
            return new PlannedPart
            {
                PartName = PartName,
                PartUrl = PartUrl,
                DisplayName = DisplayName,
                Quantity = Quantity,
                IsEngine = IsEngine
            };
        }
    }

    // One saved KSP subassembly used by a Subassemblies stage.  Decoupler mass is stored
    // per copy; when Quantity is greater than one and AddDecoupler is enabled, each copy
    // receives its own decoupler.
    public sealed class PlannedSubassembly
    {
        public string AssemblyFile { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public double UnitMassTons { get; set; }
        public int Quantity { get; set; } = 1;
        public bool AddDecoupler { get; set; }
        public double DecouplerMassTons { get; set; }

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(AssemblyName) ? AssemblyFile : AssemblyName; }
        }

        public double TotalMassTons
        {
            get
            {
                int count = Math.Max(0, Quantity);
                double perCopy = Math.Max(0.0, UnitMassTons)
                    + (AddDecoupler ? Math.Max(0.0, DecouplerMassTons) : 0.0);
                return count * perCopy;
            }
        }

        public PlannedSubassembly Clone()
        {
            return new PlannedSubassembly
            {
                AssemblyFile = AssemblyFile,
                AssemblyName = AssemblyName,
                UnitMassTons = UnitMassTons,
                Quantity = Quantity,
                AddDecoupler = AddDecoupler,
                DecouplerMassTons = DecouplerMassTons
            };
        }
    }

    // A stage in a stage-by-stage plan.  A stage is either solved as an engine/tank stage,
    // or it is a collection of one or more saved subassemblies with optional decouplers.
    public sealed class PlannedStage
    {
        public PlannedStageKind Kind { get; set; } = PlannedStageKind.EnginesAndTanks;

        public double TargetDeltaV { get; set; }
        public DeltaVBasis TargetDeltaVBasis { get; set; } = DeltaVBasis.Vacuum;
        public double MinimumTwr { get; set; }
        public int MaxEngineCount { get; set; } = 8;
        // Additional non-propellant dry mass carried by an engine/tank stage. It is included
        // in the stage solve but is separate from the vessel payload above the stage.
        public double CargoMassTons { get; set; }
        // Optional fixed dry mass for the engine/tank stage separator. The toggle is saved
        // separately so the mass can be recalculated from decouplerMasses.cfg if the bulkhead
        // choice changes.
        public bool AddDecouplerMass { get; set; }
        public double DecouplerMassTons { get; set; }

        // Layout annotations for booster stages. These do not change the rocket-equation
        // solve by themselves; they describe how the user intends to arrange/fire the stage.
        public bool SideBoosters { get; set; }
        public bool CoreBurnsToo { get; set; }
        public bool SideBoostersHaveRadialDecouplers { get; set; }

        // Legacy/special Mission Planner assembly carried by an engine/tank stage.  This is
        // retained for backward compatibility with 0.7.20/0.7.21 plans and mission-linked
        // stages.  Newly-created standalone Subassemblies stages use Subassemblies below.
        public string AssemblyFile { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public double AssemblyMassTons { get; set; }

        // Optional link back to a saved Mission Planner maneuver. Zero/None means this
        // stage was created independently of a mission plan.
        public string MissionPlanName { get; set; } = string.Empty;
        public int MissionStepNumber { get; set; }
        public Maneuver MissionManeuver { get; set; } = Maneuver.None;

        // KSP part bulkheadProfiles tokens the candidate engines and tanks are restricted
        // to (for example size1 or srf). Empty considers every profile.
        public List<string> BulkheadProfiles { get; } = new List<string>();
        public List<PlannedPart> Parts { get; } = new List<PlannedPart>();
        public List<PlannedSubassembly> Subassemblies { get; } = new List<PlannedSubassembly>();

        // Adding the same part twice accumulates rather than listing it again, so a stage
        // built up over several passes in the planner reads as one line per part.
        public void AddPart(string partName, string partUrl, string displayName, int quantity, bool isEngine)
        {
            if (string.IsNullOrEmpty(partName) || quantity <= 0) return;

            foreach (PlannedPart existing in Parts)
            {
                if (existing.PartName == partName && existing.IsEngine == isEngine)
                {
                    existing.Quantity += quantity;
                    // Backfill in case an earlier add (or an older saved plan) went
                    // through before PartUrl existed on this entry.
                    if (string.IsNullOrEmpty(existing.PartUrl) && !string.IsNullOrEmpty(partUrl))
                        existing.PartUrl = partUrl;
                    return;
                }
            }

            Parts.Add(new PlannedPart
            {
                PartName = partName,
                PartUrl = partUrl,
                DisplayName = string.IsNullOrEmpty(displayName) ? partName : displayName,
                Quantity = quantity,
                IsEngine = isEngine
            });
        }

        public double SubassemblyStageMassTons
        {
            get
            {
                double mass = 0.0;
                foreach (PlannedSubassembly item in Subassemblies)
                    if (item != null) mass += item.TotalMassTons;
                return Math.Max(0.0, mass);
            }
        }

        public string Summary
        {
            get
            {
                if (Kind == PlannedStageKind.Subassemblies)
                {
                    int count = 0;
                    foreach (PlannedSubassembly item in Subassemblies)
                        if (item != null) count += Math.Max(0, item.Quantity);
                    return "subassemblies: " + count.ToString(CultureInfo.InvariantCulture)
                        + ", " + SubassemblyStageMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t";
                }

                string basis = TargetDeltaVBasis == DeltaVBasis.Atmospheric ? "atm" : "vac";
                return TargetDeltaV.ToString("0", CultureInfo.InvariantCulture) + " m/s " + basis
                    + ", TWR " + MinimumTwr.ToString("0.##", CultureInfo.InvariantCulture)
                    + ", max " + MaxEngineCount.ToString(CultureInfo.InvariantCulture) + " engines"
                    + (CargoMassTons > 0.0 ? ", cargo " + CargoMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t" : "")
                    + ((!string.IsNullOrEmpty(AssemblyName) || !string.IsNullOrEmpty(AssemblyFile))
                        ? ", subassembly " + AssemblyMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t" : "")
                    + (Subassemblies.Count > 0 ? ", subassemblies " + SubassemblyStageMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t" : "")
                    + (AddDecouplerMass && DecouplerMassTons > 0.0 ? ", decoupler " + DecouplerMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t" : "")
                    + (SideBoosters ? ", side boosters" : "")
                    + (CoreBurnsToo ? ", core burns too" : "")
                    + (SideBoosters && SideBoostersHaveRadialDecouplers ? ", radial decouplers" : "");
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
                stageNode.AddValue("StageKind", stage.Kind.ToString());
                stageNode.AddValue("TargetDeltaV", stage.TargetDeltaV.ToString("0.####", CultureInfo.InvariantCulture));
                stageNode.AddValue("TargetDeltaVBasis", stage.TargetDeltaVBasis.ToString());
                stageNode.AddValue("MinimumTwr", stage.MinimumTwr.ToString("0.####", CultureInfo.InvariantCulture));
                stageNode.AddValue("MaxEngineCount", stage.MaxEngineCount.ToString(CultureInfo.InvariantCulture));
                stageNode.AddValue("CargoMassTons", stage.CargoMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                stageNode.AddValue("AddDecouplerMass", stage.AddDecouplerMass.ToString());
                stageNode.AddValue("DecouplerMassTons", stage.DecouplerMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                stageNode.AddValue("SideBoosters", stage.SideBoosters.ToString());
                stageNode.AddValue("CoreBurnsToo", stage.CoreBurnsToo.ToString());
                stageNode.AddValue("SideBoostersHaveRadialDecouplers", stage.SideBoostersHaveRadialDecouplers.ToString());
                if (stage.AssemblyMassTons > 0.0 || !string.IsNullOrEmpty(stage.AssemblyName) || !string.IsNullOrEmpty(stage.AssemblyFile))
                {
                    stageNode.AddValue("AssemblyFile", stage.AssemblyFile ?? string.Empty);
                    stageNode.AddValue("AssemblyName", stage.AssemblyName ?? string.Empty);
                    stageNode.AddValue("AssemblyMassTons", stage.AssemblyMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                }
                if (stage.MissionStepNumber > 0)
                {
                    stageNode.AddValue("MissionPlanName", stage.MissionPlanName ?? string.Empty);
                    stageNode.AddValue("MissionStepNumber", stage.MissionStepNumber.ToString(CultureInfo.InvariantCulture));
                    stageNode.AddValue("MissionManeuver", stage.MissionManeuver.ToString());
                }
                foreach (string profile in stage.BulkheadProfiles)
                {
                    if (string.IsNullOrEmpty(profile)) continue;
                    stageNode.AddValue("BulkheadProfile", profile);

                    int legacySize;
                    if (VesselPlanner.Core.BulkheadProfiles.TryGetSizeForProfile(profile, out legacySize))
                        stageNode.AddValue("BulkheadSize", legacySize.ToString(CultureInfo.InvariantCulture));
                }

                foreach (PlannedPart part in stage.Parts)
                {
                    ConfigNode partNode = stageNode.AddNode("PART_ENTRY");
                    partNode.AddValue("PartName", part.PartName ?? "");
                    partNode.AddValue("PartUrl", part.PartUrl ?? "");
                    partNode.AddValue("DisplayName", part.DisplayName ?? "");
                    partNode.AddValue("Quantity", part.Quantity.ToString(CultureInfo.InvariantCulture));
                    partNode.AddValue("IsEngine", part.IsEngine.ToString());
                }

                foreach (PlannedSubassembly item in stage.Subassemblies)
                {
                    if (item == null) continue;
                    ConfigNode itemNode = stageNode.AddNode("SUBASSEMBLY");
                    itemNode.AddValue("AssemblyFile", item.AssemblyFile ?? string.Empty);
                    itemNode.AddValue("AssemblyName", item.AssemblyName ?? string.Empty);
                    itemNode.AddValue("UnitMassTons", item.UnitMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                    itemNode.AddValue("Quantity", Math.Max(1, item.Quantity).ToString(CultureInfo.InvariantCulture));
                    itemNode.AddValue("AddDecoupler", item.AddDecoupler.ToString());
                    itemNode.AddValue("DecouplerMassTons", item.DecouplerMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                }
            }

            return root;
        }

        public static StagePlan FromConfigNode(ConfigNode root)
        {
            if (root == null) return null;

            var plan = new StagePlan
            {
                Name = CommonRoutines.ReadString(root, "Name", "Plan"),
                VesselName = CommonRoutines.ReadString(root, "VesselName", ""),
                PayloadMassTons = CommonRoutines.ReadDouble(root, "PayloadMassTons", 0.0),
                BodyName = CommonRoutines.ReadString(root, "BodyName", ""),
                Finalized = CommonRoutines.ReadBool(root, "Finalized", false)
            };

            foreach (ConfigNode stageNode in root.GetNodes("STAGE"))
            {
                PlannedStageKind kind = PlannedStageKind.EnginesAndTanks;
                PlannedStageKind parsedKind;
                if (Enum.TryParse(CommonRoutines.ReadString(stageNode, "StageKind", "EnginesAndTanks"), true, out parsedKind))
                    kind = parsedKind;

                var stage = new PlannedStage
                {
                    Kind = kind,
                    TargetDeltaV = CommonRoutines.ReadDouble(stageNode, "TargetDeltaV", 0.0),
                    TargetDeltaVBasis = string.Equals(CommonRoutines.ReadString(stageNode, "TargetDeltaVBasis", "Vacuum"), "Atmospheric", StringComparison.OrdinalIgnoreCase)
                        ? DeltaVBasis.Atmospheric
                        : DeltaVBasis.Vacuum,
                    MinimumTwr = CommonRoutines.ReadDouble(stageNode, "MinimumTwr", 0.0),
                    MaxEngineCount = (int)CommonRoutines.ReadDouble(stageNode, "MaxEngineCount", 8.0),
                    CargoMassTons = Math.Max(0.0, CommonRoutines.ReadDouble(stageNode, "CargoMassTons", 0.0)),
                    AddDecouplerMass = CommonRoutines.ReadBool(stageNode, "AddDecouplerMass", false),
                    DecouplerMassTons = Math.Max(0.0, CommonRoutines.ReadDouble(stageNode, "DecouplerMassTons", 0.0)),
                    SideBoosters = CommonRoutines.ReadBool(stageNode, "SideBoosters", false),
                    CoreBurnsToo = CommonRoutines.ReadBool(stageNode, "CoreBurnsToo", false),
                    SideBoostersHaveRadialDecouplers = CommonRoutines.ReadBool(stageNode, "SideBoostersHaveRadialDecouplers", false),
                    AssemblyFile = CommonRoutines.ReadString(stageNode, "AssemblyFile", ""),
                    AssemblyName = CommonRoutines.ReadString(stageNode, "AssemblyName", ""),
                    AssemblyMassTons = Math.Max(0.0, CommonRoutines.ReadDouble(stageNode, "AssemblyMassTons", 0.0)),
                    MissionPlanName = CommonRoutines.ReadString(stageNode, "MissionPlanName", ""),
                    MissionStepNumber = Math.Max(0, (int)CommonRoutines.ReadDouble(stageNode, "MissionStepNumber", 0.0))
                };

                Maneuver missionManeuver;
                if (Enum.TryParse(CommonRoutines.ReadString(stageNode, "MissionManeuver", "None"), true, out missionManeuver))
                    stage.MissionManeuver = missionManeuver;

                if (stageNode.HasValue("BulkheadProfile"))
                {
                    foreach (string value in stageNode.GetValues("BulkheadProfile"))
                        CommonRoutines.AddUniqueIgnoreCase(stage.BulkheadProfiles, value);
                }

                // Plans saved before 0.6.11 stored only integer stack sizes. Translate
                // those entries to the corresponding KSP bulkheadProfiles token.
                if (stageNode.HasValue("BulkheadSize"))
                {
                    foreach (string value in stageNode.GetValues("BulkheadSize"))
                    {
                        int size;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out size) && size >= 0)
                            CommonRoutines.AddUniqueIgnoreCase(stage.BulkheadProfiles, VesselPlanner.Core.BulkheadProfiles.ProfileForSize(size));
                    }
                }

                if (stage.AddDecouplerMass && stage.DecouplerMassTons <= 0.0)
                    stage.DecouplerMassTons = DecouplerMasses.GetDecouplerMassTons(stage.BulkheadProfiles);

                foreach (ConfigNode partNode in stageNode.GetNodes("PART_ENTRY"))
                {
                    stage.Parts.Add(new PlannedPart
                    {
                        PartName = CommonRoutines.ReadString(partNode, "PartName", ""),
                        // Absent in plans saved before this field existed - falls back to
                        // empty, same as it always did, so old saves still load fine and
                        // just resolve/cache their thumbnails by name until re-added.
                        PartUrl = CommonRoutines.ReadString(partNode, "PartUrl", ""),
                        DisplayName = CommonRoutines.ReadString(partNode, "DisplayName", ""),
                        Quantity = (int)CommonRoutines.ReadDouble(partNode, "Quantity", 1.0),
                        IsEngine = CommonRoutines.ReadBool(partNode, "IsEngine", false)
                    });
                }

                foreach (ConfigNode itemNode in stageNode.GetNodes("SUBASSEMBLY"))
                {
                    stage.Subassemblies.Add(new PlannedSubassembly
                    {
                        AssemblyFile = CommonRoutines.ReadString(itemNode, "AssemblyFile", ""),
                        AssemblyName = CommonRoutines.ReadString(itemNode, "AssemblyName", ""),
                        UnitMassTons = Math.Max(0.0, CommonRoutines.ReadDouble(itemNode, "UnitMassTons", 0.0)),
                        Quantity = Math.Max(1, (int)CommonRoutines.ReadDouble(itemNode, "Quantity", 1.0)),
                        AddDecoupler = CommonRoutines.ReadBool(itemNode, "AddDecoupler", false),
                        DecouplerMassTons = Math.Max(0.0, CommonRoutines.ReadDouble(itemNode, "DecouplerMassTons", 0.0))
                    });
                }

                // If a future/manual file contains subassembly rows but omitted StageKind,
                // infer the intended stage type.  Normal pre-0.7.22 plans have no rows and
                // therefore remain EnginesAndTanks stages.
                if (!stageNode.HasValue("StageKind") && stage.Subassemblies.Count > 0)
                    stage.Kind = PlannedStageKind.Subassemblies;

                plan.Stages.Add(stage);
            }

            return plan;
        }
    }
}
