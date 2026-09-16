using ClickThroughFix;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using VesselPlanner.Core;
using VesselPlanner.KSP;

namespace VesselPlanner.UI
{
    public sealed class PlannerWindow
    {
        private Rect _window = new Rect(100, 70, 1150, 800);
        private Rect _settingsWindow = new Rect(180, 120, SettingsWindowWidth, SettingsWindowBaseHeight);
        private bool _settingsVisible;
        private bool _bringMainToFrontRequested;
        private int _settingsTab;
        private Vector2 _settingsScroll;
        private readonly PlannerUiSettings _uiSettings = new PlannerUiSettings();
        private StagePlanWindow _stagePlan;
        private readonly MissionPlannerPage _missionPlanner = new MissionPlannerPage();
        private readonly DeltaVTablePage _deltaVTablePage = new DeltaVTablePage();
        private Vector2 _resultsScroll;
        private Vector2 _detailScroll;
        private Vector2 _tankScroll;
        private Vector2 _planningRequirementsScroll;
        private Vector2 _analysisStageScroll;
        private Vector2 _currentEnginesScroll;
        private bool _planningMode = true;
        private bool _stageByStageMode;
        private bool _missionPlannerMode;
        private bool _deltaVTableMode;
        private bool _simulateOnAnalyzeEntry;
        private int _stage;
        private string _stageText = "0";
        private string _targetDv = "2500";
        private string _minTwr = "1.25";
        private string _payload = "5.0";
        // Stage-By-Stage-only dry cargo mass belonging to the stage currently being solved.
        private double _plannedStageCargoMassTons;
        private double _plannedStageDecouplerMassTons;
        private double _plannedStageAssemblyMassTons;
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
        private const string PickStageInputLockId = "VesselPlanner_PickStageFromPart";
        private const int PlanningBodyComboId = 41007;
        private const int AnalysisBodyComboId = 41008;
        private const int RotatingImageBackgroundComboId = 41010;
        private const float SettingsWindowWidth = 640f;
        private const float SettingsWindowBaseHeight = 720f;
        private const float SettingsWindowButtonExtraHeight = 28f;
        private const int KspSkinExtraSettingsLines = 6;
        private static readonly FieldInfo StageGroupDragHandlerField = typeof(StageGroup).GetField("dragHandler", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private string _engineNameFilter = "";
        private string _engineExcludeFilter = "";
        private string _tankNameFilter = "";
        private string _tankExcludeFilter = "";
        private string _iconZoomFactorText = "0.8";
        private string _rotatingImageZoomFactorText = "1.0";
        private string _cameraYawDegreesText = "45";
        private string _cameraPitchDegreesText = "20";
        private string _rotatingPreviewSizeText = "100";
        private string _rotatingPreviewDegreesPerFrameText = "60";
        private static readonly string[] RotatingImageBackgroundNames = { "Transparent", "Black", "Dark Gray", "Gray", "White" };
        private readonly List<CelestialBody> _bodies = new List<CelestialBody>();
        private int _selectedBodyIndex;
        private double _altitudeMeters;
        private Rect _stageByStageButtonWindowRect;
        private const float PlanetButtonWidth = 180f;
        private const float MinWindowWidth = 1150f;
        private const float MaxWindowWidth = 1850f;
        private const float MinWindowHeight = 620f;
        private const float MinEngineListHeight = 150f;
        private const float MaxEngineListHeight = 300f;

        // Selected Engine / Tanks pane sizing.  The pane widths are draggable and are
        // stored as a fraction of the available detail width so the layout keeps its
        // proportions when the planner window itself is resized.  The detail list
        // heights are fixed, so neither pane carries a bottom grip.
        private PaneSplitter _activeSplitter = PaneSplitter.None;
        private PaneSplitter _pendingSplitterTarget = PaneSplitter.None;
        private Vector2 _pendingSplitterDelta;
        private bool _resizingMainWindowWidth;
        private float _pendingMainWindowWidthDelta;
        private const float MainWindowWidthGripWidth = 10f;
        private const float MainWindowWidthGripHeight = 72f;
        private static readonly int MainWindowWidthGripHint = "VesselPlannerMainWindowWidthGrip".GetHashCode();
        static private GUIStyle _gripStyle;
        static private GUIStyle _darkBox;
        static private Texture2D _darkBoxTexture;
        static private int _styleSkinRevision = -1;
        private const float SplitterGripThickness = 8f;
        private const float MinDetailPaneWidth = 220f;
        private const float MinDetailPaneFraction = 0.15f;
        private const float MaxDetailPaneFraction = 0.9f;
        private const float SelectedEngineScrollHeightPx = 130f;
        private const float TankListScrollHeightPx = 145f;
        private const float MinMatchedScrollHeight = 60f;
        private const float MaxMatchedScrollHeight = 900f;
        private const float MinPlanningRequirementsHeight = 140f;
        private const float MinDetailListHeight = 100f;
        private const float MaxDetailListHeight = 900f;

        // In Planning the two detail panes sit side by side and are made the same height.
        // The panes have different chrome (the Tanks pane adds a column header row and a
        // footer line that may wrap at narrow widths), so the offset cannot be a constant.
        // Both pane heights are measured on Repaint and the Selected Engine scroll height
        // is corrected by the difference, which converges in a single frame and re-converges
        // whenever the footer rewraps as the width grip is dragged.
        private float _planningEngineScrollHeight = TankListScrollHeightPx;
        private float _measuredPlanningEnginePaneHeight;
        private float _measuredPlanningTankPaneHeight;
        private float _planningPaneRowHeight;

        // Analyze Existing lines up the bottom of the Selected Engine pane with the bottom
        // of the Existing stage box.  That box's height depends on which analysis lines and
        // sections the user has enabled in Settings, on whether the stock stage masses and
        // Current Engines rows are present, and on how many engines and stage resources the
        // selected stage has, so the two bottom edges are measured on Repaint rather than
        // assuming any fixed content.  Both measurements are taken from the "box" groups
        // themselves: bottom edges are compared rather than heights because a wrapping
        // layout group can be stretched to the height of the row it sits in, and its rect
        // can also extend past its last child by that child's margin.
        private float _analysisEngineScrollHeight = SelectedEngineScrollHeightPx;
        private float _measuredAnalysisLeftColumnBottom;
        private float _measuredAnalysisPaneBottom;

        // Planning lines the bottom of the engine list box up with the bottom of the
        // Requirements box beside it.  The Requirements box is content-sized and varies
        // with the status line and the Δv/TWR help text wrapping, so the engine list
        // height is corrected from the measured bottom edges rather than a fixed offset.
        private float _planningResultsHeightOffset;
        private float _measuredPlanningRequirementsBottom;
        private float _measuredPlanningResultsBottom;
        private float _measuredPlanningRequirementsHeight;
        private float _measuredPlanningResultsHeight;
        private float _planningRequirementsViewport;

        private enum PaneSplitter
        {
            None,
            PlanningEngineTankWidth,
            PlanningListHeight,
            AnalysisListHeight
        }
        private OptimizationMode _mode = OptimizationMode.LowestStageMass;
        private List<EngineCandidate> _engines = new List<EngineCandidate>();
        private List<TankCandidate> _tanks = new List<TankCandidate>();
        private List<TankSuggestion> _tankSuggestions = new List<TankSuggestion>();
        private TankSuggestion _selectedTank;
        private List<StageSolution> _solutions = new List<StageSolution>();
        private StageSolution _selected;
        private ExistingStageSnapshot _snapshot;
        private string _status = "";
        private Texture2D _hoverPartPreviewTexture;
        private Rect _hoverPartPreviewSourceScreenRect;
        private const float HoverPartPreviewSize = 160f;
        static private GUIStyle _right;
        private SolutionSortColumn _solutionSortColumn = SolutionSortColumn.None;
        private bool _solutionSortAscending = true;
        private TankSortColumn _tankSortColumn = TankSortColumn.None;
        private bool _tankSortAscending = true;

        private enum SolutionSortColumn
        {
            None, Engine, Count, EngineMass, StageWetMass, StartMass, AtmosphericDeltaV, VacuumDeltaV, SeaLevelThrust, VacuumThrust, CostEfficiency, Twr, MaxTwr, Isp, Burn, Fuel, Bulkhead
        }

        private enum TankSortColumn
        {
            None, Tank, Count, DryMass, Excess, Bulkhead, Capacity
        }

        private enum SolutionColumn
        {
            Engine,
            Count,
            EngineMass,
            StageWetMass,
            StartMass,
            AtmosphericDeltaV,
            VacuumDeltaV,
            SeaLevelThrust,
            VacuumThrust,
            CostEfficiency,
            Twr,
            MaxTwr,
            Isp,
            Burn,
            Fuel,
            Bulkhead,
            Add
        }

        private enum AnalysisLine
        {
            CraftWetMass,
            CraftDryMass,
            PayloadAboveStage,
            StagePropellantCurrent,
            StagePropellantFull,
            KspStageWetMass,
            KspStageDryMass,
            KspStageFuelMass,
            KspCurrentEngineMass,
            KspVehicleStartMass,
            KspVehicleEndMass,
            KspCurrentBurn,
            StageTankCapacity,
            KnownTankVolume,
            CurrentEngines,
            StageResources
        }

        private sealed class PlannerUiSettings
        {
            private readonly Dictionary<SolutionColumn, bool> _columns = new Dictionary<SolutionColumn, bool>();
            private readonly Dictionary<AnalysisLine, bool> _analysisLines = new Dictionary<AnalysisLine, bool>();
            private bool _closeAnalyzeExistingAfterAdd;
            private bool _closePlanningAfterAdd;
            private bool _solidEditorWindowBackgrounds = true;
            private bool _useAltSkin;
            private bool _showTooltips = true;
            private DeltaVBasis _targetDeltaVBasis = DeltaVBasis.Vacuum;
            private string _csvExportDirectory = "VesselPlanner/PluginData/CSV";
            private string _pngExportDirectory = "Screenshots";
            private float _planningEnginePaneFraction = 0.35f;
            private float _analysisListHeightOffset;
            private float _planningListHeightOffset;
            private float _mainWindowWidth = 1150f;
            private bool _saveEngineFilter = true;
            private bool _saveEngineExcludeFilter = true;
            private bool _saveTankFilter = true;
            private bool _saveTankExcludeFilter = true;
            private string _engineFilter = "";
            private string _engineExcludeFilter = "";
            private string _tankFilter = "";
            private string _tankExcludeFilter = "";
            private float _iconZoomFactor = 0.8f;
            private float _rotatingImageZoomFactor = 1.0f;
            private float _cameraYawDegrees = 45f;
            private float _cameraPitchDegrees = 20f;
            private int _rotatingImageBackground = 0;
            private int _rotatingPreviewSize = 100;
            private int _rotatingPreviewDegreesPerFrame = 60;

            public PlannerUiSettings()
            {
                foreach (SolutionColumn column in Enum.GetValues(typeof(SolutionColumn)))
                    _columns[column] = true;
                foreach (AnalysisLine line in Enum.GetValues(typeof(AnalysisLine)))
                    _analysisLines[line] = true;
            }

            public bool ColumnVisible(SolutionColumn column)
            {
                bool value;
                return !_columns.TryGetValue(column, out value) || value;
            }

            public void SetColumnVisible(SolutionColumn column, bool visible)
            {
                _columns[column] = visible;
            }

            public bool AnalysisLineVisible(AnalysisLine line)
            {
                bool value;
                return !_analysisLines.TryGetValue(line, out value) || value;
            }

            public void SetAnalysisLineVisible(AnalysisLine line, bool visible)
            {
                _analysisLines[line] = visible;
            }

            public bool CloseAnalyzeExistingAfterAdd
            {
                get { return _closeAnalyzeExistingAfterAdd; }
                set { _closeAnalyzeExistingAfterAdd = value; }
            }

            public bool ClosePlanningAfterAdd
            {
                get { return _closePlanningAfterAdd; }
                set { _closePlanningAfterAdd = value; }
            }

            // Whether the Planning target delta-v is measured in vacuum or at the selected
            // body and altitude.
            public DeltaVBasis TargetDeltaVBasis
            {
                get { return _targetDeltaVBasis; }
                set { _targetDeltaVBasis = value; }
            }

            // False draws the mod's windows with the KSP skin, true with the stock Unity
            // skin.  The value is shared with the flight windows through the settings file.
            public bool UseAltSkin
            {
                get { return _useAltSkin; }
                set
                {
                    _useAltSkin = value;
                    WindowSkin.UseAltSkin = value;
                }
            }

            public bool SolidEditorWindowBackgrounds
            {
                get { return _solidEditorWindowBackgrounds; }
                set { _solidEditorWindowBackgrounds = value; }
            }

            public bool ShowTooltips
            {
                get { return _showTooltips; }
                set { _showTooltips = value; }
            }

            // Pixels the Planning grip above the detail panes has been dragged below the
            // position where the engine list ends level with the Requirements box.
            public float PlanningListHeightOffset
            {
                get { return _planningListHeightOffset; }
                set { _planningListHeightOffset = value; }
            }

            // Pixels added to (or removed from) the Analyze Existing candidate list height
            // by the grip above the Selected Engine pane.  Stored as an offset rather than
            // an absolute height so the list still tracks the planner window height.
            public float AnalysisListHeightOffset
            {
                get { return _analysisListHeightOffset; }
                set { _analysisListHeightOffset = value; }
            }

            // Width of the Planning Selected Engine pane as a fraction of the width the
            // Selected Engine + Tanks row has available.  The Tanks pane always receives
            // the remainder, so one value drives both panes.
            public float PlanningEnginePaneFraction
            {
                get { return _planningEnginePaneFraction; }
                set { _planningEnginePaneFraction = value; }
            }

            public float MainWindowWidth
            {
                get { return _mainWindowWidth; }
                set { _mainWindowWidth = Mathf.Clamp(value, MinWindowWidth, MaxWindowWidth); }
            }

            public bool SaveEngineFilter
            {
                get { return _saveEngineFilter; }
                set { _saveEngineFilter = value; }
            }

            public bool SaveEngineExcludeFilter
            {
                get { return _saveEngineExcludeFilter; }
                set { _saveEngineExcludeFilter = value; }
            }

            public bool SaveTankFilter
            {
                get { return _saveTankFilter; }
                set { _saveTankFilter = value; }
            }

            public bool SaveTankExcludeFilter
            {
                get { return _saveTankExcludeFilter; }
                set { _saveTankExcludeFilter = value; }
            }

            public string EngineFilter
            {
                get { return _engineFilter ?? string.Empty; }
                set { _engineFilter = value ?? string.Empty; }
            }

            public string EngineExcludeFilter
            {
                get { return _engineExcludeFilter ?? string.Empty; }
                set { _engineExcludeFilter = value ?? string.Empty; }
            }

            public string TankFilter
            {
                get { return _tankFilter ?? string.Empty; }
                set { _tankFilter = value ?? string.Empty; }
            }

            public string TankExcludeFilter
            {
                get { return _tankExcludeFilter ?? string.Empty; }
                set { _tankExcludeFilter = value ?? string.Empty; }
            }

            public float IconZoomFactor
            {
                get { return _iconZoomFactor; }
                set { _iconZoomFactor = Mathf.Clamp(value, 0.1f, 5f); }
            }

            public float RotatingImageZoomFactor
            {
                get { return _rotatingImageZoomFactor; }
                set { _rotatingImageZoomFactor = Mathf.Clamp(value, 0.1f, 5f); }
            }

            public float CameraYawDegrees
            {
                get { return _cameraYawDegrees; }
                set { _cameraYawDegrees = Mathf.Clamp(value, 0f, 180f); }
            }

            public float CameraPitchDegrees
            {
                get { return _cameraPitchDegrees; }
                set { _cameraPitchDegrees = Mathf.Clamp(value, 0f, 90f); }
            }

            public int RotatingImageBackground
            {
                get { return _rotatingImageBackground; }
                set { _rotatingImageBackground = Mathf.Clamp(value, 0, RotatingImageBackgroundNames.Length - 1); }
            }

            public int RotatingPreviewSize
            {
                get { return _rotatingPreviewSize; }
                set { _rotatingPreviewSize = Mathf.Clamp(value, 32, 256); }
            }

            public int RotatingPreviewDegreesPerFrame
            {
                get { return _rotatingPreviewDegreesPerFrame; }
                set { _rotatingPreviewDegreesPerFrame = Mathf.Clamp(value, 1, 180); }
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

            public void Load()
            {
                try
                {
                    string path = SettingsPath;
                    if (!File.Exists(path) && File.Exists(LegacySettingsPath)) path = LegacySettingsPath;
                    if (!File.Exists(path)) return;
                    ConfigNode node = ConfigNode.Load(path);
                    if (node == null) return;
                    ConfigNode settings = node.GetNode("ENGINE_STAGE_PLANNER_SETTINGS") ?? node;

                    foreach (SolutionColumn column in Enum.GetValues(typeof(SolutionColumn)))
                        _columns[column] = CommonRoutines.ReadBool(settings, "Column_" + column, _columns[column]);
                    foreach (AnalysisLine line in Enum.GetValues(typeof(AnalysisLine)))
                        _analysisLines[line] = CommonRoutines.ReadBool(settings, "Analysis_" + line, _analysisLines[line]);
                    _closeAnalyzeExistingAfterAdd = CommonRoutines.ReadBool(settings, "CloseAnalyzeExistingAfterAdd", _closeAnalyzeExistingAfterAdd);
                    _closePlanningAfterAdd = CommonRoutines.ReadBool(settings, "ClosePlanningAfterAdd", _closePlanningAfterAdd);
                    _solidEditorWindowBackgrounds = CommonRoutines.ReadBool(settings, "SolidEditorWindowBackgrounds", _solidEditorWindowBackgrounds);
                    _showTooltips = CommonRoutines.ReadBool(settings, "ShowTooltips", _showTooltips);
                    UseAltSkin = CommonRoutines.ReadBool(settings, WindowSkin.SettingsKey, _useAltSkin);
                    if (settings.HasValue("TargetDeltaVBasis"))
                    {
                        _targetDeltaVBasis = string.Equals(settings.GetValue("TargetDeltaVBasis"), "Atmospheric", StringComparison.OrdinalIgnoreCase)
                            ? DeltaVBasis.Atmospheric
                            : DeltaVBasis.Vacuum;
                    }
                    if (settings.HasValue("CsvExportDirectory"))
                    {
                        string csvDirectory = settings.GetValue("CsvExportDirectory");
                        if (!string.IsNullOrWhiteSpace(csvDirectory))
                        {
                            csvDirectory = csvDirectory.Trim();
                            _csvExportDirectory = string.Equals(csvDirectory, "EngineStagePlanner/PluginData/CSV", StringComparison.OrdinalIgnoreCase)
                                ? "VesselPlanner/PluginData/CSV"
                                : csvDirectory;
                        }
                    }
                    if (settings.HasValue("PngExportDirectory"))
                    {
                        string pngDirectory = settings.GetValue("PngExportDirectory");
                        if (!string.IsNullOrWhiteSpace(pngDirectory)) _pngExportDirectory = pngDirectory.Trim();
                    }
                    _planningEnginePaneFraction = ReadFloat(settings, "PlanningEnginePaneFraction", _planningEnginePaneFraction);
                    _analysisListHeightOffset = ReadFloat(settings, "AnalysisListHeightOffset", _analysisListHeightOffset);
                    _planningListHeightOffset = ReadFloat(settings, "PlanningListHeightOffset", _planningListHeightOffset);
                    MainWindowWidth = ReadFloat(settings, "MainWindowWidth", _mainWindowWidth);
                    IconZoomFactor = ReadFloat(settings, "IconZoomFactor", _iconZoomFactor);
                    RotatingImageZoomFactor = ReadFloat(settings, "RotatingImageZoomFactor", _rotatingImageZoomFactor);
                    CameraYawDegrees = ReadFloat(settings, "CameraYawDegrees", _cameraYawDegrees);
                    CameraPitchDegrees = ReadFloat(settings, "CameraPitchDegrees", _cameraPitchDegrees);
                    RotatingImageBackground = ReadInt(settings, "RotatingImageBackground", _rotatingImageBackground);
                    RotatingPreviewSize = ReadInt(settings, "RotatingPreviewSize", _rotatingPreviewSize);
                    RotatingPreviewDegreesPerFrame = ReadInt(settings, "RotatingPreviewDegreesPerFrame", _rotatingPreviewDegreesPerFrame);
                    // 0.7.32 stored one persistence flag per category. Use those legacy
                    // flags as the defaults for the new per-field settings so existing users
                    // keep the behavior they already selected.
                    bool legacySaveEngineFilters = CommonRoutines.ReadBool(settings, "SaveEngineFilters", true);
                    bool legacySaveTankFilters = CommonRoutines.ReadBool(settings, "SaveTankFilters", true);
                    _saveEngineFilter = CommonRoutines.ReadBool(settings, "SaveEngineFilter", legacySaveEngineFilters);
                    _saveEngineExcludeFilter = CommonRoutines.ReadBool(settings, "SaveEngineExcludeFilter", legacySaveEngineFilters);
                    _saveTankFilter = CommonRoutines.ReadBool(settings, "SaveTankFilter", legacySaveTankFilters);
                    _saveTankExcludeFilter = CommonRoutines.ReadBool(settings, "SaveTankExcludeFilter", legacySaveTankFilters);
                    _engineFilter = _saveEngineFilter && settings.HasValue("EngineFilter") ? settings.GetValue("EngineFilter") ?? string.Empty : string.Empty;
                    _engineExcludeFilter = _saveEngineExcludeFilter && settings.HasValue("EngineExcludeFilter") ? settings.GetValue("EngineExcludeFilter") ?? string.Empty : string.Empty;
                    _tankFilter = _saveTankFilter && settings.HasValue("TankFilter") ? settings.GetValue("TankFilter") ?? string.Empty : string.Empty;
                    _tankExcludeFilter = _saveTankExcludeFilter && settings.HasValue("TankExcludeFilter") ? settings.GetValue("TankExcludeFilter") ?? string.Empty : string.Empty;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[VesselPlanner] Unable to load UI settings: " + ex.Message);
                }
            }

            public void Save()
            {
                try
                {
                    string path = SettingsPath;
                    string directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                    // Seed the renamed settings file from either the new file or the legacy
                    // EngineStagePlanner file so flight-only keys (for example axis choices)
                    // survive the rename and editor-side saves.
                    string readPath = File.Exists(path) ? path : (File.Exists(LegacySettingsPath) ? LegacySettingsPath : null);
                    ConfigNode root = !string.IsNullOrEmpty(readPath) ? ConfigNode.Load(readPath) : null;
                    if (root == null) root = new ConfigNode();
                    ConfigNode settings = root.GetNode("ENGINE_STAGE_PLANNER_SETTINGS");
                    if (settings == null) settings = root.AddNode("ENGINE_STAGE_PLANNER_SETTINGS");
                    foreach (SolutionColumn column in Enum.GetValues(typeof(SolutionColumn)))
                        settings.SetValue("Column_" + column, ColumnVisible(column), true);
                    foreach (AnalysisLine line in Enum.GetValues(typeof(AnalysisLine)))
                        settings.SetValue("Analysis_" + line, AnalysisLineVisible(line), true);
                    settings.SetValue("CloseAnalyzeExistingAfterAdd", CloseAnalyzeExistingAfterAdd, true);
                    settings.SetValue("ClosePlanningAfterAdd", ClosePlanningAfterAdd, true);
                    settings.SetValue("SolidEditorWindowBackgrounds", SolidEditorWindowBackgrounds, true);
                    settings.SetValue("ShowTooltips", ShowTooltips, true);
                    settings.SetValue(WindowSkin.SettingsKey, UseAltSkin, true);
                    settings.SetValue("TargetDeltaVBasis", _targetDeltaVBasis.ToString(), true);
                    settings.SetValue("CsvExportDirectory", _csvExportDirectory, true);
                    settings.SetValue("PngExportDirectory", _pngExportDirectory, true);
                    settings.SetValue("PlanningEnginePaneFraction", _planningEnginePaneFraction.ToString("0.####", CultureInfo.InvariantCulture), true);
                    settings.SetValue("AnalysisListHeightOffset", _analysisListHeightOffset.ToString("0.##", CultureInfo.InvariantCulture), true);
                    settings.SetValue("PlanningListHeightOffset", _planningListHeightOffset.ToString("0.##", CultureInfo.InvariantCulture), true);
                    settings.SetValue("MainWindowWidth", _mainWindowWidth.ToString("0.##", CultureInfo.InvariantCulture), true);
                    settings.SetValue("IconZoomFactor", _iconZoomFactor.ToString("0.###", CultureInfo.InvariantCulture), true);
                    settings.SetValue("RotatingImageZoomFactor", _rotatingImageZoomFactor.ToString("0.###", CultureInfo.InvariantCulture), true);
                    settings.SetValue("CameraYawDegrees", _cameraYawDegrees.ToString("0.###", CultureInfo.InvariantCulture), true);
                    settings.SetValue("CameraPitchDegrees", _cameraPitchDegrees.ToString("0.###", CultureInfo.InvariantCulture), true);
                    settings.SetValue("RotatingImageBackground", _rotatingImageBackground.ToString(CultureInfo.InvariantCulture), true);
                    settings.SetValue("RotatingPreviewSize", _rotatingPreviewSize.ToString(CultureInfo.InvariantCulture), true);
                    settings.SetValue("RotatingPreviewDegreesPerFrame", _rotatingPreviewDegreesPerFrame.ToString(CultureInfo.InvariantCulture), true);
                    // Keep the old category flags for downgrade compatibility while the
                    // new keys control each include/exclude field independently.
                    settings.SetValue("SaveEngineFilters", _saveEngineFilter && _saveEngineExcludeFilter, true);
                    settings.SetValue("SaveTankFilters", _saveTankFilter && _saveTankExcludeFilter, true);
                    settings.SetValue("SaveEngineFilter", _saveEngineFilter, true);
                    settings.SetValue("SaveEngineExcludeFilter", _saveEngineExcludeFilter, true);
                    settings.SetValue("SaveTankFilter", _saveTankFilter, true);
                    settings.SetValue("SaveTankExcludeFilter", _saveTankExcludeFilter, true);
                    settings.SetValue("EngineFilter", _saveEngineFilter ? EngineFilter : string.Empty, true);
                    settings.SetValue("EngineExcludeFilter", _saveEngineExcludeFilter ? EngineExcludeFilter : string.Empty, true);
                    settings.SetValue("TankFilter", _saveTankFilter ? TankFilter : string.Empty, true);
                    settings.SetValue("TankExcludeFilter", _saveTankExcludeFilter ? TankExcludeFilter : string.Empty, true);
                    root.Save(path);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[VesselPlanner] Unable to save UI settings: " + ex.Message);
                }
            }

            private static float ReadFloat(ConfigNode node, string key, float defaultValue)
            {
                if (node == null || !node.HasValue(key)) return defaultValue;
                float value;
                if (!float.TryParse(node.GetValue(key), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return defaultValue;
                return float.IsNaN(value) || float.IsInfinity(value) ? defaultValue : value;
            }

            private static int ReadInt(ConfigNode node, string key, int defaultValue)
            {
                if (node == null || !node.HasValue(key)) return defaultValue;
                int value;
                return int.TryParse(node.GetValue(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : defaultValue;
            }
        }

        public bool Visible { get; set; } = false;

        public void Initialize()
        {
            _uiSettings.Load();
            _engineNameFilter = _uiSettings.SaveEngineFilter ? _uiSettings.EngineFilter : string.Empty;
            _engineExcludeFilter = _uiSettings.SaveEngineExcludeFilter ? _uiSettings.EngineExcludeFilter : string.Empty;
            _tankNameFilter = _uiSettings.SaveTankFilter ? _uiSettings.TankFilter : string.Empty;
            _tankExcludeFilter = _uiSettings.SaveTankExcludeFilter ? _uiSettings.TankExcludeFilter : string.Empty;
            SyncPartImageSettingText();
            ApplyPartImageSettings();
            _window.width = Mathf.Clamp(_uiSettings.MainWindowWidth, MinWindowWidth, MaxWindowWidth);
            RefreshBodies();
            RefreshDatabases();
            _missionPlanner.TooltipsEnabled = _uiSettings.ShowTooltips;
            _missionPlanner.Initialize();
            _deltaVTablePage.Initialize();
            _stage = Math.Min(EditorStageScanner.MaxStage, Math.Max(0, _stage));
            _stageText = _stage.ToString(CultureInfo.InvariantCulture);
            RefreshStage();
        }

        public void Draw()
        {
            WindowSkin.Apply();
            EnsureStyles();

            // Draw the main/settings windows first. Stage-By-Stage is rendered afterward so
            // its opaque underlay stays above the planner when the windows overlap. The plan
            // still draws when the planner itself is hidden (for example after Finalize).
            if (!Visible) _missionPlanner.CloseEntry();

            if (Visible)
            {
                ReleaseAbandonedSplitterDrag();
                ReleaseAbandonedMainWindowWidthDrag();
                _window.width = Mathf.Clamp(_window.width, MinWindowWidth, MaxWindowWidth);
                // Draw combo-box popups before the GUILayout windows, as required by the shared popup implementation.
                ComboBox.DrawGUI();
                //DrawSolidEditorWindowBackground(_window);
                // Width is controlled explicitly by the right-edge resize grip.  Fixing the
                // GUILayout width prevents child controls from growing the outer window.
                _window = ClickThruBlocker.GUILayoutWindow(19041968, _window, DrawWindow, "VesselPlanner", ToolbarRegistration.winDarker, GUILayout.Width(_window.width), GUILayout.MinHeight(MinWindowHeight));
                if (_settingsVisible)
                {
                    // The KSP skin uses taller controls than the alternate Unity skin.
                    // Give it four additional label-line heights so the same settings fit
                    // without making the alternate-skin window unnecessarily tall.
                    float settingsHeight = GetSettingsWindowHeight();
                    _settingsWindow.width = SettingsWindowWidth;
                    _settingsWindow.height = settingsHeight;
                    //DrawSolidEditorWindowBackground(_settingsWindow);
                    _settingsWindow = ClickThruBlocker.GUILayoutWindow(19041969, _settingsWindow, DrawSettingsWindow, "VesselPlanner Settings", ToolbarRegistration.winDarker, GUILayout.Width(SettingsWindowWidth), GUILayout.Height(settingsHeight));
                }
                else
                {
                    ComboBox.Close(RotatingImageBackgroundComboId);
                }
                ApplyPendingSplitterDrag();
                ApplyPendingMainWindowWidthResize();
                RunQueuedAnalyzeSimulation();
                MatchPlanningResultsHeight();
                MatchPlanningPaneHeights();
                MatchAnalysisColumnHeights();
                ClampWindow();
                ClampSettingsWindow();
            }

            _missionPlanner.TooltipsEnabled = _uiSettings.ShowTooltips;
            if (Visible) _missionPlanner.DrawEntryWindow();
            StagePlan.Draw();

            // Starting or recalculating a Stage-By-Stage stage happens from the modal
            // New/Edit Stage dialog, which is drawn after this main planner window.  Raise
            // the planner only after StagePlan.Draw() returns so Calculate closes the modal
            // and the engine/tank selection window is immediately in front.
            if (_bringMainToFrontRequested && Visible)
            {
                GUI.BringWindowToFront(19041968);
                _bringMainToFrontRequested = false;
            }
        }

        private void DrawWindow(int id)
        {
            if (Event.current != null && Event.current.type == EventType.Repaint)
                _hoverPartPreviewTexture = null;

            using (new GUILayout.HorizontalScope())
            {
                // Selecting Analyze Existing rescans the stage and simulates every candidate
                // engine, so the candidate list is populated without a separate button press.
                // Only the transition triggers it: GUILayout.Toggle keeps returning true for
                // every frame the mode stays selected.
                // Closing the plan window from its own × leaves the mode behind, so the flag is
                // brought back into line before the buttons are drawn.
                if (!StagePlan.Visible) _stageByStageMode = false;

                bool analyzeSelected = !_missionPlannerMode && !_deltaVTableMode && !_planningMode && !_stageByStageMode;
                bool planningSelected = !_missionPlannerMode && !_deltaVTableMode && _planningMode && !_stageByStageMode;

                bool missionSelected = GUILayout.Toggle(_missionPlannerMode, "Mission Planner", "Button", GUILayout.Height(25));
                if (missionSelected && !_missionPlannerMode)
                {
                    ComboBox.Close(PlanningBodyComboId);
                    ComboBox.Close(AnalysisBodyComboId);
                    _missionPlannerMode = true;
                    _deltaVTableMode = false;
                    LeaveStageByStage();
                    CancelStagePartPick(null);
                }
                // Stage-By-Stage is a third mode rather than a separate window toggle, so picking
                // it deselects the other two. It still shows the Planning layout underneath,
                // because that is where the plan's engines and tanks are chosen.
                bool stageByStageSelected = GUILayout.Toggle(_stageByStageMode, "Stage-By-Stage", "Button", GUILayout.Height(25));
                if (Event.current.type != EventType.Layout)
                    _stageByStageButtonWindowRect = GUILayoutUtility.GetLastRect();
                if (stageByStageSelected && !_stageByStageMode)
                {
                    _missionPlanner.CloseEntry();
                    ComboBox.Close(PlanningBodyComboId);
                    ComboBox.Close(AnalysisBodyComboId);
                    _missionPlannerMode = false;
                    _deltaVTableMode = false;
                    _stageByStageMode = true;
                    _planningMode = true;
                    StagePlan.PrepareForStageByStageStart();
                    StagePlan.PositionBelowPlannerButtons();
                    StagePlan.Visible = true;
                }

                GUILayout.Space(18f);

                if (GUILayout.Toggle(analyzeSelected, "Analyze Existing", "Button", GUILayout.Height(25)) && !analyzeSelected)
                {
                    _missionPlanner.CloseEntry();
                    ComboBox.Close(PlanningBodyComboId);
                    ComboBox.Close(AnalysisBodyComboId);
                    _missionPlannerMode = false;
                    _deltaVTableMode = false;
                    _planningMode = false;
                    _simulateOnAnalyzeEntry = true;
                    LeaveStageByStage();
                }
                if (GUILayout.Toggle(planningSelected, "Planning", "Button", GUILayout.Height(25)) && !planningSelected)
                {
                    _missionPlanner.CloseEntry();
                    ComboBox.Close(PlanningBodyComboId);
                    ComboBox.Close(AnalysisBodyComboId);
                    _missionPlannerMode = false;
                    _deltaVTableMode = false;
                    _planningMode = true;
                    LeaveStageByStage();
                }

                GUILayout.Space(18f);

                bool deltaVTableSelected = GUILayout.Toggle(_deltaVTableMode, "Delta-V Table", "Button", GUILayout.Height(25));
                if (deltaVTableSelected && !_deltaVTableMode)
                {
                    _missionPlanner.CloseEntry();
                    ComboBox.Close(PlanningBodyComboId);
                    ComboBox.Close(AnalysisBodyComboId);
                    _missionPlannerMode = false;
                    _deltaVTableMode = true;
                    LeaveStageByStage();
                    CancelStagePartPick(null);
                }

                // Planning may intentionally target stages beyond the craft that currently exists,
                // but Analyze Existing can only inspect real vessel stages.  Enforce the current
                // vessel maximum every frame so a staging edit which lowers the maximum also brings
                // an already-selected stage back into range.
                if (!_missionPlannerMode && !_deltaVTableMode && !_planningMode && _stage > EditorStageScanner.MaxStage)
                    SetStage(EditorStageScanner.MaxStage);

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Settings", GUILayout.Width(80)))
                    _settingsVisible = !_settingsVisible;
                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                {
                    RefreshDatabases();
                    RefreshStage();
                    if (_deltaVTableMode) DeltaVTable.Reload();
                    RecalculateForFilterChange();
                }
                if (GUILayout.Button("×", GUILayout.Width(30)))
                {
                    _missionPlanner.CloseEntry();
                    Visible = false;
                    _settingsVisible = false;
                }
            }

            if (_missionPlannerMode)
            {
                GUILayout.Space(4);
                _missionPlanner.DrawPage();
            }
            else if (_deltaVTableMode)
            {
                GUILayout.Space(4);
                _deltaVTablePage.DrawPage();
            }
            else
            {
                DrawStageSelector();
                GUILayout.Space(4);
                if (_planningMode) DrawPlanning(); else DrawAnalysis();
            }

            DrawMainWindowWidthResizeGrip();
            DrawHoveredPartPreview();

            // Skipped mid-drag so a grip that has reached its limit cannot hand the rest of
            // the movement to the window. GUI.DragWindow is not a layout control, so leaving
            // it out on these passes does not disturb the layout.
            if (_activeSplitter == PaneSplitter.None && !_resizingMainWindowWidth)
                GUI.DragWindow(new Rect(0f, 0f, _window.width, _window.height));
        }

        private void DrawStageSelector()
        {
            using (new GUILayout.HorizontalScope())
            {
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
                // In Analyze Existing, disable it at the vessel's highest real stage.
                bool oldGuiEnabled = GUI.enabled;
                GUI.enabled = oldGuiEnabled && (_planningMode || _stage < EditorStageScanner.MaxStage);
                if (GUILayout.Button("+", GUILayout.Width(28))) SetStage(_stage + 1);
                GUI.enabled = oldGuiEnabled;
                if (GUILayout.Button("-", GUILayout.Width(28))) SetStage(Math.Max(0, _stage - 1));
                if (!_planningMode)
                {
                    if (GUILayout.Button(_pickStageFromPart ? "Cancel Pick" : "Pick Stage", GUILayout.Width(85)))
                    {
                        if (_pickStageFromPart) CancelStagePartPick("Stage selection cancelled.");
                        else BeginStagePartPick();
                    }

                    GUILayout.Label("Craft max: " + EditorStageScanner.MaxStage, GUILayout.Width(90));
                }
                else if (_pickStageFromPart)
                {
                    // Pick Stage is an Analyze Existing aid.  Do not leave the editor in
                    // pick mode if the user changes to Planning mode.
                    CancelStagePartPick(null);
                }

                if (_snapshot != null)
                {
                    if (!_planningMode)
                        GUILayout.Label("Craft wet: " + F(_snapshot.VesselWetMassTons) + " t");
                    GUILayout.Label("Stage fuel: " + F(_snapshot.StagePropellantMassTons) + " t");
                    if (_snapshot.TopNodeSizes.Count > 0)
                        GUILayout.Label("Stage top node: " + FormatNodeSizes(_snapshot.TopNodeSizes));
                }
            }
            if (!_planningMode && _pickStageFromPart)
                GUILayout.Label("Click a part on the current vessel to use its staging-display stage. Esc or right-click cancels.");
        }

        public void Update()
        {
            // Stage-By-Stage keyboard navigation is polled from Update so Tab is not
            // dependent on the modal IMGUI event stream.
            if (_stagePlan != null) _stagePlan.Update();

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

            if (_missionPlannerMode || _deltaVTableMode) return;

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
            PartThumbnailCache.Clear();
            _uiSettings.Save();
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
            if (!_planningMode)
                stage = Math.Min(EditorStageScanner.MaxStage, stage);

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
            using (new GUILayout.HorizontalScope())
            {

                // Keep the controls that drive the replacement-engine simulation together in
                // their own pane at the top of the Analyze Existing left column.  Stage
                // diagnostics can grow substantially, so putting the simulation controls first
                // keeps the controls and Simulate button immediately accessible.
                GUILayout.BeginVertical(GUILayout.Width(350));
                GUILayout.BeginVertical("box");
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

                GUILayout.Space(4);

                GUILayout.BeginVertical("box");
                GUILayout.Label("Existing stage");
                // The stage diagnostics grow with the number of enabled lines, engines, and
                // stage resources, and the window is sized by its content, so the pane is given
                // a fixed viewport and scrolls instead of stretching the window. The height is
                // taken from the screen rather than the window: the window height is a result
                // of this pane's size, so reading it here would let the two grow off each other.
                _analysisStageScroll = GUILayout.BeginScrollView(_analysisStageScroll, GUILayout.Height(AnalysisStageScrollHeight()));
                AnalysisRow(AnalysisLine.CraftWetMass, "Craft wet mass", F(_snapshot.VesselWetMassTons) + " t");
                AnalysisRow(AnalysisLine.CraftDryMass, "Craft dry mass", F(_snapshot.VesselDryMassTons) + " t");
                AnalysisRow(AnalysisLine.PayloadAboveStage, "Payload above stage", F(_snapshot.PayloadAboveStageMassTons) + " t");
                AnalysisRow(AnalysisLine.StagePropellantCurrent, "Stage propellant (current)", F(_snapshot.StagePropellantMassTons) + " t");
                AnalysisRow(AnalysisLine.StagePropellantFull, "Stage propellant (full)", F(_snapshot.StagePropellantCapacityMassTons) + " t");
                if (_snapshot.HasStockStageMasses)
                {
                    AnalysisRow(AnalysisLine.KspStageWetMass, "KSP stage wet mass", F(_snapshot.StockStageMassTons) + " t");
                    AnalysisRow(AnalysisLine.KspStageDryMass, "KSP stage dry mass", F(_snapshot.StockStageDryMassTons) + " t");
                    AnalysisRow(AnalysisLine.KspStageFuelMass, "KSP stage fuel mass", F(_snapshot.StockStageFuelMassTons) + " t");
                    AnalysisRow(AnalysisLine.KspCurrentEngineMass, "KSP current engine mass", F(_snapshot.StockCurrentEngineMassTons > 0.0 ? _snapshot.StockCurrentEngineMassTons : _snapshot.CurrentEngineMassTons) + " t");
                    AnalysisRow(AnalysisLine.KspVehicleStartMass, "KSP vehicle start mass", F(_snapshot.StockStageStartMassTons) + " t");
                    AnalysisRow(AnalysisLine.KspVehicleEndMass, "KSP vehicle end mass", F(_snapshot.StockStageEndMassTons) + " t");
                    if (_snapshot.StockStageBurnTimeSeconds > 0.0)
                        AnalysisRow(AnalysisLine.KspCurrentBurn, "KSP current burn", _snapshot.StockStageBurnTimeSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
                }
                AnalysisRow(AnalysisLine.StageTankCapacity, "Stage tank capacity", F(_snapshot.StageTankCapacityUnits) + " units");
                AnalysisRow(AnalysisLine.KnownTankVolume, "Known tank volume", _snapshot.StageTankVolumeLiters > 0 ? F(_snapshot.StageTankVolumeLiters) + " L" : "n/a");

                if (_uiSettings.AnalysisLineVisible(AnalysisLine.CurrentEngines))
                {
                    GUILayout.Space(4);
                    GUILayout.Label("Current engines");
                    // The engine list sits in a darker box so it reads as a table rather than
                    // as more of the Existing stage rows above it.
                    GUILayout.BeginVertical(_darkBox);
                    if (_snapshot.CurrentEngineDetails.Count == 0)
                    {
                        GUILayout.Label("  none found");
                    }
                    else
                    {
                        // The column header stays put while the engine rows scroll beneath it.
                        // Columns are narrower than the pane's other rows to leave room for the
                        // two scrollbars this list now sits inside.
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Engine", GUILayout.Width(130));
                            GUILayout.Label("ASL kN", GUILayout.Width(58));
                            GUILayout.Label("Vac kN", GUILayout.Width(58));
                            GUILayout.Label("Isp ASL/Vac", GUILayout.Width(68));
                        }
                        _currentEnginesScroll = GUILayout.BeginScrollView(_currentEnginesScroll, GUILayout.Height(CurrentEnginesScrollHeight()));
                        foreach (ExistingEngineInfo engine in _snapshot.CurrentEngineDetails)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                using (new GUILayout.HorizontalScope(GUILayout.Width(130f), GUILayout.Height(28f)))
                                {
                                    DrawPartThumbnail(engine.PartName, engine.PartUrl, 28f);
                                    GUILayout.Label(engine.DisplayName, GUILayout.Width(98f), GUILayout.Height(28f));
                                }
                                GUILayout.Label(F(engine.SeaLevelThrustKn), GUILayout.Width(58));
                                GUILayout.Label(F(engine.VacuumThrustKn), GUILayout.Width(58));
                                GUILayout.Label(engine.SeaLevelIsp.ToString("0") + "/" + engine.VacuumIsp.ToString("0"), GUILayout.Width(68));
                            }
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();
                }

                if (_uiSettings.AnalysisLineVisible(AnalysisLine.StageResources))
                {
                    GUILayout.Space(4);
                    GUILayout.Label("Stage Resources");
                    foreach (var r in _snapshot.Resources)
                        GUILayout.Label("  " + r.Name + ": " + F(r.Capacity) + " units capacity");
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                // Measure the Existing stage box itself, not the column group that wraps it.
                // A layout group's rect can extend past its last child by that child's margin,
                // which left the Selected Engine pane sitting roughly half a line low.  Both
                // measured rects are now "box" groups, so their bottom edges are comparable.
                if (Event.current != null && Event.current.type == EventType.Repaint)
                    _measuredAnalysisLeftColumnBottom = GUILayoutUtility.GetLastRect().yMax;
                GUILayout.EndVertical();

                // Keep the candidate list and the selected replacement-engine details in
                // the right-hand column.  This makes the selected engine a continuation of
                // the engine list instead of a full-width pane below both columns.
                GUILayout.BeginVertical();
                GUILayout.BeginVertical("box");
                DrawResults();
                GUILayout.EndVertical();
                if (_selected != null)
                {
                    // Dragging this grip moves the boundary between the candidate list and the
                    // Selected Engine pane.  Only the list height is stored: the pane re-levels
                    // itself against the Existing stage box, so the column bottom stays put and
                    // the drag simply redistributes the height between the two.
                    SplitterGrip(PaneSplitter.AnalysisListHeight, false, GUILayout.ExpandWidth(true), GUILayout.Height(SplitterGripThickness));
                    // The pane has no width grip of its own here; it simply fills the
                    // right-hand column beneath the candidate engine list.
                    DrawSelectedEnginePane(false);
                    if (Event.current != null && Event.current.type == EventType.Repaint)
                        _measuredAnalysisPaneBottom = GUILayoutUtility.GetLastRect().yMax;
                }
                GUILayout.EndVertical();
            }
        }

        private void DrawPlanning()
        {
            using (new GUILayout.HorizontalScope())
            {
                // Widened from 310 so the widest row still fits once the scroll view below
                // takes its scrollbar out of the available width.
                GUILayout.BeginVertical("box", GUILayout.Width(350));
                GUILayout.Label("Requirements");
                // Like the candidate list beside it, the pane is given a fixed viewport rather
                // than being allowed to stretch the window, and the grip above the detail panes
                // resizes it. Calculate and the status line stay pinned below the scroll view.
                _planningRequirementsViewport = PlanningRequirementsScrollHeight();
                _planningRequirementsScroll = GUILayout.BeginScrollView(_planningRequirementsScroll, GUILayout.Height(_planningRequirementsViewport));
                bool atmosphericTarget = _uiSettings.TargetDeltaVBasis == DeltaVBasis.Atmospheric;
                Field(atmosphericTarget ? "Target Atm Δv (m/s)" : "Target Vac Δv (m/s)", ref _targetDv);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Δv basis", GUILayout.Width(80));
                    if (GUILayout.Toggle(!atmosphericTarget, "Vacuum", "Button", GUILayout.Width(90)) && atmosphericTarget)
                    {
                        _uiSettings.TargetDeltaVBasis = DeltaVBasis.Vacuum;
                        _uiSettings.Save();
                        RecalculateForFilterChange();
                    }
                    if (GUILayout.Toggle(atmosphericTarget, "Atmosphere", "Button", GUILayout.Width(90)) && !atmosphericTarget)
                    {
                        _uiSettings.TargetDeltaVBasis = DeltaVBasis.Atmospheric;
                        _uiSettings.Save();
                        RecalculateForFilterChange();
                    }
                }
                GUILayout.Label(atmosphericTarget
                    ? "Target is met at the selected body and altitude, so changing either resizes the stage."
                    : "Target sizes the stage in vacuum; altitude only changes atmospheric Δv/thrust/TWR.");
                Field("Minimum TWR", ref _minTwr);
                _useCraftPayload = GUILayout.Toggle(_useCraftPayload, "Use craft payload above selected stage");
                GUI.enabled = !_useCraftPayload;
                Field("Payload mass (t)", ref _payload);
                GUI.enabled = true;
                if (StagePlan.IsCapturing)
                {
                    Row("Stage cargo mass", F(_plannedStageCargoMassTons) + " t");
                    if (_plannedStageAssemblyMassTons > 0.0)
                        Row("Subassembly mass", F(_plannedStageAssemblyMassTons) + " t");
                    if (_plannedStageDecouplerMassTons > 0.0)
                        Row("Decoupler mass", F(_plannedStageDecouplerMassTons) + " t");
                }
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
                GUILayout.EndScrollView();
                GUILayout.Space(6);
                if (GUILayout.Button("Calculate", GUILayout.Height(32))) Calculate();
                GUILayout.Label(_status);
                GUILayout.EndVertical();
                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    Rect requirementsBox = GUILayoutUtility.GetLastRect();
                    _measuredPlanningRequirementsBottom = requirementsBox.yMax;
                    _measuredPlanningRequirementsHeight = requirementsBox.height;
                }

                GUILayout.BeginVertical("box");
                DrawResults();
                GUILayout.EndVertical();
                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    Rect resultsBox = GUILayoutUtility.GetLastRect();
                    _measuredPlanningResultsBottom = resultsBox.yMax;
                    _measuredPlanningResultsHeight = resultsBox.height;
                }
            }

            // Planning keeps the selected engine and its tank suggestions visible at
            // the same level, in two side-by-side panes.  The engine details are on the
            // left and the tank suggestions are on the right.
            if (_selected != null)
            {
                // Dragging this grip moves height between the two rows: what the Requirements
                // viewport gains, the Tanks table gives up, and the other way round. The
                // candidate engine list follows Requirements because it is aligned to the
                // bottom of that box, and the Selected Engine pane follows the Tanks pane it
                // is matched to. The drag stops where either pane would hit its limit, so the
                // window never has to grow to take the difference.
                SplitterGrip(PaneSplitter.PlanningListHeight, false, GUILayout.ExpandWidth(true), GUILayout.Height(SplitterGripThickness));
                using (new GUILayout.HorizontalScope())
                {
                    // Tanks need more horizontal room for the wider descriptive columns and
                    // the full Add Tank button, so the engine pane starts at 35% of the
                    // available detail-row width.  The grip between the two panes moves that
                    // split; the two panes are kept at a matching height.
                    float planningDetailAvailableWidth = PlanningDetailAvailableWidth();
                    float selectedEnginePaneWidth = PlanningEnginePaneWidth();
                    float tankPaneWidth = Mathf.Max(MinDetailPaneWidth, planningDetailAvailableWidth - selectedEnginePaneWidth);
                    DrawSelectedEnginePane(true, GUILayout.Width(selectedEnginePaneWidth));
                    if (Event.current != null && Event.current.type == EventType.Repaint)
                        _measuredPlanningEnginePaneHeight = GUILayoutUtility.GetLastRect().height;
                    SplitterGrip(PaneSplitter.PlanningEngineTankWidth, true, GUILayout.Width(SplitterGripThickness), GUILayout.Height(PlanningSplitterHeight()));
                    DrawTankSuggestionsPane(GUILayout.Width(tankPaneWidth));
                    if (Event.current != null && Event.current.type == EventType.Repaint)
                    {
                        _measuredPlanningTankPaneHeight = GUILayoutUtility.GetLastRect().height;
                        _planningPaneRowHeight = _measuredPlanningTankPaneHeight;
                    }
                }
            }
        }

        private void DrawResults()
        {
            GUILayout.Label("Candidate engine filters");
            GUILayout.BeginVertical("box");

            DrawEnvironmentControls();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Filter:", GUILayout.Width(85));
                string newEngineNameFilter = GUILayout.TextField(_engineNameFilter ?? string.Empty, GUILayout.Width(220));
                if (!string.Equals(newEngineNameFilter, _engineNameFilter, StringComparison.Ordinal))
                {
                    _engineNameFilter = newEngineNameFilter;
                    SaveEngineFilterIfEnabled();
                    RecalculateForFilterChange();
                }
                GUILayout.Label("Exclude:", GUILayout.Width(65));
                string newEngineExcludeFilter = GUILayout.TextField(_engineExcludeFilter ?? string.Empty, GUILayout.Width(220));
                if (!string.Equals(newEngineExcludeFilter, _engineExcludeFilter, StringComparison.Ordinal))
                {
                    _engineExcludeFilter = newEngineExcludeFilter;
                    SaveEngineExcludeFilterIfEnabled();
                    RecalculateForFilterChange();
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.Label("Separate multiple filter terms with commas. Include terms are OR matches; any exclusion term removes a match.");

            bool bulkheadFilterChanged = false, planningStageOpen = false;
            using (new GUILayout.HorizontalScope())
            {
                // A stage-by-stage stage has no craft to read a bulkhead from; its own list of
                // sizes replaces this filter while it is open, so the toggle is not offered.
                planningStageOpen = StagePlan.IsCapturing;
                bool oldBulkheadFilter = _filterByBulkheadSize;
                if (!planningStageOpen)
                    _filterByBulkheadSize = GUILayout.Toggle(_filterByBulkheadSize, "Match stage bulkhead size", GUILayout.Width(250));
                else
                    GUILayout.Label("Bulkhead profiles: " + StagePlan.ActiveBulkheadSummary, GUILayout.Width(300));
                bulkheadFilterChanged = oldBulkheadFilter != _filterByBulkheadSize;
                if (_snapshot != null && _snapshot.TopNodeSizes.Count > 0)
                    GUILayout.Label("Stage top node: " + FormatNodeSizes(_snapshot.TopNodeSizes));
                else
                    GUILayout.Label("Stage: no engine top node detected");
            }
            if (bulkheadFilterChanged) RecalculateForFilterChange();

            if (!planningStageOpen && _filterByBulkheadSize && (_snapshot == null || _snapshot.TopNodeSizes.Count == 0))
                GUILayout.Label("Bulkhead filtering cannot be applied until an engine top node is detected for the selected stage.");

            GUILayout.EndVertical();
            GUILayout.Space(3);

            GUILayout.Label("Candidates (click a column heading to sort; click engine name for details; Add selects it for editor placement)");
            GUILayout.Label("Cost Efficiency = atmospheric Δv at the selected planet/altitude per Fund of engine cost.");
            // Keep the sortable headings horizontally synchronized with the engine rows.
            // This header scroll view intentionally has no visible scrollbars; the horizontal
            // scrollbar in the engine-list view below remains the single control for both.
            Vector2 headerScroll = new Vector2(_resultsScroll.x, 0f);
            headerScroll = GUILayout.BeginScrollView(
                headerScroll,
                false, false,
                GUIStyle.none, GUIStyle.none,
                GUILayout.Height(34f));
            using (new GUILayout.HorizontalScope())
            {
                if (ColumnVisible(SolutionColumn.Engine)) SortHeader("Engine", 225, SolutionSortColumn.Engine);
                if (ColumnVisible(SolutionColumn.Count)) SortHeader("#", 28, SolutionSortColumn.Count);
                if (ColumnVisible(SolutionColumn.EngineMass)) SortHeader("Mass t/eng", 78, SolutionSortColumn.EngineMass);
                if (ColumnVisible(SolutionColumn.StageWetMass)) SortHeader("Stage Wet t", 95, SolutionSortColumn.StageWetMass);
                if (ColumnVisible(SolutionColumn.StartMass)) SortHeader("Start Mass t", 100, SolutionSortColumn.StartMass);
                if (ColumnVisible(SolutionColumn.AtmosphericDeltaV)) SortHeader("Atm Δv", 65, SolutionSortColumn.AtmosphericDeltaV);
                if (ColumnVisible(SolutionColumn.VacuumDeltaV)) SortHeader("Vac Δv", 65, SolutionSortColumn.VacuumDeltaV);
                if (ColumnVisible(SolutionColumn.SeaLevelThrust)) SortHeader("ASL kN/eng", 84, SolutionSortColumn.SeaLevelThrust);
                if (ColumnVisible(SolutionColumn.VacuumThrust)) SortHeader("Vac kN/eng", 84, SolutionSortColumn.VacuumThrust);
                if (ColumnVisible(SolutionColumn.CostEfficiency)) SortHeader("Cost Eff.", 70, SolutionSortColumn.CostEfficiency);
                if (ColumnVisible(SolutionColumn.Twr)) SortHeader("TWR", 55, SolutionSortColumn.Twr);
                if (ColumnVisible(SolutionColumn.MaxTwr)) SortHeader("Max TWR", 70, SolutionSortColumn.MaxTwr);
                if (ColumnVisible(SolutionColumn.Isp)) SortHeader("Isp ASL/Vac", 88, SolutionSortColumn.Isp);
                if (ColumnVisible(SolutionColumn.Burn)) SortHeader("Burn", 55, SolutionSortColumn.Burn);
                if (ColumnVisible(SolutionColumn.Fuel)) SortHeader("Fuel", 120, SolutionSortColumn.Fuel);
                if (ColumnVisible(SolutionColumn.Bulkhead)) SortHeader("Bulkhead", 72, SolutionSortColumn.Bulkhead);
                if (ColumnVisible(SolutionColumn.Add)) Header("", 48);
            }
            GUILayout.EndScrollView();
            _resultsScroll.x = headerScroll.x;

            _resultsScroll = GUILayout.BeginScrollView(_resultsScroll, GUILayout.Height(GetResultsHeight()));
            foreach (StageSolution s in _solutions.Take(250))
            {
                if (s == _selected) GUILayout.BeginHorizontal("box"); else GUILayout.BeginHorizontal();
                if (ColumnVisible(SolutionColumn.Engine))
                {
                    using (new GUILayout.HorizontalScope(GUILayout.Width(225f), GUILayout.Height(36f)))
                    {
                        DrawPartThumbnail(s.Engine.PartName, s.Engine.PartUrl, 36f);
                        if (GUILayout.Button(s.Engine.DisplayName, GUI.skin.label, GUILayout.Width(185f), GUILayout.Height(36f)))
                            SelectSolution(s);
                    }
                }
                if (ColumnVisible(SolutionColumn.Count)) GUILayout.Label(s.EngineCount.ToString(), GUILayout.Width(28));
                if (ColumnVisible(SolutionColumn.EngineMass)) GUILayout.Label(F(s.Engine.MassTons), GUILayout.Width(78));
                if (ColumnVisible(SolutionColumn.StageWetMass)) GUILayout.Label(F(s.StageWetMassTons), GUILayout.Width(95));
                if (ColumnVisible(SolutionColumn.StartMass)) GUILayout.Label(F(s.StartMassTons), GUILayout.Width(100));
                if (ColumnVisible(SolutionColumn.AtmosphericDeltaV)) GUILayout.Label(Math.Round(s.AtmosphericDeltaV).ToString("0"), GUILayout.Width(65));
                if (ColumnVisible(SolutionColumn.VacuumDeltaV)) GUILayout.Label(Math.Round(s.VacuumDeltaV).ToString("0"), GUILayout.Width(65));
                if (ColumnVisible(SolutionColumn.SeaLevelThrust)) GUILayout.Label(F(s.Engine.SeaLevelThrustKn), GUILayout.Width(84));
                if (ColumnVisible(SolutionColumn.VacuumThrust)) GUILayout.Label(F(s.Engine.MaxThrustVacuumKn), GUILayout.Width(84));
                if (ColumnVisible(SolutionColumn.CostEfficiency)) GUILayout.Label(s.CostEfficiency.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(70));
                if (ColumnVisible(SolutionColumn.Twr)) GUILayout.Label(s.InitialTwr.ToString("0.00"), GUILayout.Width(55));
                if (ColumnVisible(SolutionColumn.MaxTwr)) GUILayout.Label(s.MaxTwr.ToString("0.00"), GUILayout.Width(70));
                if (ColumnVisible(SolutionColumn.Isp)) GUILayout.Label(s.Engine.SeaLevelIsp.ToString("0") + "/" + s.Engine.VacuumIsp.ToString("0"), GUILayout.Width(88));
                if (ColumnVisible(SolutionColumn.Burn)) GUILayout.Label(s.BurnTimeSeconds.ToString("0") + "s", GUILayout.Width(55));
                if (ColumnVisible(SolutionColumn.Fuel)) GUILayout.Label(s.PropellantSummary, GUILayout.Width(120));
                if (ColumnVisible(SolutionColumn.Bulkhead)) GUILayout.Label(BulkheadLabel(s.Engine), GUILayout.Width(72));
                if (ColumnVisible(SolutionColumn.Add) && GUILayout.Button("Add", GUILayout.Width(48))) SpawnEngine(s);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private bool ColumnVisible(SolutionColumn column)
        {
            return _uiSettings.ColumnVisible(column);
        }

        private void AnalysisRow(AnalysisLine line, string label, string value)
        {
            if (_uiSettings.AnalysisLineVisible(line))
                Row(label, value);
        }

        private void DrawSettingsWindow(int id)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(_settingsTab == 0, "Engine Columns", "Button", GUILayout.Height(28))) _settingsTab = 0;
                if (GUILayout.Toggle(_settingsTab == 1, "Analyze Existing", "Button", GUILayout.Height(28))) _settingsTab = 1;
                if (GUILayout.Toggle(_settingsTab == 2, "Planning", "Button", GUILayout.Height(28))) _settingsTab = 2;
                if (GUILayout.Toggle(_settingsTab == 3, "Filters", "Button", GUILayout.Height(28))) _settingsTab = 3;
                if (GUILayout.Toggle(_settingsTab == 4, "Appearance", "Button", GUILayout.Height(28))) _settingsTab = 4;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("×", GUILayout.Width(30))) _settingsVisible = false;
            }
            GUILayout.Space(4);
            _settingsScroll = GUILayout.BeginScrollView(_settingsScroll);
            bool changed = false;
            bool partImageSettingsChanged = false;
            if (_settingsTab == 0)
            {
                GUILayout.Label("Choose which columns are shown in the candidate engine list.");
                GUILayout.Space(4);
                changed |= SettingsColumnToggle(SolutionColumn.Engine, "Engine");
                changed |= SettingsColumnToggle(SolutionColumn.Count, "# (engine count)");
                changed |= SettingsColumnToggle(SolutionColumn.EngineMass, "Mass t/eng");
                changed |= SettingsColumnToggle(SolutionColumn.StageWetMass, "Stage Wet t");
                changed |= SettingsColumnToggle(SolutionColumn.StartMass, "Start Mass t");
                changed |= SettingsColumnToggle(SolutionColumn.AtmosphericDeltaV, "Atm Δv");
                changed |= SettingsColumnToggle(SolutionColumn.VacuumDeltaV, "Vac Δv");
                changed |= SettingsColumnToggle(SolutionColumn.SeaLevelThrust, "ASL kN/eng");
                changed |= SettingsColumnToggle(SolutionColumn.VacuumThrust, "Vac kN/eng");
                changed |= SettingsColumnToggle(SolutionColumn.CostEfficiency, "Cost Eff.");
                changed |= SettingsColumnToggle(SolutionColumn.Twr, "TWR");
                changed |= SettingsColumnToggle(SolutionColumn.MaxTwr, "Max TWR");
                changed |= SettingsColumnToggle(SolutionColumn.Isp, "Isp ASL/Vac");
                changed |= SettingsColumnToggle(SolutionColumn.Burn, "Burn");
                changed |= SettingsColumnToggle(SolutionColumn.Fuel, "Fuel");
                changed |= SettingsColumnToggle(SolutionColumn.Bulkhead, "Bulkhead");
                changed |= SettingsColumnToggle(SolutionColumn.Add, "Add button");

                GUILayout.Space(8);
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Show All", GUILayout.Width(100)))
                    {
                        foreach (SolutionColumn column in Enum.GetValues(typeof(SolutionColumn)))
                            _uiSettings.SetColumnVisible(column, true);
                        changed = true;
                    }
                    if (GUILayout.Button("Hide All", GUILayout.Width(100)))
                    {
                        foreach (SolutionColumn column in Enum.GetValues(typeof(SolutionColumn)))
                            _uiSettings.SetColumnVisible(column, false);
                        changed = true;
                    }
                }
            }
            else if (_settingsTab == 1)
            {
                GUILayout.Label("Choose which informational lines and sections are shown in the left pane while Analyze Existing is active.");
                GUILayout.Space(4);
                changed |= SettingsAnalysisToggle(AnalysisLine.CraftWetMass, "Craft wet mass");
                changed |= SettingsAnalysisToggle(AnalysisLine.CraftDryMass, "Craft dry mass");
                changed |= SettingsAnalysisToggle(AnalysisLine.PayloadAboveStage, "Payload above stage");
                changed |= SettingsAnalysisToggle(AnalysisLine.StagePropellantCurrent, "Stage propellant (current)");
                changed |= SettingsAnalysisToggle(AnalysisLine.StagePropellantFull, "Stage propellant (full)");
                changed |= SettingsAnalysisToggle(AnalysisLine.KspStageWetMass, "KSP stage wet mass");
                changed |= SettingsAnalysisToggle(AnalysisLine.KspStageDryMass, "KSP stage dry mass");
                changed |= SettingsAnalysisToggle(AnalysisLine.KspStageFuelMass, "KSP stage fuel mass");
                changed |= SettingsAnalysisToggle(AnalysisLine.KspCurrentEngineMass, "KSP current engine mass");
                changed |= SettingsAnalysisToggle(AnalysisLine.KspVehicleStartMass, "KSP vehicle start mass");
                changed |= SettingsAnalysisToggle(AnalysisLine.KspVehicleEndMass, "KSP vehicle end mass");
                changed |= SettingsAnalysisToggle(AnalysisLine.KspCurrentBurn, "KSP current burn");
                changed |= SettingsAnalysisToggle(AnalysisLine.StageTankCapacity, "Stage tank capacity");
                changed |= SettingsAnalysisToggle(AnalysisLine.KnownTankVolume, "Known tank volume");
                changed |= SettingsAnalysisToggle(AnalysisLine.CurrentEngines, "Current engines section");
                changed |= SettingsAnalysisToggle(AnalysisLine.StageResources, "Stage resource lines");

                GUILayout.Space(10);
                GUILayout.Label("Add behavior");
                bool oldCloseAfterAdd = _uiSettings.CloseAnalyzeExistingAfterAdd;
                bool newCloseAfterAdd = GUILayout.Toggle(oldCloseAfterAdd, "Close planner after Add selects an engine for placement");
                if (newCloseAfterAdd != oldCloseAfterAdd)
                {
                    _uiSettings.CloseAnalyzeExistingAfterAdd = newCloseAfterAdd;
                    changed = true;
                }

                GUILayout.Space(8);
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Show All", GUILayout.Width(100)))
                    {
                        foreach (AnalysisLine line in Enum.GetValues(typeof(AnalysisLine)))
                            _uiSettings.SetAnalysisLineVisible(line, true);
                        changed = true;
                    }
                    if (GUILayout.Button("Hide All", GUILayout.Width(100)))
                    {
                        foreach (AnalysisLine line in Enum.GetValues(typeof(AnalysisLine)))
                            _uiSettings.SetAnalysisLineVisible(line, false);
                        changed = true;
                    }
                }
            }
            else if (_settingsTab == 2)
            {
                SettingsSectionHeader("Planning mode behavior");
                GUILayout.Space(8);
                GUILayout.Label("Add behavior");
                bool oldCloseAfterAdd = _uiSettings.ClosePlanningAfterAdd;
                bool newCloseAfterAdd = GUILayout.Toggle(oldCloseAfterAdd, "Close planner after Add selects a part for placement");
                if (newCloseAfterAdd != oldCloseAfterAdd)
                {
                    _uiSettings.ClosePlanningAfterAdd = newCloseAfterAdd;
                    changed = true;
                }
                GUILayout.Label("Applies to engine Add, Add Engine, and Add Tank buttons.");
            }
            else if (_settingsTab == 3)
            {
                SettingsSectionHeader("Filter persistence");
                GUILayout.Label("Filter text is applied immediately. Enable these options to also save the include and exclusion text automatically for the next KSP session.");
                GUILayout.Space(8);

                GUILayout.Label("Engines");
                bool oldSaveEngineFilter = _uiSettings.SaveEngineFilter;
                bool newSaveEngineFilter = GUILayout.Toggle(oldSaveEngineFilter, "Save engine include Filter");
                if (newSaveEngineFilter != oldSaveEngineFilter)
                {
                    _uiSettings.SaveEngineFilter = newSaveEngineFilter;
                    _uiSettings.EngineFilter = newSaveEngineFilter ? _engineNameFilter : string.Empty;
                    changed = true;
                }

                bool oldSaveEngineExcludeFilter = _uiSettings.SaveEngineExcludeFilter;
                bool newSaveEngineExcludeFilter = GUILayout.Toggle(oldSaveEngineExcludeFilter, "Save engine Exclude filter");
                if (newSaveEngineExcludeFilter != oldSaveEngineExcludeFilter)
                {
                    _uiSettings.SaveEngineExcludeFilter = newSaveEngineExcludeFilter;
                    _uiSettings.EngineExcludeFilter = newSaveEngineExcludeFilter ? _engineExcludeFilter : string.Empty;
                    changed = true;
                }

                GUILayout.Space(8);
                GUILayout.Label("Tanks");
                bool oldSaveTankFilter = _uiSettings.SaveTankFilter;
                bool newSaveTankFilter = GUILayout.Toggle(oldSaveTankFilter, "Save tank include Filter");
                if (newSaveTankFilter != oldSaveTankFilter)
                {
                    _uiSettings.SaveTankFilter = newSaveTankFilter;
                    _uiSettings.TankFilter = newSaveTankFilter ? _tankNameFilter : string.Empty;
                    changed = true;
                }

                bool oldSaveTankExcludeFilter = _uiSettings.SaveTankExcludeFilter;
                bool newSaveTankExcludeFilter = GUILayout.Toggle(oldSaveTankExcludeFilter, "Save tank Exclude filter");
                if (newSaveTankExcludeFilter != oldSaveTankExcludeFilter)
                {
                    _uiSettings.SaveTankExcludeFilter = newSaveTankExcludeFilter;
                    _uiSettings.TankExcludeFilter = newSaveTankExcludeFilter ? _tankExcludeFilter : string.Empty;
                    changed = true;
                }

                GUILayout.Space(8);
                GUILayout.Label("Comma-separated terms use OR matching. A candidate is hidden if any Exclude term matches its display name or internal part name.");
            }
            else
            {
                SettingsSectionHeader("Editor window appearance");
                GUILayout.Space(8);
                GUILayout.Label("Window skin");
                bool oldAltSkin = _uiSettings.UseAltSkin;
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(!oldAltSkin, "KSP skin", "Button", GUILayout.Width(140)) && oldAltSkin)
                    {
                        _uiSettings.UseAltSkin = false;
                        changed = true;
                    }
                    if (GUILayout.Toggle(oldAltSkin, "Alternate skin", "Button", GUILayout.Width(140)) && !oldAltSkin)
                    {
                        _uiSettings.UseAltSkin = true;
                        changed = true;
                    }
                }
                GUILayout.Label("The alternate skin is Unity's stock GUI skin. The change applies to every VesselPlanner window, in the editor and in flight, and takes effect immediately.");

                GUILayout.Space(10);
                bool oldSolidBackgrounds = _uiSettings.SolidEditorWindowBackgrounds;
                bool newSolidBackgrounds = GUILayout.Toggle(oldSolidBackgrounds, "Use solid backgrounds for all editor windows");
                if (newSolidBackgrounds != oldSolidBackgrounds)
                {
                    _uiSettings.SolidEditorWindowBackgrounds = newSolidBackgrounds;
                    changed = true;
                }
                GUILayout.Label("Applies to the main VesselPlanner, its Settings window, and all Stage-By-Stage editor windows. Flight windows are unchanged.");
                GUILayout.Label("Main window width: drag the grip on the right edge to resize from 1150 to 1850 px. The selected width is remembered.");

                GUILayout.Space(8);
                bool oldShowTooltips = _uiSettings.ShowTooltips;
                bool newShowTooltips = GUILayout.Toggle(oldShowTooltips, "Show tooltips");
                if (newShowTooltips != oldShowTooltips)
                {
                    _uiSettings.ShowTooltips = newShowTooltips;
                    _missionPlanner.TooltipsEnabled = newShowTooltips;
                    changed = true;
                }
                GUILayout.Label("Controls VesselPlanner hover help, including Part Images settings and Mission Planner button tooltips.");

                GUILayout.Space(12);
                SettingsSectionHeader("Part images");
                GUILayout.Label("Changes apply immediately to newly rendered list icons and rotating hover previews.");

                float numericValue;
                if (SettingsFloatField("ZoomFactor for icons", ref _iconZoomFactorText, _uiSettings.IconZoomFactor, 0.1f, 5f,
                    "Controls the camera zoom used for static list icons. Lower values make the part image larger; higher values make it smaller.", out numericValue))
                {
                    _uiSettings.IconZoomFactor = numericValue;
                    changed = true;
                    partImageSettingsChanged = true;
                }
                if (SettingsFloatField("ZoomFactor for Rotating Images", ref _rotatingImageZoomFactorText, _uiSettings.RotatingImageZoomFactor, 0.1f, 5f,
                    "Controls the camera zoom used for the enlarged rotating hover preview. Lower values make the part image larger; higher values make it smaller.", out numericValue))
                {
                    _uiSettings.RotatingImageZoomFactor = numericValue;
                    changed = true;
                    partImageSettingsChanged = true;
                }
                if (SettingsFloatField("Camera Yaw Degrees", ref _cameraYawDegreesText, _uiSettings.CameraYawDegrees, 0f, 180f,
                    "Sets the horizontal camera viewing angle around the part, from 0 to 180 degrees.", out numericValue))
                {
                    _uiSettings.CameraYawDegrees = numericValue;
                    changed = true;
                    partImageSettingsChanged = true;
                }
                if (SettingsFloatField("Camera Pitch Degrees", ref _cameraPitchDegreesText, _uiSettings.CameraPitchDegrees, 0f, 90f,
                    "Sets the vertical camera viewing angle for the part, from 0 to 90 degrees.", out numericValue))
                {
                    _uiSettings.CameraPitchDegrees = numericValue;
                    changed = true;
                    partImageSettingsChanged = true;
                }

                int integerValue;
                if (SettingsIntField("RotatingPreviewSize", ref _rotatingPreviewSizeText, _uiSettings.RotatingPreviewSize, 32, 256,
                    "Sets the pixel resolution of each rotating preview frame. Higher values are sharper but use more memory and take longer to render.", out integerValue))
                {
                    _uiSettings.RotatingPreviewSize = integerValue;
                    changed = true;
                    partImageSettingsChanged = true;
                }
                if (SettingsIntField("Degrees per frame", ref _rotatingPreviewDegreesPerFrameText, _uiSettings.RotatingPreviewDegreesPerFrame, 1, 180,
                    "Controls how quickly the rotating preview advances through its pre-rendered orientations. Higher values rotate the displayed part faster.", out integerValue))
                {
                    _uiSettings.RotatingPreviewDegreesPerFrame = integerValue;
                    changed = true;
                    partImageSettingsChanged = true;
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Rotating Image Background", GUILayout.Width(220));
                    int oldBackground = _uiSettings.RotatingImageBackground;
                    int newBackground = ComboBox.Box(
                        RotatingImageBackgroundComboId,
                        oldBackground,
                        RotatingImageBackgroundNames,
                        this,
                        220f,
                        false,
                        false);
                    if (newBackground != oldBackground)
                    {
                        _uiSettings.RotatingImageBackground = newBackground;
                        changed = true;
                        partImageSettingsChanged = true;
                    }
                }
                GUILayout.Label("Controls only the enlarged rotating hover preview. Static list icons remain transparent.");

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(224);
                    if (GUILayout.Button("Reset image settings", GUILayout.Width(160)))
                    {
                        _uiSettings.IconZoomFactor = 0.8f;
                        _uiSettings.RotatingImageZoomFactor = 1.0f;
                        _uiSettings.CameraYawDegrees = 45f;
                        _uiSettings.CameraPitchDegrees = 20f;
                        _uiSettings.RotatingImageBackground = 0;
                        _uiSettings.RotatingPreviewSize = 100;
                        _uiSettings.RotatingPreviewDegreesPerFrame = 60;
                        SyncPartImageSettingText();
                        changed = true;
                        partImageSettingsChanged = true;
                    }
                }

