using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using VesselPlanner.Core;

namespace VesselPlanner.UI
{
    /// <summary>
    /// Read-only tree view of the currently loaded delta-v table. Solar-orbiting bodies
    /// are the roots; their moons (and nested moons) are expanded beneath them.
    /// </summary>
    public sealed class DeltaVTablePage
    {
        private const float BodyColumnWidth = 220f;
        private const float LowOrbitColumnWidth = 115f;
        private const float EjectionColumnWidth = 85f;
        private const float CaptureColumnWidth = 85f;
        private const float PlaneChangeColumnWidth = 105f;
        private const float TotalColumnWidth = 85f;
        private const float LandingColumnWidth = 85f;
        private const float AscentColumnWidth = 85f;
        private const float ColumnGap = 4f;
        private const float TreeIndent = 18f;
        private const float RowHeight = 24f;
        private const float HeaderHeight = 26f;
        private const float StatusLineHeight = 26f;
        private const float TableWidth = BodyColumnWidth + LowOrbitColumnWidth + EjectionColumnWidth +
                                         CaptureColumnWidth + PlaneChangeColumnWidth + TotalColumnWidth +
                                         LandingColumnWidth + AscentColumnWidth + (ColumnGap * 7f);

        private Vector2 _scroll;
        private readonly HashSet<string> _expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _lastTable = string.Empty;
        private string _clipboardStatus = string.Empty;

        public void Initialize()
        {
            EnsureLoaded();
        }

        public void DrawPage()
        {
            EnsureLoaded();

            if (!string.Equals(_lastTable, DeltaVTable.LoadedTable, StringComparison.OrdinalIgnoreCase))
            {
                _expanded.Clear();
                _clipboardStatus = string.Empty;
                _lastTable = DeltaVTable.LoadedTable ?? string.Empty;
            }

            DeltaV[] allRows = DeltaVTable.GetRows();
            DeltaV[] bodyRows = allRows
                .Where(IsSelfRow)
                .OrderBy(r => r.sortOrder)
                .ThenBy(r => r.Destination, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            string homeBody = GetHomeBodyName();
            string centralBody = FindCentralBody(bodyRows);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Delta-V Table", GUILayout.Width(120));
                    GUILayout.Label("Table: " + (string.IsNullOrEmpty(DeltaVTable.LoadedTable) ? DeltaVTable.planetPack : DeltaVTable.LoadedTable));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Home body: " + homeBody, GUILayout.Width(180));
                }

                GUILayout.Label("Click a body with moons to expand or collapse its children. Click any displayed delta-v value to copy it to the clipboard.");
            }

            GUILayout.Space(5f);

