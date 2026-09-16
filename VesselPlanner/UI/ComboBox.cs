using System;
using System.Collections.Generic;
using UnityEngine;

namespace VesselPlanner.UI
{
    internal static class ComboBox
    {
        private static GUIStyle _yellowOnHover;
        private static GUIStyle _style;
        private static int _styleRevision = -1;
        private const int WindowId = 19041978;

        internal sealed class ComboBoxData
        {
            internal Rect rect;
            internal object popupOwner;
            internal string[] entries = new string[0];
            internal float minWidth;
            internal bool popupActive;
            internal int selectedItem;
        }

        private static readonly Dictionary<int, ComboBoxData> ComboBoxDataById = new Dictionary<int, ComboBoxData>();

        private static GUIStyle YellowOnHover
        {
            get
            {
                EnsureStyles();
                return _yellowOnHover;
            }
        }

        private static void EnsureStyles()
        {
            if (_style != null && _styleRevision == WindowSkin.Revision) return;
            _styleRevision = WindowSkin.Revision;
            _style = new GUIStyle(GUI.skin.window);
            _style.normal.background = null;
            _style.onNormal.background = null;
            _style.border.top = _style.border.bottom;
            _style.padding.top = _style.padding.bottom;

            // Match the active skin's normal button typography exactly. The combo's
            // closed control is already a GUILayout.Button; using the same font/font size
            // for popup rows keeps the two states visually identical.
            _style.font = GUI.skin.button.font;
            _style.fontSize = GUI.skin.button.fontSize;
            _style.fontStyle = GUI.skin.button.fontStyle;

            _yellowOnHover = new GUIStyle(GUI.skin.button);
            _yellowOnHover.hover.textColor = Color.yellow;
        }

        public static void UpdateStyles(int fontSize)
        {
            // Retained for compatibility with older callers. Combo boxes now deliberately
            // use the current button style rather than a separate hard-coded font size.
            _style = null;
            _yellowOnHover = null;
            EnsureStyles();
        }

        public static void DrawGUI()
        {
            EnsureStyles();
            foreach (ComboBoxData c in ComboBoxDataById.Values)
            {
                if (c.popupOwner == null || c.rect.height == 0 || !c.popupActive)
                    continue;

                c.rect.x = Math.Max(0, Math.Min(c.rect.x, Screen.width - c.rect.width));
                c.rect.y = Math.Max(0, Math.Min(c.rect.y, Screen.height - c.rect.height));

                GUIStyle popupStyle = _style.normal.background == null ? GUI.skin.button : _style;
                c.rect = GUILayout.Window(WindowId, c.rect, identifier =>
                {
                    c.selectedItem = GUILayout.SelectionGrid(-1, c.entries, 1, YellowOnHover, GUILayout.MinWidth(c.minWidth));
                    if (GUI.changed) c.popupActive = false;
                }, "", popupStyle);
                GUI.BringWindowToFront(WindowId);

                if (Event.current.type == EventType.MouseDown && !c.rect.Contains(Event.current.mousePosition))
                    c.popupOwner = null;
            }
        }

        public static void Close(int id)
        {
            ComboBoxData c;
            if (!ComboBoxDataById.TryGetValue(id, out c)) return;
            c.popupActive = false;
            c.popupOwner = null;
        }

        public static int Box(int id, int selectedItem, string[] entries, object caller, float width, bool locked, bool expandWidth = true)
        {
            int oldSelectedItem = selectedItem;
            if (entries == null || entries.Length == 0) return 0;
            if (entries.Length == 1)
            {
                GUILayout.Label(entries[0], GUILayout.Width(width));
                return 0;
            }

            if (selectedItem >= entries.Length) selectedItem = entries.Length - 1;
            if (selectedItem < 0) selectedItem = 0;

            ComboBoxData c;
            if (!ComboBoxDataById.TryGetValue(id, out c))
            {
                c = new ComboBoxData();
                ComboBoxDataById[id] = c;
            }

            if (c.popupOwner == caller && !c.popupActive)
            {
                c.popupOwner = null;
                selectedItem = c.selectedItem;
                GUI.changed = true;
            }

            try
            {
                bool guiChanged = GUI.changed;
                if (GUILayout.Button("↓ " + entries[selectedItem] + " ↓", GUILayout.Width(width)))
                {
                    GUI.changed = guiChanged;
                    foreach (ComboBoxData other in ComboBoxDataById.Values)
                    {
                        if (!ReferenceEquals(other, c))
                        {
                            other.popupActive = false;
                            other.popupOwner = null;
                        }
                    }
                    c.popupOwner = caller;
                    c.popupActive = true;
                    c.entries = entries;
                    c.selectedItem = selectedItem;
                    c.minWidth = width;
                    c.rect = new Rect(0, 0, 0, 0);
                }
            }
            catch { }

            if (Event.current.type == EventType.Repaint && c.popupOwner == caller && c.rect.height == 0)
            {
                c.rect = GUILayoutUtility.GetLastRect();
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;
                Vector2 clippedMousePos = Event.current.mousePosition;
                c.rect.x = c.rect.x + mousePos.x - clippedMousePos.x;
                c.rect.y = c.rect.y + mousePos.y - clippedMousePos.y + c.rect.height;
                c.rect.width = Math.Max(c.rect.width, width);
                c.rect.height = Math.Max(28f, c.entries.Length * 26f);
            }

            if (Event.current.type == EventType.MouseDown && c.popupOwner == caller && c.rect.height > 0 && !c.rect.Contains(Event.current.mousePosition))
                c.popupOwner = null;

            return locked ? oldSelectedItem : selectedItem;
        }
    }
}
