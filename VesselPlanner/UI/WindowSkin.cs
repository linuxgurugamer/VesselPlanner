using System;
using System.IO;
using UnityEngine;

namespace VesselPlanner.UI
{
    // Chooses the GUISkin the mod's windows draw with: the KSP skin KSP mods normally use,
    // or the stock Unity skin.
    internal static class WindowSkin
    {
        public const string SettingsKey = "UseAltSkin";

        private static GUISkin _altSkin;
        private static bool _useAltSkin;
        private static int _revision;

        public static bool UseAltSkin
        {
            get { return _useAltSkin; }
            set
            {
                if (_useAltSkin == value) return;
                _useAltSkin = value;
                _revision++;
            }
        }

        // Bumped whenever the skin changes.  Cached GUIStyles are copied from GUI.skin, so
        // whoever holds one compares this against the revision it was built with and
        // rebuilds when they differ.
        public static int Revision
        {
            get { return _revision; }
        }

        // Unity resets GUI.skin to its default at the start of every OnGUI, so a read taken
        // before anything assigns HighLogic.Skin gives the stock Unity skin.  Capturing it
        // at the main menu, ahead of the mod's first assignment, keeps that true.
        public static void CaptureAltSkin()
        {
            if (_altSkin == null) _altSkin = GUI.skin;
        }

        public static void Apply()
        {
            CaptureAltSkin();
            GUI.skin = _useAltSkin && _altSkin != null ? _altSkin : HighLogic.Skin;
        }

        // Read straight from the shared settings file instead of through the planner's
        // settings object, so flight scenes use the chosen skin even when the editor
        // planner has not been opened this session.
        public static void Load()
        {
            try
            {
                string path = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "VesselPlanner", "PluginData", "VesselPlannerSettings.cfg");
                if (!File.Exists(path))
                {
                    string legacyPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "EngineStagePlanner", "PluginData", "EngineStagePlannerSettings.cfg");
                    if (File.Exists(legacyPath)) path = legacyPath;
                }
                if (!File.Exists(path)) return;

                ConfigNode root = ConfigNode.Load(path);
                if (root == null) return;
                ConfigNode settings = root.GetNode("ENGINE_STAGE_PLANNER_SETTINGS") ?? root;
                if (!settings.HasValue(SettingsKey)) return;

                bool value;
                if (bool.TryParse(settings.GetValue(SettingsKey), out value)) UseAltSkin = value;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VesselPlanner] Unable to load skin setting: " + ex.Message);
            }
        }
    }
}
