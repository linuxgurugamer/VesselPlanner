using ClickThroughFix;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using VesselPlanner.Core;
using VesselPlanner.KSP;

namespace VesselPlanner.UI
{
    // Builds a craft one stage at a time. The window holds the plan; the requirements for
    // each new stage are handed to the main planner window, and whatever is picked there
    // comes back into the stage being built.
    public sealed class StagePlanWindow
    {
        private sealed class StageSubassemblyDraft
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

        private readonly PlannerWindow _planner;
        private readonly StagePlan _plan = new StagePlan();

        private Rect _window = new Rect(140, 100, 520, 560);
        private Rect _newStageWindow = new Rect(260, 180, 340, 260);
        private Rect _loadWindow = new Rect(260, 180, 360, 380);
        private Rect _missionSelectWindow = new Rect(280, 190, 420, 390);
        private Vector2 _stageScroll;
        private Vector2 _loadScroll;
        private Vector2 _missionSelectScroll;
        private Vector2 _missionManeuverScroll;

        private bool _newStageVisible;
        private bool _loadVisible;
        private bool _missionSelectVisible;
        private string _payloadText = "5.0";
        private string _vesselNameText = "";
        private string _status = "";
        private bool _bringPlanToFrontRequested;
        private Texture2D _hoverPartPreviewTexture;
        private Rect _hoverPartPreviewSourceScreenRect;
        private const float HoverPartPreviewSize = 160f;

        // New-stage dialog inputs.
        private string _newTargetDv = "";
        private string _newMinTwr = "";
        private string _newMaxEngines = "";
        private string _newCargoMass = "0";
        private bool _newAddDecouplerMass;
        private bool _newSideBoosters;
        private bool _newCoreBurnsToo;
        private bool _newSideBoostersHaveRadialDecouplers;
        private string _newAssemblyFile = string.Empty;
        private string _newAssemblyName = string.Empty;
        private double _newAssemblyMassTons;
        private PlannedStageKind _newStageKind = PlannedStageKind.EnginesAndTanks;
        private readonly List<SavedAssemblyInfo> _newStageAssemblyCatalog = new List<SavedAssemblyInfo>();
        private readonly List<StageSubassemblyDraft> _newStageSubassemblies = new List<StageSubassemblyDraft>();
        private Vector2 _newStageAssemblyCatalogScroll;
        private Vector2 _newStageSubassemblyScroll;
        private int _pendingSubassemblyCatalogAdd = -1;
        private int _pendingSubassemblyDraftRemove = -1;
        private PlannedStageKind? _pendingNewStageKind;

        // Explicit IMGUI control names let the modal New/Edit Stage form implement
        // normal keyboard navigation instead of leaving Tab to KSP/Unity.
        private const string StageTargetDvControl = "VesselPlanner.Stage.TargetDv";
        private const string StageMinTwrControl = "VesselPlanner.Stage.MinTwr";
        private const string StageMaxEnginesControl = "VesselPlanner.Stage.MaxEngines";
        private const string StageCargoMassControl = "VesselPlanner.Stage.CargoMass";
        private static readonly string[] StageEntryControls =
        {
            StageTargetDvControl,
            StageMinTwrControl,
            StageMaxEnginesControl,
            StageCargoMassControl
        };

        // KSP's modal IMGUI wrapper does not always report Tab as KeyCode.Tab.  Keep our
        // own focus position and accept both the IMGUI Tab character and Unity's keyboard
        // state so keyboard navigation remains reliable.
        private int _stageEntryFocusIndex = -1;
        private string _pendingStageFocusControl;
        private int _lastStageTabFrame = -1;

        private DeltaVBasis _newBasis = DeltaVBasis.Vacuum;
        private readonly List<string> _newBulkheadProfiles = new List<string>();
        private bool _bulkheadDropdownOpen;
        private bool _bulkheadProfilesArePreset;
        private Vector2 _bulkheadScroll;

        // Optional Mission Planner link for the stage currently being created/edited.
        private string _newMissionPlanName = string.Empty;
        private int _newMissionStepNumber;
        private Maneuver _newMissionManeuver = Maneuver.None;

        // Index of the stage the planner is currently feeding, or -1 when nothing is being
        // built. Parts added in the planner go to this stage.
        private int _activeStageIndex = -1;

        // Which stage the dialog is editing, or -1 when it is creating one.
        private int _editingStageIndex = -1;
        private int _pendingEditIndex = -1;

        private readonly List<string> _planFiles = new List<string>();
        private readonly List<string> _missionPlanFiles = new List<string>();
        private MissionPlan _selectedMissionPlan;

        // Deleting a stage or part, loading a plan and starting a new stage all change how
        // many controls these windows draw. Applying them inside the window function would
        // change that between a frame's layout and repaint passes, so they are recorded here
        // and carried out once both windows have been drawn.
        private int _pendingStageRemoval = -1;
        private int _pendingPartStage = -1;
        private int _pendingPartIndex = -1;
        private string _pendingLoadPath;
        private string _pendingMissionLoadPath;
        private bool _pendingStageStart;
        private bool _pendingFinalize;
        private bool _pendingReopen;
        private bool _pendingClear;
        private bool _clearArmed;
        private string _pendingDeletePath;
        private string _deleteArmedPath;
        private double _pendingTargetDv;
        private double _pendingMinTwr;
        private int _pendingMaxEngines;
        private double _pendingCargoMass;
        private bool _pendingAddDecouplerMass;
        private bool _pendingSideBoosters;
        private bool _pendingCoreBurnsToo;
        private bool _pendingSideBoostersHaveRadialDecouplers;
        private string _pendingAssemblyFile = string.Empty;
        private string _pendingAssemblyName = string.Empty;
        private double _pendingAssemblyMassTons;
        private readonly List<string> _pendingBulkheadProfiles = new List<string>();
        private DeltaVBasis _pendingBasis = DeltaVBasis.Vacuum;
        private string _pendingMissionPlanName = string.Empty;
        private int _pendingMissionStepNumber;
        private Maneuver _pendingMissionManeuver = Maneuver.None;

        public bool Visible { get; set; }

        public StagePlanWindow(PlannerWindow planner)
        {
            _planner = planner;
        }

        // Called when the user enters Stage-By-Stage mode. A brand-new plan starts
        // from the wet mass of whatever vessel is already in the editor; an empty
        // editor starts at 5 t. Existing plans keep the mass they already contain.
        internal void PositionBelowPlannerButtons()
        {
            Vector2 position = _planner.StagePlanOpenPosition;
            _window.x = position.x;
            _window.y = position.y;
            CommonRoutines.ClampWindow(ref _window);
        }

        private void PositionNewStageNextToPlan()
        {
            // Open the modal directly beside the plan so the two windows read as one
            // stage-building workspace: tops aligned, dialog left edge touching the
            // plan's right edge. The dialog remains draggable after it opens.
            _newStageWindow.width = 400f;
            _newStageWindow.x = _window.xMax;
            _newStageWindow.y = _window.y;
            CommonRoutines.ClampWindow(ref _newStageWindow);
        }

        internal void PrepareForStageByStageStart()
        {
            if (_plan.Stages.Count > 0 || _plan.Finalized) return;

            double startingMass;
            if (!EditorStageScanner.TryGetVesselWetMassTons(out startingMass) || startingMass <= 0.0)
                startingMass = 5.0;

            _payloadText = startingMass.ToString("0.###", CultureInfo.InvariantCulture);
            _plan.PayloadMassTons = startingMass;
        }

        // Poll Tab from Unity's normal Update loop instead of depending on the modal
        // IMGUI event stream. ClickThroughBlocker/KSP can consume or rewrite the GUI Tab
        // event, but Input.GetKeyDown still reports the physical key press reliably.
        public void Update()
        {
            if (!_newStageVisible || _newStageKind != PlannedStageKind.EnginesAndTanks || !Input.GetKeyDown(KeyCode.Tab)) return;

            bool backwards = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            QueueStageTabNavigation(backwards, false);
        }

        public bool IsCapturing
        {
            get { return _activeStageIndex >= 0 && _activeStageIndex < _plan.Stages.Count; }
        }

        public string CapturingStageLabel
        {
            get { return IsCapturing ? "Stage " + (_activeStageIndex + 1) : ""; }
        }

        // Called by the planner when an engine or tank is picked while a stage is being
        // built. Returns false when nothing is being built, so the planner falls back to
        // placing the part in the editor as usual.
        public bool CapturePart(string partName, string partUrl, string displayName, int quantity, bool isEngine)
        {
            if (!IsCapturing) return false;

            _plan.Stages[_activeStageIndex].AddPart(partName, partUrl, displayName, quantity, isEngine);
            _status = "Added " + quantity + " x " + (string.IsNullOrEmpty(displayName) ? partName : displayName)
                + " to stage " + (_activeStageIndex + 1) + ".";
            return true;
        }

        // Null when no stage is open, so the planner falls back to its own bulkhead
        // setting; empty means the stage considers every profile.
        internal IList<string> ActiveBulkheadProfiles
        {
            get { return IsCapturing ? _plan.Stages[_activeStageIndex].BulkheadProfiles : null; }
        }

        // Adds the engine and a one-to-three-type tank set in one go, for the
        // Add Engine & Tanks button. Every tank type is captured with its solved count.
        internal bool CaptureEngineAndTanks(string enginePart, string enginePartUrl, string engineName, int engineCount, IEnumerable<TankSuggestionPart> tanks)
        {
            if (!IsCapturing) return false;

            PlannedStage stage = _plan.Stages[_activeStageIndex];
            stage.AddPart(enginePart, enginePartUrl, engineName, engineCount, true);
            int tankTypes = 0;
            if (tanks != null)
            {
                foreach (TankSuggestionPart item in tanks)
                {
                    if (item == null || item.Tank == null || item.Count <= 0) continue;
                    stage.AddPart(item.Tank.PartName, item.Tank.PartUrl, item.Tank.DisplayName, item.Count, false);
                    tankTypes++;
                }
            }
            _status = "Added engine" + (tankTypes > 0 ? " and tank set" : "") + " to stage " + (_activeStageIndex + 1) + ".";

            // The combined add is initiated from the main planner, which can leave the
            // Stage-By-Stage plan visually behind it. Request the plan window be raised
            // after the current planner GUI callback finishes.
            _bringPlanToFrontRequested = true;
            return true;
        }

        // Called when the planner leaves Stage-By-Stage: the plan is kept, but nothing is
        // being fed from the planner any more.
        internal void CloseOpenStage()
        {
            _activeStageIndex = -1;
            _newStageVisible = false;
            _editingStageIndex = -1;
        }