            _scroll = GUILayout.BeginScrollView(_scroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            DrawHeader();
            if (!DeltaVTable.IsLoaded || bodyRows.Length == 0)
            {
                GUILayout.Space(12f);
                GUILayout.Label("No delta-v table is loaded.");
            }
            else
            {
                List<DeltaV> roots = bodyRows
                    .Where(r => !r.isMoon &&
                                !string.Equals(r.Destination, centralBody, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(r.parent, centralBody, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r.sortOrder)
                    .ThenBy(r => r.Destination, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Keep user-authored tables useful even when they omit a central-body self row.
                if (roots.Count == 0)
                {
                    roots = bodyRows
                        .Where(r => !r.isMoon && !string.Equals(r.Destination, centralBody, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(r => r.sortOrder)
                        .ThenBy(r => r.Destination, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var children = bodyRows
                    .Where(r => r.isMoon && !string.IsNullOrWhiteSpace(r.parent))
                    .GroupBy(r => r.parent, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(r => r.sortOrder).ThenBy(r => r.Destination, StringComparer.OrdinalIgnoreCase).ToList(),
                        StringComparer.OrdinalIgnoreCase);

                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DeltaV root in roots)
                    DrawBodyRow(root, 0, homeBody, allRows, children, visited);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            using (new GUILayout.HorizontalScope(GUI.skin.box, GUILayout.Height(StatusLineHeight)))
            {
                GUILayout.Label(string.IsNullOrEmpty(_clipboardStatus) ? " " : _clipboardStatus);
            }
        }

        private static void DrawHeader()
        {
            Rect row = GUILayoutUtility.GetRect(TableWidth, HeaderHeight, GUILayout.Width(TableWidth), GUILayout.Height(HeaderHeight));
            GUI.Box(row, GUIContent.none);

            float x = row.x;
            DrawHeaderCell(ref x, row.y, row.height, "Body", BodyColumnWidth);
            DrawHeaderCell(ref x, row.y, row.height, "dV to low orbit", LowOrbitColumnWidth);
            DrawHeaderCell(ref x, row.y, row.height, "Ejection", EjectionColumnWidth);
            DrawHeaderCell(ref x, row.y, row.height, "Capture", CaptureColumnWidth);
            DrawHeaderCell(ref x, row.y, row.height, "Plane Change", PlaneChangeColumnWidth);
            DrawHeaderCell(ref x, row.y, row.height, "Total", TotalColumnWidth);
            DrawHeaderCell(ref x, row.y, row.height, "Landing", LandingColumnWidth);
            DrawHeaderCell(ref x, row.y, row.height, "Ascent", AscentColumnWidth, false);
        }

        private static void DrawHeaderCell(ref float x, float y, float height, string text, float width, bool addGap = true)
        {
            GUI.Label(new Rect(x, y, width, height), text);
            x += width;
            if (addGap) x += ColumnGap;
        }

        private void DrawBodyRow(
            DeltaV body,
            int depth,
            string homeBody,
            DeltaV[] allRows,
            Dictionary<string, List<DeltaV>> children,
            HashSet<string> visited)
        {
            if (body == null || string.IsNullOrEmpty(body.Destination)) return;
            if (!visited.Add(body.Destination)) return;

            List<DeltaV> childRows;
            bool hasChildren = children.TryGetValue(body.Destination, out childRows) && childRows.Count > 0;
            bool expanded = hasChildren && _expanded.Contains(body.Destination);
            bool isHomeBody = string.Equals(body.Destination, homeBody, StringComparison.OrdinalIgnoreCase);

            DeltaV route = null;
            if (!isHomeBody)
            {
                string origin = body.isMoon && !string.IsNullOrWhiteSpace(body.parent)
                    ? body.parent
                    : homeBody;
                route = FindRoute(allRows, origin, body.Destination);
            }

            Rect row = GUILayoutUtility.GetRect(TableWidth, RowHeight, GUILayout.Width(TableWidth), GUILayout.Height(RowHeight));
            float x = row.x;

            Rect bodyRect = NextColumn(ref x, row.y, row.height, BodyColumnWidth);
            float indent = Math.Min(depth * TreeIndent, BodyColumnWidth - 60f);
            Rect bodyTextRect = new Rect(bodyRect.x + indent, bodyRect.y, bodyRect.width - indent, bodyRect.height);
            string bodyCaption = hasChildren
                ? (expanded ? "▼ " : "▶ ") + body.Destination
                : "  " + body.Destination;

            if (hasChildren)
            {
                if (GUI.Button(bodyTextRect, bodyCaption, GUI.skin.label))
                {
                    if (expanded) _expanded.Remove(body.Destination);
                    else _expanded.Add(body.Destination);
                }
            }
            else
            {
                GUI.Label(bodyTextRect, bodyCaption);
            }

            Rect lowOrbitRect = NextColumn(ref x, row.y, row.height, LowOrbitColumnWidth);
            DrawClickableValue(lowOrbitRect, body.Destination, "Low Orbit", body.dV_to_low_orbit);

            Rect ejectionRect = NextColumn(ref x, row.y, row.height, EjectionColumnWidth);
            DrawRouteValue(ejectionRect, body.Destination, "Ejection", isHomeBody ? null : route, r => r.ejection_dV);

            Rect captureRect = NextColumn(ref x, row.y, row.height, CaptureColumnWidth);
            DrawRouteValue(captureRect, body.Destination, "Capture", isHomeBody ? null : route, r => r.capture_dV);

            Rect planeChangeRect = NextColumn(ref x, row.y, row.height, PlaneChangeColumnWidth);
            DrawRouteValue(planeChangeRect, body.Destination, "Plane Change", isHomeBody ? null : route, r => r.plane_change_dV);

            Rect totalRect = NextColumn(ref x, row.y, row.height, TotalColumnWidth);
            DrawRouteValue(totalRect, body.Destination, "Total", isHomeBody ? null : route, r => r.total_capture_dV);

            Rect landingRect = NextColumn(ref x, row.y, row.height, LandingColumnWidth);
            DrawClickableValue(landingRect, body.Destination, "Landing", body.dV_low_orbit_to_surface);

            Rect ascentRect = NextColumn(ref x, row.y, row.height, AscentColumnWidth, false);
            DrawClickableValue(ascentRect, body.Destination, "Ascent", body.ascent_dV);

            if (expanded)
            {
                foreach (DeltaV child in childRows)
                    DrawBodyRow(child, depth + 1, homeBody, allRows, children, visited);
            }
        }

        private static Rect NextColumn(ref float x, float y, float height, float width, bool addGap = true)
        {
            Rect rect = new Rect(x, y, width, height);
            x += width;
            if (addGap) x += ColumnGap;
            return rect;
        }

        private void DrawRouteValue(Rect rect, string bodyName, string caption, DeltaV route, Func<DeltaV, float> getter)
        {
            if (route == null) return;
            DrawClickableValue(rect, bodyName, caption, getter(route));
        }

        private void DrawClickableValue(Rect rect, string bodyName, string caption, float metricValue)
        {
            if (metricValue <= 0f) return;

            if (GUI.Button(rect, FormatDeltaV(metricValue), GUI.skin.label))
                CopyValue(bodyName, caption, metricValue);
        }

        private void CopyValue(string bodyName, string caption, float metricValue)
        {
            string clipboardValue = metricValue.ToString("0.##", CultureInfo.InvariantCulture);
            GUIUtility.systemCopyBuffer = clipboardValue;
            _clipboardStatus = "Copied " + caption + " " + FormatDeltaV(metricValue) +
                               " for " + bodyName + " to clipboard.";
        }

        private static string FormatDeltaV(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + " m/s";
        }

        private static DeltaV FindRoute(IEnumerable<DeltaV> rows, string origin, string destination)
        {
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination)) return null;
            return rows.FirstOrDefault(r =>
                string.Equals(r.Origin, origin, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.Destination, destination, StringComparison.OrdinalIgnoreCase) &&
                !IsSelfRow(r));
        }

        private static string FindCentralBody(IEnumerable<DeltaV> bodyRows)
        {
            DeltaV central = bodyRows
                .Where(r => !r.isMoon && string.IsNullOrWhiteSpace(r.parent))
                .OrderBy(r => r.sortOrder)
                .FirstOrDefault();
            return central != null ? central.Destination : string.Empty;
        }

        private static bool IsSelfRow(DeltaV row)
        {
            return row != null &&
                   !string.IsNullOrWhiteSpace(row.Destination) &&
                   string.Equals(row.Origin, row.Destination, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetHomeBodyName()
        {
            try
            {
                string homeBody = FlightGlobals.GetHomeBodyName();
                if (!string.IsNullOrEmpty(homeBody)) return homeBody;
            }
            catch
            {
                // KSP can briefly have no initialized body list while editor scenes are loading.
            }

            return "Kerbin";
        }

        private static void EnsureLoaded()
        {
            if (DeltaVTable.IsLoaded) return;
            if (string.IsNullOrEmpty(DeltaVTable.LoadedTable))
                DeltaVTable.DetectPlanetPack();
            DeltaVTable.LoadDeltaV(DeltaVTable.planetPack);
        }
    }
}