                GUILayout.Space(10);
                SettingsSectionHeader("Detail pane sizes");
                GUILayout.Label("In Planning, drag the grip between Selected Engine and Tanks to change the pane widths, and the grip above them to give the engine list more room. In Analyze Existing, drag the grip above Selected Engine to trade height between the candidate engine list and the pane below it. All are remembered between sessions.");
                GUILayout.Space(4);
                if (GUILayout.Button("Reset pane sizes", GUILayout.Width(160)))
                {
                    _uiSettings.PlanningEnginePaneFraction = 0.35f;
                    _uiSettings.AnalysisListHeightOffset = 0f;
                    _uiSettings.PlanningListHeightOffset = 0f;
                    changed = true;
                }
            }
            GUILayout.EndScrollView();

            CommonRoutines.DrawTooltip(_uiSettings.ShowTooltips, _settingsWindow.width, _settingsWindow.height);
            if (partImageSettingsChanged) ApplyPartImageSettings();
            if (changed) _uiSettings.Save();
            // Allow the Settings window to be dragged from any unused/background
            // area instead of limiting dragging to the title strip. Controls still
            // receive their normal clicks before DragWindow sees the event.
            GUI.DragWindow(new Rect(0f, 0f, _settingsWindow.width, _settingsWindow.height));
        }

        private static void SettingsSectionHeader(string text)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label(text, style);
        }

        private void SyncPartImageSettingText()
        {
            _iconZoomFactorText = _uiSettings.IconZoomFactor.ToString("0.###", CultureInfo.InvariantCulture);
            _rotatingImageZoomFactorText = _uiSettings.RotatingImageZoomFactor.ToString("0.###", CultureInfo.InvariantCulture);
            _cameraYawDegreesText = _uiSettings.CameraYawDegrees.ToString("0.###", CultureInfo.InvariantCulture);
            _cameraPitchDegreesText = _uiSettings.CameraPitchDegrees.ToString("0.###", CultureInfo.InvariantCulture);
            _rotatingPreviewSizeText = _uiSettings.RotatingPreviewSize.ToString(CultureInfo.InvariantCulture);
            _rotatingPreviewDegreesPerFrameText = _uiSettings.RotatingPreviewDegreesPerFrame.ToString(CultureInfo.InvariantCulture);
        }

        private void ApplyPartImageSettings()
        {
            PartThumbnailCache.Configure(
                _uiSettings.IconZoomFactor,
                _uiSettings.RotatingImageZoomFactor,
                _uiSettings.CameraYawDegrees,
                _uiSettings.CameraPitchDegrees,
                _uiSettings.RotatingImageBackground,
                _uiSettings.RotatingPreviewSize,
                _uiSettings.RotatingPreviewDegreesPerFrame);
        }

        private void RegisterSettingsTooltip(Rect controlRect, string tooltip)
        {
            if (!_uiSettings.ShowTooltips || string.IsNullOrEmpty(tooltip)) return;
            GUI.Label(controlRect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
        }

        private bool SettingsFloatField(string label, ref string text, float currentValue, float minValue, float maxValue, string tooltip, out float newValue)
        {
            newValue = currentValue;
            float sliderValue;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(220));
                string edited = GUILayout.TextField(text ?? string.Empty, GUILayout.Width(70));
                Rect textFieldRect = GUILayoutUtility.GetLastRect();
                RegisterSettingsTooltip(textFieldRect, tooltip);
                if (!string.Equals(edited, text, StringComparison.Ordinal)) text = edited;
                using (new GUILayout.VerticalScope(GUILayout.Width(180)))
                {
                    GUILayout.Space(5);
                    sliderValue = GUILayout.HorizontalSlider(currentValue, minValue, maxValue, GUILayout.Width(180));
                    Rect sliderRect = GUILayoutUtility.GetLastRect();
                    RegisterSettingsTooltip(sliderRect, tooltip);
                }
                GUILayout.Label(minValue.ToString("0.###", CultureInfo.InvariantCulture) + " - " + maxValue.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(85));
            }

            // The slider wins when it moved this frame and keeps the text box synchronized.
            if (Math.Abs(sliderValue - currentValue) > 0.0001f)
            {
                newValue = Mathf.Clamp(sliderValue, minValue, maxValue);
                text = newValue.ToString("0.###", CultureInfo.InvariantCulture);
                return true;
            }

            float parsed;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) || float.IsNaN(parsed) || float.IsInfinity(parsed))
                return false;

            float clamped = Mathf.Clamp(parsed, minValue, maxValue);
            if (Math.Abs(clamped - parsed) > 0.0001f)
                text = clamped.ToString("0.###", CultureInfo.InvariantCulture);

            if (Math.Abs(clamped - currentValue) <= 0.0001f)
                return false;

            newValue = clamped;
            return true;
        }

        private bool SettingsIntField(string label, ref string text, int currentValue, int minValue, int maxValue, string tooltip, out int newValue)
        {
            newValue = currentValue;
            float sliderValue;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(220));
                string edited = GUILayout.TextField(text ?? string.Empty, GUILayout.Width(70));
                Rect textFieldRect = GUILayoutUtility.GetLastRect();
                RegisterSettingsTooltip(textFieldRect, tooltip);
                if (!string.Equals(edited, text, StringComparison.Ordinal)) text = edited;
                using (new GUILayout.VerticalScope(GUILayout.Width(180)))
                {
                    GUILayout.Space(5);
                    sliderValue = GUILayout.HorizontalSlider(currentValue, minValue, maxValue, GUILayout.Width(180));
                    Rect sliderRect = GUILayoutUtility.GetLastRect();
                    RegisterSettingsTooltip(sliderRect, tooltip);
                }
                GUILayout.Label(minValue.ToString(CultureInfo.InvariantCulture) + " - " + maxValue.ToString(CultureInfo.InvariantCulture), GUILayout.Width(85));
            }

            int sliderInt = Mathf.Clamp(Mathf.RoundToInt(sliderValue), minValue, maxValue);
            if (sliderInt != currentValue)
            {
                newValue = sliderInt;
                text = sliderInt.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            int parsed;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                return false;

            int clamped = Mathf.Clamp(parsed, minValue, maxValue);
            if (clamped != parsed) text = clamped.ToString(CultureInfo.InvariantCulture);
            if (clamped == currentValue) return false;

            newValue = clamped;
            return true;
        }

        private bool SettingsColumnToggle(SolutionColumn column, string label)
        {
            bool oldValue = _uiSettings.ColumnVisible(column);
            bool newValue = GUILayout.Toggle(oldValue, label);
            if (newValue == oldValue) return false;
            _uiSettings.SetColumnVisible(column, newValue);
            return true;
        }

        private bool SettingsAnalysisToggle(AnalysisLine line, string label)
        {
            bool oldValue = _uiSettings.AnalysisLineVisible(line);
            bool newValue = GUILayout.Toggle(oldValue, label);
            if (newValue == oldValue) return false;
            _uiSettings.SetAnalysisLineVisible(line, newValue);
            return true;
        }

        private void DrawSelectedEnginePane(bool compact, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical("box", options);
            using (new GUILayout.HorizontalScope())
            {
                string selectedEngineHeader = "Selected Engine: " + _selected.Engine.DisplayName + " × " + _selected.EngineCount;
                GUILayout.Label(selectedEngineHeader);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Engine", GUILayout.Width(110))) SpawnEngine(_selected);
                // Only meaningful while a stage-by-stage stage is open: it fills that stage with
                // the engine and the tank the suggestions currently recommend in one press.
                // A stage must be open and a tank suggestion must be explicitly selected before
                // the combined add operation can run. This prevents the first row from being used
                // implicitly when the user has not chosen a tank.
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && StagePlan.IsCapturing && _selectedTank != null;
                if (GUILayout.Button("Add Engine & Tanks", GUILayout.Width(150))) AddEngineAndTanksToPlan();
                GUI.enabled = previousEnabled;
            }

            if (_selectedTank != null)
            {
                GUILayout.Label("Selected Tanks: " + _selectedTank.TankSummary);
            }

            // Give the selected-engine contents a little breathing room below the
            // header rows before the detail scroll area begins.
            GUILayout.Space(10);
            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.Height(compact ? PlanningEngineScrollHeight() : AnalysisEngineScrollHeight()));
            if (compact)
            {
                // In Planning this pane shares the row with Tanks, so use one detail
                // column to keep the selected-engine pane readable in the narrower left pane.
                Row("Burnable propellant", F(_selected.PropellantMassTons) + " t");
                Row("Estimated tank dry mass", F(_selected.TankDryMassTons) + " t");
                Row("Engine mass", F(_selected.EngineMassTons) + " t");
                Row("Stage wet mass", F(_selected.StageWetMassTons) + " t");
                Row("Start mass", F(_selected.StartMassTons) + " t");
                Row("End mass", F(_selected.DryMassTons) + " t");
                Row("Atmosphere Δv", _selected.AtmosphericDeltaV.ToString("0") + " m/s");
                Row("Vacuum Δv", _selected.VacuumDeltaV.ToString("0") + " m/s");
                Row("Thrust/engine ASL / Vac", F(_selected.Engine.SeaLevelThrustKn) + " / " + F(_selected.Engine.MaxThrustVacuumKn) + " kN");
                Row("Total thrust ASL / Vac", F(_selected.SeaLevelThrustKn) + " / " + F(_selected.VacuumThrustKn) + " kN");
                Row("Isp ASL / Vac", _selected.Engine.SeaLevelIsp.ToString("0") + " / " + _selected.Engine.VacuumIsp.ToString("0") + " s");
                Row("Cost efficiency", _selected.CostEfficiency.ToString("0.###", CultureInfo.InvariantCulture) + " Δv/Fund");
                Row("TWR initial / final", _selected.InitialTwr.ToString("0.00") + " / " + _selected.FinalTwr.ToString("0.00"));
                Row("Max TWR (vacuum)", _selected.MaxTwr.ToString("0.00"));
                Row("Burn", _selected.BurnTimeSeconds.ToString("0.0") + " s");
                Row("Resource volume", _selected.TankVolumeLiters > 0 ? F(_selected.TankVolumeLiters) + " L" : "0 L / undefined");
            }
            else
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.BeginVertical();
                    Row("Burnable propellant", F(_selected.PropellantMassTons) + " t");
                    Row("Estimated tank dry mass", F(_selected.TankDryMassTons) + " t");
                    Row("Engine mass", F(_selected.EngineMassTons) + " t");
                    Row("Stage wet mass", F(_selected.StageWetMassTons) + " t");
                    Row("Start mass", F(_selected.StartMassTons) + " t");
                    Row("End mass", F(_selected.DryMassTons) + " t");
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    Row("Atmosphere Δv", _selected.AtmosphericDeltaV.ToString("0") + " m/s");
                    Row("Vacuum Δv", _selected.VacuumDeltaV.ToString("0") + " m/s");
                    Row("Thrust/engine ASL / Vac", F(_selected.Engine.SeaLevelThrustKn) + " / " + F(_selected.Engine.MaxThrustVacuumKn) + " kN");
                    Row("Total thrust ASL / Vac", F(_selected.SeaLevelThrustKn) + " / " + F(_selected.VacuumThrustKn) + " kN");
                    Row("Isp ASL / Vac", _selected.Engine.SeaLevelIsp.ToString("0") + " / " + _selected.Engine.VacuumIsp.ToString("0") + " s");
                    Row("Cost efficiency", _selected.CostEfficiency.ToString("0.###", CultureInfo.InvariantCulture) + " Δv/Fund");
                    Row("TWR initial / final", _selected.InitialTwr.ToString("0.00") + " / " + _selected.FinalTwr.ToString("0.00"));
                    Row("Max TWR (vacuum)", _selected.MaxTwr.ToString("0.00"));
                    Row("Burn", _selected.BurnTimeSeconds.ToString("0.0") + " s");
                    Row("Resource volume", _selected.TankVolumeLiters > 0 ? F(_selected.TankVolumeLiters) + " L" : "0 L / undefined");
                    GUILayout.EndVertical();
                }
            }
            GUILayout.Label("Fuel");
            foreach (var p in _selected.Propellants)
                GUILayout.Label(p.ResourceName + (p.IgnoreForIsp ? " (aux)" : "") + ": " + F(p.Units) + " units, " + F(p.MassTons) + " t" + (p.VolumeLiters > 0 ? ", " + F(p.VolumeLiters) + " L" : ""));
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawTankSuggestionsPane(params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical("box", options);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Tanks");
                GUILayout.FlexibleSpace();
                GUILayout.Label("Tanks needed for the selected propellant requirement");
            }
            DrawTankSuggestions();
            GUILayout.EndVertical();
        }

        private void DrawTankSuggestions()
        {
            // Match the engine filter layout: include and exclude filters share one row.
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Filter:", GUILayout.Width(52f));
                string oldFilter = _tankNameFilter;
                _tankNameFilter = GUILayout.TextField(_tankNameFilter ?? string.Empty, GUILayout.Width(220f));
                if (!string.Equals(oldFilter, _tankNameFilter, StringComparison.Ordinal))
                {
                    SaveTankFilterIfEnabled();
                    if (_selectedTank != null && !TankMatchesTextFilter(_selectedTank))
                        _selectedTank = null;
                }

                GUILayout.Label("Exclude:", GUILayout.Width(65f));
                string oldExclude = _tankExcludeFilter;
                _tankExcludeFilter = GUILayout.TextField(_tankExcludeFilter ?? string.Empty, GUILayout.ExpandWidth(true));
                if (!string.Equals(oldExclude, _tankExcludeFilter, StringComparison.Ordinal))
                {
                    SaveTankExcludeFilterIfEnabled();
                    if (_selectedTank != null && !TankMatchesTextFilter(_selectedTank))
                        _selectedTank = null;
                }
            }

            if (_tankSuggestions.Count == 0)
            {
                GUILayout.Label("No compatible one-, two-, or three-type tank set can provide all required engine resources.");
                return;
            }

            List<TankSuggestion> visibleTanks = _tankSuggestions
                .Where(TankMatchesTextFilter)
                .Take(100)
                .ToList();
            if (visibleTanks.Count == 0)
            {
                GUILayout.Label("No tank suggestions match the filter.");
                return;
            }

            // The tank table occupies the wider right-hand Planning detail pane.
            // These widths favor the descriptive columns while retaining room for the
            // complete Add Tank button at the planner's minimum supported width.
            const float tankNameWidth = 250f;
            const float countWidth = 26f;
            const float dryMassWidth = 55f;
            const float excessWidth = 65f;
            const float bulkheadWidth = 70f;
            const float capacityWidth = 170f;
            const float addWidth = 95f;

            using (new GUILayout.HorizontalScope())
            {
                TankSortHeader("Tank Set", tankNameWidth, TankSortColumn.Tank);
                TankSortHeader("#", countWidth, TankSortColumn.Count);
                TankSortHeader("Dry t", dryMassWidth, TankSortColumn.DryMass);
                TankSortHeader("Excess", excessWidth, TankSortColumn.Excess);
                TankSortHeader("Bulkhead", bulkheadWidth, TankSortColumn.Bulkhead);
                TankSortHeader("Capacity", capacityWidth, TankSortColumn.Capacity);
                Header("", addWidth);
            }
            _tankScroll = GUILayout.BeginScrollView(_tankScroll, GUILayout.Height(TankListScrollHeight()));
            foreach (TankSuggestion t in visibleTanks)
            {
                bool selected = ReferenceEquals(t, _selectedTank);
                if (selected) GUILayout.BeginHorizontal("box"); else GUILayout.BeginHorizontal();
                TankSelectionCellWithThumbnails(t, tankNameWidth);
                TankSelectionCell(t, t.Count.ToString(), countWidth);
                TankSelectionCell(t, F(t.TotalDryMassTons), dryMassWidth);
                TankSelectionCell(t, (t.ExcessFraction * 100.0).ToString("0.0") + "%", excessWidth);
                TankSelectionCell(t, TankSuggestionBulkheadLabel(t), bulkheadWidth);
                TankSelectionCell(t, t.CapacitySummary, capacityWidth);
                string addLabel = StagePlan.IsCapturing
                    ? "Add to Stage"
                    : (t.DifferentTankTypes > 1 ? "Add Set" : "Add Tank");
                if (GUILayout.Button(addLabel, GUILayout.Width(addWidth))) SpawnTank(t);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.Label("Tank sets may use up to three different tank types, all with the same bulkhead profile. # is the total number of tanks. In Stage-By-Stage, Add Engine & Tanks captures the complete selected set.");
        }

        private void TankSelectionCellWithThumbnails(TankSuggestion tank, float width)
        {
            if (tank == null) return;

            List<TankSuggestionPart> parts = tank.Tanks
                .Where(item => item != null && item.Tank != null && item.Count > 0)
                .Take(3)
                .ToList();

            const float thumbnailSize = 28f;
            float textWidth = Mathf.Max(50f, width - (parts.Count * thumbnailSize) - 6f);
            bool selected = false;
            using (new GUILayout.HorizontalScope(GUILayout.Width(width), GUILayout.Height(thumbnailSize)))
            {
                foreach (TankSuggestionPart item in parts)
                    DrawPartThumbnail(item.Tank.PartName, item.Tank.PartUrl, thumbnailSize);

                selected = GUILayout.Button(
                    new GUIContent(tank.TankSummary, tank.TankSummary),
                    GUI.skin.label,
                    GUILayout.Width(textWidth),
                    GUILayout.Height(thumbnailSize));
            }

            if (!selected) return;
            _selectedTank = tank;
            _status = "Selected tanks: " + tank.TankSummary + ".";
        }

        private void DrawPartThumbnail(string partName, float size)
        {
            DrawPartThumbnail(partName, null, size);
        }

        private void DrawPartThumbnail(string partName, string partUrl, float size)
        {
            // Always reserve the rectangle during Layout and Repaint. The texture itself is
            // generated lazily only during Repaint so thumbnail creation cannot change the
            // IMGUI control tree between event passes.
            Rect thumbnailRect = GUILayoutUtility.GetRect(
                size, size,
                GUILayout.Width(size),
                GUILayout.Height(size));

            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            Texture2D thumbnail = PartThumbnailCache.Get(partName, partUrl);
            if (thumbnail != null)
                GUI.DrawTexture(thumbnailRect, thumbnail, ScaleMode.ScaleToFit, true);

            if (thumbnail == null || !thumbnailRect.Contains(Event.current.mousePosition)) return;

            Texture2D preview = PartThumbnailCache.GetRotatingPreview(partName, partUrl);
            if (preview == null) preview = thumbnail;
            _hoverPartPreviewTexture = preview;

            Vector2 topLeft = GUIUtility.GUIToScreenPoint(new Vector2(thumbnailRect.xMin, thumbnailRect.yMin));
            Vector2 bottomRight = GUIUtility.GUIToScreenPoint(new Vector2(thumbnailRect.xMax, thumbnailRect.yMax));
            _hoverPartPreviewSourceScreenRect = Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
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

        private void TankSelectionCell(TankSuggestion tank, string text, float width)
        {
            if (!GUILayout.Button(text, GUI.skin.label, GUILayout.Width(width))) return;
            _selectedTank = tank;
            _status = "Selected tanks: " + tank.TankSummary + ".";
        }

        private bool TankMatchesTextFilter(TankSuggestion suggestion)
        {
            if (suggestion == null || suggestion.Tanks == null || suggestion.Tanks.Count == 0) return false;
            string[] includeTerms = SplitFilterTerms(_tankNameFilter);
            string[] excludeTerms = SplitFilterTerms(_tankExcludeFilter);

            bool includeMatch = includeTerms.Length == 0 || suggestion.Tanks.Any(item =>
                item != null && item.Tank != null && TextMatchesAnyFilterTerm(item.Tank.DisplayName, item.Tank.PartName, includeTerms));
            if (!includeMatch) return false;

            return !suggestion.Tanks.Any(item =>
                item != null && item.Tank != null && TextMatchesAnyFilterTerm(item.Tank.DisplayName, item.Tank.PartName, excludeTerms));
        }

        private void SelectSolution(StageSolution s)
        {
            _selected = s;
            _tankSuggestions = _planningMode ? TankPlanner.Suggest(ApplyTankBulkheadFilter(_tanks), _selected) : new List<TankSuggestion>();
            _selectedTank = null;
            ApplyTankSort();
        }

        // Leaving the mode puts the plan window away and closes whatever stage was being
        // built. The plan itself is kept, so returning to the mode brings it back.
        private void LeaveStageByStage()
        {
            if (!_stageByStageMode) return;
            _stageByStageMode = false;
            StagePlan.Visible = false;
            StagePlan.CloseOpenStage();
        }

        private StagePlanWindow StagePlan
        {
            get
            {
                if (_stagePlan == null) _stagePlan = new StagePlanWindow(this);
                return _stagePlan;
            }
        }

        internal Vector2 StagePlanOpenPosition
        {
            get
            {
                // The Stage-By-Stage plan opens left-aligned with the main planner and
                // immediately below the mode-button row. The Stage-By-Stage button is in
                // that row, so its bottom edge is the most reliable live anchor.
                float buttonBottom = _stageByStageButtonWindowRect.height > 0f
                    ? _stageByStageButtonWindowRect.yMax
                    : 50f;
                return new Vector2(_window.x, _window.y + buttonBottom);
            }
        }

        internal string SelectedBodyName
        {
            get { return BodyName(SelectedBody); }
        }

        // Selects an environment body by display/name without triggering an intermediate
        // recalculation. Stage-By-Stage uses this immediately before its own Calculate call.
        internal bool SetSelectedBodyByName(string bodyName)
        {
            if (string.IsNullOrWhiteSpace(bodyName)) return false;
            if (_bodies.Count == 0) RefreshBodies();

            int index = _bodies.FindIndex(b =>
                string.Equals(BodyName(b), bodyName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(b == null ? string.Empty : b.name, bodyName, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return false;

            _selectedBodyIndex = index;
            _altitudeMeters = Math.Min(_altitudeMeters, GetAtmosphereDepth(SelectedBody));
            return true;
        }

        // Used by the Stage-By-Stage New/Edit Stage dialog after Calculate.  The request
        // is serviced after StagePlan.Draw() has completed so the modal window has closed
        // before the main planner is raised.
        internal void RequestBringToFront()
        {
            _bringMainToFrontRequested = true;
        }

        // Loads one planned stage's requirements into Planning mode and solves it, so the
        // candidate list is already populated when the user turns to this window.
        internal void BeginPlannedStage(double targetDeltaV, DeltaVBasis basis, double minimumTwr, int maxEngines, double payloadTons, double cargoMassTons, double decouplerMassTons, double assemblyMassTons, string bodyName)
        {
            _planningMode = true;
            _stageByStageMode = true;
            _targetDv = targetDeltaV.ToString("0.###", CultureInfo.InvariantCulture);
            _minTwr = minimumTwr.ToString("0.###", CultureInfo.InvariantCulture);
            _maxEngines = Math.Max(1, maxEngines).ToString(CultureInfo.InvariantCulture);
            _uiSettings.TargetDeltaVBasis = basis;

            // The plan carries its own payload, which is what the stage has to lift.
            _useCraftPayload = false;
            _payload = payloadTons.ToString("0.###", CultureInfo.InvariantCulture);
            _plannedStageCargoMassTons = Math.Max(0.0, cargoMassTons);
            _plannedStageDecouplerMassTons = Math.Max(0.0, decouplerMassTons);
            _plannedStageAssemblyMassTons = Math.Max(0.0, assemblyMassTons);

            if (!string.IsNullOrWhiteSpace(bodyName))
                SetSelectedBodyByName(bodyName);

            Visible = true;
            Calculate();
        }

        // Adds the selected engine and the explicitly selected tank suggestion to the open
        // plan stage. The button is disabled until a tank is selected, but keep the guard
        // here as well so stale/programmatic calls cannot silently use another tank.
        private void AddEngineAndTanksToPlan()
        {
            if (_selected == null || _selected.Engine == null) return;
            if (_selectedTank == null || !_tankSuggestions.Contains(_selectedTank))
            {
                _status = "Select a tank before using Add Engine & Tanks.";
                return;
            }

            TankSuggestion tank = _selectedTank;
            bool captured = StagePlan.CaptureEngineAndTanks(
                _selected.Engine.PartName,
                _selected.Engine.PartUrl,
                _selected.Engine.DisplayName,
                Math.Max(1, _selected.EngineCount),
                tank.Tanks);

            if (!captured)
            {
                _status = "Add Engine & Tanks only applies while a Stage-By-Stage stage is open.";
                return;
            }

            _status = "Added the engine and tank set " + tank.TankSummary + " to " + StagePlan.CapturingStageLabel + ".";
        }

        private void SpawnEngine(StageSolution s)
        {
            // While a stage is being built the part goes into the plan instead of onto the
            // editor cursor; the plan places its parts itself once it is finalised.
            if (s != null && s.Engine != null && StagePlan.CapturePart(s.Engine.PartName, s.Engine.PartUrl, s.Engine.DisplayName, Math.Max(1, s.EngineCount), true))
            {
                _status = "Added " + s.EngineCount + " x " + s.Engine.DisplayName + " to " + StagePlan.CapturingStageLabel + ".";
                return;
            }

            string message;
            bool spawned = EditorPartSpawner.Spawn(s.Engine.PartName, out message);
            _status = message;

            // Close only after KSP successfully selects the engine for placement.
            // Analyze Existing and Planning keep separate persistent preferences.
            if (spawned && ((!_planningMode && _uiSettings.CloseAnalyzeExistingAfterAdd) ||
                            (_planningMode && _uiSettings.ClosePlanningAfterAdd)))
            {
                Visible = false;
                _settingsVisible = false;
                CancelStagePartPick(null);
            }
        }

        private void SpawnTank(TankSuggestion t)
        {
            if (t == null || t.Tanks == null || t.Tanks.Count == 0) return;

            if (StagePlan.IsCapturing)
            {
                bool capturedAny = false;
                foreach (TankSuggestionPart item in t.Tanks)
                {
                    if (item == null || item.Tank == null || item.Count <= 0) continue;
                    capturedAny |= StagePlan.CapturePart(item.Tank.PartName, item.Tank.PartUrl, item.Tank.DisplayName, item.Count, false);
                }
                if (capturedAny)
                {
                    _status = "Added tank set " + t.TankSummary + " to " + StagePlan.CapturingStageLabel + ".";
                    return;
                }
            }

            TankSuggestionPart first = t.Tanks.FirstOrDefault(item => item != null && item.Tank != null && item.Count > 0);
            if (first == null) return;
            string message;
            bool spawned = EditorPartSpawner.Spawn(first.Tank.PartName, out message);
            _status = t.DifferentTankTypes > 1
                ? message + " Mixed set: place " + t.TankSummary + " as indicated."
                : message;

            if (spawned && _planningMode && _uiSettings.ClosePlanningAfterAdd)
            {
                Visible = false;
                _settingsVisible = false;
                CancelStagePartPick(null);
            }
        }

        private void SimulateExisting()
        {
            // Re-read the editor exclusion-filter pipeline each time. This allows mods
            // which change their part visibility dynamically to affect candidates without
            // requiring VesselPlanner to be reopened.
            _engines = EngineDatabase.ScanAvailableEngines();

            double twr;
            int max;
            if (!CommonRoutines.TryParseDouble(_minTwr, out twr) || !int.TryParse(_maxEngines, out max))
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
            _selectedTank = null;
            _status = _solutions.Count + " compatible engine configurations at " + BodyName(SelectedBody) + " " + FormatAltitude(_altitudeMeters) + ".";
        }

        private void Calculate()
        {
            // Re-read the editor exclusion-filter pipeline each time. This allows mods
            // which change their part visibility dynamically to affect candidates without
            // requiring VesselPlanner to be reopened.
            _engines = EngineDatabase.ScanAvailableEngines();

            double dv, twr, payload, ratio;
            int max;
            if (!CommonRoutines.TryParseDouble(_targetDv, out dv) || !CommonRoutines.TryParseDouble(_minTwr, out twr) || !CommonRoutines.TryParseDouble(_payload, out payload) || !CommonRoutines.TryParseDouble(_tankRatio, out ratio) || !int.TryParse(_maxEngines, out max))
            { _status = "Check numeric inputs."; return; }

            if (_snapshot == null) RefreshStage();
            double atmospheres, temperatureK, density;
            GetSelectedAtmosphereEnvironment(out atmospheres, out temperatureK, out density);
            var req = new StageRequirements
            {
                PayloadDryMassTons = _useCraftPayload ? _snapshot.PayloadAboveStageMassTons : payload,
                OtherStageDryMassTons = StagePlan.IsCapturing ? _plannedStageCargoMassTons + _plannedStageDecouplerMassTons + _plannedStageAssemblyMassTons : 0.0,
                TargetDeltaV = dv,
                TargetDeltaVBasis = _uiSettings.TargetDeltaVBasis,
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
            _tankSuggestions = _selected != null ? TankPlanner.Suggest(ApplyTankBulkheadFilter(_tanks), _selected) : new List<TankSuggestion>();
            _selectedTank = null;
            ApplyTankSort();
            _status = _solutions.Count + " valid configurations at " + BodyName(SelectedBody) + " " + FormatAltitude(_altitudeMeters) + "; " + _tankSuggestions.Count + " matching tank choices for the selected result.";
        }

        private IEnumerable<EngineCandidate> ApplyEngineNameFilter(IEnumerable<EngineCandidate> candidates)
        {
            string[] includeTerms = SplitFilterTerms(_engineNameFilter);
            string[] excludeTerms = SplitFilterTerms(_engineExcludeFilter);
            if (includeTerms.Length == 0 && excludeTerms.Length == 0) return candidates;

            return candidates.Where(e =>
                (includeTerms.Length == 0 || TextMatchesAnyFilterTerm(e.DisplayName, e.PartName, includeTerms)) &&
                !TextMatchesAnyFilterTerm(e.DisplayName, e.PartName, excludeTerms));
        }

        private static string[] SplitFilterTerms(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return new string[0];
            return filter.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(term => term.Trim())
                .Where(term => term.Length > 0)
                .ToArray();
        }

        private static bool TextMatchesAnyFilterTerm(string displayName, string partName, string[] terms)
        {
            if (terms == null || terms.Length == 0) return false;
            string display = displayName ?? string.Empty;
            string part = partName ?? string.Empty;
            return terms.Any(term =>
                display.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                part.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void SaveEngineFilterIfEnabled()
        {
            if (!_uiSettings.SaveEngineFilter) return;
            _uiSettings.EngineFilter = _engineNameFilter;
            _uiSettings.Save();
        }

        private void SaveEngineExcludeFilterIfEnabled()
        {
            if (!_uiSettings.SaveEngineExcludeFilter) return;
            _uiSettings.EngineExcludeFilter = _engineExcludeFilter;
            _uiSettings.Save();
        }

        private void SaveTankFilterIfEnabled()
        {
            if (!_uiSettings.SaveTankFilter) return;
            _uiSettings.TankFilter = _tankNameFilter;
            _uiSettings.Save();
        }

        private void SaveTankExcludeFilterIfEnabled()
        {
            if (!_uiSettings.SaveTankExcludeFilter) return;
            _uiSettings.TankExcludeFilter = _tankExcludeFilter;
            _uiSettings.Save();
        }

        private void RecalculateForFilterChange()
        {
            // Keep the results list live while typing without requiring a separate Calculate click.
            if (_planningMode) Calculate(); else SimulateExisting();
        }

        // The stack size an engine's top node mates with, named by BulkheadProfiles.cfg.
        private static string BulkheadLabel(EngineCandidate engine)
        {
            return engine == null ? "none" : BulkheadProfiles.LabelFor(engine.TopNodeSize);
        }

        private IEnumerable<EngineCandidate> ApplyBulkheadFilter(IEnumerable<EngineCandidate> candidates)
        {
            // A stage being built stage-by-stage has no craft to read a bulkhead from, so its
            // own list of sizes stands in for the stage match while it is open.
            IList<string> plannedProfiles = StagePlan.ActiveBulkheadProfiles;
            if (plannedProfiles != null)
            {
                if (plannedProfiles.Count == 0) return candidates;
                var allowedProfiles = new HashSet<string>(plannedProfiles, StringComparer.OrdinalIgnoreCase);
                return candidates.Where(e => EngineMatchesBulkheadProfiles(e, allowedProfiles));
            }

            if (!_filterByBulkheadSize || _snapshot == null || _snapshot.TopNodeSizes.Count == 0)
                return candidates;

            var stageTopNodeSizes = new HashSet<int>(_snapshot.TopNodeSizes);
            return candidates.Where(e => e.TopNodeSize >= 0 && stageTopNodeSizes.Contains(e.TopNodeSize));
        }

        private static bool EngineMatchesBulkheadProfiles(EngineCandidate engine, HashSet<string> allowedProfiles)
        {
            if (engine == null || allowedProfiles == null || allowedProfiles.Count == 0) return true;
            foreach (string profile in engine.BulkheadProfiles)
                if (allowedProfiles.Contains(profile)) return true;

            // Keep compatibility with engines whose part-wide bulkheadProfiles omits the
            // stack token but whose top attach node still carries a usable size.
            string topProfile = BulkheadProfiles.ProfileForSize(engine.TopNodeSize);
            return !string.IsNullOrEmpty(topProfile) && allowedProfiles.Contains(topProfile);
        }

        private IEnumerable<TankCandidate> ApplyTankBulkheadFilter(IEnumerable<TankCandidate> tanks)
        {
            IList<string> plannedProfiles = StagePlan.ActiveBulkheadProfiles;
            if (plannedProfiles == null || plannedProfiles.Count == 0) return tanks;

            var allowedProfiles = new HashSet<string>(plannedProfiles, StringComparer.OrdinalIgnoreCase);
            return tanks.Where(t => t != null && t.BulkheadProfiles.Any(profile => allowedProfiles.Contains(profile)));
        }

        private static string TankBulkheadLabel(TankCandidate tank)
        {
            if (tank == null || tank.BulkheadProfiles == null || tank.BulkheadProfiles.Count == 0) return "none";
            return string.Join(", ", tank.BulkheadProfiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
        }

        private static string TankSuggestionBulkheadLabel(TankSuggestion suggestion)
        {
            if (suggestion == null) return "none";
            if (!string.IsNullOrEmpty(suggestion.BulkheadProfile)) return suggestion.BulkheadProfile;
            return TankBulkheadLabel(suggestion.PrimaryTank);
        }

        private static string FormatNodeSizes(IEnumerable<int> sizes)
        {
            return string.Join(", ", sizes.OrderBy(x => x).Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
        }


        private void DrawEnvironmentControls()
        {
            CelestialBody body = SelectedBody;
            double maxAltitude = GetAtmosphereDepth(body);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Planet:", GUILayout.Width(70));
                int oldBodyIndex = _selectedBodyIndex;
                string[] bodyEntries = _bodies.Select(BodyName).ToArray();
                _selectedBodyIndex = ComboBox.Box(
                    _planningMode ? PlanningBodyComboId : AnalysisBodyComboId,
                    _selectedBodyIndex,
                    bodyEntries,
                    this,
                    PlanetButtonWidth,
                    false);

                if (_selectedBodyIndex != oldBodyIndex)
                {
                    body = SelectedBody;
                    _altitudeMeters = Math.Min(_altitudeMeters, GetAtmosphereDepth(body));
                    RecalculateForEnvironmentChange();
                }

                GUILayout.Space(12);
                GUILayout.Label("Pressure:", GUILayout.Width(65));
                GUILayout.Label(GetSelectedAtmospheres().ToString("0.####", CultureInfo.InvariantCulture) + " atm", GUILayout.Width(105));
                GUILayout.FlexibleSpace();
            }

            double oldAltitude = 0;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Altitude:", GUILayout.Width(70));
                oldAltitude = _altitudeMeters;
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
            }

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
            RecalculateForFilterChange();
        }

        // The height the candidate engine list is measured against before either mode's
        // offset is applied.  It is taken from the screen and not from the planner window,
        // which is sized by its content: with the window in this expression the list fed its
        // own height back through the window and the two never settled, which showed up as a
        // flickering window until a grip drag pinned the height against one of its clamps.
        private static float ResultsBaseHeight()
        {
            float screenHeight = Screen.height > 0 ? Screen.height : 1080f;
            return Mathf.Clamp(screenHeight * 0.28f, MinEngineListHeight, MaxEngineListHeight);
        }

        private float GetResultsHeight()
        {
            // Both modes offset the base height, so the list can run past the shared
            // MaxEngineListHeight cap in either direction.  In Planning the offset is
            // derived from the Requirements box beside the list; in Analyze Existing it
            // comes from the grip above the Selected Engine pane.
            float offset = _planningMode ? _planningResultsHeightOffset : _uiSettings.AnalysisListHeightOffset;
            return Mathf.Clamp(ResultsBaseHeight() + offset, MinDetailListHeight, MaxDetailListHeight);
        }

        private static string FormatAltitude(double meters)
        {
            if (meters >= 1000.0) return (meters / 1000.0).ToString("0.##", CultureInfo.InvariantCulture) + " km";
            return meters.ToString("0", CultureInfo.InvariantCulture) + " m";
        }

        // Width the Selected Engine + Tanks row has to share in Planning mode, after the
        // window chrome and the vertical splitter between the two panes are removed.
        private float PlanningDetailAvailableWidth()
        {
            return Mathf.Max(2f * MinDetailPaneWidth, _window.width - 28f - SplitterGripThickness - 8f);
        }

        private float PlanningEnginePaneWidth()
        {
            float available = PlanningDetailAvailableWidth();
            float fraction = Mathf.Clamp(_uiSettings.PlanningEnginePaneFraction, MinDetailPaneFraction, MaxDetailPaneFraction);
            float maximum = Mathf.Max(MinDetailPaneWidth, available - MinDetailPaneWidth);
            return Mathf.Clamp(available * fraction, MinDetailPaneWidth, maximum);
        }

        // The Tanks table takes the other half of the grip's drag: the offset that grows
        // the Requirements viewport above shrinks this table by the same amount, so the grip
        // trades height between the two rows instead of resizing only the top one. Dragging
        // up therefore enlarges the Tanks pane, and the Selected Engine list follows because
        // its pane is matched to this one.  In Analyze Existing the engine list is instead
        // sized so its pane ends level with the left diagnostics column.
        private float TankListScrollHeight()
        {
            return Mathf.Clamp(TankListScrollHeightPx - PlanningListHeightOffset(), MinMatchedScrollHeight, MaxMatchedScrollHeight);
        }

        private float PlanningEngineScrollHeight()
        {
            return Mathf.Clamp(_planningEngineScrollHeight, MinMatchedScrollHeight, MaxMatchedScrollHeight);
        }

        // Fixed viewport heights for the Analyze Existing left column.  Both are derived
        // from the screen, never from the planner window, because the window is sized by
        // its content: measuring it here would feed these values their own output.
        private static float AnalysisStageScrollHeight()
        {
            float screenHeight = Screen.height > 0 ? Screen.height : 1080f;
            return Mathf.Clamp(screenHeight * 0.42f, 240f, 520f);
        }

        private float CurrentEnginesScrollHeight()
        {
            int rows = _snapshot == null ? 0 : _snapshot.CurrentEngineDetails.Count;
            // Show the whole list when it is short rather than a scrollbar around two rows.
            return Mathf.Clamp(rows * 22f, 26f, 110f);
        }

        private float AnalysisEngineScrollHeight()
        {
            return Mathf.Clamp(_analysisEngineScrollHeight, MinMatchedScrollHeight, MaxMatchedScrollHeight);
        }

        // Span the width grip across the full pane row once the panes have been measured.
        private float PlanningSplitterHeight()
        {
            if (_planningPaneRowHeight > 1f) return _planningPaneRowHeight;
            return VerticalSplitterHeight(TankListScrollHeight());
        }

        // Correct the Selected Engine scroll height by the difference between the two
        // measured pane heights.  Skipped when the Tanks pane holds no suggestions, since
        // matching that short pane would squeeze the engine details to a few rows.
        private void MatchPlanningPaneHeights()
        {
            if (!_planningMode || _selected == null || _tankSuggestions.Count == 0)
            {
                _planningEngineScrollHeight = TankListScrollHeightPx;
                return;
            }
            if (_measuredPlanningEnginePaneHeight <= 1f || _measuredPlanningTankPaneHeight <= 1f) return;

            float difference = _measuredPlanningTankPaneHeight - _measuredPlanningEnginePaneHeight;
            if (Mathf.Abs(difference) < 1f) return;
            _planningEngineScrollHeight = Mathf.Clamp(PlanningEngineScrollHeight() + difference, MinMatchedScrollHeight, MaxMatchedScrollHeight);

            // Discard the measurements that produced this correction.  Draw() runs once per
            // event, so leaving them in place would apply the same difference again on the
            // next pass before a fresh Repaint has measured the corrected layout.
            _measuredPlanningEnginePaneHeight = 0f;
            _measuredPlanningTankPaneHeight = 0f;
        }

        // How far the Planning grip has been dragged below the aligned position.  Negative
        // values are not stored: dragging up stops once the engine list is level with the
        // Requirements box, since the box beside it cannot shrink any further.
        // How far the grip above the detail panes has been dragged from the default split.
        // The offset is added to the Requirements viewport and taken off the Tanks table, so
        // the two rows always add up to the same total and the window keeps its height. That
        // only holds while both panes are within their limits, so the offset is confined to
        // the range where neither clamps: past that the grip would grow one row without the
        // other giving anything back, and the window would expand.
        private float PlanningListHeightOffset()
        {
            return Mathf.Clamp(_uiSettings.PlanningListHeightOffset, MinPlanningListHeightOffset(), MaxPlanningListHeightOffset());
        }

        private float MinPlanningListHeightOffset()
        {
            return Math.Min(0f, MinPlanningRequirementsViewport() - PlanningRequirementsBaseHeight());
        }

        // The candidate engine list is aligned to the bottom of the Requirements box, but it
        // can only shrink to MinDetailListHeight. Below that the results box is the taller of
        // the two, so it -- not Requirements -- sets the row height: the grip stops with the
        // row, while the Requirements box carries on shrinking and its bottom edge pulls away
        // upwards. The floor here is the viewport at which the two boxes are the same height,
        // worked out from the last measured pair, so the drag stops before they can diverge.
        //
        // The chrome comes from the viewport the pane was last drawn with rather than from
        // PlanningRequirementsScrollHeight(), which would call back into this method.
        private float MinPlanningRequirementsViewport()
        {
            float floor = MinPlanningRequirementsHeight;
            if (_measuredPlanningRequirementsHeight > 1f && _measuredPlanningResultsHeight > 1f && _planningRequirementsViewport > 1f)
            {
                float requirementsChrome = _measuredPlanningRequirementsHeight - _planningRequirementsViewport;
                float resultsFloorHeight = _measuredPlanningResultsHeight - GetResultsHeight() + MinDetailListHeight;
                floor = Mathf.Max(floor, resultsFloorHeight - requirementsChrome);
            }
            return floor;
        }

        private static float MaxPlanningListHeightOffset()
        {
            return Math.Max(0f, TankListScrollHeightPx - MinMatchedScrollHeight);
        }

        // Taken from the screen rather than the planner window: the window is sized by its
        // content, so reading its height here would feed this value its own output.
        private static float PlanningRequirementsBaseHeight()
        {
            float screenHeight = Screen.height > 0 ? Screen.height : 1080f;
            return Mathf.Clamp(screenHeight * 0.36f, 220f, 460f);
        }

        private float PlanningRequirementsScrollHeight()
        {
            return Mathf.Max(MinPlanningRequirementsViewport(), PlanningRequirementsBaseHeight() + PlanningListHeightOffset());
        }

        // Grow or shrink the Planning engine list until its box ends the drag offset below
        // the bottom of the Requirements box on its left.
        private void MatchPlanningResultsHeight()
        {
            if (!_planningMode)
            {
                _planningResultsHeightOffset = 0f;
                return;
            }
            if (_measuredPlanningRequirementsBottom <= 1f || _measuredPlanningResultsBottom <= 1f) return;

            // The drag now resizes the Requirements viewport itself, so its measured bottom
            // already carries the offset and the target is simply that bottom edge.
            float difference = _measuredPlanningRequirementsBottom - _measuredPlanningResultsBottom;
            if (Mathf.Abs(difference) < 1f) return;

            float target = Mathf.Clamp(GetResultsHeight() + difference, MinDetailListHeight, MaxDetailListHeight);
            _planningResultsHeightOffset = target - ResultsBaseHeight();

            _measuredPlanningRequirementsBottom = 0f;
            _measuredPlanningResultsBottom = 0f;
        }

        // Grow or shrink the Analyze Existing Selected Engine list until the right-hand
        // column ends level with the left diagnostics column.  Working from the measured
        // column heights means hidden analysis lines, absent stock stage masses, and a
        // longer or shorter Current Engines list are all accounted for automatically.
        private void MatchAnalysisColumnHeights()
        {
            if (_planningMode || _selected == null)
            {
                _analysisEngineScrollHeight = SelectedEngineScrollHeightPx;
                return;
            }
            if (_measuredAnalysisLeftColumnBottom <= 1f || _measuredAnalysisPaneBottom <= 1f) return;

            float difference = _measuredAnalysisLeftColumnBottom - _measuredAnalysisPaneBottom;
            if (Mathf.Abs(difference) < 1f) return;
            _analysisEngineScrollHeight = Mathf.Clamp(AnalysisEngineScrollHeight() + difference, MinMatchedScrollHeight, MaxMatchedScrollHeight);

            _measuredAnalysisLeftColumnBottom = 0f;
            _measuredAnalysisPaneBottom = 0f;
        }

        private void DrawMainWindowWidthResizeGrip()
        {
            Event e = Event.current;
            if (e == null) return;

            int controlId = GUIUtility.GetControlID(MainWindowWidthGripHint, FocusType.Passive);
            float gripY = Mathf.Max(34f, (_window.height - MainWindowWidthGripHeight) * 0.5f);
            Rect grip = new Rect(
                Mathf.Max(0f, _window.width - MainWindowWidthGripWidth - 1f),
                gripY,
                MainWindowWidthGripWidth,
                MainWindowWidthGripHeight);

            if (e.type == EventType.Repaint)
                GUI.Box(grip, "\u22ee", _gripStyle);

            Rect hitArea = new Rect(grip.x - 4f, grip.y - 4f, grip.width + 8f, grip.height + 8f);
            if (e.type == EventType.MouseDown && e.button == 0 && hitArea.Contains(e.mousePosition))
            {
                _resizingMainWindowWidth = true;
                _pendingMainWindowWidthDelta = 0f;
                GUIUtility.hotControl = controlId;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _resizingMainWindowWidth)
            {
                _pendingMainWindowWidthDelta += e.delta.x;
                GUIUtility.hotControl = controlId;
                e.Use();
            }
            else if ((e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow) && _resizingMainWindowWidth)
            {
                _resizingMainWindowWidth = false;
                if (GUIUtility.hotControl == controlId) GUIUtility.hotControl = 0;
                _uiSettings.MainWindowWidth = _window.width;
                _uiSettings.Save();
                if (e.type == EventType.MouseUp) e.Use();
            }
        }

        private void ApplyPendingMainWindowWidthResize()
        {
            if (Mathf.Abs(_pendingMainWindowWidthDelta) < 0.01f) return;
            _window.width = Mathf.Clamp(_window.width + _pendingMainWindowWidthDelta, MinWindowWidth, MaxWindowWidth);
            _uiSettings.MainWindowWidth = _window.width;
            _pendingMainWindowWidthDelta = 0f;
        }

        private void ReleaseAbandonedMainWindowWidthDrag()
        {
            if (!_resizingMainWindowWidth) return;
            if (Input.GetMouseButton(0)) return;
            _resizingMainWindowWidth = false;
            GUIUtility.hotControl = 0;
            _uiSettings.MainWindowWidth = _window.width;
            _uiSettings.Save();
        }

        // The planner window is content-sized, so a stretched grip would collapse to its
        // minimum height.  Size the vertical splitters from the scroll height of the pane
        // they sit beside plus the pane header, footer, and box padding instead.
        private static readonly int SplitterGripHint = "VesselPlannerSplitterGrip".GetHashCode();

        private static float VerticalSplitterHeight(float scrollHeight)
        {
            return Mathf.Max(40f, scrollHeight + 62f);
        }

        // Draws a drag grip for one pane edge.  Horizontal drags move a vertical splitter
        // between two side-by-side panes.  The drag delta is accumulated and applied once
        // the window has finished drawing, matching how the window's own resize handle
        // avoids changing layout mid-frame.
        private void SplitterGrip(PaneSplitter splitter, bool horizontal, params GUILayoutOption[] options)
        {
            GUILayout.Box(horizontal ? "\u22ee" : "\u22ef", _gripStyle, options);

            Event e = Event.current;
            if (e == null) return;

            // Claim a control id on every pass, Layout included, so the id stays stable
            // across the events of a frame.  Holding it as the hot control for the duration
            // of the drag is what keeps the rest of the window out of it: without that, once
            // the pane reaches its limit and the grip stops following the cursor, the
            // remaining mouse movement is free to be picked up by whatever else lies under
            // it, which drags the window itself or scrolls the pane being resized.
            int controlId = GUIUtility.GetControlID(SplitterGripHint, FocusType.Passive);
            if (e.type == EventType.Layout) return;

            Rect grip = GUILayoutUtility.GetLastRect();
            Rect hitArea = new Rect(grip.x - 2f, grip.y - 2f, grip.width + 4f, grip.height + 4f);

            if (e.type == EventType.MouseDown && e.button == 0 && hitArea.Contains(e.mousePosition))
            {
                _activeSplitter = splitter;
                _pendingSplitterTarget = splitter;
                _pendingSplitterDelta = Vector2.zero;
                GUIUtility.hotControl = controlId;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _activeSplitter == splitter)
            {
                _pendingSplitterTarget = splitter;
                _pendingSplitterDelta += e.delta;
                GUIUtility.hotControl = controlId;
                e.Use();
            }
            else if ((e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow) && _activeSplitter == splitter)
            {
                _activeSplitter = PaneSplitter.None;
                if (GUIUtility.hotControl == controlId) GUIUtility.hotControl = 0;
                _uiSettings.Save();
                if (e.type == EventType.MouseUp) e.Use();
            }
        }

        // A mouse button released outside the planner window never reaches the grip, so
        // the drag is also cleared here once the button is no longer physically held.
        private void ReleaseAbandonedSplitterDrag()
        {
            if (_activeSplitter == PaneSplitter.None) return;
            if (Input.GetMouseButton(0)) return;
            _activeSplitter = PaneSplitter.None;
            GUIUtility.hotControl = 0;
            _uiSettings.Save();
        }

        // Simulating replaces the candidate list and the selected engine, which changes
        // how many controls this window draws.  Running it after the window has been drawn
        // keeps that change out of the middle of a layout pass.
        private void RunQueuedAnalyzeSimulation()
        {
            if (!_simulateOnAnalyzeEntry) return;
            _simulateOnAnalyzeEntry = false;
            if (_planningMode) return;

            // Rescan first: the craft may have been edited while Planning was active, and
            // the simulation is only meaningful against the current stage.
            RefreshStage();
            SimulateExisting();
        }

        private void ApplyPendingSplitterDrag()
        {
            if (_pendingSplitterTarget == PaneSplitter.None) return;
            if (_pendingSplitterDelta.sqrMagnitude <= 0f)
            {
                if (_activeSplitter == PaneSplitter.None) _pendingSplitterTarget = PaneSplitter.None;
                return;
            }

            switch (_pendingSplitterTarget)
            {
                case PaneSplitter.PlanningListHeight:
                    _uiSettings.PlanningListHeightOffset = Mathf.Clamp(PlanningListHeightOffset() + _pendingSplitterDelta.y, MinPlanningListHeightOffset(), MaxPlanningListHeightOffset());
                    // The list is about to move, so the alignment must be recomputed from
                    // fresh measurements rather than from ones taken before this drag.
                    _measuredPlanningRequirementsBottom = 0f;
                    _measuredPlanningResultsBottom = 0f;
                    break;
                case PaneSplitter.AnalysisListHeight:
                    {
                        // Refuse to grow the list further once the pane below has been squeezed
                        // to its minimum, otherwise the drag would push the column bottom past
                        // the Existing stage box instead of trading height with the pane.
                        if (_pendingSplitterDelta.y > 0f && AnalysisEngineScrollHeight() <= MinMatchedScrollHeight + 1f) break;
                        float listHeight = Mathf.Clamp(GetResultsHeight() + _pendingSplitterDelta.y, MinDetailListHeight, MaxDetailListHeight);
                        _uiSettings.AnalysisListHeightOffset = listHeight - ResultsBaseHeight();
                        // The pane has just moved, so the height matcher must wait for fresh
                        // measurements rather than act on ones taken before this drag.
                        _measuredAnalysisLeftColumnBottom = 0f;
                        _measuredAnalysisPaneBottom = 0f;
                        break;
                    }
                case PaneSplitter.PlanningEngineTankWidth:
                    {
                        float available = PlanningDetailAvailableWidth();
                        float width = PlanningEnginePaneWidth() + _pendingSplitterDelta.x;
                        float maximum = Mathf.Max(MinDetailPaneWidth, available - MinDetailPaneWidth);
                        width = Mathf.Clamp(width, MinDetailPaneWidth, maximum);
                        _uiSettings.PlanningEnginePaneFraction = Mathf.Clamp(width / Mathf.Max(1f, available), MinDetailPaneFraction, MaxDetailPaneFraction);
                        // A width change can rewrap the Tanks footer, so the matched pane
                        // heights are recomputed from the next Repaint rather than these.
                        _measuredPlanningEnginePaneHeight = 0f;
                        _measuredPlanningTankPaneHeight = 0f;
                        break;
                    }
            }

            _pendingSplitterDelta = Vector2.zero;
            if (_activeSplitter == PaneSplitter.None) _pendingSplitterTarget = PaneSplitter.None;
        }

        private void RefreshDatabases()
        {
            _engines = EngineDatabase.ScanAvailableEngines();
            _tanks = TankDatabase.ScanAvailableTanks();
            _tankSuggestions = _selected != null && _planningMode ? TankPlanner.Suggest(ApplyTankBulkheadFilter(_tanks), _selected) : new List<TankSuggestion>();
            _selectedTank = null;
            ApplyTankSort();
            _status = _engines.Count + " engine definitions and " + _tanks.Count + " tank definitions loaded.";
        }

        private void RefreshStage()
        {
            _snapshot = EditorStageScanner.Scan(_stage);
            if (_snapshot != null && _snapshot.InferredTankDryRatio > 0.0)
                _tankRatio = _snapshot.InferredTankDryRatio.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static internal void EnsureStyles()
        {
            // Both styles are copied from GUI.skin, so they have to be rebuilt whenever the
            // skin setting changes.
            if (_styleSkinRevision != WindowSkin.Revision)
            {
                _right = null;
                _gripStyle = null;
                _darkBox = null;
                _styleSkinRevision = WindowSkin.Revision;
            }
            if (_right == null)
            {
                _right = new GUIStyle(GUI.skin.label);
                _right.alignment = TextAnchor.MiddleRight;
            }
            if (_gripStyle == null)
            {
                // A thin box with no internal padding so the grip stays visually slim
                // whether it is drawn as a vertical splitter or a bottom drag bar.
                _gripStyle = new GUIStyle(GUI.skin.box);
                _gripStyle.padding = new RectOffset(0, 0, 0, 0);
                _gripStyle.margin = new RectOffset(1, 1, 1, 1);
                _gripStyle.alignment = TextAnchor.MiddleCenter;
                _gripStyle.fontSize = 10;
                _gripStyle.stretchWidth = false;
                _gripStyle.stretchHeight = false;
            }
            if (_darkBox == null)
            {
                // A box drawn over a flat dark fill, used to set a list apart from the
                // pane around it.  The skin's box background is a bordered texture, so
                // the border and overflow are cleared as well: sliced against a 1x1 fill
                // they would stretch the corners across the whole box.
                if (_darkBoxTexture == null)
                {
                    _darkBoxTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    _darkBoxTexture.SetPixel(0, 0, new Color(0.09f, 0.09f, 0.10f, 0.90f));
                    _darkBoxTexture.wrapMode = TextureWrapMode.Clamp;
                    _darkBoxTexture.Apply();
                    _darkBoxTexture.hideFlags = HideFlags.HideAndDontSave;
                }
                _darkBox = new GUIStyle(GUI.skin.box);
                _darkBox.normal.background = _darkBoxTexture;
                _darkBox.onNormal.background = _darkBoxTexture;
                _darkBox.border = new RectOffset(0, 0, 0, 0);
                _darkBox.overflow = new RectOffset(0, 0, 0, 0);
                // The engine table's fixed column widths nearly fill the 350 px left
                // column, so the horizontal padding stays small to avoid pushing the last
                // column past the pane edge.
                _darkBox.padding = new RectOffset(2, 2, 3, 3);
                _darkBox.margin = new RectOffset(0, 0, 2, 2);
            }
        }

        // A free-standing GUI.DrawTexture outside a GUI.Window can still end up below another
        // Unity GUI window even when it is issued later in OnGUI. Stage-By-Stage uses this
        // helper from inside each window callback so the opaque fill participates in that
        // window's own draw layer. The normal KSP window style is then repainted on top to
        // retain the title, border and skin artwork.

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
                case SolutionSortColumn.EngineMass: selector = s => s.Engine.MassTons; break;
                case SolutionSortColumn.StageWetMass: selector = s => s.StageWetMassTons; break;
                case SolutionSortColumn.StartMass: selector = s => s.StartMassTons; break;
                case SolutionSortColumn.AtmosphericDeltaV: selector = s => s.AtmosphericDeltaV; break;
                case SolutionSortColumn.VacuumDeltaV: selector = s => s.VacuumDeltaV; break;
                case SolutionSortColumn.SeaLevelThrust: selector = s => s.Engine.SeaLevelThrustKn; break;
                case SolutionSortColumn.VacuumThrust: selector = s => s.Engine.MaxThrustVacuumKn; break;
                case SolutionSortColumn.CostEfficiency: selector = s => s.CostEfficiency; break;
                case SolutionSortColumn.Twr: selector = s => s.InitialTwr; break;
                case SolutionSortColumn.MaxTwr: selector = s => s.MaxTwr; break;
                case SolutionSortColumn.Isp: selector = s => s.Engine.VacuumIsp; break;
                case SolutionSortColumn.Burn: selector = s => s.BurnTimeSeconds; break;
                case SolutionSortColumn.Fuel: selector = s => s.PropellantSummary ?? string.Empty; break;
                // Sorted on the node size itself rather than its label, so the diameters come
                // out in order instead of alphabetically.
                case SolutionSortColumn.Bulkhead: selector = s => s.Engine.TopNodeSize; break;
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
                case TankSortColumn.Tank: selector = t => t.TankSummary; break;
                case TankSortColumn.Count: selector = t => t.Count; break;
                case TankSortColumn.DryMass: selector = t => t.TotalDryMassTons; break;
                case TankSortColumn.Excess: selector = t => t.ExcessFraction; break;
                case TankSortColumn.Bulkhead: selector = t => TankSuggestionBulkheadLabel(t); break;
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
            bool requested = GUILayout.Toggle(active, CommonRoutines.AddSpacesToString(mode.ToString()));
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
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(185));
                GUILayout.Label(value, _right, GUILayout.Width(120));
            }
        }
        private static void Header(string s, float w) { GUILayout.Label(s, GUILayout.Width(w)); }
        private static string F(double v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }
        private void ClampWindow()
        {
            _window.width = Mathf.Clamp(_window.width, MinWindowWidth, MaxWindowWidth);
            _window.height = Mathf.Max(MinWindowHeight, _window.height);
            CommonRoutines.ClampWindow(ref _window);
        }

        private float GetSettingsWindowHeight()
        {
            float height = SettingsWindowBaseHeight + SettingsWindowButtonExtraHeight;
            if (_uiSettings.UseAltSkin) return height;

            float lineHeight = 22f;
            if (GUI.skin != null && GUI.skin.label != null)
            {
                Vector2 size = GUI.skin.label.CalcSize(new GUIContent("Ag"));
                if (size.y > 0f) lineHeight = size.y;
            }
            return height + KspSkinExtraSettingsLines * lineHeight;
        }

        private void ClampSettingsWindow()
        {
            if (!_settingsVisible) return;
            CommonRoutines.ClampWindow(ref _settingsWindow);
        }
    }
}