        public void Draw()
        {
            if (!Visible) return;

            // A selected mission adds a fixed sidebar on the left of the normal plan UI.
            float minimumPlanWidth = _selectedMissionPlan == null ? 480f : 790f;
            if (_selectedMissionPlan != null && _window.width < minimumPlanWidth)
                _window.width = minimumPlanWidth;

            // _planner.DrawSolidBackground(_window, true);
            _window = ClickThruBlocker.GUILayoutWindow(19041970, _window, DrawPlanWindow, "Stage-By-Stage Plan", RegisterToolbar.winLighter,
                GUILayout.MinWidth(minimumPlanWidth), GUILayout.MinHeight(420f));

            if (_newStageVisible)
            {
                string stageDialogTitle = _editingStageIndex >= 0 ? "Edit Stage " + (_editingStageIndex + 1) : "New Stage";

                // This dialog must be modal: while it is open it stays above every other
                // VesselPlanner/KSP IMGUI window and is the sole receiver of GUI input.
                // ClickThroughBlocker's modal wrapper keeps the normal editor click-through
                // protection as well.  The height grows only while the bulkhead list is open.
                if (_newStageKind == PlannedStageKind.Subassemblies)
                {
                    _newStageWindow.width = 620f;
                    // The two subassembly scroll areas plus the stage-mass/action rows need
                    // enough vertical room for the validation/status line even when no assembly
                    // is selected, while keeping the action buttons clear of the bottom edge.
                    _newStageWindow.height = 715f;
                }
                else
                {
                    _newStageWindow.width = 400f;
                    float assemblyHeight = (_newAssemblyMassTons > 0.0 || _newStageSubassemblies.Count > 0) ? 30f : 0f;
                    float boosterOptionsHeight = _newSideBoosters ? 54f : 32f;
                    _newStageWindow.height = (_bulkheadDropdownOpen ? 560f : 425f) + assemblyHeight + boosterOptionsHeight;
                }
                //_planner.DrawSolidBackground(_newStageWindow, true);
                _newStageWindow = ClickThruBlocker.GUIModalWindow(19041971, _newStageWindow, DrawNewStageWindow,
                    stageDialogTitle /*, ToolbarRegistration.winLighter */ , GUI.skin.window);
            }

            if (_loadVisible)
            {
                // _planner.DrawSolidBackground(_loadWindow);
                _loadWindow = ClickThruBlocker.GUILayoutWindow(19041972, _loadWindow, DrawLoadWindow, "Load Plan", RegisterToolbar.winDarker,
                    GUILayout.Width(360f), GUILayout.Height(380f));
            }

            if (_missionSelectVisible)
            {
                _missionSelectWindow = ClickThruBlocker.GUILayoutWindow(19041973, _missionSelectWindow, DrawMissionSelectWindow,
                    "Select Mission Plan", RegisterToolbar.winDarker, GUILayout.Width(420f), GUILayout.Height(390f));
            }

            if (_bringPlanToFrontRequested && !_newStageVisible && !_loadVisible && !_missionSelectVisible)
            {
                GUI.BringWindowToFront(19041970);
                _bringPlanToFrontRequested = false;
            }

            ApplyPendingActions();

            CommonRoutines.ClampWindow(ref _window);
            CommonRoutines.ClampWindow(ref _newStageWindow);
            CommonRoutines.ClampWindow(ref _loadWindow);
            CommonRoutines.ClampWindow(ref _missionSelectWindow);
        }

        private void ApplyPendingActions()
        {
            if (_pendingStageRemoval >= 0)
            {
                int index = _pendingStageRemoval;
                _pendingStageRemoval = -1;
                if (index < _plan.Stages.Count)
                {
                    _plan.Stages.RemoveAt(index);
                    if (_activeStageIndex == index) _activeStageIndex = -1;
                    else if (_activeStageIndex > index) _activeStageIndex--;
                    _status = "Stage removed.";
                }
            }

            if (_pendingPartStage >= 0)
            {
                int stageIndex = _pendingPartStage;
                int partIndex = _pendingPartIndex;
                _pendingPartStage = -1;
                _pendingPartIndex = -1;
                if (stageIndex < _plan.Stages.Count && partIndex >= 0 && partIndex < _plan.Stages[stageIndex].Parts.Count)
                {
                    _plan.Stages[stageIndex].Parts.RemoveAt(partIndex);
                    _status = "Part removed.";
                }
            }

            if (!string.IsNullOrEmpty(_pendingLoadPath))
            {
                string path = _pendingLoadPath;
                _pendingLoadPath = null;
                _deleteArmedPath = null;
                LoadPlan(path);
            }

            if (!string.IsNullOrEmpty(_pendingDeletePath))
            {
                string path = _pendingDeletePath;
                _pendingDeletePath = null;
                _deleteArmedPath = null;
                DeletePlan(path);
            }

            if (!string.IsNullOrEmpty(_pendingMissionLoadPath))
            {
                string path = _pendingMissionLoadPath;
                _pendingMissionLoadPath = null;
                SelectMissionPlan(path);
            }

            if (_pendingNewStageKind.HasValue)
            {
                _newStageKind = _pendingNewStageKind.Value;
                _pendingNewStageKind = null;
                _bulkheadDropdownOpen = false;
                if (_newStageKind == PlannedStageKind.Subassemblies) RefreshNewStageAssemblyCatalog();
                else ResetStageEntryFocus();
            }

            if (_pendingSubassemblyCatalogAdd >= 0)
            {
                int catalogIndex = _pendingSubassemblyCatalogAdd;
                _pendingSubassemblyCatalogAdd = -1;
                AddSubassemblyDraftFromCatalog(catalogIndex);
            }

            if (_pendingSubassemblyDraftRemove >= 0)
            {
                int draftIndex = _pendingSubassemblyDraftRemove;
                _pendingSubassemblyDraftRemove = -1;
                if (draftIndex < _newStageSubassemblies.Count) _newStageSubassemblies.RemoveAt(draftIndex);
            }

            if (_pendingStageStart)
            {
                _pendingStageStart = false;
                if (_newStageKind == PlannedStageKind.Subassemblies)
                    StartSubassemblyStage();
                else
                    StartStage(_pendingTargetDv, _pendingMinTwr, _pendingMaxEngines, _pendingCargoMass);
            }

            if (_pendingFinalize)
            {
                _pendingFinalize = false;
                _plan.Finalized = true;
                _activeStageIndex = -1;
                _clearArmed = false;
                // The finalised state is what gets saved, so reloading the plan reopens it
                // as a build list rather than as something still being assembled.
                SavePlan();
                _planner.Visible = false;
                _status = "Plan finalised and saved. Use the Add buttons to place parts in the editor.";
            }

            if (_pendingReopen)
            {
                _pendingReopen = false;
                _plan.Finalized = false;
                _clearArmed = false;
                _planner.Visible = true;
                _status = "Plan reopened for editing. The planner window is back.";
            }

            if (_pendingClear)
            {
                _pendingClear = false;
                _clearArmed = false;
                _plan.Stages.Clear();
                _plan.Finalized = false;
                _activeStageIndex = -1;
                _editingStageIndex = -1;
                _newStageVisible = false;
                _status = "Plan cleared.";
            }
        }

