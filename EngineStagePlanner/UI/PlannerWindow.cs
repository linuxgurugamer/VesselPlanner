using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using EngineStagePlanner.Core;
using EngineStagePlanner.KSP;
using KSP.UI.Screens;
using ClickThroughFix;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EngineStagePlanner.UI
{
    public sealed class PlannerWindow
    {
        private Rect _window = new Rect(100, 70, 1220, 800);
        private Vector2 _resultsScroll;
        private Vector2 _detailScroll;
        private Vector2 _tankScroll;
        private bool _planningMode = true;
        private int _stage;
        private string _stageText = "0";
        private string _targetDv = "2500";
        private string _minTwr = "1.25";
        private string _payload = "5.0";
        private string _maxEngines = "8";
        private string _tankRatio = "0.125";
        private bool _useCraftPayload = true;
        private bool _includeSolid;
        private bool _includeAir;
        private bool _includeElectric;
        private bool _ignoreMonoprop = true;
        private bool _filterByBulkheadSize;
        private bool _pickStageFromPart;
        private int _pickStageArmedFrame = -1;
        private bool _pickInputLockApplied;
        private bool _editorLogicDisabledForPick;
        private bool _editorLogicWasEnabled;
        private bool _releasePickGuardsWhenMouseReleased;
        private const string PickStageInputLockId = "EngineStagePlanner_PickStageFromPart";
        private static readonly FieldInfo StageGroupDragHandlerField = typeof(StageGroup).GetField("dragHandler", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private string _engineNameFilter = "";
        private readonly List<CelestialBody> _bodies = new List<CelestialBody>();
        private int _selectedBodyIndex;
        private double _altitudeMeters;
        private bool _showPlanetDropdown;
        private Vector2 _planetScroll;
        private bool _resizing;
        private Vector2 _pendingResizeDelta;
        private const float MinWindowWidth = 1000f;
        private const float MinWindowHeight = 620f;
        private const float MinEngineListHeight = 150f;
        private const float MaxEngineListHeight = 300f;
        private OptimizationMode _mode = OptimizationMode.LowestStageMass;
        private List<EngineCandidate> _engines = new List<EngineCandidate>();
        private List<TankCandidate> _tanks = new List<TankCandidate>();
        private List<TankSuggestion> _tankSuggestions = new List<TankSuggestion>();
        private List<StageSolution> _solutions = new List<StageSolution>();
        private StageSolution _selected;
        private ExistingStageSnapshot _snapshot;
        private string _status = "";
        private GUIStyle _right;
        private SolutionSortColumn _solutionSortColumn = SolutionSortColumn.None;
        private bool _solutionSortAscending = true;
        private TankSortColumn _tankSortColumn = TankSortColumn.None;
        private bool _tankSortAscending = true;

        private enum SolutionSortColumn
        {
            None, Engine, Count, WetMass, AtmosphericDeltaV, VacuumDeltaV, CostEfficiency, Twr, MaxTwr, Isp, Burn, Fuel
        }

        private enum TankSortColumn
        {
            None, Tank, Count, DryMass, Excess, Capacity
        }

        public bool Visible { get; set; } = true;

        public void Initialize()
        {
            RefreshBodies();
            RefreshDatabases();
            _stage = Math.Min(EditorStageScanner.MaxStage, Math.Max(0, _stage));
            _stageText = _stage.ToString(CultureInfo.InvariantCulture);
            RefreshStage();
        }

        public void Draw()
        {
            if (!Visible) return;
            EnsureStyles();
            _window = ClickThruBlocker.GUILayoutWindow(19041968, _window, DrawWindow, "Engine Stage Planner", GUILayout.MinWidth(MinWindowWidth), GUILayout.MinHeight(MinWindowHeight));
            if (_pendingResizeDelta.sqrMagnitude > 0f)
            {
                _window.width = Mathf.Max(MinWindowWidth, _window.width + _pendingResizeDelta.x);
                _window.height = Mathf.Max(MinWindowHeight, _window.height + _pendingResizeDelta.y);
                _pendingResizeDelta = Vector2.zero;
            }
            ClampWindow();
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(!_planningMode, "Analyze Existing", "Button", GUILayout.Height(25))) _planningMode = false;
            if (GUILayout.Toggle(_planningMode, "Planning", "Button", GUILayout.Height(25))) _planningMode = true;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshDatabases();
                RefreshStage();
                RecalculateForFilterChange();
            }
            if (GUILayout.Button("×", GUILayout.Width(30))) Visible = false;
            GUILayout.EndHorizontal();

            DrawStageSelector();
            GUILayout.Space(4);
            if (_planningMode) DrawPlanning(); else DrawAnalysis();
            DrawResizeHandle();
            GUI.DragWindow(new Rect(0f, 0f, _window.width, _window.height));
        }

        private void DrawStageSelector()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Stage", GUILayout.Width(45));

            string newStageText = GUILayout.TextField(_stageText ?? string.Empty, GUILayout.Width(48));
            if (!string.Equals(newStageText, _stageText, StringComparison.Ordinal))
            {
                _stageText = newStageText;
                int enteredStage;
                if (int.TryParse(_stageText, NumberStyles.Integer, CultureInfo.InvariantCulture, out enteredStage) && enteredStage >= 0 && enteredStage != _stage)
                    SetStage(enteredStage);
            }

            // Keep the increment button immediately adjacent to the editable stage field.
            if (GUILayout.Button("+", GUILayout.Width(28))) SetStage(_stage + 1);
            if (GUILayout.Button("-", GUILayout.Width(28))) SetStage(Math.Max(0, _stage - 1));
            if (GUILayout.Button(_pickStageFromPart ? "Cancel Pick" : "Pick Stage", GUILayout.Width(85)))
            {
                if (_pickStageFromPart) CancelStagePartPick("Stage selection cancelled.");
                else BeginStagePartPick();
            }

            GUILayout.Label("Craft max: " + EditorStageScanner.MaxStage, GUILayout.Width(90));
            if (_snapshot != null)
            {
                GUILayout.Label("Craft wet: " + F(_snapshot.VesselWetMassTons) + " t");
                GUILayout.Label("Stage fuel: " + F(_snapshot.StagePropellantMassTons) + " t");
                if (_snapshot.TopNodeSizes.Count > 0)
                    GUILayout.Label("Stage top node: " + FormatNodeSizes(_snapshot.TopNodeSizes));
            }
            GUILayout.EndHorizontal();
            if (_pickStageFromPart)
                GUILayout.Label("Click a part on the current vessel to use its staging-display stage. Esc or right-click cancels.");
        }

        public void Update()
        {
            // A successful pick is detected before stock EditorLogic.Update runs. Keep
            // the editor disabled and the KSP input lock in place through the entire
            // mouse-down frame, then release them only after the left button is up.
            // Otherwise EditorLogic sees the same click later in the frame and grabs
            // the clicked part plus its attached branch.
            if (_releasePickGuardsWhenMouseReleased)
            {
                if (!Input.GetMouseButton(0))
                {
                    _releasePickGuardsWhenMouseReleased = false;
                    ReleaseStagePartPickGuards();
                }
                return;
            }

            if (!_pickStageFromPart)
            {
                TrySelectStageFromStageListClick();
                return;
            }
            if (!Visible)
            {
                CancelStagePartPick(null);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelStagePartPick("Stage selection cancelled.");
                return;
            }

            // Do not consume the same mouse-down that armed Pick Stage.
            if (Time.frameCount <= _pickStageArmedFrame || !Input.GetMouseButtonDown(0)) return;

            Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (_window.Contains(guiMouse)) return;

            Part part = FindEditorPartUnderMouse();
            if (part == null)
            {
                _status = "No vessel part under the cursor. Click a vessel part, or Esc/right-click to cancel.";
                return;
            }

            int pickedStage = EditorStageScanner.ResolveStageForPart(part);
            if (pickedStage < 0)
            {
                string partName = part.partInfo != null ? part.partInfo.title : part.name;
                _status = partName + " is not assigned to an activation stage.";
                return;
            }

            string pickedName = part.partInfo != null ? part.partInfo.title : part.name;

            // Do not release the editor/input guards here.  This Update executes
            // before stock EditorLogic.Update, so releasing here would allow KSP to
            // process this exact mouse-down as a normal part grab.
            _pickStageFromPart = false;
            _pickStageArmedFrame = -1;
            _releasePickGuardsWhenMouseReleased = true;

            if (pickedStage == _stage)
            {
                RefreshStage();
                RecalculateForFilterChange();
            }
            else
            {
                SetStage(pickedStage);
            }
            _status = "Selected stage " + pickedStage.ToString(CultureInfo.InvariantCulture) + " from " + pickedName + ".";
        }

        public void Dispose()
        {
            CancelStagePartPick(null);
        }

        private void BeginStagePartPick()
        {
            _pickStageFromPart = true;
            _pickStageArmedFrame = Time.frameCount;
            _status = "Click a part on the current vessel to select its stage. Esc or right-click cancels.";
            try
            {
                // Keep an input lock as a secondary guard, but also disable the stock
                // EditorLogic behaviour while picking.  InputLockManager by itself does
                // not stop EditorLogic's vessel-part grab/detach handler from seeing the
                // mouse click on all KSP 1.12 installations.
                InputLockManager.SetControlLock(PickStageInputLockId);
                _pickInputLockApplied = true;
            }
            catch
            {
                _pickInputLockApplied = false;
            }

            try
            {
                if (EditorLogic.fetch != null)
                {
                    _editorLogicWasEnabled = EditorLogic.fetch.enabled;
                    if (_editorLogicWasEnabled)
                    {
                        EditorLogic.fetch.enabled = false;
                        _editorLogicDisabledForPick = true;
                    }
                }
            }
            catch
            {
                _editorLogicDisabledForPick = false;
            }
        }

        private void CancelStagePartPick(string message)
        {
            _pickStageFromPart = false;
            _pickStageArmedFrame = -1;
            _releasePickGuardsWhenMouseReleased = false;
            ReleaseStagePartPickGuards();
            if (!string.IsNullOrEmpty(message)) _status = message;
        }

        private void ReleaseStagePartPickGuards()
        {
            if (_editorLogicDisabledForPick)
            {
                try
                {
                    if (EditorLogic.fetch != null)
                        EditorLogic.fetch.enabled = _editorLogicWasEnabled;
                }
                catch { }
                _editorLogicDisabledForPick = false;
            }
            _editorLogicWasEnabled = false;

            if (_pickInputLockApplied)
            {
                try { InputLockManager.RemoveControlLock(PickStageInputLockId); }
                catch { }
                _pickInputLockApplied = false;
            }
        }

        private bool TrySelectStageFromStageListClick()
        {
            // This is separate from Pick Stage.  It passively watches the stock editor
            // staging list and follows a normal click on the orange stage header/tab.
            // Individual part icons in the stage remain untouched.
            if (!Visible || !Input.GetMouseButtonDown(0) || EventSystem.current == null)
                return false;

            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = Input.mousePosition;
            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);

            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject == null) continue;

                StageGroup group = FindStageGroupInParents(hit.gameObject);
                if (group == null) continue;

                // Clicking a part icon is not the same thing as clicking the orange
                // stage tab/header, so explicitly ignore StageIcon descendants.
                if (IsStageIconHit(hit.gameObject, group)) continue;
                if (!IsStageHeaderHit(hit.gameObject, group)) continue;

                int clickedStage = group.inverseStageIndex;
                if (clickedStage < 0) continue;

                if (clickedStage == _stage)
                {
                    RefreshStage();
                    RecalculateForFilterChange();
                }
                else
                {
                    SetStage(clickedStage);
                }

                _status = "Selected stage " + clickedStage.ToString(CultureInfo.InvariantCulture) + " from the staging list.";
                return true;
            }

            return false;
        }

        private static StageGroup FindStageGroupInParents(GameObject gameObject)
        {
            Transform current = gameObject != null ? gameObject.transform : null;
            while (current != null)
            {
                StageGroup group = current.GetComponent<StageGroup>();
                if (group != null) return group;
                current = current.parent;
            }
            return null;
        }

        private static bool IsStageIconHit(GameObject gameObject, StageGroup group)
        {
            Transform current = gameObject != null ? gameObject.transform : null;
            Transform groupTransform = group != null ? group.transform : null;
            while (current != null)
            {
                if (current.GetComponent<StageIcon>() != null) return true;
                if (current == groupTransform) break;
                current = current.parent;
            }
            return false;
        }

        private static bool IsStageHeaderHit(GameObject gameObject, StageGroup group)
        {
            if (gameObject == null || group == null) return false;

            // KSP's StageGroup stores the orange stage header's UIDragPanel in its
            // protected dragHandler field.  Reflection lets us test the actual stock
            // header without depending on a protected member at compile time.
            try
            {
                if (StageGroupDragHandlerField != null)
                {
                    Component dragHandler = StageGroupDragHandlerField.GetValue(group) as Component;
                    if (dragHandler != null && IsTransformWithin(gameObject.transform, dragHandler.transform))
                        return true;
                }
            }
            catch { }

            // Fallback for UI-prefab variations: the root/header objects use stage,
            // header, or drag in their names.  Still stop at the StageGroup root and
            // never accept a StageIcon (filtered by the caller above).
            if (gameObject.transform == group.transform) return true;

            Transform current = gameObject.transform;
            while (current != null && current != group.transform)
            {
                string name = current.name ?? string.Empty;
                string lower = name.ToLowerInvariant();
                if (lower.Contains("header") || lower.Contains("drag") || lower.Contains("stageheader"))
                    return true;
                current = current.parent;
            }

            return false;
        }

        private static bool IsTransformWithin(Transform child, Transform parent)
        {
            if (child == null || parent == null) return false;
            Transform current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = current.parent;
            }
            return false;
        }

        private static Part FindEditorPartUnderMouse()
        {
            if (EditorLogic.fetch == null || EditorLogic.fetch.ship == null || EditorLogic.fetch.ship.parts == null)
                return null;

            Camera camera = EditorLogic.fetch.editorCamera != null ? EditorLogic.fetch.editorCamera : Camera.main;
            if (camera == null) return null;

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
            if (hits == null || hits.Length == 0) return null;
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                Part part = null;
                try { part = FlightGlobals.GetPartUpwardsCached(hit.collider.gameObject); }
                catch { }
                if (part != null && EditorLogic.fetch.ship.parts.Contains(part))
                    return part;
            }
            return null;
        }

        private void SetStage(int stage)
        {
            stage = Math.Max(0, stage);
            if (_stage == stage && string.Equals(_stageText, stage.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                return;

            _stage = stage;
            _stageText = _stage.ToString(CultureInfo.InvariantCulture);
            RefreshStage();
            RecalculateForFilterChange();
        }

        private void DrawAnalysis()
        {
            if (_snapshot == null) RefreshStage();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("box", GUILayout.Width(320));
            GUILayout.Label("Existing stage");
            Row("Craft wet mass", F(_snapshot.VesselWetMassTons) + " t");
            Row("Craft dry mass", F(_snapshot.VesselDryMassTons) + " t");
            Row("Payload above stage", F(_snapshot.PayloadAboveStageMassTons) + " t");
            Row("Stage propellant (current)", F(_snapshot.StagePropellantMassTons) + " t");
            Row("Stage propellant (full)", F(_snapshot.StagePropellantCapacityMassTons) + " t");
            Row("Stage tank capacity", F(_snapshot.StageTankCapacityUnits) + " units");
            Row("Known tank volume", _snapshot.StageTankVolumeLiters > 0 ? F(_snapshot.StageTankVolumeLiters) + " L" : "n/a");
            GUILayout.Space(4);
            GUILayout.Label("Current engines: " + (_snapshot.CurrentEngines.Count > 0 ? string.Join(", ", _snapshot.CurrentEngines.ToArray()) : "none found"));
            foreach (var r in _snapshot.Resources)
                GUILayout.Label("  " + r.Name + ": " + F(r.Capacity) + " units capacity");
            GUILayout.Space(6);
            GUILayout.Label("Simulation environment");
            Field("Minimum TWR", ref _minTwr);
            Row("TWR gravity", F(GetSelectedTwrGravity()) + " m/s² (surface)");
            Field("Max engines", ref _maxEngines);
            bool analysisFilterChanged = false;
            analysisFilterChanged |= ToggleChanged(ref _ignoreMonoprop, "Ignore monopropellant");
            analysisFilterChanged |= ToggleChanged(ref _includeSolid, "Include solid fuel");
            analysisFilterChanged |= ToggleChanged(ref _includeAir, "Include air-breathing");
            analysisFilterChanged |= ToggleChanged(ref _includeElectric, "Include electric-propellant");
            if (analysisFilterChanged) RecalculateForFilterChange();
            if (GUILayout.Button("Simulate all engines", GUILayout.Height(30))) SimulateExisting();
            GUILayout.Label(_status);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawResults();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (_selected != null) DrawSelected(false);
        }

        private void DrawPlanning()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("box", GUILayout.Width(310));
            GUILayout.Label("Requirements");
            Field("Target Δv (m/s)", ref _targetDv);
            GUILayout.Label("Target Δv is solved at the selected planet/altitude atmosphere.");
            Field("Minimum TWR", ref _minTwr);
            _useCraftPayload = GUILayout.Toggle(_useCraftPayload, "Use craft payload above selected stage");
            GUI.enabled = !_useCraftPayload;
            Field("Payload mass (t)", ref _payload);
            GUI.enabled = true;
            Row("TWR gravity", F(GetSelectedTwrGravity()) + " m/s² (surface)");
            Field("Max engines", ref _maxEngines);
            Field("Tank dry/fuel mass ratio", ref _tankRatio);

            GUILayout.Space(4);
            GUILayout.Label("Include engine classes");
            bool planningFilterChanged = false;
            planningFilterChanged |= ToggleChanged(ref _includeSolid, "Solid fuel");
            planningFilterChanged |= ToggleChanged(ref _includeAir, "Air-breathing");
            planningFilterChanged |= ToggleChanged(ref _includeElectric, "Electric-propellant");
            if (planningFilterChanged) RecalculateForFilterChange();
            GUILayout.Space(4);
            GUILayout.Label("Optimize");
            bool optimizationChanged = false;
            GUILayout.BeginVertical("box");
            optimizationChanged |= OptimizationToggle(OptimizationMode.LowestStageMass);
            optimizationChanged |= OptimizationToggle(OptimizationMode.LowestPropellantMass);
            optimizationChanged |= OptimizationToggle(OptimizationMode.LowestCost);
            optimizationChanged |= OptimizationToggle(OptimizationMode.ShortestBurn);
            optimizationChanged |= OptimizationToggle(OptimizationMode.HighestTwr);
            optimizationChanged |= OptimizationToggle(OptimizationMode.HighestIsp);
            GUILayout.EndVertical();
            if (optimizationChanged) RecalculateForFilterChange();
            GUILayout.Space(6);
            if (GUILayout.Button("Calculate", GUILayout.Height(32))) Calculate();
            GUILayout.Label(_status);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawResults();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (_selected != null) DrawSelected(true);
        }

        private void DrawResults()
        {
            GUILayout.Label("Candidate engine filters");
            GUILayout.BeginVertical("box");

            DrawEnvironmentControls();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Engine name:", GUILayout.Width(85));
            string newEngineNameFilter = GUILayout.TextField(_engineNameFilter ?? string.Empty, GUILayout.Width(220));
            if (!string.Equals(newEngineNameFilter, _engineNameFilter, StringComparison.Ordinal))
            {
                _engineNameFilter = newEngineNameFilter;
                RecalculateForFilterChange();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool oldBulkheadFilter = _filterByBulkheadSize;
            _filterByBulkheadSize = GUILayout.Toggle(_filterByBulkheadSize, "Match stage bulkhead size", GUILayout.Width(190));
            bool bulkheadFilterChanged = oldBulkheadFilter != _filterByBulkheadSize;
            if (_snapshot != null && _snapshot.TopNodeSizes.Count > 0)
                GUILayout.Label("Stage top node: " + FormatNodeSizes(_snapshot.TopNodeSizes));
            else
                GUILayout.Label("Stage: no engine top node detected");
            GUILayout.EndHorizontal();
            if (bulkheadFilterChanged) RecalculateForFilterChange();

            if (_filterByBulkheadSize && (_snapshot == null || _snapshot.TopNodeSizes.Count == 0))
                GUILayout.Label("Bulkhead filtering cannot be applied until an engine top node is detected for the selected stage.");

            GUILayout.EndVertical();
            GUILayout.Space(3);

            GUILayout.Label("Candidates (click a column heading to sort; click engine name for details; Add selects it for editor placement)");
            GUILayout.Label("Cost Efficiency = atmospheric Δv at the selected planet/altitude per Fund of engine cost.");
            GUILayout.BeginHorizontal();
            SortHeader("Engine", 185, SolutionSortColumn.Engine);
            SortHeader("#", 28, SolutionSortColumn.Count);
            SortHeader("Wet t", 55, SolutionSortColumn.WetMass);
            SortHeader("Atm Δv", 65, SolutionSortColumn.AtmosphericDeltaV);
            SortHeader("Vac Δv", 65, SolutionSortColumn.VacuumDeltaV);
            SortHeader("Cost Eff.", 70, SolutionSortColumn.CostEfficiency);
            SortHeader("TWR", 55, SolutionSortColumn.Twr);
            SortHeader("Max TWR", 70, SolutionSortColumn.MaxTwr);
            SortHeader("Isp", 50, SolutionSortColumn.Isp);
            SortHeader("Burn", 55, SolutionSortColumn.Burn);
            SortHeader("Fuel", 120, SolutionSortColumn.Fuel);
            Header("", 48);
            GUILayout.EndHorizontal();
            _resultsScroll = GUILayout.BeginScrollView(_resultsScroll, GUILayout.Height(GetResultsHeight()));
            foreach (StageSolution s in _solutions.Take(250))
            {
                if (s == _selected) GUILayout.BeginHorizontal("box"); else GUILayout.BeginHorizontal();
                if (GUILayout.Button(s.Engine.DisplayName, GUI.skin.label, GUILayout.Width(185))) SelectSolution(s);
                GUILayout.Label(s.EngineCount.ToString(), GUILayout.Width(28));
                GUILayout.Label(F(s.WetMassTons), GUILayout.Width(55));
                GUILayout.Label(Math.Round(s.AtmosphericDeltaV).ToString("0"), GUILayout.Width(65));
                GUILayout.Label(Math.Round(s.VacuumDeltaV).ToString("0"), GUILayout.Width(65));
                GUILayout.Label(s.CostEfficiency.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(70));
                GUILayout.Label(s.InitialTwr.ToString("0.00"), GUILayout.Width(55));
                GUILayout.Label(s.MaxTwr.ToString("0.00"), GUILayout.Width(70));
                GUILayout.Label(s.Isp.ToString("0"), GUILayout.Width(50));
                GUILayout.Label(s.BurnTimeSeconds.ToString("0") + "s", GUILayout.Width(55));
                GUILayout.Label(s.PropellantSummary, GUILayout.Width(120));
                if (GUILayout.Button("Add", GUILayout.Width(48))) SpawnEngine(s);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawSelected(bool showTanks)
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Selected: " + _selected.Engine.DisplayName + " × " + _selected.EngineCount);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Engine", GUILayout.Width(110))) SpawnEngine(_selected);
            GUILayout.EndHorizontal();

            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.Height(130));
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            Row("Propellant", F(_selected.PropellantMassTons) + " t");
            Row("Estimated tank dry mass", F(_selected.TankDryMassTons) + " t");
            Row("Engine mass", F(_selected.EngineMassTons) + " t");
            Row("Wet / dry", F(_selected.WetMassTons) + " / " + F(_selected.DryMassTons) + " t");
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            Row("Atmosphere Δv", _selected.AtmosphericDeltaV.ToString("0") + " m/s");
            Row("Vacuum Δv", _selected.VacuumDeltaV.ToString("0") + " m/s");
            Row("Cost efficiency", _selected.CostEfficiency.ToString("0.###", CultureInfo.InvariantCulture) + " Δv/Fund");
            Row("TWR initial / final", _selected.InitialTwr.ToString("0.00") + " / " + _selected.FinalTwr.ToString("0.00"));
            Row("Max TWR (vacuum)", _selected.MaxTwr.ToString("0.00"));
            Row("Burn", _selected.BurnTimeSeconds.ToString("0.0") + " s");
            Row("Known volume", _selected.TankVolumeLiters > 0 ? F(_selected.TankVolumeLiters) + " L" : "n/a for these resources");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            foreach (var p in _selected.Propellants)
                GUILayout.Label(p.ResourceName + ": " + F(p.Units) + " units, " + F(p.MassTons) + " t" + (p.VolumeLiters > 0 ? ", " + F(p.VolumeLiters) + " L" : ""));
            GUILayout.EndScrollView();

            if (showTanks) DrawTankSuggestions();
            GUILayout.EndVertical();
        }

        private void DrawTankSuggestions()
        {
            GUILayout.Space(4);
            GUILayout.Label("Tanks needed for the selected propellant requirement");
            if (_tankSuggestions.Count == 0)
            {
                GUILayout.Label("No single available tank type can provide all required mass-bearing propellants.");
                return;
            }

            GUILayout.BeginHorizontal();
            TankSortHeader("Tank", 245, TankSortColumn.Tank);
            TankSortHeader("#", 32, TankSortColumn.Count);
            TankSortHeader("Tank dry t", 80, TankSortColumn.DryMass);
            TankSortHeader("Excess", 65, TankSortColumn.Excess);
            TankSortHeader("Capacity", 300, TankSortColumn.Capacity);
            Header("", 70);
            GUILayout.EndHorizontal();
            _tankScroll = GUILayout.BeginScrollView(_tankScroll, GUILayout.Height(145));
            foreach (TankSuggestion t in _tankSuggestions.Take(100))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(t.Tank.DisplayName, GUILayout.Width(245));
                GUILayout.Label(t.Count.ToString(), GUILayout.Width(32));
                GUILayout.Label(F(t.TotalDryMassTons), GUILayout.Width(80));
                GUILayout.Label((t.ExcessFraction * 100.0).ToString("0.0") + "%", GUILayout.Width(65));
                GUILayout.Label(t.CapacitySummary, GUILayout.Width(300));
                if (GUILayout.Button("Add Tank", GUILayout.Width(70))) SpawnTank(t);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.Label("The # column is the number of copies of that tank required. Add Tank places one copy on the editor cursor; use it repeatedly for the indicated count.");
        }

        private void SelectSolution(StageSolution s)
        {
            _selected = s;
            _tankSuggestions = _planningMode ? TankPlanner.Suggest(_tanks, _selected) : new List<TankSuggestion>();
            ApplyTankSort();
        }

        private void SpawnEngine(StageSolution s)
        {
            string message;
            EditorPartSpawner.Spawn(s.Engine.PartName, out message);
            _status = message;
        }

        private void SpawnTank(TankSuggestion t)
        {
            string message;
            EditorPartSpawner.Spawn(t.Tank.PartName, out message);
            _status = message;
        }

        private void SimulateExisting()
        {
            // Re-read the editor exclusion-filter pipeline each time. This allows mods
            // which change their part visibility dynamically to affect candidates without
            // requiring Engine Stage Planner to be reopened.
            _engines = EngineDatabase.ScanAvailableEngines();

            double twr;
            int max;
            if (!D(_minTwr, out twr) || !int.TryParse(_maxEngines, out max))
            { _status = "Check numeric inputs."; return; }
            if (_snapshot == null) RefreshStage();
            IEnumerable<EngineCandidate> candidates = _engines;
            if (_ignoreMonoprop)
                candidates = candidates.Where(e => !e.Propellants.Any(p => string.Equals(p.ResourceName, "MonoPropellant", StringComparison.OrdinalIgnoreCase)));
            if (!_includeSolid) candidates = candidates.Where(e => !e.IsSolid);
            if (!_includeAir) candidates = candidates.Where(e => !e.IsAirBreathing);
            if (!_includeElectric) candidates = candidates.Where(e => !e.IsElectric);
            candidates = ApplyEngineNameFilter(candidates);
            candidates = ApplyBulkheadFilter(candidates);
            double atmospheres, temperatureK, density;
            GetSelectedAtmosphereEnvironment(out atmospheres, out temperatureK, out density);
            double gravity = GetSelectedTwrGravity();
            _solutions = ExistingStageSimulator.Simulate(_snapshot, candidates, gravity, atmospheres, temperatureK, density, Math.Max(1, Math.Min(64, max)), twr, _mode);
            ApplySolutionSort();
            _selected = _solutions.FirstOrDefault();
            _tankSuggestions.Clear();
            _status = _solutions.Count + " compatible engine configurations at " + BodyName(SelectedBody) + " " + FormatAltitude(_altitudeMeters) + ".";
        }

        private void Calculate()
        {
            // Re-read the editor exclusion-filter pipeline each time. This allows mods
            // which change their part visibility dynamically to affect candidates without
            // requiring Engine Stage Planner to be reopened.
            _engines = EngineDatabase.ScanAvailableEngines();

            double dv, twr, payload, ratio;
            int max;
            if (!D(_targetDv, out dv) || !D(_minTwr, out twr) || !D(_payload, out payload) || !D(_tankRatio, out ratio) || !int.TryParse(_maxEngines, out max))
            { _status = "Check numeric inputs."; return; }

            if (_snapshot == null) RefreshStage();
            double atmospheres, temperatureK, density;
            GetSelectedAtmosphereEnvironment(out atmospheres, out temperatureK, out density);
            var req = new StageRequirements
            {
                PayloadDryMassTons = _useCraftPayload ? _snapshot.PayloadAboveStageMassTons : payload,
                OtherStageDryMassTons = 0.0,
                TargetDeltaV = dv,
                MinimumTwr = twr,
                Gravity = GetSelectedTwrGravity(),
                Atmospheres = atmospheres,
                AtmosphereTemperatureK = temperatureK,
                AtmosphereDensityKgPerM3 = density,
                MaxEngineCount = Math.Max(1, Math.Min(64, max)),
                TankDryMassPerPropellantMass = Math.Max(0.0, ratio)
            };

            IEnumerable<EngineCandidate> candidates = _engines;
            if (!_includeSolid) candidates = candidates.Where(e => !e.IsSolid);
            if (!_includeAir) candidates = candidates.Where(e => !e.IsAirBreathing);
            if (!_includeElectric) candidates = candidates.Where(e => !e.IsElectric);
            candidates = ApplyEngineNameFilter(candidates);
            candidates = ApplyBulkheadFilter(candidates);
            _solutions = PlannerEngine.Calculate(candidates, req, _mode);
            ApplySolutionSort();
            _selected = _solutions.FirstOrDefault();
            _tankSuggestions = _selected != null ? TankPlanner.Suggest(_tanks, _selected) : new List<TankSuggestion>();
            ApplyTankSort();
            _status = _solutions.Count + " valid configurations at " + BodyName(SelectedBody) + " " + FormatAltitude(_altitudeMeters) + "; " + _tankSuggestions.Count + " matching tank choices for the selected result.";
        }

        private IEnumerable<EngineCandidate> ApplyEngineNameFilter(IEnumerable<EngineCandidate> candidates)
        {
            if (string.IsNullOrWhiteSpace(_engineNameFilter)) return candidates;
            string filter = _engineNameFilter.Trim();
            return candidates.Where(e =>
                (e.DisplayName ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (e.PartName ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void RecalculateForFilterChange()
        {
            // Keep the results list live while typing without requiring a separate Calculate click.
            if (_planningMode) Calculate(); else SimulateExisting();
        }

        private IEnumerable<EngineCandidate> ApplyBulkheadFilter(IEnumerable<EngineCandidate> candidates)
        {
            if (!_filterByBulkheadSize || _snapshot == null || _snapshot.TopNodeSizes.Count == 0)
                return candidates;

            var stageTopNodeSizes = new HashSet<int>(_snapshot.TopNodeSizes);
            return candidates.Where(e => e.TopNodeSize >= 0 && stageTopNodeSizes.Contains(e.TopNodeSize));
        }

        private static string FormatNodeSizes(IEnumerable<int> sizes)
        {
            return string.Join(", ", sizes.OrderBy(x => x).Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
        }


        private void DrawEnvironmentControls()
        {
            CelestialBody body = SelectedBody;
            double maxAltitude = GetAtmosphereDepth(body);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Planet:", GUILayout.Width(70));
            string bodyName = body != null ? BodyName(body) : "None";
            if (GUILayout.Button(bodyName + " ▼", GUILayout.Width(180)))
                _showPlanetDropdown = !_showPlanetDropdown;

            GUILayout.Space(12);
            GUILayout.Label("Pressure:", GUILayout.Width(65));
            GUILayout.Label(GetSelectedAtmospheres().ToString("0.####", CultureInfo.InvariantCulture) + " atm", GUILayout.Width(105));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_showPlanetDropdown)
            {
                GUILayout.BeginVertical("box");
                _planetScroll = GUILayout.BeginScrollView(_planetScroll, GUILayout.Height(Math.Min(170f, 28f * Math.Max(1, _bodies.Count))));
                for (int i = 0; i < _bodies.Count; i++)
                {
                    CelestialBody candidate = _bodies[i];
                    if (GUILayout.Button(BodyName(candidate), GUILayout.Height(24)))
                    {
                        _selectedBodyIndex = i;
                        _altitudeMeters = Math.Min(_altitudeMeters, GetAtmosphereDepth(candidate));
                        _showPlanetDropdown = false;
                        RecalculateForEnvironmentChange();
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Altitude:", GUILayout.Width(70));
            double oldAltitude = _altitudeMeters;
            if (maxAltitude > 0.0)
            {
                float slider = GUILayout.HorizontalSlider((float)_altitudeMeters, 0f, (float)maxAltitude, GUILayout.MinWidth(180));
                _altitudeMeters = Math.Max(0.0, Math.Min(maxAltitude, slider));
            }
            else
            {
                _altitudeMeters = 0.0;
                GUI.enabled = false;
                GUILayout.HorizontalSlider(0f, 0f, 1f, GUILayout.MinWidth(180));
                GUI.enabled = true;
            }
            GUILayout.Label(FormatAltitude(_altitudeMeters) + " / " + FormatAltitude(maxAltitude), GUILayout.Width(180));
            GUILayout.EndHorizontal();

            if (Math.Abs(oldAltitude - _altitudeMeters) >= 0.5)
                RecalculateForEnvironmentChange();

            if (body == null || !body.atmosphere)
                GUILayout.Label("Selected body has no atmosphere; atmospheric Δv equals vacuum Δv.");
        }

        private void RefreshBodies()
        {
            string oldName = SelectedBody != null ? BodyName(SelectedBody) : "Kerbin";
            _bodies.Clear();
            if (FlightGlobals.Bodies != null)
            {
                foreach (CelestialBody body in FlightGlobals.Bodies)
                    if (body != null && body.Radius > 0.0) _bodies.Add(body);
            }

            int selected = _bodies.FindIndex(b => string.Equals(BodyName(b), oldName, StringComparison.OrdinalIgnoreCase));
            if (selected < 0) selected = _bodies.FindIndex(b => string.Equals(BodyName(b), "Kerbin", StringComparison.OrdinalIgnoreCase));
            _selectedBodyIndex = selected >= 0 ? selected : 0;
            _altitudeMeters = Math.Min(_altitudeMeters, GetAtmosphereDepth(SelectedBody));
        }

        private CelestialBody SelectedBody
        {
            get
            {
                return _selectedBodyIndex >= 0 && _selectedBodyIndex < _bodies.Count ? _bodies[_selectedBodyIndex] : null;
            }
        }

        private static string BodyName(CelestialBody body)
        {
            if (body == null) return "None";
            return !string.IsNullOrEmpty(body.bodyName) ? body.bodyName : body.name;
        }

        private static double GetAtmosphereDepth(CelestialBody body)
        {
            return body != null && body.atmosphere ? Math.Max(0.0, body.atmosphereDepth) : 0.0;
        }

        private void GetSelectedAtmosphereEnvironment(out double atmospheres, out double temperatureK, out double densityKgPerM3)
        {
            atmospheres = 0.0;
            temperatureK = 288.15;
            densityKgPerM3 = 0.0;

            CelestialBody body = SelectedBody;
            if (body == null || !body.atmosphere) return;

            double maxAltitude = GetAtmosphereDepth(body);
            double altitude = Math.Max(0.0, Math.Min(maxAltitude, _altitudeMeters));
            try
            {
                double pressureKpa = Math.Max(0.0, FlightGlobals.getStaticPressure(altitude, body));
                atmospheres = pressureKpa * PhysicsGlobals.KpaToAtmospheres;
                temperatureK = Math.Max(1.0, FlightGlobals.getExternalTemperature(altitude, body));
                densityKgPerM3 = Math.Max(0.0, FlightGlobals.getAtmDensity(pressureKpa, temperatureK, body));
            }
            catch
            {
                atmospheres = 0.0;
                temperatureK = 288.15;
                densityKgPerM3 = 0.0;
            }
        }

        private double GetSelectedAtmospheres()
        {
            double atmospheres, temperatureK, density;
            GetSelectedAtmosphereEnvironment(out atmospheres, out temperatureK, out density);
            return atmospheres;
        }

        private double GetSelectedTwrGravity()
        {
            CelestialBody body = SelectedBody;
            if (body == null || body.Radius <= 0.0) return StageSolver.StandardGravity;
            try
            {
                // MechJeb's stage StartTWR uses the body's surface gee (GeeASL * g0),
                // not altitude-adjusted local gravity. Use the same KSP body value directly.
                double g = body.GeeASL * StageSolver.StandardGravity;
                return g > 0.0 && !double.IsNaN(g) && !double.IsInfinity(g) ? g : StageSolver.StandardGravity;
            }
            catch
            {
                return StageSolver.StandardGravity;
            }
        }

        private void RecalculateForEnvironmentChange()
        {
            if (_planningMode) Calculate(); else SimulateExisting();
        }

        private float GetResultsHeight()
        {
            float reserved = _planningMode ? 500f : 360f;
            return Mathf.Clamp(_window.height - reserved, MinEngineListHeight, MaxEngineListHeight);
        }

        private static string FormatAltitude(double meters)
        {
            if (meters >= 1000.0) return (meters / 1000.0).ToString("0.##", CultureInfo.InvariantCulture) + " km";
            return meters.ToString("0", CultureInfo.InvariantCulture) + " m";
        }

        private void DrawResizeHandle()
        {
            Rect handle = new Rect(Math.Max(0f, _window.width - 20f), Math.Max(0f, _window.height - 20f), 18f, 18f);
            GUI.Box(handle, "//");
            Event e = Event.current;
            if (e == null) return;

            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition))
            {
                _resizing = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _resizing)
            {
                _pendingResizeDelta += e.delta;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _resizing)
            {
                _resizing = false;
                e.Use();
            }
        }

        private void RefreshDatabases()
        {
            _engines = EngineDatabase.ScanAvailableEngines();
            _tanks = TankDatabase.ScanAvailableTanks();
            _tankSuggestions = _selected != null && _planningMode ? TankPlanner.Suggest(_tanks, _selected) : new List<TankSuggestion>();
            ApplyTankSort();
            _status = _engines.Count + " engine definitions and " + _tanks.Count + " tank definitions loaded.";
        }

        private void RefreshStage()
        {
            _snapshot = EditorStageScanner.Scan(_stage);
            if (_snapshot != null && _snapshot.InferredTankDryRatio > 0.0)
                _tankRatio = _snapshot.InferredTankDryRatio.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void EnsureStyles()
        {
            if (_right == null) { _right = new GUIStyle(GUI.skin.label); _right.alignment = TextAnchor.MiddleRight; }
        }


        private void SortHeader(string text, float width, SolutionSortColumn column)
        {
            string label = text;
            if (_solutionSortColumn == column)
                label += _solutionSortAscending ? " ▲" : " ▼";

            if (GUILayout.Button(label, GUILayout.Width(width)))
            {
                if (_solutionSortColumn == column)
                    _solutionSortAscending = !_solutionSortAscending;
                else
                {
                    _solutionSortColumn = column;
                    _solutionSortAscending = true;
                }
                ApplySolutionSort();
            }
        }

        private void TankSortHeader(string text, float width, TankSortColumn column)
        {
            string label = text;
            if (_tankSortColumn == column)
                label += _tankSortAscending ? " ▲" : " ▼";

            if (GUILayout.Button(label, GUILayout.Width(width)))
            {
                if (_tankSortColumn == column)
                    _tankSortAscending = !_tankSortAscending;
                else
                {
                    _tankSortColumn = column;
                    _tankSortAscending = true;
                }
                ApplyTankSort();
            }
        }

        private void ApplySolutionSort()
        {
            if (_solutions == null || _solutions.Count < 2 || _solutionSortColumn == SolutionSortColumn.None)
                return;

            Func<StageSolution, object> selector;
            switch (_solutionSortColumn)
            {
                case SolutionSortColumn.Engine: selector = s => s.Engine.DisplayName; break;
                case SolutionSortColumn.Count: selector = s => s.EngineCount; break;
                case SolutionSortColumn.WetMass: selector = s => s.WetMassTons; break;
                case SolutionSortColumn.AtmosphericDeltaV: selector = s => s.AtmosphericDeltaV; break;
                case SolutionSortColumn.VacuumDeltaV: selector = s => s.VacuumDeltaV; break;
                case SolutionSortColumn.CostEfficiency: selector = s => s.CostEfficiency; break;
                case SolutionSortColumn.Twr: selector = s => s.InitialTwr; break;
                case SolutionSortColumn.MaxTwr: selector = s => s.MaxTwr; break;
                case SolutionSortColumn.Isp: selector = s => s.Isp; break;
                case SolutionSortColumn.Burn: selector = s => s.BurnTimeSeconds; break;
                case SolutionSortColumn.Fuel: selector = s => s.PropellantSummary ?? string.Empty; break;
                default: return;
            }

            _solutions = (_solutionSortAscending
                ? _solutions.OrderBy(selector)
                : _solutions.OrderByDescending(selector)).ToList();
        }

        private void ApplyTankSort()
        {
            if (_tankSuggestions == null || _tankSuggestions.Count < 2 || _tankSortColumn == TankSortColumn.None)
                return;

            Func<TankSuggestion, object> selector;
            switch (_tankSortColumn)
            {
                case TankSortColumn.Tank: selector = t => t.Tank.DisplayName; break;
                case TankSortColumn.Count: selector = t => t.Count; break;
                case TankSortColumn.DryMass: selector = t => t.TotalDryMassTons; break;
                case TankSortColumn.Excess: selector = t => t.ExcessFraction; break;
                case TankSortColumn.Capacity: selector = t => t.CapacitySummary ?? string.Empty; break;
                default: return;
            }

            _tankSuggestions = (_tankSortAscending
                ? _tankSuggestions.OrderBy(selector)
                : _tankSuggestions.OrderByDescending(selector)).ToList();
        }

        private bool ToggleChanged(ref bool value, string label)
        {
            bool old = value;
            value = GUILayout.Toggle(value, label);
            return old != value;
        }

        private bool OptimizationToggle(OptimizationMode mode)
        {
            bool active = _mode == mode;
            bool requested = GUILayout.Toggle(active, ModeLabel(mode));
            if (requested && !active)
            {
                _mode = mode;
                return true;
            }
            return false;
        }

        private static void Field(string label, ref string value)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(175)); value = GUILayout.TextField(value, GUILayout.Width(105)); GUILayout.EndHorizontal();
        }
        private void Row(string label, string value)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label); GUILayout.Label(value, _right, GUILayout.Width(185)); GUILayout.EndHorizontal();
        }
        private static void Header(string s, float w) { GUILayout.Label(s, GUILayout.Width(w)); }
        private static bool D(string s, out double v) { return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v); }
        private static string F(double v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }
        private static string ModeLabel(OptimizationMode mode) { return mode.ToString().Replace("Lowest", "Lowest ").Replace("Highest", "Highest ").Replace("Shortest", "Shortest "); }
        private void ClampWindow()
        {
            _window.width = Mathf.Max(MinWindowWidth, _window.width);
            _window.height = Mathf.Max(MinWindowHeight, _window.height);
            _window.x = Mathf.Clamp(_window.x, -_window.width + 40f, Screen.width - 40f);
            _window.y = Mathf.Clamp(_window.y, 0f, Screen.height - 30f);
        }
    }
}
