using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VesselPlanner.Core
{
    public static class CommonRoutines
    {
        public static string AddSpacesToString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            string name = value.Trim();
            name = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1 $2");
            return Regex.Replace(name, "([A-Z])([A-Z][a-z])", "$1 $2");
        }

        public static string FormatManeuver(Maneuver maneuver)
        {
            return AddSpacesToString(maneuver.ToString());
        }

        public static void DrawTooltip(bool enabled, float windowWidth, float windowHeight)
        {
            if (!enabled) return;

            string tooltip = GUI.tooltip;
            if (string.IsNullOrEmpty(tooltip)) return;

            float width = Mathf.Min(360f, Mathf.Max(220f, windowWidth - 16f));
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                padding = new RectOffset(8, 8, 6, 6)
            };
            GUIContent content = new GUIContent(tooltip);
            float height = style.CalcHeight(content, width);
            Vector2 mouse = Event.current.mousePosition;
            float x = Mathf.Clamp(mouse.x + 16f, 4f, Mathf.Max(4f, windowWidth - width - 4f));
            float y = mouse.y + 20f;
            if (y + height > windowHeight - 4f)
                y = Mathf.Max(4f, mouse.y - height - 10f);
            GUI.Box(new Rect(x, y, width, height), content, style);
        }


        public static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static StageSolution FailStageSolution(StageSolution result, string reason)
        {
            if (result == null) return null;
            result.IsValid = false;
            result.FailureReason = reason;
            return result;
        }

        public static string SanitiseFileName(string name, string defaultName, bool stripCfgExtension = false, bool trimTrailingDotsAndSpaces = false)
        {
            string fallback = string.IsNullOrEmpty(defaultName) ? "File" : defaultName;
            if (string.IsNullOrWhiteSpace(name)) return fallback;

            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            var builder = new System.Text.StringBuilder();
            foreach (char c in name.Trim())
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);

            string result = builder.ToString();
            if (trimTrailingDotsAndSpaces)
                result = result.Trim().TrimEnd('.', ' ');

            if (stripCfgExtension && result.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
                result = System.IO.Path.GetFileNameWithoutExtension(result);

            return result.Length == 0 ? fallback : result;
        }

        public static string ReadString(ConfigNode node, string key, string defaultValue)
        {
            return node != null && node.HasValue(key) ? node.GetValue(key) : defaultValue;
        }

        public static double ReadDouble(ConfigNode node, string key, double defaultValue)
        {
            if (node == null || !node.HasValue(key)) return defaultValue;
            double value;
            if (!double.TryParse(node.GetValue(key), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return defaultValue;
            return double.IsNaN(value) || double.IsInfinity(value) ? defaultValue : value;
        }

        public static bool ReadBool(ConfigNode node, string key, bool defaultValue)
        {
            if (node == null || !node.HasValue(key)) return defaultValue;
            bool value;
            return bool.TryParse(node.GetValue(key), out value) ? value : defaultValue;
        }

        public static void AddUniqueIgnoreCase(List<string> output, string value)
        {
            if (output == null || string.IsNullOrWhiteSpace(value)) return;
            string item = value.Trim();
            if (item.Length == 0) return;

            foreach (string existing in output)
            {
                if (string.Equals(existing, item, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            output.Add(item);
        }

        public static void AddBulkheadProfiles(List<string> output, string profiles, bool excludeSurfaceAttach = false)
        {
            if (output == null || string.IsNullOrWhiteSpace(profiles)) return;

            foreach (string raw in profiles.Split(','))
            {
                string profile = raw.Trim();
                if (profile.Length == 0) continue;
                if (excludeSurfaceAttach && string.Equals(profile, "srf", StringComparison.OrdinalIgnoreCase)) continue;
                AddUniqueIgnoreCase(output, profile);
            }
        }

        public static bool IsAvailableToPlayer(AvailablePart part)
        {
            if (part == null || part.category == PartCategories.none) return false;
            if (HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) return true;
            if (ResearchAndDevelopment.Instance == null) return true;
            return ResearchAndDevelopment.PartModelPurchased(part);
        }

        public static double GetDryPartMass(Part part)
        {
            if (part == null) return 0.0;
            double mass = part.mass;
            try { mass += part.GetModuleMass(part.mass, ModifierStagingSituation.CURRENT); }
            catch { }
            return Math.Max(0.0, mass);
        }

        public static void ClampWindow(ref Rect window)
        {
            window.x = Mathf.Clamp(window.x, -window.width + 40f, Screen.width - 40f);
            window.y = Mathf.Clamp(window.y, 0f, Screen.height - 30f);
        }
    }
}