        private void DrawPlanWindow(int id)
        {
            if (Event.current != null && Event.current.type == EventType.Repaint)
                _hoverPartPreviewTexture = null;

            if (_selectedMissionPlan != null)
            {
                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(280f), GUILayout.ExpandHeight(true)))
                        DrawMissionPlanSidebar();
                    GUILayout.Space(6f);
                    using (new GUILayout.VerticalScope(GUILayout.MinWidth(480f), GUILayout.ExpandHeight(true)))
                        DrawPlanContent();
                }
            }
            else
            {
                DrawPlanContent();
            }

            DrawHoveredPartPreview();
            GUI.DragWindow(new Rect(0f, 0f, _window.width, _window.height));
        }

        private void DrawPlanContent()
        {
            // Seeded from the craft in the editor the first time the window is opened, then
            // owned by the field so a plan can be named independently of the craft.
            if (string.IsNullOrEmpty(_vesselNameText)) _vesselNameText = CurrentVesselName();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Vessel", GUILayout.Width(70));
                _vesselNameText = GUILayout.TextField(_vesselNameText, GUILayout.Width(220));
                GUILayout.FlexibleSpace();
                if (GUI.Button(new Rect(_window.width - 32, 2, 30, 20), "×", RegisterToolbar.styleXButtonSettings))
                    Visible = false;
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Payload (t)", GUILayout.Width(70));
                _payloadText = GUILayout.TextField(_payloadText, GUILayout.Width(80));
                GUILayout.Space(10);
                GUILayout.Label("Starting body", GUILayout.Width(90));
                GUILayout.Label(_planner.SelectedBodyName);
            }
            GUILayout.Label("The starting body is the one selected in the planner window.");

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Mission Plan", GUILayout.Width(150), GUILayout.Height(24)))
                    OpenMissionPlanSelection();
                if (_selectedMissionPlan != null)
                {
                    GUILayout.Label("Selected: " + _selectedMissionPlan.Name);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Clear Mission", GUILayout.Width(100), GUILayout.Height(24)))
                    {
                        _selectedMissionPlan = null;
                        _window.width = 520f;
                        _status = "Mission plan selection cleared.";
                    }
                }
                else
                {
                    GUILayout.Label("No mission plan selected.");
                    GUILayout.FlexibleSpace();
                }
            }

            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                // Finalising ends stage building, so the button that starts a stage goes away
                // and the parts become placeable instead.
                // One dialog at a time: a second press would overwrite the inputs already there.
                GUI.enabled = !_newStageVisible;
                if (!_plan.Finalized && GUILayout.Button("New Stage", GUILayout.Width(110), GUILayout.Height(26)))
                    OpenNewStageDialog();
                GUI.enabled = true;
                if (IsCapturing)
                {
                    GUILayout.Label("Building " + CapturingStageLabel + " - add engines and tanks in the planner window.");
                    if (GUILayout.Button("Done", GUILayout.Width(70), GUILayout.Height(26)))
                    {
                        _activeStageIndex = -1;
                        _status = "Stage closed.";
                    }
                }
            }

            GUILayout.Space(4);
            _stageScroll = GUILayout.BeginScrollView(_stageScroll, GUILayout.Height(StageListHeight()));
            if (_plan.Stages.Count == 0)
            {
                GUILayout.Label("No stages yet. Use New Stage or click a mission line to size one in the planner window.");
            }
            else
            {
                for (int i = 0; i < _plan.Stages.Count; i++)
                    DrawStage(i);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope())
            {
                // Finalising an empty plan would produce a build list with nothing in it.
                GUI.enabled = _plan.Stages.Count > 0;
                if (!_plan.Finalized && GUILayout.Button("Finalize", GUILayout.Width(90), GUILayout.Height(26)))
                    _pendingFinalize = true;
                GUI.enabled = true;
                if (_plan.Finalized && GUILayout.Button("Reopen", GUILayout.Width(90), GUILayout.Height(26)))
                    _pendingReopen = true;
                GUILayout.FlexibleSpace();
                GUILayout.Label("Saved as " + CommonRoutines.SanitiseFileName(_vesselNameText, "Plan") + ".cfg");
                if (GUILayout.Button("Save", GUILayout.Width(70), GUILayout.Height(26))) SavePlan();
                if (GUILayout.Button("Load", GUILayout.Width(70), GUILayout.Height(26))) OpenLoadDialog();
                if (GUILayout.Button(_clearArmed ? "Confirm" : "Clear", GUILayout.Width(70), GUILayout.Height(26)))
                {
                    if (_clearArmed) _pendingClear = true;
                    else
                    {
                        _clearArmed = true;
                        _status = "Press Confirm to clear the plan.";
                    }
                }
            }

            GUILayout.Label(_status);
        }

        private void DrawMissionPlanSidebar()
        {
            var leftLabel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            var leftBox = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            var missionButton = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            GUILayout.Label("Mission Plan", leftBox);
            GUILayout.Label(_selectedMissionPlan == null ? string.Empty : _selectedMissionPlan.Name, leftLabel);
            GUILayout.Label("Last mission step first. Click a line to create its matching stage, or edit the stage already linked to that mission step.", leftLabel);
            GUILayout.Space(4f);

            _missionManeuverScroll = GUILayout.BeginScrollView(_missionManeuverScroll, GUILayout.ExpandHeight(true));
            if (_selectedMissionPlan == null || _selectedMissionPlan.Maneuvers.Count == 0)
            {
                GUILayout.Label("No mission steps in the selected mission.");
            }
            else
            {
                for (int i = _selectedMissionPlan.Maneuvers.Count - 1; i >= 0; i--)
                {
                    MissionManeuver maneuver = _selectedMissionPlan.Maneuvers[i];
                    bool subassemblyStep = maneuver.StepKind == MissionStepKind.Subassemblies;
                    string location = maneuver.LocationSummary;
                    string basis = maneuver.DeltaVBasis == DeltaVBasis.Atmospheric ? "ASL" : "VAC";
                    string line = subassemblyStep
                        ? (i + 1).ToString(CultureInfo.InvariantCulture) + ". Subassemblies\n" + maneuver.AssemblySummary
                        : (i + 1).ToString(CultureInfo.InvariantCulture) + ". " + CommonRoutines.FormatManeuver(maneuver.Kind) +
                            (string.IsNullOrEmpty(location) ? string.Empty : "\n" + location) +
                            "\n" + maneuver.DeltaV.ToString("0", CultureInfo.InvariantCulture) + " m/s " + basis;

                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = oldEnabled && !_plan.Finalized && !_newStageVisible;
                    if (GUILayout.Button(line, missionButton, GUILayout.Width(250f), GUILayout.MinHeight(subassemblyStep ? 64f : 64f)))
                        OpenMissionStageDialog(i, maneuver);
                    GUI.enabled = oldEnabled;
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawStage(int index)
        {
            PlannedStage stage = _plan.Stages[index];

            GUILayout.BeginVertical("box");
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Stage " + (index + 1) + ": " + stage.Summary);
                GUILayout.FlexibleSpace();
                if (!_plan.Finalized)
                {
                    GUI.enabled = !_newStageVisible;
                    if (GUILayout.Button("Edit", GUILayout.Width(60))) OpenEditStageDialog(index);
                    GUI.enabled = true;
                    if (GUILayout.Button("Delete", GUILayout.Width(70))) _pendingStageRemoval = index;
                }
            }

            string missionLine = stage.MissionStepNumber > 0
                ? "Mission step " + stage.MissionStepNumber.ToString(CultureInfo.InvariantCulture) + ": " +
                    (stage.Kind == PlannedStageKind.Subassemblies ? "Subassemblies" :
                        (stage.MissionManeuver == Maneuver.None ? "(unknown maneuver)" : CommonRoutines.FormatManeuver(stage.MissionManeuver)))
                : "Mission step: not linked";
            GUILayout.Label(missionLine);
            if (stage.Kind == PlannedStageKind.EnginesAndTanks &&
                (stage.SideBoosters || stage.CoreBurnsToo || stage.SideBoostersHaveRadialDecouplers))
            {
                var layoutFlags = new List<string>();
                if (stage.SideBoosters) layoutFlags.Add("Side Boosters");
                if (stage.CoreBurnsToo) layoutFlags.Add("Core burns too");
                if (stage.SideBoosters && stage.SideBoostersHaveRadialDecouplers) layoutFlags.Add("Radial decouplers");
                GUILayout.Label("Booster layout: " + string.Join("; ", layoutFlags.ToArray()));
            }

            if (stage.Kind == PlannedStageKind.Subassemblies)
            {
                if (stage.Subassemblies.Count == 0)
                {
                    GUILayout.Label("  no subassemblies selected yet");
                }
                else
                {
                    foreach (PlannedSubassembly item in stage.Subassemblies)
                    {
                        if (item == null) continue;
                        int quantity = Math.Max(1, item.Quantity);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("subassembly", GUILayout.Width(78));
                            GUILayout.Label(quantity.ToString(CultureInfo.InvariantCulture) + " x " + item.DisplayName +
                                " (" + item.UnitMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t each; " +
                                (quantity * Math.Max(0.0, item.UnitMassTons)).ToString("0.###", CultureInfo.InvariantCulture) + " t)");
                            GUILayout.FlexibleSpace();
                        }
                        if (item.AddDecoupler)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("decoupler", GUILayout.Width(78));
                                GUILayout.Label(quantity.ToString(CultureInfo.InvariantCulture) + " x Decoupler (" +
                                    item.DecouplerMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t each; " +
                                    (quantity * Math.Max(0.0, item.DecouplerMassTons)).ToString("0.###", CultureInfo.InvariantCulture) + " t)");
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                }
                GUILayout.EndVertical();
                return;
            }

            bool showAssembly = !string.IsNullOrEmpty(stage.AssemblyName) || !string.IsNullOrEmpty(stage.AssemblyFile);
            bool showMissionAssemblies = stage.Subassemblies.Count > 0;
            bool showDecoupler = stage.AddDecouplerMass && stage.DecouplerMassTons > 0.0;
            if (stage.Parts.Count == 0 && !showAssembly && !showMissionAssemblies && !showDecoupler)
            {
                GUILayout.Label("  no parts selected yet");
            }
            else
            {
                // Mission-linked assemblies from 0.7.20/0.7.21 are retained as fixed mass
                // on engine/tank stages for backward compatibility.
                if (showAssembly)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("subassembly", GUILayout.Width(78));
                        string assemblyName = string.IsNullOrEmpty(stage.AssemblyName) ? stage.AssemblyFile : stage.AssemblyName;
                        GUILayout.Label("1 x " + assemblyName + " (" +
                            stage.AssemblyMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t)");
                        GUILayout.FlexibleSpace();
                    }
                }

                if (showMissionAssemblies)
                {
                    foreach (PlannedSubassembly item in stage.Subassemblies)
                    {
                        if (item == null) continue;
                        int quantity = Math.Max(1, item.Quantity);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("subassembly", GUILayout.Width(78));
                            GUILayout.Label(quantity.ToString(CultureInfo.InvariantCulture) + " x " + item.DisplayName +
                                " (" + item.UnitMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t each; " +
                                (quantity * Math.Max(0.0, item.UnitMassTons)).ToString("0.###", CultureInfo.InvariantCulture) + " t)");
                            GUILayout.FlexibleSpace();
                        }
                        if (item.AddDecoupler)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("decoupler", GUILayout.Width(70));
                                GUILayout.Label(quantity.ToString(CultureInfo.InvariantCulture) + " x Decoupler (" +
                                    item.DecouplerMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t each; " +
                                    (quantity * Math.Max(0.0, item.DecouplerMassTons)).ToString("0.###", CultureInfo.InvariantCulture) + " t)");
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                }

                for (int p = 0; p < stage.Parts.Count; p++)
                {
                    PlannedPart part = stage.Parts[p];
                    if (part.IsEngine) continue;
                    DrawStagePartRow(index, p, part);
                }

                if (showDecoupler)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("decoupler", GUILayout.Width(70));
                        GUILayout.Label("1 x Decoupler (" +
                            stage.DecouplerMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t)");
                        GUILayout.FlexibleSpace();
                    }
                }

                for (int p = 0; p < stage.Parts.Count; p++)
                {
                    PlannedPart part = stage.Parts[p];
                    if (!part.IsEngine) continue;
                    DrawStagePartRow(index, p, part);
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawHoveredPartPreview()
        {
            if (_hoverPartPreviewTexture == null || Event.current == null || Event.current.type != EventType.Repaint) return;

            Vector2 sourceTopLeft = GUIUtility.ScreenToGUIPoint(new Vector2(
                _hoverPartPreviewSourceScreenRect.xMin, _hoverPartPreviewSourceScreenRect.yMin));
            Vector2 sourceBottomRight = GUIUtility.ScreenToGUIPoint(new Vector2(
                _hoverPartPreviewSourceScreenRect.xMax, _hoverPartPreviewSourceScreenRect.yMax));
            Rect source = Rect.MinMaxRect(sourceTopLeft.x, sourceTopLeft.y, sourceBottomRight.x, sourceBottomRight.y);

            const float margin = 8f;
            const float frame = 4f;
            float x = source.xMax + margin;
            if (x + HoverPartPreviewSize + frame * 2f > _window.width - margin)
                x = source.xMin - HoverPartPreviewSize - frame * 2f - margin;
            x = Mathf.Clamp(x, margin + frame, Mathf.Max(margin + frame, _window.width - HoverPartPreviewSize - frame - margin));

            float minY = 24f + margin + frame;
            float maxY = Mathf.Max(minY, _window.height - HoverPartPreviewSize - frame - margin);
            float y = Mathf.Clamp(source.center.y - HoverPartPreviewSize * 0.5f, minY, maxY);

            Rect frameRect = new Rect(x - frame, y - frame, HoverPartPreviewSize + frame * 2f, HoverPartPreviewSize + frame * 2f);
            GUI.Box(frameRect, GUIContent.none);
            GUI.DrawTexture(new Rect(x, y, HoverPartPreviewSize, HoverPartPreviewSize),
                _hoverPartPreviewTexture, ScaleMode.ScaleToFit, true);
        }

        private void DrawStagePartRow(int stageIndex, int partIndex, PlannedPart part)
        {
            if (part == null) return;

            using (new GUILayout.HorizontalScope())
            {
                // Reserve the same 40x40 slot during both Layout and Repaint so lazily
                // generating a thumbnail cannot change the IMGUI control geometry mid-frame.
                Rect thumbnailRect = GUILayoutUtility.GetRect(40f, 40f, GUILayout.Width(40f), GUILayout.Height(40f));
                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    // part.PartUrl disambiguates parts that share an internal name across
                    // mod packs (see PlannedPart.PartUrl) - passing null here is what let
                    // the Stage-By-Stage list silently reuse one part's icon for another.
                    Texture2D thumbnail = PartThumbnailCache.Get(part.PartName, part.PartUrl);
                    if (thumbnail != null)
                        GUI.DrawTexture(thumbnailRect, thumbnail, ScaleMode.ScaleToFit, true);

                    if (thumbnail != null && thumbnailRect.Contains(Event.current.mousePosition))
                    {
                        Texture2D preview = PartThumbnailCache.GetRotatingPreview(part.PartName, part.PartUrl);
                        _hoverPartPreviewTexture = preview ?? thumbnail;
                        Vector2 topLeft = GUIUtility.GUIToScreenPoint(new Vector2(thumbnailRect.xMin, thumbnailRect.yMin));
                        Vector2 bottomRight = GUIUtility.GUIToScreenPoint(new Vector2(thumbnailRect.xMax, thumbnailRect.yMax));
                        _hoverPartPreviewSourceScreenRect = Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
                    }
                }
                GUILayout.Label(part.IsEngine ? "engine" : "tank", GUILayout.Width(55));
                GUILayout.Label(part.Quantity + " x " + part.DisplayName);
                GUILayout.FlexibleSpace();

                // Placing parts is only offered once the plan is finalised, which is
                // what turns the plan from something being built into a build list.
                if (_plan.Finalized && GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    string message;
                    EditorPartSpawner.Spawn(part.PartName, out message);
                    _status = message;
                }
                if (!_plan.Finalized && GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    _pendingPartStage = stageIndex;
                    _pendingPartIndex = partIndex;
                }
            }
        }

        private void OpenNewStageDialog()
        {
            _editingStageIndex = -1;
            _newStageKind = PlannedStageKind.EnginesAndTanks;
            _pendingNewStageKind = null;
            _newStageSubassemblies.Clear();
            _newStageAssemblyCatalogScroll = Vector2.zero;
            _newStageSubassemblyScroll = Vector2.zero;
            RefreshNewStageAssemblyCatalog();
            _newTargetDv = "";
            _newMinTwr = "";
            _newMaxEngines = "";
            _newCargoMass = "0";
            _newAddDecouplerMass = false;
            _newSideBoosters = false;
            _newCoreBurnsToo = false;
            _newSideBoostersHaveRadialDecouplers = false;
            _newAssemblyFile = string.Empty;
            _newAssemblyName = string.Empty;
            _newAssemblyMassTons = 0.0;
            _newBasis = DeltaVBasis.Vacuum;
            _newMissionPlanName = string.Empty;
            _newMissionStepNumber = 0;
            _newMissionManeuver = Maneuver.None;
            _newBulkheadProfiles.Clear();
            PresetNewStageBulkheadProfiles();
            _bulkheadProfilesArePreset = _newBulkheadProfiles.Count > 0;
            _bulkheadDropdownOpen = false;
            ResetStageEntryFocus();
            PositionNewStageNextToPlan();
            _newStageVisible = true;
        }

        private void OpenMissionStageDialog(int missionIndex, MissionManeuver maneuver)
        {
            if (maneuver == null) return;

            int missionStepNumber = missionIndex + 1;
            string missionPlanName = _selectedMissionPlan == null ? string.Empty : (_selectedMissionPlan.Name ?? string.Empty);
            bool subassemblyStep = maneuver.StepKind == MissionStepKind.Subassemblies;
            int existingStageIndex = FindStageForMissionStep(missionPlanName, missionStepNumber);
            if (existingStageIndex >= 0)
            {
                OpenEditStageDialog(existingStageIndex);
                _newMissionPlanName = missionPlanName;
                _newMissionStepNumber = missionStepNumber;
                _newMissionManeuver = subassemblyStep ? Maneuver.None : maneuver.Kind;
                _newStageKind = subassemblyStep ? PlannedStageKind.Subassemblies : PlannedStageKind.EnginesAndTanks;
                _pendingNewStageKind = null;
                _newAssemblyFile = string.Empty;
                _newAssemblyName = string.Empty;
                _newAssemblyMassTons = 0.0;
                _newStageSubassemblies.Clear();
                if (subassemblyStep)
                    LoadMissionAssemblyDrafts(maneuver);
                _newTargetDv = subassemblyStep ? "0" : maneuver.DeltaV.ToString("0.###", CultureInfo.InvariantCulture);
                _newBasis = subassemblyStep ? DeltaVBasis.Vacuum : maneuver.DeltaVBasis;
                _status = "Editing Stage " + (existingStageIndex + 1).ToString(CultureInfo.InvariantCulture) +
                    " for mission step " + missionStepNumber.ToString(CultureInfo.InvariantCulture) +
                    " (" + (subassemblyStep ? "Subassemblies" : CommonRoutines.FormatManeuver(maneuver.Kind)) + ").";
                return;
            }

            OpenNewStageDialog();
            _newMissionPlanName = missionPlanName;
            _newMissionStepNumber = missionStepNumber;
            _newMissionManeuver = subassemblyStep ? Maneuver.None : maneuver.Kind;
            _newStageKind = subassemblyStep ? PlannedStageKind.Subassemblies : PlannedStageKind.EnginesAndTanks;
            _pendingNewStageKind = null;
            _newAssemblyFile = string.Empty;
            _newAssemblyName = string.Empty;
            _newAssemblyMassTons = 0.0;
            _newStageSubassemblies.Clear();
            if (subassemblyStep)
                LoadMissionAssemblyDrafts(maneuver);
            _newTargetDv = subassemblyStep ? "0" : maneuver.DeltaV.ToString("0.###", CultureInfo.InvariantCulture);
            _newBasis = subassemblyStep ? DeltaVBasis.Vacuum : maneuver.DeltaVBasis;
            _status = "New Stage opened for mission step " + missionStepNumber.ToString(CultureInfo.InvariantCulture) +
                " (" + (subassemblyStep ? "Subassemblies" : CommonRoutines.FormatManeuver(maneuver.Kind)) + ").";
        }

        private int FindStageForMissionStep(string missionPlanName, int missionStepNumber)
        {
            if (missionStepNumber <= 0) return -1;
            for (int i = 0; i < _plan.Stages.Count; i++)
            {
                PlannedStage stage = _plan.Stages[i];
                if (stage.MissionStepNumber != missionStepNumber) continue;
                if (!string.Equals(stage.MissionPlanName ?? string.Empty, missionPlanName ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase)) continue;
                return i;
            }
            return -1;
        }

        // Reopens the dialog on an existing stage. Recalculating keeps the parts already
        // chosen for it; only the requirements are replaced.
        private void OpenEditStageDialog(int index)
        {
            if (index < 0 || index >= _plan.Stages.Count) return;

            PlannedStage stage = _plan.Stages[index];
            _editingStageIndex = index;
            _newStageKind = stage.Kind;
            _pendingNewStageKind = null;
            _newStageSubassemblies.Clear();
            foreach (PlannedSubassembly item in stage.Subassemblies)
            {
                if (item == null) continue;
                _newStageSubassemblies.Add(new StageSubassemblyDraft
                {
                    AssemblyFile = item.AssemblyFile ?? string.Empty,
                    AssemblyName = item.AssemblyName ?? string.Empty,
                    UnitMassTons = Math.Max(0.0, item.UnitMassTons),
                    CountText = Math.Max(1, item.Quantity).ToString(CultureInfo.InvariantCulture),
                    AddDecoupler = item.AddDecoupler,
                    DecouplerMassTons = Math.Max(0.0, item.DecouplerMassTons)
                });
            }
            RefreshNewStageAssemblyCatalog();
            _newTargetDv = stage.TargetDeltaV.ToString("0.###", CultureInfo.InvariantCulture);
            _newMinTwr = stage.MinimumTwr.ToString("0.###", CultureInfo.InvariantCulture);
            _newMaxEngines = stage.MaxEngineCount.ToString(CultureInfo.InvariantCulture);
            _newCargoMass = stage.CargoMassTons.ToString("0.###", CultureInfo.InvariantCulture);
            _newAddDecouplerMass = stage.AddDecouplerMass;
            _newSideBoosters = stage.SideBoosters;
            _newCoreBurnsToo = stage.CoreBurnsToo;
            _newSideBoostersHaveRadialDecouplers = stage.SideBoostersHaveRadialDecouplers;
            _newAssemblyFile = stage.AssemblyFile ?? string.Empty;
            _newAssemblyName = stage.AssemblyName ?? string.Empty;
            _newAssemblyMassTons = Math.Max(0.0, stage.AssemblyMassTons);
            _newBasis = stage.TargetDeltaVBasis;
            _newMissionPlanName = stage.MissionPlanName ?? string.Empty;
            _newMissionStepNumber = stage.MissionStepNumber;
            _newMissionManeuver = stage.MissionManeuver;
            _newBulkheadProfiles.Clear();
            _newBulkheadProfiles.AddRange(stage.BulkheadProfiles);
            _bulkheadProfilesArePreset = false;
            _bulkheadDropdownOpen = false;
            ResetStageEntryFocus();
            _newStageVisible = true;
        }

        private void DrawNewStageWindow(int id)
        {
            if (_newStageKind == PlannedStageKind.EnginesAndTanks)
                CaptureStageTabNavigation();

            GUILayout.Label("Choose what this stage contains.");
            GUILayout.Space(4f);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Stage type", GUILayout.Width(110f));
                bool enginesSelected = _newStageKind == PlannedStageKind.EnginesAndTanks;
                bool subassembliesSelected = _newStageKind == PlannedStageKind.Subassemblies;
                if (GUILayout.Toggle(enginesSelected, "Engines & Tanks", "Button", GUILayout.Width(125f)) && !enginesSelected)
                    _pendingNewStageKind = PlannedStageKind.EnginesAndTanks;
                if (GUILayout.Toggle(subassembliesSelected, "Subassemblies", "Button", GUILayout.Width(115f)) && !subassembliesSelected)
                    _pendingNewStageKind = PlannedStageKind.Subassemblies;
            }
            if (_editingStageIndex >= 0 && _editingStageIndex < _plan.Stages.Count &&
                _plan.Stages[_editingStageIndex].Kind != _newStageKind)
                GUILayout.Label("Changing the stage type will replace the existing stage contents when you save/calculate.");
            GUILayout.Space(6f);

            if (_newStageKind == PlannedStageKind.Subassemblies)
                DrawSubassemblyStageEditor();
            else
                DrawEngineTankStageEditor();

            GUI.DragWindow(new Rect(0f, 0f, _newStageWindow.width, 24f));
        }

        private void DrawEngineTankStageEditor()
        {
            GUILayout.Label("Requirements for the engine and tank stage.");
            GUILayout.Space(4);

            DialogField("Target Δv (m/s)", ref _newTargetDv, StageTargetDvControl);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Δv basis", GUILayout.Width(160));
                if (GUILayout.Toggle(_newBasis == DeltaVBasis.Vacuum, "Vacuum", "Button", GUILayout.Width(80)))
                    _newBasis = DeltaVBasis.Vacuum;
                if (GUILayout.Toggle(_newBasis == DeltaVBasis.Atmospheric, "Atmosphere", "Button", GUILayout.Width(90)))
                    _newBasis = DeltaVBasis.Atmospheric;
            }
            DialogField("Minimum TWR", ref _newMinTwr, StageMinTwrControl);
            DialogField("Max engines", ref _newMaxEngines, StageMaxEnginesControl);
            DialogField("Additional Cargo Mass", ref _newCargoMass, StageCargoMassControl);

            if (_newAssemblyMassTons > 0.0)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Mission subassembly", GUILayout.Width(160));
                    string assemblyName = string.IsNullOrEmpty(_newAssemblyName) ? _newAssemblyFile : _newAssemblyName;
                    GUILayout.Label(assemblyName + " (" + _newAssemblyMassTons.ToString("0.###", CultureInfo.InvariantCulture) + " t)");
                }
            }
            if (_newStageSubassemblies.Count > 0)
            {
                double missionAssemblyMass;
                bool validMissionAssemblies = TryGetSubassemblyDraftTotalMass(out missionAssemblyMass);
                int copies = 0;
                foreach (StageSubassemblyDraft item in _newStageSubassemblies)
                {
                    if (item == null) continue;
                    int quantity;
                    if (int.TryParse(item.CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) && quantity > 0) copies += quantity;
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Mission subassemblies", GUILayout.Width(160));
                    GUILayout.Label(copies.ToString(CultureInfo.InvariantCulture) + " copies / " +
                        _newStageSubassemblies.Count.ToString(CultureInfo.InvariantCulture) + " types (" +
                        (validMissionAssemblies ? missionAssemblyMass.ToString("0.###", CultureInfo.InvariantCulture) : "invalid") + " t)");
                }
            }

            DrawPreviousStagesMass();

            double displayedDecouplerMass = DecouplerMasses.GetDecouplerMassTons(_newBulkheadProfiles);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Decoupler mass", GUILayout.Width(160));
                _newAddDecouplerMass = GUILayout.Toggle(_newAddDecouplerMass, "Add", GUILayout.Width(75));
                GUILayout.Label(_newAddDecouplerMass
                    ? displayedDecouplerMass.ToString("0.###", CultureInfo.InvariantCulture) + " t"
                    : "not included");
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Booster layout", GUILayout.Width(160));
                _newSideBoosters = GUILayout.Toggle(_newSideBoosters, "Side Boosters", GUILayout.Width(120));
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(160);
                _newCoreBurnsToo = GUILayout.Toggle(_newCoreBurnsToo, "Core burns too", GUILayout.Width(115));
            }
            if (_newSideBoosters)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(160);
                    _newSideBoostersHaveRadialDecouplers = GUILayout.Toggle(
                        _newSideBoostersHaveRadialDecouplers, "Radial decouplers", GUILayout.Width(145));
                }
            }
            else
            {
                _newSideBoostersHaveRadialDecouplers = false;
            }

            ApplyStageEntryFocus();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Bulkhead profiles", GUILayout.Width(160));
                if (GUILayout.Button(BulkheadSelectionSummary(_newBulkheadProfiles) + (_bulkheadDropdownOpen ? "  ▲" : "  ▼"), GUILayout.Width(180)))
                    _bulkheadDropdownOpen = !_bulkheadDropdownOpen;
            }

            if (_bulkheadDropdownOpen)
            {
                _bulkheadScroll = GUILayout.BeginScrollView(_bulkheadScroll, GUILayout.Height(110f));
                foreach (BulkheadProfile profile in BulkheadProfiles.All)
                {
                    bool selected = ContainsProfile(_newBulkheadProfiles, profile.Profile);
                    bool nowSelected = GUILayout.Toggle(selected, profile.Label);
                    if (nowSelected != selected)
                    {
                        if (_bulkheadProfilesArePreset)
                        {
                            _bulkheadProfilesArePreset = false;
                            if (nowSelected && !selected) _newBulkheadProfiles.Clear();
                        }
                        if (nowSelected) CommonRoutines.AddUniqueIgnoreCase(_newBulkheadProfiles, profile.Profile);
                        else RemoveProfile(_newBulkheadProfiles, profile.Profile);
                    }
                }
                GUILayout.EndScrollView();
                if (GUILayout.Button("Clear selection", GUILayout.Width(140)))
                {
                    _newBulkheadProfiles.Clear();
                    _bulkheadProfilesArePreset = false;
                }
            }

            double targetDv, minTwr, cargoMass;
            int maxEngines;
            bool targetOk = CommonRoutines.TryParseDouble(_newTargetDv, out targetDv) && targetDv > 0.0;
            bool twrOk = CommonRoutines.TryParseDouble(_newMinTwr, out minTwr) && minTwr > 0.0;
            bool enginesOk = int.TryParse(_newMaxEngines, out maxEngines) && maxEngines > 0;
            bool cargoOk = CommonRoutines.TryParseDouble(_newCargoMass, out cargoMass) && cargoMass >= 0.0;
            bool complete = targetOk && twrOk && enginesOk && cargoOk;

            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                GUI.enabled = complete;
                if (GUILayout.Button("Calculate", GUILayout.Width(100), GUILayout.Height(26)))
                {
                    _pendingTargetDv = targetDv;
                    _pendingMinTwr = minTwr;
                    _pendingMaxEngines = maxEngines;
                    _pendingCargoMass = cargoMass;
                    _pendingAddDecouplerMass = _newAddDecouplerMass;
                    _pendingSideBoosters = _newSideBoosters;
                    _pendingCoreBurnsToo = _newCoreBurnsToo;
                    _pendingSideBoostersHaveRadialDecouplers = _newSideBoosters && _newSideBoostersHaveRadialDecouplers;
                    _pendingAssemblyFile = _newAssemblyFile ?? string.Empty;
                    _pendingAssemblyName = _newAssemblyName ?? string.Empty;
                    _pendingAssemblyMassTons = Math.Max(0.0, _newAssemblyMassTons);
                    _pendingBulkheadProfiles.Clear();
                    _pendingBulkheadProfiles.AddRange(_newBulkheadProfiles);
                    _pendingBasis = _newBasis;
                    QueueCurrentMissionLinkForStageStart();
                    _pendingEditIndex = _editingStageIndex;
                    _pendingStageStart = true;
                    _newStageVisible = false;
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                DrawStageDialogCancelButton();
            }

            if (!complete) GUILayout.Label("Enter valid target Δv, minimum TWR, max engines, and an additional cargo mass of zero or more.");
        }

        private void DrawSubassemblyStageEditor()
        {
            GUILayout.Label("Add one or more saved KSP subassemblies to this stage. Each line has its own count and decoupler option.");
            DrawPreviousStagesMass();

            GUILayout.Space(4f);
            GUILayout.Label("Available subassemblies");
            _newStageAssemblyCatalogScroll = GUILayout.BeginScrollView(_newStageAssemblyCatalogScroll, GUI.skin.box, GUILayout.Height(145f));
            if (_newStageAssemblyCatalog.Count == 0)
            {
                GUILayout.Label("No saved subassemblies were found in " + SavedAssemblyCatalog.DirectoryPath);
            }
            else
            {
                for (int i = 0; i < _newStageAssemblyCatalog.Count; i++)
                {
                    SavedAssemblyInfo assembly = _newStageAssemblyCatalog[i];
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(assembly.SelectorLabel, GUILayout.MinWidth(360f));
                        GUILayout.FlexibleSpace();
                        bool oldEnabled = GUI.enabled;
                        GUI.enabled = oldEnabled && assembly.MassAvailable;
                        if (GUILayout.Button("Add", GUILayout.Width(55f))) _pendingSubassemblyCatalogAdd = i;
                        GUI.enabled = oldEnabled;
                    }
                    if (!assembly.MassAvailable && !string.IsNullOrEmpty(assembly.Error))
                        GUILayout.Label("  " + assembly.Error);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            GUILayout.Label("Subassemblies in this stage");
            _newStageSubassemblyScroll = GUILayout.BeginScrollView(_newStageSubassemblyScroll, GUI.skin.box, GUILayout.Height(195f));
            if (_newStageSubassemblies.Count == 0)
            {
                GUILayout.Label("No subassemblies added yet.");
            }
            else
            {
                for (int i = 0; i < _newStageSubassemblies.Count; i++)
                    DrawSubassemblyDraftRow(i, _newStageSubassemblies[i]);
            }
            GUILayout.EndScrollView();

            double totalMass;
            bool complete = TryGetSubassemblyDraftTotalMass(out totalMass);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Stage mass", GUILayout.Width(110f));
                GUILayout.Label(totalMass.ToString("0.###", CultureInfo.InvariantCulture) + " t");
            }

            GUILayout.Space(6f);
            using (new GUILayout.HorizontalScope())
            {
                GUI.enabled = complete;
                string buttonText = _editingStageIndex >= 0 ? "Save Stage" : "Add Stage";
                if (GUILayout.Button(buttonText, GUILayout.Width(100f), GUILayout.Height(26f)))
                {
                    QueueCurrentMissionLinkForStageStart();
                    _pendingEditIndex = _editingStageIndex;
                    _pendingStageStart = true;
                    _newStageVisible = false;
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                DrawStageDialogCancelButton();
            }

            if (!complete)
                GUILayout.Label(_newStageSubassemblies.Count == 0 ? "Add at least one subassembly." : "Every subassembly count must be a whole number greater than zero.");
        }

        private void DrawSubassemblyDraftRow(int index, StageSubassemblyDraft item)
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
                    if (GUILayout.Button("Remove", GUILayout.Width(65f))) _pendingSubassemblyDraftRemove = index;
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

        private void DrawPreviousStagesMass()
        {
            int dialogStageIndex = _editingStageIndex >= 0 ? _editingStageIndex : _plan.Stages.Count;
            if (dialogStageIndex <= 0) return;
            double previousStagesMass = GetPayloadForStageIndex(dialogStageIndex);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Previous stages mass", GUILayout.Width(160));
                GUILayout.Label(previousStagesMass.ToString("0.###", CultureInfo.InvariantCulture) + " t");
            }
        }

        private void DrawStageDialogCancelButton()
        {
            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(26)))
            {
                _newStageVisible = false;
                _editingStageIndex = -1;
            }
        }

        private void QueueCurrentMissionLinkForStageStart()
        {
            _pendingMissionPlanName = _newMissionPlanName;
            _pendingMissionStepNumber = _newMissionStepNumber;
            _pendingMissionManeuver = _newMissionManeuver;
        }

        private void RefreshNewStageAssemblyCatalog()
        {
            _newStageAssemblyCatalog.Clear();
            try
            {
                _newStageAssemblyCatalog.AddRange(SavedAssemblyCatalog.Scan());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VesselPlanner] Unable to scan subassemblies for stage: " + ex.Message);
            }
        }

        private void LoadMissionAssemblyDrafts(MissionManeuver maneuver)
        {
            _newStageSubassemblies.Clear();
            if (maneuver == null || !maneuver.HasAssembly) return;

            foreach (PlannedSubassembly saved in maneuver.Assemblies)
            {
                if (saved == null) continue;
                double decouplerMass = Math.Max(0.0, saved.DecouplerMassTons);
                if (decouplerMass <= 0.0)
                {
                    SavedAssemblyInfo catalogItem = _newStageAssemblyCatalog.FirstOrDefault(item => item != null &&
                        ((!string.IsNullOrEmpty(saved.AssemblyFile) && string.Equals(item.RelativePath, saved.AssemblyFile, StringComparison.OrdinalIgnoreCase)) ||
                         (string.IsNullOrEmpty(saved.AssemblyFile) && string.Equals(item.DisplayName, saved.AssemblyName, StringComparison.OrdinalIgnoreCase))));
                    if (catalogItem != null)
                        decouplerMass = Math.Max(0.0, DecouplerMasses.GetDecouplerMassTons(catalogItem.BulkheadProfiles));
                }

                _newStageSubassemblies.Add(new StageSubassemblyDraft
                {
                    AssemblyFile = saved.AssemblyFile ?? string.Empty,
                    AssemblyName = saved.AssemblyName ?? string.Empty,
                    UnitMassTons = Math.Max(0.0, saved.UnitMassTons),
                    CountText = Math.Max(1, saved.Quantity).ToString(CultureInfo.InvariantCulture),
                    AddDecoupler = saved.AddDecoupler,
                    DecouplerMassTons = decouplerMass
                });
            }

            // Backward compatibility for an in-memory maneuver still using the 0.7.20/0.7.21
            // single-assembly fields rather than the new collection.
            if (_newStageSubassemblies.Count == 0 &&
                (!string.IsNullOrEmpty(maneuver.AssemblyFile) || !string.IsNullOrEmpty(maneuver.AssemblyName)))
            {
                string file = maneuver.AssemblyFile ?? string.Empty;
                string name = maneuver.AssemblyName ?? string.Empty;
                double decouplerMass = 0.0;
                SavedAssemblyInfo catalogItem = _newStageAssemblyCatalog.FirstOrDefault(item => item != null &&
                    ((!string.IsNullOrEmpty(file) && string.Equals(item.RelativePath, file, StringComparison.OrdinalIgnoreCase)) ||
                     (string.IsNullOrEmpty(file) && string.Equals(item.DisplayName, name, StringComparison.OrdinalIgnoreCase))));
                if (catalogItem != null)
                    decouplerMass = Math.Max(0.0, DecouplerMasses.GetDecouplerMassTons(catalogItem.BulkheadProfiles));

                _newStageSubassemblies.Add(new StageSubassemblyDraft
                {
                    AssemblyFile = file,
                    AssemblyName = name,
                    UnitMassTons = Math.Max(0.0, maneuver.AssemblyMassTons),
                    CountText = "1",
                    AddDecoupler = false,
                    DecouplerMassTons = decouplerMass
                });
            }
        }

        private void AddSubassemblyDraftFromCatalog(int index)
        {
            if (index < 0 || index >= _newStageAssemblyCatalog.Count) return;
            SavedAssemblyInfo assembly = _newStageAssemblyCatalog[index];
            if (assembly == null || !assembly.MassAvailable) return;

            StageSubassemblyDraft existing = _newStageSubassemblies.FirstOrDefault(item =>
                item != null &&
                ((!string.IsNullOrEmpty(assembly.RelativePath) && string.Equals(item.AssemblyFile, assembly.RelativePath, StringComparison.OrdinalIgnoreCase)) ||
                 (string.IsNullOrEmpty(assembly.RelativePath) && string.Equals(item.AssemblyName, assembly.DisplayName, StringComparison.OrdinalIgnoreCase))));
            if (existing != null)
            {
                int count;
                if (!int.TryParse(existing.CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count < 1) count = 1;
                existing.CountText = (count + 1).ToString(CultureInfo.InvariantCulture);
                return;
            }

            _newStageSubassemblies.Add(new StageSubassemblyDraft
            {
                AssemblyFile = assembly.RelativePath ?? string.Empty,
                AssemblyName = assembly.DisplayName ?? string.Empty,
                UnitMassTons = Math.Max(0.0, assembly.MassTons),
                CountText = "1",
                AddDecoupler = false,
                DecouplerMassTons = Math.Max(0.0, DecouplerMasses.GetDecouplerMassTons(assembly.BulkheadProfiles))
            });
        }

        private bool TryGetSubassemblyDraftTotalMass(out double totalMass)
        {
            totalMass = 0.0;
            if (_newStageSubassemblies.Count == 0) return false;
            foreach (StageSubassemblyDraft item in _newStageSubassemblies)
            {
                if (item == null) return false;
                int quantity;
                if (!int.TryParse(item.CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) || quantity <= 0)
                    return false;
                double perCopy = Math.Max(0.0, item.UnitMassTons) + (item.AddDecoupler ? Math.Max(0.0, item.DecouplerMassTons) : 0.0);
                totalMass += quantity * perCopy;
            }
            return true;
        }

        private void StartSubassemblyStage()
        {
            bool editingExistingStage = _pendingEditIndex >= 0 && _pendingEditIndex < _plan.Stages.Count;
            PlannedStage stage;
            if (editingExistingStage)
                stage = _plan.Stages[_pendingEditIndex];
            else
            {
                stage = new PlannedStage();
                _plan.Stages.Add(stage);
            }

            stage.Kind = PlannedStageKind.Subassemblies;
            stage.TargetDeltaV = 0.0;
            stage.MinimumTwr = 0.0;
            stage.MaxEngineCount = 0;
            stage.CargoMassTons = 0.0;
            stage.AddDecouplerMass = false;
            stage.DecouplerMassTons = 0.0;
            stage.SideBoosters = false;
            stage.CoreBurnsToo = false;
            stage.SideBoostersHaveRadialDecouplers = false;
            stage.AssemblyFile = string.Empty;
            stage.AssemblyName = string.Empty;
            stage.AssemblyMassTons = 0.0;
            stage.BulkheadProfiles.Clear();
            stage.Parts.Clear();
            stage.Subassemblies.Clear();
            stage.MissionPlanName = _pendingMissionPlanName ?? string.Empty;
            stage.MissionStepNumber = _pendingMissionStepNumber;
            stage.MissionManeuver = _pendingMissionManeuver;

            foreach (StageSubassemblyDraft draft in _newStageSubassemblies)
            {
                if (draft == null) continue;
                int quantity;
                if (!int.TryParse(draft.CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) || quantity <= 0) continue;
                stage.Subassemblies.Add(new PlannedSubassembly
                {
                    AssemblyFile = draft.AssemblyFile ?? string.Empty,
                    AssemblyName = draft.AssemblyName ?? string.Empty,
                    UnitMassTons = Math.Max(0.0, draft.UnitMassTons),
                    Quantity = quantity,
                    AddDecoupler = draft.AddDecoupler,
                    DecouplerMassTons = Math.Max(0.0, draft.DecouplerMassTons)
                });
            }

            double basePayload;
            if (!CommonRoutines.TryParseDouble(_payloadText, out basePayload)) basePayload = 0.0;
            _plan.PayloadMassTons = Math.Max(0.0, basePayload);
            _activeStageIndex = -1;
            _pendingEditIndex = -1;
            _newStageVisible = false;
            int stageIndex = _plan.Stages.IndexOf(stage);
            _status = "Stage " + (stageIndex + 1).ToString(CultureInfo.InvariantCulture) +
                " saved with " + stage.Subassemblies.Count.ToString(CultureInfo.InvariantCulture) + " subassembly line" +
                (stage.Subassemblies.Count == 1 ? "." : "s.");
        }

        private void CopyDraftSubassembliesToStage(PlannedStage stage)
        {
            if (stage == null) return;
            foreach (StageSubassemblyDraft draft in _newStageSubassemblies)
            {
                if (draft == null) continue;
                int quantity;
                if (!int.TryParse(draft.CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) || quantity <= 0) continue;
                stage.Subassemblies.Add(new PlannedSubassembly
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

        private void StartStage(double targetDv, double minTwr, int maxEngines, double cargoMass)
        {
            bool editingExistingStage = _pendingEditIndex >= 0 && _pendingEditIndex < _plan.Stages.Count;
            PlannedStage stage;
            if (editingExistingStage)
            {
                // Editing keeps the parts already chosen for the stage and replaces only the
                // requirements they were chosen against.
                stage = _plan.Stages[_pendingEditIndex];
                _activeStageIndex = _pendingEditIndex;
            }
            else
            {
                stage = new PlannedStage();
                _plan.Stages.Add(stage);
                _activeStageIndex = _plan.Stages.Count - 1;
            }

            if (stage.Kind != PlannedStageKind.EnginesAndTanks) stage.Parts.Clear();
            stage.Kind = PlannedStageKind.EnginesAndTanks;
            stage.Subassemblies.Clear();
            if (_pendingMissionStepNumber > 0)
                CopyDraftSubassembliesToStage(stage);
            stage.TargetDeltaV = targetDv;
            stage.TargetDeltaVBasis = _pendingBasis;
            stage.MissionPlanName = _pendingMissionPlanName ?? string.Empty;
            stage.MissionStepNumber = _pendingMissionStepNumber;
            stage.MissionManeuver = _pendingMissionManeuver;
            stage.MinimumTwr = minTwr;
            stage.MaxEngineCount = maxEngines;
            stage.CargoMassTons = Math.Max(0.0, cargoMass);
            stage.AssemblyFile = _pendingAssemblyFile ?? string.Empty;
            stage.AssemblyName = _pendingAssemblyName ?? string.Empty;
            stage.AssemblyMassTons = Math.Max(0.0, _pendingAssemblyMassTons);
            stage.BulkheadProfiles.Clear();
            stage.BulkheadProfiles.AddRange(_pendingBulkheadProfiles);
            stage.AddDecouplerMass = _pendingAddDecouplerMass;
            stage.DecouplerMassTons = stage.AddDecouplerMass
                ? DecouplerMasses.GetDecouplerMassTons(stage.BulkheadProfiles)
                : 0.0;
            stage.SideBoosters = _pendingSideBoosters;
            stage.CoreBurnsToo = _pendingCoreBurnsToo;
            stage.SideBoostersHaveRadialDecouplers = stage.SideBoosters && _pendingSideBoostersHaveRadialDecouplers;
            int stageIndex = _activeStageIndex;
            _pendingEditIndex = -1;
            _newStageVisible = false;

            double basePayload;
            if (!CommonRoutines.TryParseDouble(_payloadText, out basePayload)) basePayload = 0.0;
            _plan.PayloadMassTons = basePayload;

            // Stage 1 lifts the plan's original payload. Every later stage lifts that
            // payload plus all previously planned stages, using the full wet mass of the
            // selected engine/tank parts already recorded in the plan.
            double payload = GetPayloadForStageIndex(stageIndex);

            // When recalculating an existing stage, use the final body referenced by the
            // selected/linked mission plan as the planner environment before Calculate runs.
            // New stages retain the planner's current body until they are edited later.
            string calculationBody = editingExistingStage ? GetLastMissionPlanBodyName(stage) : string.Empty;
            double carriedAssemblyMass = Math.Max(0.0, stage.AssemblyMassTons) + stage.SubassemblyStageMassTons;
            _planner.BeginPlannedStage(targetDv, stage.TargetDeltaVBasis, minTwr, maxEngines, payload, stage.CargoMassTons, stage.DecouplerMassTons, carriedAssemblyMass, calculationBody);
            _planner.RequestBringToFront();
            _status = "Stage " + _plan.Stages.Count + " sent to the planner. Add an engine and tanks there.";
        }

        private string GetLastMissionPlanBodyName(PlannedStage stage)
        {
            MissionPlan plan = null;
            string linkedName = stage == null ? string.Empty : (stage.MissionPlanName ?? string.Empty);

            if (_selectedMissionPlan != null &&
                (string.IsNullOrEmpty(linkedName) || string.Equals(_selectedMissionPlan.Name ?? string.Empty, linkedName, StringComparison.OrdinalIgnoreCase)))
            {
                plan = _selectedMissionPlan;
            }
            else if (!string.IsNullOrEmpty(linkedName))
            {
                try
                {
                    foreach (string path in MissionPlanPersistence.ListFiles())
                    {
                        MissionPlan candidate = MissionPlanPersistence.Load(path);
                        if (candidate == null) continue;
                        if (!string.Equals(candidate.Name ?? string.Empty, linkedName, StringComparison.OrdinalIgnoreCase)) continue;
                        plan = candidate;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[VesselPlanner] Unable to resolve linked mission plan body: " + ex.Message);
                }
            }

            if (plan == null) return string.Empty;
            for (int i = plan.Maneuvers.Count - 1; i >= 0; i--)
            {
                MissionManeuver maneuver = plan.Maneuvers[i];
                if (maneuver == null) continue;

                if (maneuver.Kind == Maneuver.TransferToAnotherPlanet)
                {
                    if (!string.IsNullOrWhiteSpace(maneuver.DestinationBody)) return maneuver.DestinationBody.Trim();
                    if (!string.IsNullOrWhiteSpace(maneuver.SourceBody)) return maneuver.SourceBody.Trim();
                }

                if (!string.IsNullOrWhiteSpace(maneuver.Body)) return maneuver.Body.Trim();
            }

            return string.Empty;
        }

        private void OpenMissionPlanSelection()
        {
            _missionPlanFiles.Clear();
            try
            {
                _missionPlanFiles.AddRange(MissionPlanPersistence.ListFiles());
                _missionSelectVisible = true;
            }
            catch (Exception ex)
            {
                _status = "Unable to list mission plans: " + ex.Message;
                Debug.LogWarning("[VesselPlanner] Unable to list mission plans: " + ex.Message);
            }
        }

        private void DrawMissionSelectWindow(int id)
        {
            GUILayout.Label("Saved mission plans in " + MissionPlanPersistence.DirectoryPath);
            _missionSelectScroll = GUILayout.BeginScrollView(_missionSelectScroll, GUILayout.Height(300f));
            if (_missionPlanFiles.Count == 0)
            {
                GUILayout.Label("No saved mission plans found. Save one from Mission Planner first.");
            }
            else
            {
                foreach (string file in _missionPlanFiles)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(Path.GetFileNameWithoutExtension(file));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Select", GUILayout.Width(70))) _pendingMissionLoadPath = file;
                    }
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close", GUILayout.Width(80))) _missionSelectVisible = false;
            GUI.DragWindow(new Rect(0f, 0f, _missionSelectWindow.width, _missionSelectWindow.height));
        }

        private void SelectMissionPlan(string path)
        {
            try
            {
                MissionPlan plan = MissionPlanPersistence.Load(path);
                if (plan == null)
                {
                    _status = "Mission plan could not be read.";
                    return;
                }

                _selectedMissionPlan = plan;
                _missionSelectVisible = false;
                _missionManeuverScroll = Vector2.zero;

                // A Stage-By-Stage plan built from a Mission Planner plan starts with
                // the same name. The user can still edit the Vessel field afterwards.
                string missionName = string.IsNullOrWhiteSpace(plan.Name) ? "Plan" : plan.Name.Trim();
                _vesselNameText = missionName;
                _plan.Name = CommonRoutines.SanitiseFileName(missionName, "Plan");
                _plan.VesselName = missionName;

                if (_window.width < 790f) _window.width = 790f;
                _status = "Selected mission plan " + plan.Name + ".";
            }
            catch (Exception ex)
            {
                _status = "Unable to load mission plan: " + ex.Message;
                Debug.LogWarning("[VesselPlanner] Unable to load mission plan: " + ex.Message);
            }
        }


        private void OpenLoadDialog()
        {
            _planFiles.Clear();
            _deleteArmedPath = null;
            try
            {
                AddPlanFiles(PlansDirectory);
                AddPlanFiles(LegacyPlansDirectory);
                _planFiles.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _status = "Unable to list plans: " + ex.Message;
            }
            _loadVisible = true;
        }


        private void AddPlanFiles(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;
            foreach (string file in Directory.GetFiles(directory, "*.cfg"))
            {
                string name = Path.GetFileName(file);
                bool alreadyListed = _planFiles.Exists(existing => string.Equals(Path.GetFileName(existing), name, StringComparison.OrdinalIgnoreCase));
                if (!alreadyListed) _planFiles.Add(file);
            }
        }

        private void DrawLoadWindow(int id)
        {
            //_planner.DrawSolidWindowOverlay(new Rect(0f, 0f, _loadWindow.width, _loadWindow.height), "Load Plan");

            GUILayout.Label("Saved plans in " + PlansDirectory + " (legacy EngineStagePlanner plans are also read)");
            _loadScroll = GUILayout.BeginScrollView(_loadScroll, GUILayout.Height(260f));
            if (_planFiles.Count == 0)
            {
                GUILayout.Label("No saved plans found.");
            }
            else
            {
                foreach (string file in _planFiles)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(Path.GetFileNameWithoutExtension(file));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Load", GUILayout.Width(60))) _pendingLoadPath = file;
                        // Two presses, like Clear: the first arms this row so a misplaced click
                        // cannot delete a saved plan.
                        bool armed = string.Equals(_deleteArmedPath, file, StringComparison.OrdinalIgnoreCase);
                        if (GUILayout.Button(armed ? "Confirm" : "Delete", GUILayout.Width(70)))
                        {
                            if (armed) _pendingDeletePath = file;
                            else _deleteArmedPath = file;
                        }
                    }
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Close", GUILayout.Width(80))) _loadVisible = false;
            GUI.DragWindow(new Rect(0f, 0f, _loadWindow.width, _loadWindow.height));
        }

        // Deletes the file only; the plan currently loaded in the window is left alone.
        private void DeletePlan(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                _planFiles.Remove(path);
                _status = "Deleted " + Path.GetFileName(path) + ".";
            }
            catch (Exception ex)
            {
                _status = "Unable to delete plan: " + ex.Message;
                Debug.LogWarning("[VesselPlanner] Unable to delete plan: " + ex.Message);
            }
        }

        private void SavePlan()
        {
            try
            {
                double payload;
                if (CommonRoutines.TryParseDouble(_payloadText, out payload)) _plan.PayloadMassTons = payload;
                _plan.Name = CommonRoutines.SanitiseFileName(_vesselNameText, "Plan");
                _plan.VesselName = _vesselNameText;
                _plan.BodyName = _planner.SelectedBodyName;

                Directory.CreateDirectory(PlansDirectory);
                string path = Path.Combine(PlansDirectory, _plan.Name + ".cfg");

                var root = new ConfigNode();
                root.AddNode(_plan.ToConfigNode());
                root.Save(path);
                _status = "Saved to " + path;
            }
            catch (Exception ex)
            {
                _status = "Unable to save plan: " + ex.Message;
                Debug.LogWarning("[VesselPlanner] Unable to save plan: " + ex.Message);
            }
        }

        private void LoadPlan(string path)
        {
            try
            {
                ConfigNode root = ConfigNode.Load(path);
                ConfigNode planNode = root == null ? null : root.GetNode(StagePlan.RootNodeName);
                StagePlan loaded = StagePlan.FromConfigNode(planNode);
                if (loaded == null)
                {
                    _status = "Plan file could not be read.";
                    return;
                }

                _plan.Name = loaded.Name;
                _plan.VesselName = loaded.VesselName;
                _plan.PayloadMassTons = loaded.PayloadMassTons;
                _plan.BodyName = loaded.BodyName;
                _plan.Finalized = loaded.Finalized;
                _plan.Stages.Clear();
                _plan.Stages.AddRange(loaded.Stages);

                _vesselNameText = string.IsNullOrEmpty(_plan.VesselName) ? _plan.Name : _plan.VesselName;
                _payloadText = _plan.PayloadMassTons.ToString("0.###", CultureInfo.InvariantCulture);
                _activeStageIndex = -1;
                _loadVisible = false;
                _clearArmed = false;

                // A finalised plan is a build list, so the planner has nothing left to do
                // with it; an unfinished one is loaded with the planner still available.
                if (_plan.Finalized) _planner.Visible = false;
                _status = "Loaded " + Path.GetFileName(path) + (_plan.Finalized ? " (finalised)." : ".");
            }
            catch (Exception ex)
            {
                _status = "Unable to load plan: " + ex.Message;
                Debug.LogWarning("[VesselPlanner] Unable to load plan: " + ex.Message);
            }
        }

        // "All profiles" when nothing is picked, since an empty selection filters nothing.
        private void PresetNewStageBulkheadProfiles()
        {
            string profile;
            if (_plan.Stages.Count == 0)
            {
                if (EditorStageScanner.TryGetVesselBottomOrTopBulkheadProfile(out profile))
                    CommonRoutines.AddUniqueIgnoreCase(_newBulkheadProfiles, profile);
                return;
            }

            PlannedStage previousStage = _plan.Stages[_plan.Stages.Count - 1];
            if (TryGetPreviousStageEngineProfile(previousStage, out profile))
                CommonRoutines.AddUniqueIgnoreCase(_newBulkheadProfiles, profile);
        }

        private static bool TryGetPreviousStageEngineProfile(PlannedStage stage, out string profile)
        {
            profile = null;
            if (stage == null) return false;

            PlannedPart engine = stage.Parts.FirstOrDefault(p => p != null && p.IsEngine && !string.IsNullOrEmpty(p.PartName));
            if (engine == null) return false;

            string partName = BasePartName(engine.PartName);
            try
            {
                AvailablePart available = PartLoader.LoadedPartsList == null
                    ? null
                    : PartLoader.LoadedPartsList.FirstOrDefault(p => p != null && string.Equals(p.name, partName, StringComparison.OrdinalIgnoreCase));
                Part prefab = available == null ? null : available.partPrefab;
                if (prefab == null) return false;

                AttachNode node = prefab.FindAttachNode("bottom") ?? prefab.FindAttachNode("top");
                if (node == null || node.size < 0) return false;
                profile = ResolvePartNodeBulkheadProfile(available, node);
                return !string.IsNullOrEmpty(profile);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VesselPlanner] Unable to preset bulkhead profile from previous engine: " + ex.Message);
                return false;
            }
        }

        private static string ResolvePartNodeBulkheadProfile(AvailablePart available, AttachNode node)
        {
            if (node == null || node.size < 0) return null;
            string fallback = BulkheadProfiles.ProfileForSize(node.size);
            string profiles = available == null ? null : available.bulkheadProfiles;
            if (string.IsNullOrEmpty(profiles)) return fallback;

            List<string> stackProfiles = profiles.Split(',')
                .Select(value => value.Trim())
                .Where(value => value.StartsWith("size", StringComparison.OrdinalIgnoreCase))
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (stackProfiles.Count == 1) return stackProfiles[0];
            if (!string.IsNullOrEmpty(fallback) && stackProfiles.Any(value => string.Equals(value, fallback, StringComparison.OrdinalIgnoreCase)))
                return fallback;
            return stackProfiles.Count > 0 ? stackProfiles[0] : fallback;
        }

        private double GetPayloadForStageIndex(int stageIndex)
        {
            double payload;
            if (!CommonRoutines.TryParseDouble(_payloadText, out payload)) payload = _plan.PayloadMassTons;
            payload = Math.Max(0.0, payload);
            int count = Math.Max(0, Math.Min(stageIndex, _plan.Stages.Count));
            for (int i = 0; i < count; i++)
                payload += GetPlannedStageWetMassTons(_plan.Stages[i]);
            return payload;
        }

        private static double GetPlannedStageWetMassTons(PlannedStage stage)
        {
            if (stage == null) return 0.0;

            if (stage.Kind == PlannedStageKind.Subassemblies)
                return stage.SubassemblyStageMassTons;

            double mass = Math.Max(0.0, stage.CargoMassTons)
                + Math.Max(0.0, stage.AssemblyMassTons)
                + stage.SubassemblyStageMassTons
                + (stage.AddDecouplerMass ? Math.Max(0.0, stage.DecouplerMassTons) : 0.0);

            foreach (PlannedPart part in stage.Parts)
            {
                if (part == null || part.Quantity <= 0 || string.IsNullOrEmpty(part.PartName)) continue;
                mass += GetFullWetPartMassTons(part.PartName) * part.Quantity;
            }
            return Math.Max(0.0, mass);
        }

        private static double GetFullWetPartMassTons(string plannedPartName)
        {
            string partName = BasePartName(plannedPartName);
            if (string.IsNullOrEmpty(partName) || PartLoader.LoadedPartsList == null) return 0.0;

            try
            {
                AvailablePart available = PartLoader.LoadedPartsList.FirstOrDefault(p => p != null && string.Equals(p.name, partName, StringComparison.OrdinalIgnoreCase));
                Part prefab = available == null ? null : available.partPrefab;
                if (prefab == null) return 0.0;

                double mass = Math.Max(0.0, prefab.mass);
                if (prefab.Resources != null)
                {
                    foreach (PartResource resource in prefab.Resources)
                    {
                        if (resource == null || resource.maxAmount <= 0.0) continue;
                        PartResourceDefinition def = resource.info ?? PartResourceLibrary.Instance.GetDefinition(resource.resourceName);
                        if (def != null && def.density > 0.0)
                            mass += resource.maxAmount * def.density;
                    }
                }
                return Math.Max(0.0, mass);
            }
            catch
            {
                return 0.0;
            }
        }

        private static string BasePartName(string plannedPartName)
        {
            if (string.IsNullOrEmpty(plannedPartName)) return plannedPartName;
            int modeSeparator = plannedPartName.LastIndexOf(':');
            if (modeSeparator > 0)
            {
                int ignored;
                if (int.TryParse(plannedPartName.Substring(modeSeparator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out ignored))
                    return plannedPartName.Substring(0, modeSeparator);
            }
            return plannedPartName;
        }

        internal static string BulkheadSelectionSummary(IList<string> profiles)
        {
            if (profiles == null || profiles.Count == 0) return "All profiles";

            var names = new List<string>();
            foreach (BulkheadProfile profile in BulkheadProfiles.All)
                if (ContainsProfile(profiles, profile.Profile)) names.Add(string.IsNullOrEmpty(profile.Name) ? profile.Profile : profile.Name);

            // Preserve unknown/mod-defined tokens in the summary even if they are not in
            // VesselPlanner's description config.
            foreach (string selected in profiles)
            {
                bool known = false;
                foreach (BulkheadProfile profile in BulkheadProfiles.All)
                {
                    if (!string.Equals(profile.Profile, selected, StringComparison.OrdinalIgnoreCase)) continue;
                    known = true;
                    break;
                }
                if (!known && !string.IsNullOrEmpty(selected)) names.Add(selected);
            }

            if (names.Count == 0) return "All profiles";
            return string.Join(", ", names.ToArray());
        }

        internal string ActiveBulkheadSummary
        {
            get { return BulkheadSelectionSummary(ActiveBulkheadProfiles); }
        }

        private static bool ContainsProfile(IList<string> profiles, string profile)
        {
            if (profiles == null || string.IsNullOrEmpty(profile)) return false;
            foreach (string value in profiles)
                if (string.Equals(value, profile, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void RemoveProfile(List<string> profiles, string profile)
        {
            if (profiles == null || string.IsNullOrEmpty(profile)) return;
            for (int i = profiles.Count - 1; i >= 0; i--)
                if (string.Equals(profiles[i], profile, StringComparison.OrdinalIgnoreCase)) profiles.RemoveAt(i);
        }

        private void ResetStageEntryFocus()
        {
            _stageEntryFocusIndex = 0;
            _pendingStageFocusControl = StageTargetDvControl;
            _lastStageTabFrame = -1;
        }

        private void CaptureStageTabNavigation()
        {
            Event current = Event.current;
            if (current == null) return;

            bool keyDown = current.type == EventType.KeyDown || current.rawType == EventType.KeyDown;
            bool eventTab = keyDown && (current.keyCode == KeyCode.Tab || current.character == '\t');
            if (!eventTab) return;

            bool backwards = current.shift
                || Input.GetKey(KeyCode.LeftShift)
                || Input.GetKey(KeyCode.RightShift);

            QueueStageTabNavigation(backwards, true);
            if (current.type != EventType.Used) current.Use();
        }

        private void QueueStageTabNavigation(bool backwards, bool readGuiFocus)
        {
            // Update and OnGUI can both observe the same physical Tab press. Process it once
            // per Unity frame so one key press advances exactly one field.
            if (_lastStageTabFrame == Time.frameCount) return;
            _lastStageTabFrame = Time.frameCount;

            if (readGuiFocus)
            {
                string focused = GUI.GetNameOfFocusedControl();
                int focusedIndex = StageEntryControlIndex(focused);
                if (focusedIndex >= 0) _stageEntryFocusIndex = focusedIndex;
            }

            if (backwards)
                _stageEntryFocusIndex = _stageEntryFocusIndex < 0
                    ? StageEntryControls.Length - 1
                    : (_stageEntryFocusIndex + StageEntryControls.Length - 1) % StageEntryControls.Length;
            else
                _stageEntryFocusIndex = _stageEntryFocusIndex < 0
                    ? 0
                    : (_stageEntryFocusIndex + 1) % StageEntryControls.Length;

            _pendingStageFocusControl = StageEntryControls[_stageEntryFocusIndex];
        }

        private void ApplyStageEntryFocus()
        {
            if (!string.IsNullOrEmpty(_pendingStageFocusControl))
                GUI.FocusControl(_pendingStageFocusControl);

            string focused = GUI.GetNameOfFocusedControl();
            int focusedIndex = StageEntryControlIndex(focused);
            if (focusedIndex < 0) return;

            _stageEntryFocusIndex = focusedIndex;
            if (string.Equals(focused, _pendingStageFocusControl, StringComparison.Ordinal))
                _pendingStageFocusControl = null;
        }

        private static int StageEntryControlIndex(string controlName)
        {
            if (string.IsNullOrEmpty(controlName)) return -1;
            for (int i = 0; i < StageEntryControls.Length; i++)
                if (string.Equals(StageEntryControls[i], controlName, StringComparison.Ordinal)) return i;
            return -1;
        }

        private static void DialogField(string label, ref string value, string controlName, float labelWidth = 160f)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(labelWidth));
                GUI.SetNextControlName(controlName);
                value = GUILayout.TextField(value, GUILayout.Width(90));
            }
        }

        private static string CurrentVesselName()
        {
            try
            {
                if (EditorLogic.fetch != null && EditorLogic.fetch.ship != null && !string.IsNullOrEmpty(EditorLogic.fetch.ship.shipName))
                    return EditorLogic.fetch.ship.shipName;
            }
            catch
            {
                // Fall through to the placeholder below.
            }
            return "(unnamed craft)";
        }

        private static string PlansDirectory
        {
            get
            {
                return Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "VesselPlanner", "PluginData"), "Plans");
            }
        }


        private static string LegacyPlansDirectory
        {
            get
            {
                return Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "EngineStagePlanner", "PluginData"), "Plans");
            }
        }

        private float StageListHeight()
        {
            float screenHeight = Screen.height > 0 ? Screen.height : 1080f;
            return Mathf.Clamp(screenHeight * 0.3f, 180f, 420f);
        }
    }
}
