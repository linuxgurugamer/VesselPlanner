using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using VesselPlanner.Core;

namespace VesselPlanner.KSP
{
    // One KSP subassembly from the current save.  The mass is calculated directly from
    // the .craft file without loading it into the editor, so browsing Mission Planner
    // assemblies never mutates the craft currently being edited.
    public sealed class SavedAssemblyInfo
    {
        public string DisplayName { get; set; }
        public string RelativePath { get; set; }
        public string FullPath { get; set; }
        public double MassTons { get; set; }
        public bool MassAvailable { get; set; }
        public string Error { get; set; }
        public List<string> BulkheadProfiles { get; } = new List<string>();

        public string SelectorLabel
        {
            get
            {
                string name = string.IsNullOrEmpty(DisplayName) ? Path.GetFileNameWithoutExtension(RelativePath ?? string.Empty) : DisplayName;
                return MassAvailable
                    ? name + " (" + MassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t)"
                    : name + " (mass unavailable)";
            }
        }
    }

    public static class SavedAssemblyCatalog
    {
        public static string DirectoryPath
        {
            get
            {
                string saveFolder = HighLogic.SaveFolder;
                if (string.IsNullOrEmpty(saveFolder)) return string.Empty;
                return Path.Combine(KSPUtil.ApplicationRootPath, "saves", saveFolder, "Subassemblies");
            }
        }

        public static List<SavedAssemblyInfo> Scan()
        {
            var result = new List<SavedAssemblyInfo>();
            string directory = DirectoryPath;
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return result;

            string[] files;
            try
            {
                files = Directory.GetFiles(directory, "*.craft", SearchOption.AllDirectories);
            }
            catch
            {
                return result;
            }

            foreach (string file in files)
                result.Add(Read(file, directory));

            return result
                .OrderBy(a => a.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.RelativePath ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static SavedAssemblyInfo Read(string fullPath, string rootDirectory)
        {
            var info = new SavedAssemblyInfo
            {
                FullPath = fullPath ?? string.Empty,
                RelativePath = MakeRelativePath(rootDirectory, fullPath),
                DisplayName = string.IsNullOrEmpty(fullPath) ? "Subassembly" : Path.GetFileNameWithoutExtension(fullPath),
                MassTons = 0.0,
                MassAvailable = false,
                Error = string.Empty
            };

            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                info.Error = "Subassembly file was not found.";
                return info;
            }

            try
            {
                ConfigNode craft = ConfigNode.Load(fullPath);
                if (craft == null)
                {
                    info.Error = "Subassembly file could not be read.";
                    return info;
                }

                if (craft.HasValue("ship") && !string.IsNullOrWhiteSpace(craft.GetValue("ship")))
                    info.DisplayName = craft.GetValue("ship").Trim();

                ConfigNode[] partNodes = craft.GetNodes("PART");
                if (partNodes == null || partNodes.Length == 0)
                {
                    info.Error = "Subassembly contains no parts.";
                    return info;
                }

                double totalMass = 0.0;
                var missingParts = new List<string>();
                var missingResources = new List<string>();
                bool firstPart = true;
                foreach (ConfigNode partNode in partNodes)
                {
                    string craftPart = CommonRoutines.ReadString(partNode, "part", string.Empty);
                    string partName = CraftPartName(craftPart);
                    AvailablePart available = FindAvailablePart(partName);
                    Part prefab = available == null ? null : available.partPrefab;

                    // The first PART in a saved subassembly is its root part.  Keep that
                    // part's bulkhead profiles so Stage-By-Stage can choose a reference
                    // decoupler mass for this subassembly without loading it into the editor.
                    if (firstPart && available != null)
                    {
                        CommonRoutines.AddBulkheadProfiles(info.BulkheadProfiles, available.bulkheadProfiles);
                        if (info.BulkheadProfiles.Count == 0 && prefab != null)
                        {
                            AttachNode top = prefab.FindAttachNode("top");
                            AttachNode bottom = prefab.FindAttachNode("bottom");
                            if (top != null && top.size >= 0) CommonRoutines.AddUniqueIgnoreCase(info.BulkheadProfiles, "size" + top.size.ToString(CultureInfo.InvariantCulture));
                            if (bottom != null && bottom.size >= 0) CommonRoutines.AddUniqueIgnoreCase(info.BulkheadProfiles, "size" + bottom.size.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                    firstPart = false;

                    if (prefab == null)
                    {
                        if (!string.IsNullOrEmpty(partName) && !missingParts.Contains(partName)) missingParts.Add(partName);
                        continue;
                    }

                    double modifierMass = CommonRoutines.ReadDouble(partNode, "modMass", 0.0);
                    totalMass += Math.Max(0.0, prefab.mass + modifierMass);

                    foreach (ConfigNode resourceNode in partNode.GetNodes("RESOURCE"))
                    {
                        string resourceName = CommonRoutines.ReadString(resourceNode, "name", string.Empty);
                        double amount = Math.Max(0.0, CommonRoutines.ReadDouble(resourceNode, "amount", 0.0));
                        if (string.IsNullOrEmpty(resourceName) || amount <= 0.0) continue;

                        PartResourceDefinition def = null;
                        try { def = PartResourceLibrary.Instance.GetDefinition(resourceName); }
                        catch { def = null; }
                        if (def == null)
                        {
                            if (!missingResources.Contains(resourceName)) missingResources.Add(resourceName);
                        }
                        else if (def.density > 0.0)
                        {
                            totalMass += amount * def.density;
                        }
                    }
                }

                info.MassTons = Math.Max(0.0, totalMass);
                if (missingParts.Count > 0 || missingResources.Count > 0)
                {
                    var errors = new List<string>();
                    if (missingParts.Count > 0)
                        errors.Add("Missing part" + (missingParts.Count == 1 ? ": " : "s: ") + string.Join(", ", missingParts.ToArray()));
                    if (missingResources.Count > 0)
                        errors.Add("Unknown resource" + (missingResources.Count == 1 ? ": " : "s: ") + string.Join(", ", missingResources.ToArray()));
                    info.Error = string.Join("; ", errors.ToArray());
                    return info;
                }

                info.MassAvailable = true;
                return info;
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
                return info;
            }
        }

        private static AvailablePart FindAvailablePart(string partName)
        {
            if (string.IsNullOrEmpty(partName) || PartLoader.LoadedPartsList == null) return null;
            return PartLoader.LoadedPartsList.FirstOrDefault(p => p != null && string.Equals(p.name, partName, StringComparison.OrdinalIgnoreCase));
        }

        private static string CraftPartName(string craftPart)
        {
            if (string.IsNullOrWhiteSpace(craftPart)) return string.Empty;
            string value = craftPart.Trim();
            int split = value.LastIndexOf('_');
            if (split <= 0 || split >= value.Length - 1) return value;

            uint persistentId;
            if (uint.TryParse(value.Substring(split + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out persistentId))
                return value.Substring(0, split);
            return value;
        }

        private static string MakeRelativePath(string rootDirectory, string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return string.Empty;
            if (string.IsNullOrEmpty(rootDirectory)) return Path.GetFileName(fullPath);

            try
            {
                string root = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string file = Path.GetFullPath(fullPath);
                if (file.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    return file.Substring(root.Length).Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
            }
            catch { }
            return Path.GetFileName(fullPath);
        }
    }
}
