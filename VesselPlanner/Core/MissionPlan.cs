using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace VesselPlanner.Core
{
    // A saved Mission Planner document. Mission plans are intentionally separate from
    // Stage-By-Stage plans: the latter may use one of these as a reversed build guide.
    public sealed class MissionPlan
    {
        public const string RootNodeName = "VESSELPLANNER_MISSION_PLAN";

        public string Name { get; set; } = "Mission";
        public List<MissionManeuver> Maneuvers { get; } = new List<MissionManeuver>();

        public ConfigNode ToConfigNode()
        {
            var root = new ConfigNode(RootNodeName);
            root.AddValue("Name", Name ?? "Mission");

            foreach (MissionManeuver maneuver in Maneuvers)
            {
                if (maneuver == null) continue;
                ConfigNode node = root.AddNode("MANEUVER");
                node.AddValue("StepKind", maneuver.StepKind.ToString());
                node.AddValue("Kind", maneuver.Kind.ToString());
                node.AddValue("Body", maneuver.Body ?? string.Empty);
                node.AddValue("SourceBody", maneuver.SourceBody ?? string.Empty);
                node.AddValue("DestinationBody", maneuver.DestinationBody ?? string.Empty);
                node.AddValue("DeltaV", maneuver.DeltaV.ToString("0.####", CultureInfo.InvariantCulture));
                node.AddValue("DeltaVBasis", maneuver.DeltaVBasis.ToString());
                if (maneuver.Assemblies.Count > 0)
                {
                    foreach (PlannedSubassembly item in maneuver.Assemblies)
                    {
                        if (item == null) continue;
                        ConfigNode assemblyNode = node.AddNode("SUBASSEMBLY");
                        assemblyNode.AddValue("AssemblyFile", item.AssemblyFile ?? string.Empty);
                        assemblyNode.AddValue("AssemblyName", item.AssemblyName ?? string.Empty);
                        assemblyNode.AddValue("UnitMassTons", item.UnitMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                        assemblyNode.AddValue("Quantity", Math.Max(1, item.Quantity).ToString(CultureInfo.InvariantCulture));
                        assemblyNode.AddValue("AddDecoupler", item.AddDecoupler.ToString());
                        assemblyNode.AddValue("DecouplerMassTons", item.DecouplerMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                    }
                }
                else if (maneuver.HasAssembly)
                {
                    // Keep the legacy single-assembly values if this object came from an
                    // older caller that has not populated the new collection.
                    node.AddValue("AssemblyFile", maneuver.AssemblyFile ?? string.Empty);
                    node.AddValue("AssemblyName", maneuver.AssemblyName ?? string.Empty);
                    node.AddValue("AssemblyMassTons", maneuver.AssemblyMassTons.ToString("0.####", CultureInfo.InvariantCulture));
                }
            }

            return root;
        }

        public static MissionPlan FromConfigNode(ConfigNode root)
        {
            if (root == null) return null;

            var plan = new MissionPlan
            {
                Name = CommonRoutines.ReadString(root, "Name", "Mission")
            };

            foreach (ConfigNode node in root.GetNodes("MANEUVER"))
            {
                MissionStepKind stepKind;
                if (!Enum.TryParse(CommonRoutines.ReadString(node, "StepKind", "EnginesAndTanks"), true, out stepKind))
                    stepKind = MissionStepKind.EnginesAndTanks;

                Maneuver kind;
                if (!Enum.TryParse(CommonRoutines.ReadString(node, "Kind", "None"), true, out kind))
                    kind = Maneuver.None;

                DeltaVBasis basis;
                if (!Enum.TryParse(CommonRoutines.ReadString(node, "DeltaVBasis", "Vacuum"), true, out basis))
                    basis = DeltaVBasis.Vacuum;

                var maneuver = new MissionManeuver
                {
                    StepKind = stepKind,
                    Kind = kind,
                    Body = CommonRoutines.ReadString(node, "Body", string.Empty),
                    SourceBody = CommonRoutines.ReadString(node, "SourceBody", string.Empty),
                    DestinationBody = CommonRoutines.ReadString(node, "DestinationBody", string.Empty),
                    DeltaV = Math.Max(0.0, CommonRoutines.ReadDouble(node, "DeltaV", 0.0)),
                    DeltaVBasis = basis,
                    AssemblyFile = CommonRoutines.ReadString(node, "AssemblyFile", string.Empty),
                    AssemblyName = CommonRoutines.ReadString(node, "AssemblyName", string.Empty),
                    AssemblyMassTons = Math.Max(0.0, CommonRoutines.ReadDouble(node, "AssemblyMassTons", 0.0))
                };

                foreach (ConfigNode assemblyNode in node.GetNodes("SUBASSEMBLY"))
                {
                    maneuver.Assemblies.Add(new PlannedSubassembly
                    {
                        AssemblyFile = CommonRoutines.ReadString(assemblyNode, "AssemblyFile", string.Empty),
                        AssemblyName = CommonRoutines.ReadString(assemblyNode, "AssemblyName", string.Empty),
                        UnitMassTons = Math.Max(0.0, CommonRoutines.ReadDouble(assemblyNode, "UnitMassTons", 0.0)),
                        Quantity = Math.Max(1, (int)CommonRoutines.ReadDouble(assemblyNode, "Quantity", 1.0)),
                        AddDecoupler = CommonRoutines.ReadBool(assemblyNode, "AddDecoupler", false),
                        DecouplerMassTons = Math.Max(0.0, CommonRoutines.ReadDouble(assemblyNode, "DecouplerMassTons", 0.0))
                    });
                }

                // Convert the 0.7.20/0.7.21 single-assembly format to the new list.
                if (maneuver.Assemblies.Count == 0 && maneuver.HasAssembly)
                {
                    maneuver.Assemblies.Add(new PlannedSubassembly
                    {
                        AssemblyFile = maneuver.AssemblyFile ?? string.Empty,
                        AssemblyName = maneuver.AssemblyName ?? string.Empty,
                        UnitMassTons = Math.Max(0.0, maneuver.AssemblyMassTons),
                        Quantity = 1,
                        AddDecoupler = false,
                        DecouplerMassTons = 0.0
                    });
                }

                plan.Maneuvers.Add(maneuver);
            }

            return plan;
        }
    }

    public static class MissionPlanPersistence
    {
        public static string DirectoryPath
        {
            get
            {
                return Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "VesselPlanner", "PluginData"), "MissionPlans");
            }
        }

        public static string Save(string missionName, IEnumerable<MissionManeuver> maneuvers)
        {
            string fileName = SanitiseFileName(missionName);
            var plan = new MissionPlan { Name = string.IsNullOrWhiteSpace(missionName) ? fileName : missionName.Trim() };
            if (maneuvers != null)
            {
                foreach (MissionManeuver maneuver in maneuvers)
                {
                    if (maneuver == null) continue;
                    plan.Maneuvers.Add(CloneManeuver(maneuver));
                }
            }

            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, fileName + ".cfg");
            var wrapper = new ConfigNode();
            wrapper.AddNode(plan.ToConfigNode());
            wrapper.Save(path);
            return path;
        }

        public static MissionPlan Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            ConfigNode wrapper = ConfigNode.Load(path);
            ConfigNode node = wrapper == null ? null : wrapper.GetNode(MissionPlan.RootNodeName);
            return MissionPlan.FromConfigNode(node);
        }

        public static List<string> ListFiles()
        {
            var files = new List<string>();
            if (!Directory.Exists(DirectoryPath)) return files;
            files.AddRange(Directory.GetFiles(DirectoryPath, "*.cfg"));
            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }
        public static string SanitiseFileName(string name)
        {
            return CommonRoutines.SanitiseFileName(name, "Mission", true, true);
        }

        public static MissionManeuver CloneManeuver(MissionManeuver maneuver)
        {
            if (maneuver == null) return null;
            var clone = new MissionManeuver
            {
                StepKind = maneuver.StepKind,
                Kind = maneuver.Kind,
                Body = maneuver.Body ?? string.Empty,
                SourceBody = maneuver.SourceBody ?? string.Empty,
                DestinationBody = maneuver.DestinationBody ?? string.Empty,
                DeltaV = maneuver.DeltaV,
                DeltaVBasis = maneuver.DeltaVBasis,
                AssemblyFile = maneuver.AssemblyFile ?? string.Empty,
                AssemblyName = maneuver.AssemblyName ?? string.Empty,
                AssemblyMassTons = maneuver.AssemblyMassTons
            };
            foreach (PlannedSubassembly item in maneuver.Assemblies)
                if (item != null) clone.Assemblies.Add(item.Clone());
            return clone;
        }
    }
}
