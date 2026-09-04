using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EngineStagePlanner.Core;
using EngineStagePlanner.KSP;
using UnityEngine;

namespace EngineStagePlanner.UI
{
    public sealed class PlannerWindow
    {
        private Rect _window = new Rect(100, 70, 980, 760);
        private Vector2 _resultsScroll;
        private Vector2 _detailScroll;
        private Vector2 _tankScroll;
        private bool _planningMode = true;
        private int _stage;
        private string _targetDv = "2500";
        private string _minTwr = "1.25";
        private string _payload = "5.0";
        private string _gravity = "9.80665";
        private string _maxEngines = "8";
        private string _tankRatio = "0.125";
        private bool _useCraftPayload = true;
        private bool _includeSolid;
        private bool _includeAir;
        private bool _includeElectric;
        private bool _ignoreMonoprop = true;
        private bool _filterByBulkheadSize;
        private bool _useSeaLevelDv;
        private OptimizationMode _mode = OptimizationMode.LowestStageMass;
        private List<EngineCandidate> _engines = new List<EngineCandidate>();
        private List<TankCandidate> _tanks = new List<TankCandidate>();
        private List<TankSuggestion> _tankSuggestions = new List<TankSuggestion>();
        private List<StageSolution> _solutions = new List<StageSolution>();
        private StageSolution _selected;
        private ExistingStageSnapshot _snapshot;
        private string _status = "";
        private GUIStyle _right;

        public bool Visible { get; set; } = true;

        public void Initialize()
        {
            RefreshDatabases();
            _stage = Math.Min(EditorStageScanner.MaxStage, Math.Max(0, _stage));
            RefreshStage();
        }

