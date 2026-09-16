using ClickThroughFix;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using VesselPlanner.Core;
using VesselPlanner.KSP;

namespace VesselPlanner.UI
{
    internal sealed class MissionPlannerPage
    {
        private sealed class MissionAssemblyDraft
        {
            public string AssemblyFile = string.Empty;
            public string AssemblyName = string.Empty;
            public double UnitMassTons;
            public string CountText = "1";
            public bool AddDecoupler;
            public double DecouplerMassTons;

            public string DisplayName
            {
                get { return string.IsNullOrEmpty(AssemblyName) ? AssemblyFile : AssemblyName; }
            }
        }

        private readonly List<MissionManeuver> _maneuvers = new List<MissionManeuver>();
        private readonly Dictionary<int, Rect> _rowDataRects = new Dictionary<int, Rect>();
        private Vector2 _listScroll;
        private Vector2 _loadScroll;
        private Rect _entryWindow = new Rect(0, 0, 560, 430);
        private Rect _loadWindow = new Rect(0, 0, 420, 390);
        private bool _entryVisible;
        private bool _entryPositioned;
        private bool _loadVisible;
        private bool _loadPositioned;
        private int _insertIndex;
        private int _editIndex = -1;
        private int _selectedManeuver;
        private int _selectedBody;
        private int _selectedDestination;
        private int _selectedMoon;
        private int _selectedClipboardDeltaV = 2;
        private string _deltaVText = "0";
        private DeltaVBasis _basis = DeltaVBasis.Vacuum;
        private string _entryStatus = string.Empty;
        private string _selectionSignature = string.Empty;
        private string _clipboardCheckSignature = string.Empty;
        private bool _forceClipboardDeltaVApply;
        private string _missionName = "Unnamed Mission";
        private string _missionStatus = string.Empty;
        private readonly List<string> _missionFiles = new List<string>();
        private string _pendingMissionLoadPath;
        private string[] _bodies = new string[0];
        private string[] _moons = new string[0];
        private readonly List<SavedAssemblyInfo> _assemblies = new List<SavedAssemblyInfo>();
        private readonly List<MissionAssemblyDraft> _missionAssemblies = new List<MissionAssemblyDraft>();
        private MissionStepKind _stepKind = MissionStepKind.EnginesAndTanks;
        private Vector2 _assemblyCatalogScroll;
        private Vector2 _assemblySelectedScroll;
        private int _pendingAssemblyCatalogAdd = -1;
        private int _pendingAssemblyDraftRemove = -1;

        private static readonly string[] ManeuverNames = Enum.GetValues(typeof(Maneuver))
            .Cast<Maneuver>()
            .Select(CommonRoutines.FormatManeuver)
            .ToArray();
        private static readonly string[] ClipboardDeltaVNames = { "Ejection", "Insertion", "Total" };

        static public GUIContent upContent = new GUIContent("▲", "Move up");
        static public GUIContent downContent = new GUIContent("▼", "Move down");
        static public GUIContent addContent = new GUIContent("<B>+</B>", "Add child");
        static public GUIContent addAboveContent = new GUIContent("+▲", "Add above");
        static public GUIContent addBelowContent = new GUIContent("-▼", "Add below");
        static public GUIContent deleteContent = new GUIContent("✖", "Delete");

        private const int ManeuverComboId = 41001;
        private const int BodyComboId = 41002;
        private const int DestinationComboId = 41004;
        private const int MoonComboId = 41005;
        private const int ClipboardDeltaVComboId = 41006;
        private const int AssemblyComboId = 41009;
        private const int EntryWindowId = 19041977;
        private const int LoadWindowId = 19041978;

        public bool EntryVisible { get { return _entryVisible || _loadVisible; } }

        public void CloseEntry()
        {
            _entryVisible = false;
            _loadVisible = false;
            _editIndex = -1;
        }

        public void Initialize()
        {
            DeltaVTable.DetectPlanetPack();
            DeltaVTable.LoadDeltaV(DeltaVTable.planetPack);
            RefreshBodyLists();
        }

        public void DrawPage()
        {
            RefreshDerivedBodies();
            GUIStyle iconButtonStyle = new GUIStyle(GUI.skin.button);
            iconButtonStyle.richText = true;
            GUIStyle deleteButtonStyle = new GUIStyle(iconButtonStyle);
            deleteButtonStyle.normal.textColor = Color.red;
            deleteButtonStyle.hover.textColor = Color.red;
            deleteButtonStyle.active.textColor = Color.red;
            deleteButtonStyle.focused.textColor = Color.red;
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Mission Planner", GUILayout.Width(130));
                    GUILayout.Label("Delta-v table: " + DeltaVTable.planetPack + " (" + DeltaVTable.Count.ToString(CultureInfo.InvariantCulture) + " rows)");
                    GUILayout.FlexibleSpace();
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Mission name", GUILayout.Width(90));
                    _missionName = GUILayout.TextField(_missionName ?? string.Empty, GUILayout.Width(240));
                    if (GUILayout.Button("Save", GUILayout.Width(70))) SaveMissionPlan();
                    if (GUILayout.Button("Load", GUILayout.Width(70))) OpenMissionLoadDialog();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Saved as " + MissionPlanPersistence.SanitiseFileName(_missionName) + ".cfg");
                }
                GUILayout.Label("Build an ordered mission-step list. Each step is either Engines & Tanks or Subassemblies. Double-click a row to edit it; add entries above or below, or append with Add New Step.");
                if (!string.IsNullOrEmpty(_missionStatus)) GUILayout.Label(_missionStatus);
            }

