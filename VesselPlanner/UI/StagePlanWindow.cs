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
        private readonly PlannerWindow _planner;
        private readonly StagePlan _plan = new StagePlan();

        private Rect _window = new Rect(140, 100, 520, 560);
        private Rect _newStageWindow = new Rect(260, 180, 340, 260);
        private Rect _loadWindow = new Rect(260, 180, 360, 380);
        private Vector2 _stageScroll;
        private Vector2 _loadScroll;

        private bool _newStageVisible;
        private bool _loadVisible;
        private string _payloadText = "5.0";
        private string _vesselNameText = "";
        private string _status = "";
        private bool _bringPlanToFrontRequested;

        // New-stage dialog inputs.
        private string _newTargetDv = "";
        private string _newMinTwr = "";
        private string _newMaxEngines = "";
        private string _newCargoMass = "0";
        private bool _newAddDecouplerMass;

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
        private Vector2 _bulkheadScroll;

        // Index of the stage the planner is currently feeding, or -1 when nothing is being
        // built. Parts added in the planner go to this stage.
        private int _activeStageIndex = -1;

        // Which stage the dialog is editing, or -1 when it is creating one.
        private int _editingStageIndex = -1;
        private int _pendingEditIndex = -1;

        private readonly List<string> _planFiles = new List<string>();

        // Deleting a stage or part, loading a plan and starting a new stage all change how
        // many controls these windows draw. Applying them inside the window function would
        // change that between a frame's layout and repaint passes, so they are recorded here
        // and carried out once both windows have been drawn.
        private int _pendingStageRemoval = -1;
        private int _pendingPartStage = -1;
        private int _pendingPartIndex = -1;
        private string _pendingLoadPath;
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
        private readonly List<string> _pendingBulkheadProfiles = new List<string>();
        private DeltaVBasis _pendingBasis = DeltaVBasis.Vacuum;

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
            ClampWindow(ref _window);
        }

        private void PositionNewStageNextToPlan()
        {
            // Open the modal directly beside the plan so the two windows read as one
            // stage-building workspace: tops aligned, dialog left edge touching the
            // plan's right edge. The dialog remains draggable after it opens.
            _newStageWindow.width = 400f;
            _newStageWindow.x = _window.xMax;
            _newStageWindow.y = _window.y;
            ClampWindow(ref _newStageWindow);
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
            if (!_newStageVisible || !Input.GetKeyDown(KeyCode.Tab)) return;

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
        public bool CapturePart(string partName, string displayName, int quantity, bool isEngine)
        {
            if (!IsCapturing) return false;

            _plan.Stages[_activeStageIndex].AddPart(partName, displayName, quantity, isEngine);
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

        // Adds the engine and the tank in one go, for the Add Engine & Tanks button.
        internal bool CaptureEngineAndTanks(string enginePart, string engineName, int engineCount, string tankPart, string tankName, int tankCount)
        {
            if (!IsCapturing) return false;

            PlannedStage stage = _plan.Stages[_activeStageIndex];
            stage.AddPart(enginePart, engineName, engineCount, true);
            if (!string.IsNullOrEmpty(tankPart)) stage.AddPart(tankPart, tankName, tankCount, false);
            _status = "Added engine" + (string.IsNullOrEmpty(tankPart) ? "" : " and tank") + " to stage " + (_activeStageIndex + 1) + ".";

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

            _planner.DrawSolidBackground(_window, true);
            _window = ClickThruBlocker.GUILayoutWindow(19041970, _window, DrawPlanWindow, "Stage-By-Stage Plan",
                GUILayout.MinWidth(480f), GUILayout.MinHeight(420f));

            if (_newStageVisible)
            {
                string stageDialogTitle = _editingStageIndex >= 0 ? "Edit Stage " + (_editingStageIndex + 1) : "New Stage";

                // This dialog must be modal: while it is open it stays above every other
                // VesselPlanner/KSP IMGUI window and is the sole receiver of GUI input.
                // ClickThroughBlocker's modal wrapper keeps the normal editor click-through
                // protection as well.  The height grows only while the bulkhead list is open.
                _newStageWindow.width = 400f;
                _newStageWindow.height = _bulkheadDropdownOpen ? 500f : 365f;
                _planner.DrawSolidBackground(_newStageWindow, true);
                _newStageWindow = ClickThruBlocker.GUIModalWindow(19041971, _newStageWindow, DrawNewStageWindow,
                    stageDialogTitle, GUI.skin.window);
            }

            if (_loadVisible)
            {
                _planner.DrawSolidBackground(_loadWindow);
                _loadWindow = ClickThruBlocker.GUILayoutWindow(19041972, _loadWindow, DrawLoadWindow, "Load Plan",
                    GUILayout.Width(360f), GUILayout.Height(380f));
            }

            if (_bringPlanToFrontRequested && !_newStageVisible && !_loadVisible)
            {
                GUI.BringWindowToFront(19041970);
                _bringPlanToFrontRequested = false;
            }

            ApplyPendingActions();

            ClampWindow(ref _window);
            ClampWindow(ref _newStageWindow);
            ClampWindow(ref _loadWindow);
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

            if (_pendingStageStart)
            {
                _pendingStageStart = false;
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
            // Repaint the complete stock window over an opaque local background. Doing this
            // inside the window callback guarantees the solid fill is in the same GUI/window
            // layer as the Stage-By-Stage window itself, so an overlapping planner window
            // cannot bleed through it.
            _planner.DrawSolidWindowOverlay(new Rect(0f, 0f, _window.width, _window.height), "Stage-By-Stage Plan", true);

            // Seeded from the craft in the editor the first time the window is opened, then
            // owned by the field so a plan can be named independently of the craft.
            if (string.IsNullOrEmpty(_vesselNameText)) _vesselNameText = CurrentVesselName();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Vessel", GUILayout.Width(70));
                _vesselNameText = GUILayout.TextField(_vesselNameText, GUILayout.Width(220));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("×", GUILayout.Width(30))) Visible = false;
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
                GUILayout.Label("No stages yet. Use New Stage to size one in the planner window.");
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
                // The plan is filed under the vessel name, so there is no second name to keep
                // in step with it.
                GUILayout.Label("Saved as " + SanitiseFileName(_vesselNameText) + ".cfg");
                if (GUILayout.Button("Save", GUILayout.Width(70), GUILayout.Height(26))) SavePlan();
                if (GUILayout.Button("Load", GUILayout.Width(70), GUILayout.Height(26))) OpenLoadDialog();
                // Two presses: the first arms the button, so a stray click cannot discard a plan.
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
            GUI.DragWindow(new Rect(0f, 0f, _window.width, _window.height));
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

            if (stage.Parts.Count == 0)
            {
                GUILayout.Label("  no parts selected yet");
            }
            else
            {
                for (int p = 0; p < stage.Parts.Count; p++)
                {
                    PlannedPart part = stage.Parts[p];
                    using (new GUILayout.HorizontalScope())
                    {
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
                            _pendingPartStage = index;
                            _pendingPartIndex = p;
                        }
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private void OpenNewStageDialog()
        {
            _editingStageIndex = -1;
            _newTargetDv = "";
            _newMinTwr = "";
            _newMaxEngines = "";
            _newCargoMass = "0";
            _newAddDecouplerMass = false;
            _newBasis = DeltaVBasis.Vacuum;
            _newBulkheadProfiles.Clear();
            PresetNewStageBulkheadProfiles();
            _bulkheadDropdownOpen = false;
            ResetStageEntryFocus();
            PositionNewStageNextToPlan();
            _newStageVisible = true;
        }

        // Reopens the dialog on an existing stage. Recalculating keeps the parts already
        // chosen for it; only the requirements are replaced.
        private void OpenEditStageDialog(int index)
        {
            if (index < 0 || index >= _plan.Stages.Count) return;

            PlannedStage stage = _plan.Stages[index];
            _editingStageIndex = index;
            _newTargetDv = stage.TargetDeltaV.ToString("0.###", CultureInfo.InvariantCulture);
            _newMinTwr = stage.MinimumTwr.ToString("0.###", CultureInfo.InvariantCulture);
            _newMaxEngines = stage.MaxEngineCount.ToString(CultureInfo.InvariantCulture);
            _newCargoMass = stage.CargoMassTons.ToString("0.###", CultureInfo.InvariantCulture);
            _newAddDecouplerMass = stage.AddDecouplerMass;
            _newBasis = stage.TargetDeltaVBasis;
            _newBulkheadProfiles.Clear();
            _newBulkheadProfiles.AddRange(stage.BulkheadProfiles);
            _bulkheadDropdownOpen = false;
            ResetStageEntryFocus();
            _newStageVisible = true;
        }

        private void DrawNewStageWindow(int id)
        {
            string title = _editingStageIndex >= 0 ? "Edit Stage " + (_editingStageIndex + 1) : "New Stage";
            _planner.DrawSolidWindowOverlay(new Rect(0f, 0f, _newStageWindow.width, _newStageWindow.height), title, true);

            // KSP/Unity can deliver Tab either as KeyCode.Tab or only as a '\t' character
            // through a modal IMGUI window. Capture it before any TextField can consume the
            // event, then apply the requested focus after the named fields have been drawn.
            CaptureStageTabNavigation();

            GUILayout.Label("Requirements for the next stage.");
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

            int dialogStageIndex = _editingStageIndex >= 0 ? _editingStageIndex : _plan.Stages.Count;
            if (dialogStageIndex > 0)
            {
                double previousStagesMass = GetPayloadForStageIndex(dialogStageIndex);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Previous stages mass", GUILayout.Width(160));
                    GUILayout.Label(previousStagesMass.ToString("0.###", CultureInfo.InvariantCulture) + " t");
                }
            }

            double displayedDecouplerMass = DecouplerMasses.GetDecouplerMassTons(_newBulkheadProfiles);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Decoupler mass", GUILayout.Width(160));
                _newAddDecouplerMass = GUILayout.Toggle(_newAddDecouplerMass, "Add", GUILayout.Width(75));
                GUILayout.Label(_newAddDecouplerMass
                    ? displayedDecouplerMass.ToString("0.###", CultureInfo.InvariantCulture) + " t"
                    : "not included");
            }

            ApplyStageEntryFocus();

            // Stage-by-stage plans are built before the craft exists, so there is no stage
            // bulkhead to match against. The profiles chosen here filter the candidate engines and tanks
            // in place of that check; leaving them all off considers every profile.
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Bulkhead profiles", GUILayout.Width(160));
                if (GUILayout.Button(BulkheadSelectionSummary(_newBulkheadProfiles) + (_bulkheadDropdownOpen ? "  \u25b2" : "  \u25bc"), GUILayout.Width(180)))
                    _bulkheadDropdownOpen = !_bulkheadDropdownOpen;
            }

            if (_bulkheadDropdownOpen)
            {
                // Several profiles can apply to one stage, so the list stays open and each row
                // toggles rather than closing on the first pick.
                _bulkheadScroll = GUILayout.BeginScrollView(_bulkheadScroll, GUILayout.Height(110f));
                foreach (BulkheadProfile profile in BulkheadProfiles.All)
                {
                    bool selected = ContainsProfile(_newBulkheadProfiles, profile.Profile);
                    if (GUILayout.Toggle(selected, profile.Label) != selected)
                    {
                        if (selected) RemoveProfile(_newBulkheadProfiles, profile.Profile);
                        else AddProfile(_newBulkheadProfiles, profile.Profile);
                    }
                }
                GUILayout.EndScrollView();
                if (GUILayout.Button("Clear selection", GUILayout.Width(140))) _newBulkheadProfiles.Clear();
            }

            // Each value is parsed in its own statement rather than in one && chain: short
            // circuiting would leave the later out parameters unassigned as far as the
            // compiler is concerned, even though the button only reads them when all four
            // parsed. Cargo may be zero, but it must still be numeric.
            double targetDv, minTwr, cargoMass;
            int maxEngines;
            bool targetOk = TryParseDouble(_newTargetDv, out targetDv) && targetDv > 0.0;
            bool twrOk = TryParseDouble(_newMinTwr, out minTwr) && minTwr > 0.0;
            bool enginesOk = int.TryParse(_newMaxEngines, out maxEngines) && maxEngines > 0;
            bool cargoOk = TryParseDouble(_newCargoMass, out cargoMass) && cargoMass >= 0.0;
            bool complete = targetOk && twrOk && enginesOk && cargoOk;

            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                // Calculate stays disabled until all four values are present and usable, so a
                // half-filled dialog cannot start a stage.
                GUI.enabled = complete;
                if (GUILayout.Button("Calculate", GUILayout.Width(100), GUILayout.Height(26)))
                {
                    _pendingTargetDv = targetDv;
                    _pendingMinTwr = minTwr;
                    _pendingMaxEngines = maxEngines;
                    _pendingCargoMass = cargoMass;
                    _pendingAddDecouplerMass = _newAddDecouplerMass;
                    _pendingBulkheadProfiles.Clear();
                    _pendingBulkheadProfiles.AddRange(_newBulkheadProfiles);
                    _pendingBasis = _newBasis;
                    _pendingEditIndex = _editingStageIndex;
                    _pendingStageStart = true;
                    _newStageVisible = false;
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(26)))
                {
                    _newStageVisible = false;
                    _editingStageIndex = -1;
                }
            }

            if (!complete) GUILayout.Label("Enter valid target Δv, minimum TWR, max engines, and an additional cargo mass of zero or more.");
            GUI.DragWindow(new Rect(0f, 0f, _newStageWindow.width, _newStageWindow.height));
        }

        private void StartStage(double targetDv, double minTwr, int maxEngines, double cargoMass)
        {
            PlannedStage stage;
            if (_pendingEditIndex >= 0 && _pendingEditIndex < _plan.Stages.Count)
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

            stage.TargetDeltaV = targetDv;
            stage.TargetDeltaVBasis = _pendingBasis;
            stage.MinimumTwr = minTwr;
            stage.MaxEngineCount = maxEngines;
            stage.CargoMassTons = Math.Max(0.0, cargoMass);
            stage.BulkheadProfiles.Clear();
            stage.BulkheadProfiles.AddRange(_pendingBulkheadProfiles);
            stage.AddDecouplerMass = _pendingAddDecouplerMass;
            stage.DecouplerMassTons = stage.AddDecouplerMass
                ? DecouplerMasses.GetDecouplerMassTons(stage.BulkheadProfiles)
                : 0.0;
            int stageIndex = _activeStageIndex;
            _pendingEditIndex = -1;
            _newStageVisible = false;

            double basePayload;
            if (!TryParseDouble(_payloadText, out basePayload)) basePayload = 0.0;
            _plan.PayloadMassTons = basePayload;

            // Stage 1 lifts the plan's original payload. Every later stage lifts that
            // payload plus all previously planned stages, using the full wet mass of the
            // selected engine/tank parts already recorded in the plan.
            double payload = GetPayloadForStageIndex(stageIndex);

            _planner.BeginPlannedStage(targetDv, stage.TargetDeltaVBasis, minTwr, maxEngines, payload, stage.CargoMassTons, stage.DecouplerMassTons);
            _planner.RequestBringToFront();
            _status = "Stage " + _plan.Stages.Count + " sent to the planner. Add an engine and tanks there.";
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
            _planner.DrawSolidWindowOverlay(new Rect(0f, 0f, _loadWindow.width, _loadWindow.height), "Load Plan");

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
                if (TryParseDouble(_payloadText, out payload)) _plan.PayloadMassTons = payload;
                _plan.Name = SanitiseFileName(_vesselNameText);
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
                    AddProfile(_newBulkheadProfiles, profile);
                return;
            }

            PlannedStage previousStage = _plan.Stages[_plan.Stages.Count - 1];
            if (TryGetPreviousStageEngineProfile(previousStage, out profile))
                AddProfile(_newBulkheadProfiles, profile);
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
            if (!TryParseDouble(_payloadText, out payload)) payload = _plan.PayloadMassTons;
            payload = Math.Max(0.0, payload);
            int count = Math.Max(0, Math.Min(stageIndex, _plan.Stages.Count));
            for (int i = 0; i < count; i++)
                payload += GetPlannedStageWetMassTons(_plan.Stages[i]);
            return payload;
        }

        private static double GetPlannedStageWetMassTons(PlannedStage stage)
        {
            if (stage == null) return 0.0;

            double mass = Math.Max(0.0, stage.CargoMassTons)
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

        private static void AddProfile(List<string> profiles, string profile)
        {
            if (profiles == null || string.IsNullOrEmpty(profile) || ContainsProfile(profiles, profile)) return;
            profiles.Add(profile);
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

        private static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string SanitiseFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Plan";
            char[] invalid = Path.GetInvalidFileNameChars();
            var builder = new System.Text.StringBuilder();
            foreach (char c in name.Trim())
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            string result = builder.ToString();
            return result.Length == 0 ? "Plan" : result;
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

        private static void ClampWindow(ref Rect rect)
        {
            rect.x = Mathf.Clamp(rect.x, -rect.width + 40f, Screen.width - 40f);
            rect.y = Mathf.Clamp(rect.y, 0f, Screen.height - 30f);
        }
    }
}