        public void Draw()
        {
            if (!Visible) return;
            EnsureStyles();
            _window = GUILayout.Window(19041968, _window, DrawWindow, "Engine Stage Planner", GUILayout.MinWidth(900), GUILayout.MinHeight(620));
            ClampWindow();
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(!_planningMode, "Analyze Existing", "Button", GUILayout.Height(25))) _planningMode = false;
            if (GUILayout.Toggle(_planningMode, "Planning", "Button", GUILayout.Height(25))) _planningMode = true;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(80))) { RefreshDatabases(); RefreshStage(); }
            if (GUILayout.Button("×", GUILayout.Width(30))) Visible = false;
            GUILayout.EndHorizontal();

            DrawStageSelector();
            GUILayout.Space(4);
            if (_planningMode) DrawPlanning(); else DrawAnalysis();
            GUI.DragWindow();
        }

        private void DrawStageSelector()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Stage", GUILayout.Width(45));
            if (GUILayout.Button("-", GUILayout.Width(28))) { _stage = Math.Max(0, _stage - 1); RefreshStage(); }
            GUILayout.Label(_stage.ToString(CultureInfo.InvariantCulture), GUILayout.Width(30));
            if (GUILayout.Button("+", GUILayout.Width(28))) { _stage = Math.Min(EditorStageScanner.MaxStage, _stage + 1); RefreshStage(); }
            GUILayout.Label("Max: " + EditorStageScanner.MaxStage, GUILayout.Width(65));
            if (_snapshot != null)
            {
                GUILayout.Label("Craft wet: " + F(_snapshot.VesselWetMassTons) + " t");
                GUILayout.Label("Stage fuel: " + F(_snapshot.StagePropellantMassTons) + " t");
                if (_snapshot.BulkheadProfiles.Count > 0)
                    GUILayout.Label("Bulkhead: " + string.Join(", ", _snapshot.BulkheadProfiles.ToArray()));
            }
            GUILayout.EndHorizontal();
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
            Field("Gravity (m/s²)", ref _gravity);
            Field("Max engines", ref _maxEngines);
            _ignoreMonoprop = GUILayout.Toggle(_ignoreMonoprop, "Ignore monopropellant");
            _includeSolid = GUILayout.Toggle(_includeSolid, "Include solid fuel");
            _includeAir = GUILayout.Toggle(_includeAir, "Include air-breathing");
            _includeElectric = GUILayout.Toggle(_includeElectric, "Include electric-propellant");
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
            Field("Minimum TWR", ref _minTwr);
            _useCraftPayload = GUILayout.Toggle(_useCraftPayload, "Use craft payload above selected stage");
            GUI.enabled = !_useCraftPayload;
            Field("Payload mass (t)", ref _payload);
            GUI.enabled = true;
            Field("Gravity (m/s²)", ref _gravity);
            Field("Max engines", ref _maxEngines);
            Field("Tank dry/fuel mass ratio", ref _tankRatio);

            GUILayout.Space(4);
            GUILayout.Label("Include engine classes");
            _includeSolid = GUILayout.Toggle(_includeSolid, "Solid fuel");
            _includeAir = GUILayout.Toggle(_includeAir, "Air-breathing");
            _includeElectric = GUILayout.Toggle(_includeElectric, "Electric-propellant");
            GUILayout.Space(4);
            GUILayout.Label("Optimize");
            if (GUILayout.Button(ModeLabel(_mode))) _mode = (OptimizationMode)(((int)_mode + 1) % Enum.GetValues(typeof(OptimizationMode)).Length);
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

            GUILayout.BeginHorizontal();
            GUILayout.Label("Δv basis:", GUILayout.Width(75));
            if (GUILayout.Toggle(!_useSeaLevelDv, "Vacuum", "Button", GUILayout.Width(90), GUILayout.Height(24)))
                _useSeaLevelDv = false;
            if (GUILayout.Toggle(_useSeaLevelDv, "Sea Level", "Button", GUILayout.Width(90), GUILayout.Height(24)))
                _useSeaLevelDv = true;
            GUILayout.FlexibleSpace();
            GUILayout.Label(_useSeaLevelDv ? "Uses Isp/thrust at 1 atm" : "Uses Isp/thrust in vacuum");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _filterByBulkheadSize = GUILayout.Toggle(_filterByBulkheadSize, "Match stage bulkhead size", GUILayout.Width(190));
            if (_snapshot != null && _snapshot.BulkheadProfiles.Count > 0)
                GUILayout.Label("Stage: " + string.Join(", ", _snapshot.BulkheadProfiles.ToArray()));
            else
                GUILayout.Label("Stage: no bulkhead profile detected");
            GUILayout.EndHorizontal();

            if (_filterByBulkheadSize && (_snapshot == null || _snapshot.BulkheadProfiles.Count == 0))
                GUILayout.Label("Bulkhead filtering cannot be applied until a stage profile is detected.");

            GUILayout.EndVertical();
            GUILayout.Space(3);

            GUILayout.Label("Candidates (click engine name for details; Add selects it for editor placement)");
            GUILayout.BeginHorizontal();
            Header("Engine", 185); Header("#", 28); Header("Wet t", 55); Header("Δv", 55); Header("TWR", 55); Header("Isp", 50); Header("Burn", 55); Header("Fuel", 120); Header("", 48);
            GUILayout.EndHorizontal();
            _resultsScroll = GUILayout.BeginScrollView(_resultsScroll, GUILayout.Height(290));
            foreach (StageSolution s in _solutions.Take(250))
            {
                if (s == _selected) GUILayout.BeginHorizontal("box"); else GUILayout.BeginHorizontal();
                if (GUILayout.Button(s.Engine.DisplayName, GUI.skin.label, GUILayout.Width(185))) SelectSolution(s);
                GUILayout.Label(s.EngineCount.ToString(), GUILayout.Width(28));
                GUILayout.Label(F(s.WetMassTons), GUILayout.Width(55));
                GUILayout.Label(Math.Round(s.DeltaV).ToString("0"), GUILayout.Width(55));
                GUILayout.Label(s.InitialTwr.ToString("0.00"), GUILayout.Width(55));
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
            Row("Δv", _selected.DeltaV.ToString("0") + " m/s");
            Row("TWR initial / final", _selected.InitialTwr.ToString("0.00") + " / " + _selected.FinalTwr.ToString("0.00"));
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
            Header("Tank", 245); Header("#", 32); Header("Tank dry t", 80); Header("Excess", 65); Header("Capacity", 300); Header("", 70);
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
            double twr, gravity;
            int max;
            if (!D(_minTwr, out twr) || !D(_gravity, out gravity) || !int.TryParse(_maxEngines, out max))
            { _status = "Check numeric inputs."; return; }
            if (_snapshot == null) RefreshStage();
            IEnumerable<EngineCandidate> candidates = _engines;
            if (_ignoreMonoprop)
                candidates = candidates.Where(e => !e.Propellants.Any(p => string.Equals(p.ResourceName, "MonoPropellant", StringComparison.OrdinalIgnoreCase)));
            if (!_includeSolid) candidates = candidates.Where(e => !e.IsSolid);
            if (!_includeAir) candidates = candidates.Where(e => !e.IsAirBreathing);
            if (!_includeElectric) candidates = candidates.Where(e => !e.IsElectric);
            candidates = ApplyBulkheadFilter(candidates);
            double atmospheres = _useSeaLevelDv ? 1.0 : 0.0;
            _solutions = ExistingStageSimulator.Simulate(_snapshot, candidates, gravity, atmospheres, Math.Max(1, Math.Min(64, max)), twr, _mode);
            _selected = _solutions.FirstOrDefault();
            _tankSuggestions.Clear();
            _status = _solutions.Count + " compatible engine configurations.";
        }

        private void Calculate()
        {
            double dv, twr, payload, gravity, ratio;
            int max;
            if (!D(_targetDv, out dv) || !D(_minTwr, out twr) || !D(_payload, out payload) || !D(_gravity, out gravity) || !D(_tankRatio, out ratio) || !int.TryParse(_maxEngines, out max))
            { _status = "Check numeric inputs."; return; }

            if (_snapshot == null) RefreshStage();
            var req = new StageRequirements
            {
                PayloadDryMassTons = _useCraftPayload ? _snapshot.PayloadAboveStageMassTons : payload,
                OtherStageDryMassTons = 0.0,
                TargetDeltaV = dv,
                MinimumTwr = twr,
                Gravity = gravity,
                Atmospheres = _useSeaLevelDv ? 1.0 : 0.0,
                MaxEngineCount = Math.Max(1, Math.Min(64, max)),
                TankDryMassPerPropellantMass = Math.Max(0.0, ratio)
            };

            IEnumerable<EngineCandidate> candidates = _engines;
            if (!_includeSolid) candidates = candidates.Where(e => !e.IsSolid);
            if (!_includeAir) candidates = candidates.Where(e => !e.IsAirBreathing);
            if (!_includeElectric) candidates = candidates.Where(e => !e.IsElectric);
            candidates = ApplyBulkheadFilter(candidates);
            _solutions = PlannerEngine.Calculate(candidates, req, _mode);
            _selected = _solutions.FirstOrDefault();
            _tankSuggestions = _selected != null ? TankPlanner.Suggest(_tanks, _selected) : new List<TankSuggestion>();
            _status = _solutions.Count + " valid configurations; " + _tankSuggestions.Count + " matching tank choices for the selected result.";
        }

        private IEnumerable<EngineCandidate> ApplyBulkheadFilter(IEnumerable<EngineCandidate> candidates)
        {
            if (!_filterByBulkheadSize || _snapshot == null || _snapshot.BulkheadProfiles.Count == 0)
                return candidates;

            var stageProfiles = new HashSet<string>(_snapshot.BulkheadProfiles, StringComparer.OrdinalIgnoreCase);
            return candidates.Where(e => e.BulkheadProfiles.Any(p =>
                !string.Equals(p, "srf", StringComparison.OrdinalIgnoreCase) && stageProfiles.Contains(p)));
        }

        private void RefreshDatabases()
        {
            _engines = EngineDatabase.ScanAvailableEngines();
            _tanks = TankDatabase.ScanAvailableTanks();
            _tankSuggestions = _selected != null && _planningMode ? TankPlanner.Suggest(_tanks, _selected) : new List<TankSuggestion>();
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
            _window.x = Mathf.Clamp(_window.x, -_window.width + 40f, Screen.width - 40f);
            _window.y = Mathf.Clamp(_window.y, 0f, Screen.height - 30f);
        }
    }
}
