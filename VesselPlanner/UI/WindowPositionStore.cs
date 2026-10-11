using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using VesselPlanner.Core;

namespace VesselPlanner.UI
{
    internal static class WindowPositionStore
    {
        internal struct Position
        {
            public readonly string Key;
            public readonly Rect Rect;

            public Position(string key, Rect rect)
            {
                Key = key;
                Rect = rect;
            }
        }

        private static string SettingsPath
        {
            get
            {
                return Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "VesselPlanner", "PluginData"), "VesselPlannerSettings.cfg");
            }
        }

        private static string LegacySettingsPath
        {
            get
            {
                return Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "EngineStagePlanner", "PluginData"), "EngineStagePlannerSettings.cfg");
            }
        }

        private static string SettingsReadPath
        {
            get
            {
                if (File.Exists(SettingsPath)) return SettingsPath;
                return File.Exists(LegacySettingsPath) ? LegacySettingsPath : SettingsPath;
            }
        }

        internal static bool TryLoadPosition(string key, ref Rect window)
        {
            if (string.IsNullOrEmpty(key)) return false;
            try
            {
                string path = SettingsReadPath;
                if (!File.Exists(path)) return false;
                ConfigNode root = ConfigNode.Load(path);
                if (root == null) return false;
                ConfigNode settings = root.GetNode("ENGINE_STAGE_PLANNER_SETTINGS") ?? root;

                float x;
                float y;
                if (!TryReadFloat(settings, "Window_" + key + "_X", out x) ||
                    !TryReadFloat(settings, "Window_" + key + "_Y", out y))
                    return false;

                window.x = x;
                window.y = y;
                CommonRoutines.ClampWindow(ref window);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VesselPlanner] Unable to load " + key + " window position: " + ex.Message);
                return false;
            }
        }

        internal static void SavePositions(params Position[] positions)
        {
            if (positions == null || positions.Length == 0) return;
            try
            {
                string path = SettingsPath;
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                string readPath = SettingsReadPath;
                ConfigNode root = File.Exists(readPath) ? ConfigNode.Load(readPath) : null;
                if (root == null) root = new ConfigNode();
                ConfigNode settings = root.GetNode("ENGINE_STAGE_PLANNER_SETTINGS");
                if (settings == null) settings = root.AddNode("ENGINE_STAGE_PLANNER_SETTINGS");

                for (int i = 0; i < positions.Length; i++)
                {
                    Position position = positions[i];
                    if (string.IsNullOrEmpty(position.Key)) continue;
                    settings.SetValue("Window_" + position.Key + "_X", position.Rect.x.ToString("0.##", CultureInfo.InvariantCulture), true);
                    settings.SetValue("Window_" + position.Key + "_Y", position.Rect.y.ToString("0.##", CultureInfo.InvariantCulture), true);
                }

                root.Save(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VesselPlanner] Unable to save window positions: " + ex.Message);
            }
        }

        private static bool TryReadFloat(ConfigNode node, string key, out float value)
        {
            value = 0f;
            if (node == null || !node.HasValue(key)) return false;
            if (!float.TryParse(node.GetValue(key), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return false;
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