            GUILayout.Space(6);
            DrawHeader();
            _listScroll = GUILayout.BeginScrollView(_listScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            if (_maneuvers.Count == 0)
            {
                GUILayout.Space(15);
                GUILayout.Label("No mission steps have been added.");
            }
            else
            {
                int moveFrom = -1;
                int moveTo = -1;
                for (int i = 0; i < _maneuvers.Count; i++)
                {
                    MissionManeuver item = _maneuvers[i];
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label((i + 1).ToString(CultureInfo.InvariantCulture), GUILayout.Width(28));
                        Rect rowStart = GUILayoutUtility.GetLastRect();
                        bool subassemblyStep = item.StepKind == MissionStepKind.Subassemblies;
                        GUILayout.Label(subassemblyStep ? "Subassemblies" : CommonRoutines.FormatManeuver(item.Kind), GUILayout.Width(190));
                        GUILayout.Label(subassemblyStep ? string.Empty : item.LocationSummary, GUILayout.Width(185));
                        GUILayout.Label(subassemblyStep ? item.AssemblySummary : string.Empty, GUILayout.Width(190));
                        GUILayout.Label(subassemblyStep ? "-" : item.DeltaV.ToString("0", CultureInfo.InvariantCulture), GUILayout.Width(75));
                        GUILayout.Label(subassemblyStep ? "-" : (item.DeltaVBasis == DeltaVBasis.Atmospheric ? "ASL" : "VAC"), GUILayout.Width(45));
                        Rect rowEnd = GUILayoutUtility.GetLastRect();
                        HandleRowDoubleClick(i, rowStart, rowEnd);
                        if (GUILayout.Button(addAboveContent, iconButtonStyle, GUILayout.Width(40))) OpenEntry(i);
                        if (GUILayout.Button(addBelowContent, iconButtonStyle, GUILayout.Width(40))) OpenEntry(i + 1);

                        bool oldEnabled = GUI.enabled;
                        GUI.enabled = oldEnabled && i > 0;
                        if (GUILayout.Button(upContent, iconButtonStyle, GUILayout.Width(32))) { moveFrom = i; moveTo = i - 1; }
                        GUI.enabled = oldEnabled && i < _maneuvers.Count - 1;
                        if (GUILayout.Button(downContent, iconButtonStyle, GUILayout.Width(32))) { moveFrom = i; moveTo = i + 1; }
                        GUI.enabled = oldEnabled;
                        if (GUILayout.Button(deleteContent, deleteButtonStyle, GUILayout.Width(32)))
                        {
                            _maneuvers.RemoveAt(i);
                            RefreshDerivedBodies();
                            break;
                        }
                    }
                }

                if (moveFrom >= 0 && moveTo >= 0)
                {
                    MissionManeuver moving = _maneuvers[moveFrom];
                    _maneuvers.RemoveAt(moveFrom);
                    _maneuvers.Insert(moveTo, moving);
                    RefreshDerivedBodies();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(5);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add New Step", GUILayout.Width(150))) OpenEntry(_maneuvers.Count);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Total Δv: " + _maneuvers.Sum(m => m.DeltaV).ToString("0", CultureInfo.InvariantCulture) + " m/s", GUILayout.Width(180));
            }
        }

        private void DrawHeader()
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("#", GUILayout.Width(28));
                GUILayout.Label("Step / Maneuver", GUILayout.Width(190));
                GUILayout.Label("Body / Route", GUILayout.Width(185));
                GUILayout.Label("Subassemblies", GUILayout.Width(190));
                GUILayout.Label("Δv m/s", GUILayout.Width(75));
                GUILayout.Label("Basis", GUILayout.Width(45));
                GUILayout.Label("Insert / Order");
            }
        }

        public void DrawEntryWindow()
        {
            if (_entryVisible)
            {
                if (!_entryPositioned)
                {
                    _entryWindow.x = Mathf.Max(0f, (Screen.width - _entryWindow.width) * 0.5f);
                    _entryWindow.y = Mathf.Max(0f, (Screen.height - _entryWindow.height) * 0.5f);
                    _entryPositioned = true;
                }

                bool subassemblyStep = _stepKind == MissionStepKind.Subassemblies;
                float entryWidth = subassemblyStep ? 700f : 560f;
                float entryHeight = subassemblyStep ? 690f : 465f;
                _entryWindow.width = entryWidth;
                _entryWindow.height = Mathf.Min(entryHeight, Mathf.Max(430f, Screen.height - 40f));
                _entryWindow = ClickThruBlocker.GUILayoutWindow(EntryWindowId, _entryWindow, DrawEntryContents,
                    _editIndex >= 0 ? "Edit Mission Step" : "Add Mission Step", ToolbarRegistration.winDarker ?? GUI.skin.window,
                    GUILayout.Width(entryWidth), GUILayout.Height(_entryWindow.height));
                _entryWindow.x = Mathf.Clamp(_entryWindow.x, 0f, Mathf.Max(0f, Screen.width - _entryWindow.width));
                _entryWindow.y = Mathf.Clamp(_entryWindow.y, 0f, Mathf.Max(0f, Screen.height - _entryWindow.height));
            }

            if (_loadVisible)
            {
                if (!_loadPositioned)
                {
                    _loadWindow.x = Mathf.Max(0f, (Screen.width - _loadWindow.width) * 0.5f);
                    _loadWindow.y = Mathf.Max(0f, (Screen.height - _loadWindow.height) * 0.5f);
                    _loadPositioned = true;
                }

                _loadWindow = ClickThruBlocker.GUILayoutWindow(LoadWindowId, _loadWindow, DrawMissionLoadWindow,
                    "Load Mission Plan", ToolbarRegistration.winDarker ?? GUI.skin.window,
                    GUILayout.Width(420), GUILayout.Height(390));
                _loadWindow.x = Mathf.Clamp(_loadWindow.x, 0f, Mathf.Max(0f, Screen.width - _loadWindow.width));
                _loadWindow.y = Mathf.Clamp(_loadWindow.y, 0f, Mathf.Max(0f, Screen.height - _loadWindow.height));
            }

            if (!string.IsNullOrEmpty(_pendingMissionLoadPath))
            {
                string path = _pendingMissionLoadPath;
                _pendingMissionLoadPath = null;
                LoadMissionPlan(path);
            }
        }

        private void SaveMissionPlan()
        {
            try
            {
                string path = MissionPlanPersistence.Save(_missionName, _maneuvers);
                _missionStatus = "Saved " + Path.GetFileName(path) + ".";
            }
            catch (Exception ex)
            {
                _missionStatus = "Unable to save mission plan: " + ex.Message;
                Debug.LogWarning("[VesselPlanner] Unable to save mission plan: " + ex.Message);
            }
        }

        private void OpenMissionLoadDialog()
        {
            _missionFiles.Clear();
            try
            {
                _missionFiles.AddRange(MissionPlanPersistence.ListFiles());
                _missionStatus = string.Empty;
            }
            catch (Exception ex)
            {
                _missionStatus = "Unable to list mission plans: " + ex.Message;
            }
            _loadPositioned = false;
            _loadVisible = true;
        }

        private void DrawMissionLoadWindow(int id)
        {
            GUILayout.Label("Saved mission plans in " + MissionPlanPersistence.DirectoryPath);
            _loadScroll = GUILayout.BeginScrollView(_loadScroll, GUILayout.Height(300f));
            if (_missionFiles.Count == 0)
            {
                GUILayout.Label("No saved mission plans found.");
            }
            else
            {
                foreach (string file in _missionFiles)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(Path.GetFileNameWithoutExtension(file));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Load", GUILayout.Width(70))) _pendingMissionLoadPath = file;
                    }
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close", GUILayout.Width(80))) _loadVisible = false;
            GUI.DragWindow(new Rect(0f, 0f, _loadWindow.width, _loadWindow.height));
        }

        private void LoadMissionPlan(string path)
        {
            try
            {
                MissionPlan plan = MissionPlanPersistence.Load(path);
                if (plan == null)
                {
                    _missionStatus = "Mission plan could not be read.";
                    return;
                }

                _maneuvers.Clear();
                foreach (MissionManeuver maneuver in plan.Maneuvers)
                    _maneuvers.Add(MissionPlanPersistence.CloneManeuver(maneuver));
                _missionName = string.IsNullOrEmpty(plan.Name) ? Path.GetFileNameWithoutExtension(path) : plan.Name;
                RefreshDerivedBodies();
                _entryVisible = false;
                _editIndex = -1;
                _loadVisible = false;
                _missionStatus = "Loaded " + Path.GetFileName(path) + ".";
            }
            catch (Exception ex)
            {
                _missionStatus = "Unable to load mission plan: " + ex.Message;
                Debug.LogWarning("[VesselPlanner] Unable to load mission plan: " + ex.Message);
            }
        }

        private void DrawEntryContents(int id)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Step type", GUILayout.Width(110));
                bool enginesSelected = _stepKind == MissionStepKind.EnginesAndTanks;
                bool subassembliesSelected = _stepKind == MissionStepKind.Subassemblies;
                if (GUILayout.Toggle(enginesSelected, "Engines & Tanks", "Button", GUILayout.Width(125f)) && !enginesSelected)
                {
                    _stepKind = MissionStepKind.EnginesAndTanks;
                    _entryStatus = string.Empty;
                }
                if (GUILayout.Toggle(subassembliesSelected, "Subassemblies", "Button", GUILayout.Width(115f)) && !subassembliesSelected)
                {
                    _stepKind = MissionStepKind.Subassemblies;
                    _entryStatus = string.Empty;
                    RefreshAssemblyList(null);
                }
            }

            GUILayout.Space(8);
            if (_stepKind == MissionStepKind.EnginesAndTanks)
            {
                int oldManeuver = _selectedManeuver;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Maneuver", GUILayout.Width(110));
                    _selectedManeuver = ComboBox.Box(ManeuverComboId, _selectedManeuver, ManeuverNames, this, 300f, false);
                }
                if (oldManeuver != _selectedManeuver)
                {
                    _basis = SelectedManeuver == Maneuver.Launch || SelectedManeuver == Maneuver.SubOrbitalLaunch
                        ? DeltaVBasis.Atmospheric : DeltaVBasis.Vacuum;
                    if (SelectedManeuver == Maneuver.Launch || SelectedManeuver == Maneuver.SubOrbitalLaunch)
                    {
                        string launchDefault = GetLaunchDefaultBody(_editIndex >= 0 ? _editIndex : _insertIndex);
                        _selectedBody = FindEntryIndex(_bodies, launchDefault);
                    }
                    else if (SelectedManeuver == Maneuver.Landing)
                    {
                        string landingDefault = GetLastSpecifiedBody(_editIndex >= 0 ? _editIndex : _insertIndex);
                        _selectedBody = FindEntryIndex(_bodies, landingDefault);
                    }
                    _selectionSignature = string.Empty;
                    _clipboardCheckSignature = string.Empty;
                }

                GUILayout.Space(8);
                DrawLocationSelectors();
                DrawClipboardDeltaVSelector();
                ApplySuggestedDeltaVWhenSelectionChanges();

                GUILayout.Space(8);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Needed Δv (m/s)", GUILayout.Width(110));
                    _deltaVText = GUILayout.TextField(_deltaVText ?? string.Empty, GUILayout.Width(100));
                    GUILayout.Space(16);
                    if (GUILayout.Toggle(_basis == DeltaVBasis.Atmospheric, "ASL", "Button", GUILayout.Width(70))) _basis = DeltaVBasis.Atmospheric;
                    if (GUILayout.Toggle(_basis == DeltaVBasis.Vacuum, "VAC", "Button", GUILayout.Width(70))) _basis = DeltaVBasis.Vacuum;
                }

                // Apply clipboard values after the text field has processed this frame. Otherwise an
                // active TextField edit buffer can overwrite a newly selected Ejection/Insertion/Total value.
                ApplyClipboardTransferDeltaVWhenClipboardChanges();
            }
            else
            {
                DrawAssemblySelector();
            }

            if (!string.IsNullOrEmpty(_entryStatus))
                GUILayout.Label(_entryStatus);

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(_editIndex >= 0 ? "Save" : "Add", GUILayout.Width(90))) SaveEntry();
                if (GUILayout.Button("Cancel", GUILayout.Width(90)))
                {
                    _entryVisible = false;
                    _editIndex = -1;
                }
            }

            GUI.DragWindow(new Rect(0f, 0f, _entryWindow.width, _entryWindow.height));
        }

        private void DrawLocationSelectors()
        {
            Maneuver maneuver = SelectedManeuver;
            if (maneuver == Maneuver.Launch || maneuver == Maneuver.SubOrbitalLaunch)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Body", GUILayout.Width(110));
                    _selectedBody = ComboBox.Box(BodyComboId, SafeIndex(_selectedBody, _bodies), SafeEntries(_bodies), this, 300f, false);
                }
                return;
            }

            if (maneuver == Maneuver.TransferToAnotherPlanet)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Source body", GUILayout.Width(110));
                    string source = SelectedSource;
                    GUILayout.Label(string.IsNullOrEmpty(source) ? "(no previous maneuver body)" : source, GUI.skin.box, GUILayout.Width(300), GUILayout.Height(30));
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Destination", GUILayout.Width(110));
                    _selectedDestination = ComboBox.Box(DestinationComboId, SafeIndex(_selectedDestination, _bodies), SafeEntries(_bodies), this, 300f, false);
                }
                return;
            }

            if (maneuver == Maneuver.ReturnFromAMoon)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Moon", GUILayout.Width(110));
                    _selectedMoon = ComboBox.Box(MoonComboId, SafeIndex(_selectedMoon, _moons), SafeEntries(_moons), this, 300f, false);
                }
                return;
            }

            if (NeedsBody(maneuver))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Body", GUILayout.Width(110));
                    _selectedBody = ComboBox.Box(BodyComboId, SafeIndex(_selectedBody, _bodies), SafeEntries(_bodies), this, 300f, false);
                }
            }
        }

        private void DrawAssemblySelector()
        {
            GUILayout.Label("Subassemblies");

            // Keep the Mission Planner editor intentionally parallel to the Stage-By-Stage
            // Subassemblies editor so adding, counting, and decoupling saved craft behaves
            // the same in both workflows.
            GUILayout.Label("Add one or more saved KSP subassemblies to this mission step. Each line has its own count and decoupler option.");

            GUILayout.Space(4f);
            GUILayout.Label("Available subassemblies");
            _assemblyCatalogScroll = GUILayout.BeginScrollView(_assemblyCatalogScroll, GUI.skin.box, GUILayout.Height(145f));
            if (_assemblies.Count == 0)
            {
                GUILayout.Label("No saved subassemblies were found in " + SavedAssemblyCatalog.DirectoryPath);
            }
            else
            {
                for (int i = 0; i < _assemblies.Count; i++)
                {
                    SavedAssemblyInfo assembly = _assemblies[i];
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(assembly.SelectorLabel, GUILayout.MinWidth(360f));
                        GUILayout.FlexibleSpace();
                        bool oldEnabled = GUI.enabled;
                        GUI.enabled = oldEnabled && assembly.MassAvailable;
                        if (GUILayout.Button("Add", GUILayout.Width(55f))) _pendingAssemblyCatalogAdd = i;
                        GUI.enabled = oldEnabled;
                    }
                    if (!assembly.MassAvailable && !string.IsNullOrEmpty(assembly.Error))
                        GUILayout.Label("  " + assembly.Error);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            GUILayout.Label("Subassemblies in this mission step");
            _assemblySelectedScroll = GUILayout.BeginScrollView(_assemblySelectedScroll, GUI.skin.box, GUILayout.Height(195f));
            if (_missionAssemblies.Count == 0)
            {
                GUILayout.Label("No subassemblies added yet.");
            }
            else
            {
                for (int i = 0; i < _missionAssemblies.Count; i++)
                    DrawMissionAssemblyDraftRow(i, _missionAssemblies[i]);
            }
            GUILayout.EndScrollView();

            if (_pendingAssemblyCatalogAdd >= 0)
            {
                int index = _pendingAssemblyCatalogAdd;
                _pendingAssemblyCatalogAdd = -1;
                AddMissionAssemblyFromCatalog(index);
            }
            if (_pendingAssemblyDraftRemove >= 0)
            {
                int index = _pendingAssemblyDraftRemove;
                _pendingAssemblyDraftRemove = -1;
                if (index >= 0 && index < _missionAssemblies.Count) _missionAssemblies.RemoveAt(index);
            }

            double subassemblyMass;
            bool complete = TryGetAssemblyDraftTotalMass(out subassemblyMass);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Subassembly mass", GUILayout.Width(110f));
                GUILayout.Label(subassemblyMass.ToString("0.###", CultureInfo.InvariantCulture) + " t");
            }
            if (!complete)
                GUILayout.Label(_missionAssemblies.Count == 0
                    ? "Add at least one subassembly."
                    : "Every subassembly count must be a whole number greater than zero.");
        }

        private void DrawMissionAssemblyDraftRow(int index, MissionAssemblyDraft item)
        {
            if (item == null) return;
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(item.DisplayName, GUILayout.MinWidth(250f));
                    GUILayout.Label("Count", GUILayout.Width(42f));
                    item.CountText = GUILayout.TextField(item.CountText ?? "1", GUILayout.Width(45f));
                    item.AddDecoupler = GUILayout.Toggle(item.AddDecoupler, "Decoupler", GUILayout.Width(90f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove", GUILayout.Width(65f))) _pendingAssemblyDraftRemove = index;
                }

                int quantity;
                bool quantityOk = int.TryParse(item.CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) && quantity > 0;
                double assemblyTotal = quantityOk ? quantity * Math.Max(0.0, item.UnitMassTons) : 0.0;
                double decouplerTotal = quantityOk && item.AddDecoupler ? quantity * Math.Max(0.0, item.DecouplerMassTons) : 0.0;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Subassembly: " + item.UnitMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t each", GUILayout.MinWidth(180f));
                    GUILayout.Label(item.AddDecoupler
                        ? "Decoupler: " + item.DecouplerMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t each"
                        : "Decoupler: not included", GUILayout.MinWidth(180f));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Total: " + (assemblyTotal + decouplerTotal).ToString("0.###", CultureInfo.InvariantCulture) + " t", GUILayout.Width(100f));
                }
            }
        }

        private void AddMissionAssemblyFromCatalog(int index)
        {
            if (index < 0 || index >= _assemblies.Count) return;
            SavedAssemblyInfo assembly = _assemblies[index];
            if (assembly == null || !assembly.MassAvailable) return;

            MissionAssemblyDraft existing = _missionAssemblies.FirstOrDefault(item => item != null &&
                ((!string.IsNullOrEmpty(assembly.RelativePath) && string.Equals(item.AssemblyFile, assembly.RelativePath, StringComparison.OrdinalIgnoreCase)) ||
                 (string.IsNullOrEmpty(assembly.RelativePath) && string.Equals(item.AssemblyName, assembly.DisplayName, StringComparison.OrdinalIgnoreCase))));
            if (existing != null)
            {
                int count;
                if (!int.TryParse(existing.CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count < 1) count = 1;
                existing.CountText = (count + 1).ToString(CultureInfo.InvariantCulture);
                return;
            }

            _missionAssemblies.Add(new MissionAssemblyDraft
            {
                AssemblyFile = assembly.RelativePath ?? string.Empty,
                AssemblyName = assembly.DisplayName ?? string.Empty,
                UnitMassTons = Math.Max(0.0, assembly.MassTons),
                CountText = "1",
                AddDecoupler = false,
                DecouplerMassTons = Math.Max(0.0, DecouplerMasses.GetDecouplerMassTons(assembly.BulkheadProfiles))
            });
        }

        private bool TryGetAssemblyDraftTotalMass(out double totalMass)
        {
            totalMass = 0.0;
            if (_missionAssemblies.Count == 0) return false;
            foreach (MissionAssemblyDraft item in _missionAssemblies)
            {
                if (item == null) return false;
                int quantity;
                if (!int.TryParse(item.CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) || quantity <= 0)
                    return false;
                double perCopy = Math.Max(0.0, item.UnitMassTons) +
                    (item.AddDecoupler ? Math.Max(0.0, item.DecouplerMassTons) : 0.0);
                totalMass += quantity * perCopy;
            }
            return true;
        }

        private void DrawClipboardDeltaVSelector()
        {
            if (SelectedManeuver != Maneuver.TransferToAnotherPlanet) return;

            bool clipboardAvailable = ClipboardContainsMatchingTransferData();
            int oldClipboardDeltaV = _selectedClipboardDeltaV;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Clipboard Δv", GUILayout.Width(110));
                bool oldEnabled = GUI.enabled;
                GUI.enabled = oldEnabled && clipboardAvailable;
                _selectedClipboardDeltaV = ComboBox.Box(ClipboardDeltaVComboId,
                    SafeIndex(_selectedClipboardDeltaV, ClipboardDeltaVNames), ClipboardDeltaVNames, this, 180f, false);
                GUI.enabled = oldEnabled;
            }

            if (!clipboardAvailable)
            {
                // A popup that was already open when the clipboard/destination changed must not
                // remain selectable after the control becomes disabled.
                ComboBox.Close(ClipboardDeltaVComboId);
                _selectedClipboardDeltaV = oldClipboardDeltaV;
                return;
            }

            // ComboBox applies a popup selection on the frame after the popup closes.
            // Force the clipboard value to be re-read when that returned selection changes.
            if (_selectedClipboardDeltaV != oldClipboardDeltaV)
            {
                _clipboardCheckSignature = string.Empty;
                _forceClipboardDeltaVApply = true;
                GUIUtility.keyboardControl = 0;
            }
        }

        private bool ClipboardContainsMatchingTransferData()
        {
            if (SelectedManeuver != Maneuver.TransferToAnotherPlanet ||
                string.IsNullOrEmpty(SelectedDestination))
                return false;

            string clipboardText;
            try
            {
                clipboardText = GUIUtility.systemCopyBuffer ?? string.Empty;
            }
            catch
            {
                return false;
            }

            string clipboardSource;
            string clipboardDestination;
            if (!TryParseClipboardRoute(clipboardText, out clipboardSource, out clipboardDestination))
                return false;

            if (!string.Equals(clipboardDestination, SelectedDestination, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(SelectedSource) &&
                !string.Equals(clipboardSource, SelectedSource, StringComparison.OrdinalIgnoreCase))
                return false;

            double unused;
            return TryParseClipboardDeltaV(clipboardText, 0, out unused) ||
                   TryParseClipboardDeltaV(clipboardText, 1, out unused) ||
                   TryParseClipboardDeltaV(clipboardText, 2, out unused);
        }

        private void ApplySuggestedDeltaVWhenSelectionChanges()
        {
            string signature = BuildSelectionSignature();
            if (signature == _selectionSignature) return;
            _selectionSignature = signature;
            _entryStatus = string.Empty;

            double suggested;
            bool found = false;
            switch (SelectedManeuver)
            {
                case Maneuver.Launch:
                case Maneuver.SubOrbitalLaunch:
                    found = DeltaVTable.TryGetLaunchDeltaV(SelectedBody, out suggested);
                    break;
                case Maneuver.TransferToAnotherPlanet:
                    found = DeltaVTable.TryGetTransferDeltaV(SelectedSource, SelectedDestination, out suggested);
                    break;
                case Maneuver.Landing:
                case Maneuver.Splashdown:
                    found = DeltaVTable.TryGetLandingDeltaV(SelectedBody, out suggested);
                    break;
                case Maneuver.ReturnFromAMoon:
                    found = DeltaVTable.TryGetMoonReturnDeltaV(SelectedMoon, out suggested);
                    break;
                default:
                    return;
            }

            if (found)
            {
                _deltaVText = suggested.ToString("0", CultureInfo.InvariantCulture);
                _entryStatus = "Loaded from " + DeltaVTable.planetPack + ".csv.";
            }
            else
            {
                _entryStatus = "No matching automatic Δv value was found; enter the required value manually.";
            }
        }


        private void ApplyClipboardTransferDeltaVWhenClipboardChanges()
        {
            if (SelectedManeuver != Maneuver.TransferToAnotherPlanet)
            {
                _clipboardCheckSignature = string.Empty;
                return;
            }

            string clipboardText;
            try
            {
                clipboardText = GUIUtility.systemCopyBuffer ?? string.Empty;
            }
            catch
            {
                return;
            }

            string checkSignature = SelectedSource + "|" + SelectedDestination + "|" +
                _selectedClipboardDeltaV.ToString(CultureInfo.InvariantCulture) + "|" +
                clipboardText.Length.ToString(CultureInfo.InvariantCulture) + "|" +
                clipboardText.GetHashCode().ToString(CultureInfo.InvariantCulture);
            if (!_forceClipboardDeltaVApply && checkSignature == _clipboardCheckSignature) return;
            _clipboardCheckSignature = checkSignature;
            _forceClipboardDeltaVApply = false;

            string clipboardSource;
            string clipboardDestination;
            if (!TryParseClipboardRoute(clipboardText, out clipboardSource, out clipboardDestination))
                return;

            // If the transfer text includes a route, only use it for that route. This prevents
            // an unrelated transfer left on the clipboard from replacing the current maneuver.
            if (!string.IsNullOrEmpty(clipboardSource) && !string.IsNullOrEmpty(SelectedSource) &&
                !string.Equals(clipboardSource, SelectedSource, StringComparison.OrdinalIgnoreCase))
                return;
            if (!string.IsNullOrEmpty(clipboardDestination) && !string.IsNullOrEmpty(SelectedDestination) &&
                !string.Equals(clipboardDestination, SelectedDestination, StringComparison.OrdinalIgnoreCase))
                return;

            int clipboardSelection = SafeIndex(_selectedClipboardDeltaV, ClipboardDeltaVNames);
            string deltaVName = ClipboardDeltaVNames[clipboardSelection];
            double clipboardDeltaV;
            if (!TryParseClipboardDeltaV(clipboardText, clipboardSelection, out clipboardDeltaV))
                return;

            _deltaVText = clipboardDeltaV.ToString("0.###", CultureInfo.InvariantCulture);
            _entryStatus = "Loaded " + deltaVName + " Δv from clipboard" +
                (!string.IsNullOrEmpty(clipboardSource) && !string.IsNullOrEmpty(clipboardDestination)
                    ? " (" + clipboardSource + " -> " + clipboardDestination + ")."
                    : ".");
        }

        private static bool TryParseClipboardRoute(string clipboardText, out string source, out string destination)
        {
            source = string.Empty;
            destination = string.Empty;
            if (string.IsNullOrWhiteSpace(clipboardText)) return false;

            // Expected examples include "Kerbin (@100km) -> Moho (@100km)". The altitude
            // annotations are intentionally ignored when comparing the route to the selected bodies.
            string[] lines = clipboardText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int arrow = lines[i].IndexOf("->", StringComparison.Ordinal);
                if (arrow <= 0) continue;
                source = StripClipboardOrbitAnnotation(lines[i].Substring(0, arrow));
                destination = StripClipboardOrbitAnnotation(lines[i].Substring(arrow + 2));
                return true;
            }

            return false;
        }

        private static bool TryParseClipboardDeltaV(string clipboardText, int clipboardSelection, out double deltaV)
        {
            deltaV = 0.0;
            if (string.IsNullOrWhiteSpace(clipboardText)) return false;

            // Parse the selected field explicitly. Do not use a generic label match here: the
            // Clipboard Δv choices must map one-to-one to their corresponding transfer-planner line.
            string label;
            switch (clipboardSelection)
            {
                case 0:
                    label = "Ejection";
                    break;
                case 1:
                    label = "Insertion";
                    break;
                default:
                    label = "Total";
                    break;
            }

            string pattern = @"(?im)^\s*" + Regex.Escape(label) +
                @"\s+(?:Δ\s*[vV]|d\s*[vV])\s*:\s*(?<value>[0-9][0-9,]*(?:\.[0-9]+)?)\s*(?:m/s)?\s*$";
            Match match = Regex.Match(clipboardText, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) return false;

            string valueText = match.Groups["value"].Value.Replace(",", string.Empty);
            return double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out deltaV) && deltaV >= 0.0;
        }

        private static string StripClipboardOrbitAnnotation(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return Regex.Replace(value.Trim(), @"\s*\(@[^)]*\)\s*$", string.Empty).Trim();
        }

        private void SaveEntry()
        {
            var item = new MissionManeuver { StepKind = _stepKind };

            if (_stepKind == MissionStepKind.EnginesAndTanks)
            {
                if (SelectedManeuver == Maneuver.None)
                {
                    _entryStatus = "Select a maneuver.";
                    return;
                }

                double dv;
                if (!double.TryParse(_deltaVText, NumberStyles.Float, CultureInfo.InvariantCulture, out dv) || dv < 0.0)
                {
                    _entryStatus = "Enter a non-negative Δv value.";
                    return;
                }

                item.Kind = SelectedManeuver;
                item.DeltaV = dv;
                item.DeltaVBasis = _basis;

                if (SelectedManeuver == Maneuver.TransferToAnotherPlanet)
                {
                    item.SourceBody = SelectedSource;
                    if (string.IsNullOrEmpty(item.SourceBody))
                    {
                        _entryStatus = "Transfer source requires a preceding maneuver with a body.";
                        return;
                    }
                    item.DestinationBody = SelectedDestination;
                }
                else if (SelectedManeuver == Maneuver.ReturnFromAMoon)
                {
                    item.Body = SelectedMoon;
                }
                else if (NeedsBody(SelectedManeuver))
                {
                    item.Body = SelectedBody;
                }
            }
            else
            {
                double assemblyMass;
                if (!TryGetAssemblyDraftTotalMass(out assemblyMass))
                {
                    _entryStatus = _missionAssemblies.Count == 0
                        ? "Add at least one subassembly."
                        : "Every subassembly count must be a whole number greater than zero.";
                    return;
                }

                item.Kind = Maneuver.None;
                item.DeltaV = 0.0;
                item.DeltaVBasis = DeltaVBasis.Vacuum;
                foreach (MissionAssemblyDraft draft in _missionAssemblies)
                {
                    if (draft == null) continue;
                    int quantity;
                    if (!int.TryParse(draft.CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) || quantity <= 0) continue;
                    item.Assemblies.Add(new PlannedSubassembly
                    {
                        AssemblyFile = draft.AssemblyFile ?? string.Empty,
                        AssemblyName = draft.AssemblyName ?? string.Empty,
                        UnitMassTons = Math.Max(0.0, draft.UnitMassTons),
                        Quantity = quantity,
                        AddDecoupler = draft.AddDecoupler,
                        DecouplerMassTons = Math.Max(0.0, draft.DecouplerMassTons)
                    });
                }
            }

            if (_editIndex >= 0 && _editIndex < _maneuvers.Count)
            {
                _maneuvers[_editIndex] = item;
            }
            else
            {
                _insertIndex = Math.Max(0, Math.Min(_insertIndex, _maneuvers.Count));
                _maneuvers.Insert(_insertIndex, item);
            }
            RefreshDerivedBodies();
            _entryVisible = false;
            _editIndex = -1;
        }

        private void OpenEntry(int insertIndex)
        {
            _editIndex = -1;
            _insertIndex = insertIndex;
            _entryVisible = true;
            _entryPositioned = false;
            _selectedManeuver = 0;
            _selectedClipboardDeltaV = 2;
            _deltaVText = "0";
            _basis = DeltaVBasis.Vacuum;
            _entryStatus = string.Empty;
            _selectionSignature = string.Empty;
            _clipboardCheckSignature = string.Empty;
            _stepKind = MissionStepKind.EnginesAndTanks;
            _missionAssemblies.Clear();
            _pendingAssemblyCatalogAdd = -1;
            _pendingAssemblyDraftRemove = -1;
            RefreshBodyLists();
            RefreshAssemblyList(null);
        }

        private void OpenEdit(int index)
        {
            if (index < 0 || index >= _maneuvers.Count) return;

            RefreshBodyLists();
            MissionManeuver item = _maneuvers[index];
            RefreshAssemblyList(item);
            _editIndex = index;
            _insertIndex = index;
            _entryVisible = true;
            _entryPositioned = false;
            _selectedManeuver = (int)item.Kind;
            _selectedClipboardDeltaV = 2;
            _selectedBody = FindEntryIndex(_bodies, item.Body);
            _selectedDestination = FindEntryIndex(_bodies, item.DestinationBody);
            _selectedMoon = FindEntryIndex(_moons, item.Body);
            _deltaVText = item.DeltaV.ToString("0.###", CultureInfo.InvariantCulture);
            _basis = item.DeltaVBasis;
            _stepKind = item.StepKind;
            _missionAssemblies.Clear();
            if (_stepKind == MissionStepKind.Subassemblies)
                LoadAssemblyDraftsFromManeuver(item);
            _pendingAssemblyCatalogAdd = -1;
            _pendingAssemblyDraftRemove = -1;
            _entryStatus = string.Empty;
            _selectionSignature = BuildSelectionSignature();
            _clipboardCheckSignature = string.Empty;
        }

        private void HandleRowDoubleClick(int index, Rect rowStart, Rect rowEnd)
        {
            Event e = Event.current;
            if (e == null) return;

            // GUILayoutUtility.GetLastRect is reliable during Repaint. Cache the row's
            // data area there, then use the cached rectangle for mouse events.
            if (e.type == EventType.Repaint)
            {
                _rowDataRects[index] = Rect.MinMaxRect(rowStart.xMin, Math.Min(rowStart.yMin, rowEnd.yMin),
                    rowEnd.xMax, Math.Max(rowStart.yMax, rowEnd.yMax));
                return;
            }

            if (e.type != EventType.MouseDown || e.button != 0 || e.clickCount < 2) return;

            Rect rowRect;
            if (!_rowDataRects.TryGetValue(index, out rowRect) || !rowRect.Contains(e.mousePosition)) return;

            OpenEdit(index);
            e.Use();
        }

        private void RefreshDerivedBodies()
        {
            for (int i = 0; i < _maneuvers.Count; i++)
            {
                MissionManeuver item = _maneuvers[i];
                if (item.Kind == Maneuver.TransferToAnotherPlanet)
                    item.SourceBody = GetPreviousManeuverBody(i);
            }
        }

        private string GetLaunchDefaultBody(int index)
        {
            int lastIndex = Math.Min(index - 1, _maneuvers.Count - 1);
            for (int i = lastIndex; i >= 0; i--)
            {
                MissionManeuver previous = _maneuvers[i];
                if (previous.Kind == Maneuver.Landing && !string.IsNullOrEmpty(previous.Body))
                    return previous.Body;
            }

            return GetHomeBodyName();
        }

        private string GetLastSpecifiedBody(int index)
        {
            int lastIndex = Math.Min(index - 1, _maneuvers.Count - 1);
            for (int i = lastIndex; i >= 0; i--)
            {
                MissionManeuver previous = _maneuvers[i];
                if (previous.Kind == Maneuver.TransferToAnotherPlanet &&
                    !string.IsNullOrEmpty(previous.DestinationBody))
                    return previous.DestinationBody;

                if (!string.IsNullOrEmpty(previous.Body))
                    return previous.Body;
            }

            return GetHomeBodyName();
        }

        private static string GetHomeBodyName()
        {
            try
            {
                string homeBody = FlightGlobals.GetHomeBodyName();
                if (!string.IsNullOrEmpty(homeBody))
                    return homeBody;
            }
            catch
            {
                // Fall through to the stock default if KSP has not initialized the body list yet.
            }

            return "Kerbin";
        }

        private string GetPreviousManeuverBody(int index)
        {
            int previousIndex = Math.Min(index - 1, _maneuvers.Count - 1);
            for (int i = previousIndex; i >= 0; i--)
            {
                MissionManeuver previous = _maneuvers[i];
                if (previous == null || previous.StepKind == MissionStepKind.Subassemblies) continue;
                if (previous.Kind == Maneuver.TransferToAnotherPlanet)
                    return previous.DestinationBody ?? string.Empty;
                return previous.Body ?? string.Empty;
            }
            return string.Empty;
        }

        private string BuildSelectionSignature()
        {
            return ((int)SelectedManeuver).ToString(CultureInfo.InvariantCulture) + "|" +
                   SelectedBody + "|" + SelectedSource + "|" + SelectedDestination + "|" + SelectedMoon;
        }

        private static int FindEntryIndex(string[] entries, string value)
        {
            if (entries == null || entries.Length == 0 || string.IsNullOrEmpty(value)) return 0;
            for (int i = 0; i < entries.Length; i++)
                if (string.Equals(entries[i], value, StringComparison.OrdinalIgnoreCase)) return i;
            return 0;
        }

        private void RefreshAssemblyList(MissionManeuver preserve)
        {
            _assemblies.Clear();
            try
            {
                _assemblies.AddRange(SavedAssemblyCatalog.Scan());
            }
            catch (Exception ex)
            {
                _entryStatus = "Unable to list saved subassemblies: " + ex.Message;
            }

            if (preserve != null)
            {
                foreach (PlannedSubassembly saved in preserve.Assemblies)
                {
                    if (saved == null || FindAssemblyIndex(saved.AssemblyFile, saved.AssemblyName) >= 0) continue;
                    _assemblies.Add(new SavedAssemblyInfo
                    {
                        DisplayName = saved.AssemblyName,
                        RelativePath = saved.AssemblyFile,
                        MassTons = saved.UnitMassTons,
                        MassAvailable = saved.UnitMassTons > 0.0,
                        Error = "Saved subassembly file is not currently available; using the mass stored in the mission plan."
                    });
                }

                if (preserve.Assemblies.Count == 0 && preserve.HasAssembly &&
                    FindAssemblyIndex(preserve.AssemblyFile, preserve.AssemblyName) < 0)
                {
                    _assemblies.Add(new SavedAssemblyInfo
                    {
                        DisplayName = preserve.AssemblyName,
                        RelativePath = preserve.AssemblyFile,
                        MassTons = preserve.AssemblyMassTons,
                        MassAvailable = preserve.AssemblyMassTons > 0.0,
                        Error = "Saved subassembly file is not currently available; using the mass stored in the mission plan."
                    });
                }
            }
        }

        private int FindAssemblyIndex(string relativePath, string displayName)
        {
            for (int i = 0; i < _assemblies.Count; i++)
            {
                SavedAssemblyInfo item = _assemblies[i];
                if (!string.IsNullOrEmpty(relativePath) && string.Equals(item.RelativePath ?? string.Empty, relativePath, StringComparison.OrdinalIgnoreCase))
                    return i;
                if (string.IsNullOrEmpty(relativePath) && !string.IsNullOrEmpty(displayName) &&
                    string.Equals(item.DisplayName ?? string.Empty, displayName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void LoadAssemblyDraftsFromManeuver(MissionManeuver maneuver)
        {
            _missionAssemblies.Clear();
            if (maneuver == null) return;

            foreach (PlannedSubassembly saved in maneuver.Assemblies)
            {
                if (saved == null) continue;
                double decouplerMass = Math.Max(0.0, saved.DecouplerMassTons);
                int catalogIndex = FindAssemblyIndex(saved.AssemblyFile, saved.AssemblyName);
                if (decouplerMass <= 0.0 && catalogIndex >= 0 && catalogIndex < _assemblies.Count)
                    decouplerMass = Math.Max(0.0, DecouplerMasses.GetDecouplerMassTons(_assemblies[catalogIndex].BulkheadProfiles));

                _missionAssemblies.Add(new MissionAssemblyDraft
                {
                    AssemblyFile = saved.AssemblyFile ?? string.Empty,
                    AssemblyName = saved.AssemblyName ?? string.Empty,
                    UnitMassTons = Math.Max(0.0, saved.UnitMassTons),
                    CountText = Math.Max(1, saved.Quantity).ToString(CultureInfo.InvariantCulture),
                    AddDecoupler = saved.AddDecoupler,
                    DecouplerMassTons = decouplerMass
                });
            }

            if (_missionAssemblies.Count == 0 && maneuver.HasAssembly)
            {
                int catalogIndex = FindAssemblyIndex(maneuver.AssemblyFile, maneuver.AssemblyName);
                double decouplerMass = catalogIndex >= 0 && catalogIndex < _assemblies.Count
                    ? Math.Max(0.0, DecouplerMasses.GetDecouplerMassTons(_assemblies[catalogIndex].BulkheadProfiles))
                    : 0.0;
                _missionAssemblies.Add(new MissionAssemblyDraft
                {
                    AssemblyFile = maneuver.AssemblyFile ?? string.Empty,
                    AssemblyName = maneuver.AssemblyName ?? string.Empty,
                    UnitMassTons = Math.Max(0.0, maneuver.AssemblyMassTons),
                    CountText = "1",
                    AddDecoupler = false,
                    DecouplerMassTons = decouplerMass
                });
            }
        }

        private void RefreshBodyLists()
        {
            _bodies = DeltaVTable.GetBodies(false);
            _moons = DeltaVTable.GetBodies(true);
            if (_bodies.Length == 0) _bodies = new[] { "No bodies in CSV" };
            if (_moons.Length == 0) _moons = new[] { "No moons in CSV" };
            _selectedBody = SafeIndex(_selectedBody, _bodies);
            _selectedDestination = SafeIndex(_selectedDestination, _bodies);
            _selectedMoon = SafeIndex(_selectedMoon, _moons);
        }

        private Maneuver SelectedManeuver { get { return (Maneuver)Mathf.Clamp(_selectedManeuver, 0, ManeuverNames.Length - 1); } }
        private string SelectedBody
        {
            get { return _bodies.Length == 0 ? string.Empty : _bodies[SafeIndex(_selectedBody, _bodies)]; }
        }
        private string SelectedSource { get { return GetPreviousManeuverBody(_editIndex >= 0 ? _editIndex : _insertIndex); } }
        private string SelectedDestination { get { return _bodies.Length == 0 ? string.Empty : _bodies[SafeIndex(_selectedDestination, _bodies)]; } }
        private string SelectedMoon { get { return _moons.Length == 0 ? string.Empty : _moons[SafeIndex(_selectedMoon, _moons)]; } }

        private static int SafeIndex(int value, string[] entries) { return entries == null || entries.Length == 0 ? 0 : Mathf.Clamp(value, 0, entries.Length - 1); }
        private static string[] SafeEntries(string[] entries) { return entries != null && entries.Length > 0 ? entries : new[] { "(none)" }; }

        private static bool NeedsBody(Maneuver maneuver)
        {
            switch (maneuver)
            {
                case Maneuver.Launch:
                case Maneuver.SubOrbitalLaunch:
                case Maneuver.Orbit:
                case Maneuver.Reentry:
                case Maneuver.Landing:
                case Maneuver.Splashdown:
                case Maneuver.ChangeApoapsis:
                case Maneuver.ChangeBothPeAndAp:
                case Maneuver.ChangeInclination:
                case Maneuver.ChangePeriapsis:
                case Maneuver.ChangeSemiMajorAxis:
                    return true;
                default:
                    return false;
            }
        }

    }
}
