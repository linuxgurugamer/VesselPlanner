using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClickThroughFix;
using EngineStagePlanner.Flight;
using UnityEngine;

namespace EngineStagePlanner.UI
{
    public sealed class FlightGraphWindow
    {
        private readonly FlightSensorManager _manager = new FlightSensorManager();
        private Rect _window = new Rect(120, 90, 900, 650);
        private Rect _settingsWindow = new Rect(180, 90, 570, 720);
        private Vector2 _legendScroll;
        private Vector2 _settingsScroll;
        private Texture2D _graphTexture;

        // Which side of the graph each series is annotated on, so the plot itself says what
        // it is showing. Keyed by sensor key; sources not listed are not annotated.
        private readonly Dictionary<string, GraphAxisSide> _axisSides = new Dictionary<string, GraphAxisSide>(StringComparer.Ordinal);
        private readonly List<AxisAnnotation> _axisAnnotations = new List<AxisAnnotation>();
        private int _graphSampleCount = -1;
        private int _graphSelectionRevision = -1;
        private int _graphMaximaRevision = -1;
        private int _graphStageMarkerRevision = -1;
        private int _graphWidth = -1;
        private int _graphHeight = -1;
        private bool _graphDirty = true;
        private bool _resizing;
        private bool _settingsResizing;
        private Vector2 _pendingResizeDelta;
        private Vector2 _pendingSettingsResizeDelta;
        private string _status = "";
        private string _sampleDelayText = "1";
        private string _chartTopText = "";
        private double _chartTopAltitudeMeters;
        private string _chartTopBodyName = "";
        private string _csvExportDirectory = DefaultCsvExportDirectory;
        private string _pngExportDirectory = DefaultPngExportDirectory;

        private const string DefaultCsvExportDirectory = "EngineStagePlanner/PluginData/CSV";
        private const string DefaultPngExportDirectory = "Screenshots";
        private const float MinWindowWidth = 680f;
        private const float MinWindowHeight = 480f;
        private const float MinSettingsWidth = 520f;
        private const float MinSettingsHeight = 420f;
        private const float ScreenMargin = 24f;
        private const int PixelsPerSample = 2;
        private const int ElapsedTimeLabelCount = 5;
        private const float ElapsedTimeLabelWidth = 64f;
        private const float SampleDelayStepSeconds = 0.25f;
        private const int GlyphWidth = 5;
        private const int GlyphHeight = 7;
        private const int GlyphSpacing = 1;
        private const int ExportLabelStripHeight = 14;
        private const int GridDivisions = 10;

        private static readonly Color32[] Palette =
        {
            new Color32(255, 96, 96, 255),
            new Color32(96, 210, 255, 255),
            new Color32(140, 255, 120, 255),
            new Color32(255, 220, 96, 255),
            new Color32(210, 120, 255, 255),
            new Color32(255, 150, 80, 255),
            new Color32(100, 255, 220, 255),
            new Color32(255, 130, 200, 255),
            new Color32(190, 190, 255, 255),
            new Color32(225, 225, 225, 255)
        };

        private bool _visible;
        public bool Visible
        {
            get { return _visible; }
            set
            {
                _visible = value;
                if (!value) SettingsVisible = false;
            }
        }
        public bool SettingsVisible { get; private set; }

        public void Initialize()
        {
            _manager.Initialize();
            LoadExportDirectories();
            LoadAxisSides();
            _sampleDelayText = _manager.SampleDelaySeconds.ToString("0.##", CultureInfo.InvariantCulture);
            SetChartTopFromCurrentBody(true);
            Visible = false;
        }

        public void Update()
        {
            int oldCount = _manager.Samples.Count;
            int oldRevision = _manager.SelectionRevision;
            int oldMaximaRevision = _manager.MaximaRevision;
            int oldStageMarkerRevision = _manager.StageMarkerRevision;
            _manager.Update();
            if (_chartTopAltitudeMeters <= 0.0) SetChartTopFromCurrentBody(true);
            if (_manager.AutoStartedThisUpdate)
                _status = "Launch detected. Plotting started.";
            if (_manager.Samples.Count != oldCount || _manager.SelectionRevision != oldRevision ||
                _manager.MaximaRevision != oldMaximaRevision || _manager.StageMarkerRevision != oldStageMarkerRevision)
                _graphDirty = true;
        }

        public void Draw()
        {
            // The cached label style is copied from GUI.skin, so it is rebuilt whenever the
            // skin setting changes.
            WindowSkin.Apply();
            if (_styleSkinRevision != WindowSkin.Revision)
            {
                InitStyles();
                _styleSkinRevision = WindowSkin.Revision;
            }

            if (Visible)
            {
                WindowSkin.Apply();
                KeepSizeOnScreen(ref _window, MinWindowWidth, MinWindowHeight);
                float requestedWidth = _window.width;
                float requestedHeight = _window.height;
                Rect drawn = ClickThruBlocker.GUILayoutWindow(19041969, _window, DrawWindow, "Engine Stage Planner - Flight Data",
                    GUILayout.Width(requestedWidth), GUILayout.Height(requestedHeight));
                // GUILayout must not grow the graph window to satisfy content. Only the resize grip changes its size.
                _window.x = drawn.x;
                _window.y = drawn.y;
                _window.width = requestedWidth;
                _window.height = requestedHeight;
                ApplyResize(ref _window, ref _pendingResizeDelta, MinWindowWidth, MinWindowHeight);
                KeepSizeOnScreen(ref _window, MinWindowWidth, MinWindowHeight);
                ClampWindow(ref _window);
            }

            if (SettingsVisible)
            {
                WindowSkin.Apply();
                KeepSizeOnScreen(ref _settingsWindow, MinSettingsWidth, MinSettingsHeight);
                float requestedWidth = _settingsWindow.width;
                float requestedHeight = _settingsWindow.height;
                Rect drawn = ClickThruBlocker.GUILayoutWindow(19041970, _settingsWindow, DrawSettingsWindow, "Flight Plot Settings",
                    GUILayout.Width(requestedWidth), GUILayout.Height(requestedHeight));
                _settingsWindow.x = drawn.x;
                _settingsWindow.y = drawn.y;
                _settingsWindow.width = requestedWidth;
                _settingsWindow.height = requestedHeight;
                ApplyResize(ref _settingsWindow, ref _pendingSettingsResizeDelta, MinSettingsWidth, MinSettingsHeight);
                KeepSizeOnScreen(ref _settingsWindow, MinSettingsWidth, MinSettingsHeight);
                ClampWindow(ref _settingsWindow);
            }
        }

        public void Dispose()
        {
            _manager.Dispose();
            if (_graphTexture != null)
            {
                UnityEngine.Object.Destroy(_graphTexture);
                _graphTexture = null;
            }
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            bool newRecording = GUILayout.Toggle(_manager.IsRecording, _manager.IsRecording ? "Stop Plotting" : "Start Plotting", "Button", GUILayout.Width(120));
            if (newRecording != _manager.IsRecording)
            {
                _manager.IsRecording = newRecording;
                if (newRecording) _manager.StartOnLaunchArmed = false;
                _status = newRecording ? "Plotting started." : "Plotting stopped.";
            }
            bool previousGuiEnabled = GUI.enabled;
            GUI.enabled = previousGuiEnabled && _manager.CanArmStartOnLaunch;
            if (GUILayout.Button(_manager.StartOnLaunchArmed ? "Launch Armed" : "Start at Launch", GUILayout.Width(130)))
            {
                _manager.StartOnLaunchArmed = !_manager.StartOnLaunchArmed;
                _status = _manager.StartOnLaunchArmed
                    ? "Plotting will start when the vessel leaves PRELAUNCH."
                    : "Start-at-launch cancelled.";
            }
            GUI.enabled = previousGuiEnabled;
            if (GUILayout.Button("Reset", GUILayout.Width(70)))
            {
                _manager.Reset();
                _graphDirty = true;
                _status = "Plot data cleared.";
            }
            if (GUILayout.Button("Settings", GUILayout.Width(80))) SettingsVisible = !SettingsVisible;
            if (GUILayout.Button("Export CSV", GUILayout.Width(90))) ExportCsv();
            if (GUILayout.Button("Export PNG", GUILayout.Width(90))) ExportPng();
            GUILayout.Space(8f);
            GUILayout.Label("Sample", GUILayout.Width(48));
            if (GUILayout.Button("-", GUILayout.Width(24)))
                AdjustSampleDelay(-SampleDelayStepSeconds);
            string newDelayText = GUILayout.TextField(_sampleDelayText, GUILayout.Width(48));
            if (!string.Equals(newDelayText, _sampleDelayText, StringComparison.Ordinal))
            {
                _sampleDelayText = newDelayText;
                float parsedDelay;
                if (float.TryParse(_sampleDelayText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedDelay))
                {
                    parsedDelay = Mathf.Clamp(parsedDelay, 0.05f, 60f);
                    if (Math.Abs(parsedDelay - _manager.SampleDelaySeconds) > 0.0001f)
                    {
                        _manager.SampleDelaySeconds = parsedDelay;
                        _status = "Sample delay updated.";
                    }
                }
            }
            if (GUILayout.Button("+", GUILayout.Width(24)))
                AdjustSampleDelay(SampleDelayStepSeconds);
            GUILayout.Label("s", GUILayout.Width(15));
            GUILayout.FlexibleSpace();
            GUILayout.Label(_manager.Samples.Count.ToString(CultureInfo.InvariantCulture) + " samples", GUILayout.Width(90));
            if (GUILayout.Button("×", GUILayout.Width(30))) Visible = false;
            GUILayout.EndHorizontal();

            GUILayout.Label("The graph fills from left to right, then scrolls as new samples arrive. Altitude uses the configured chart top; other series use this vessel's recorded historical maximum as their chart top.");

            GUILayout.Label("Altitude chart top: " + _chartTopAltitudeMeters.ToString("0.###", CultureInfo.InvariantCulture) + " m");

            List<FlightSensorSource> plottedSources = _manager.SelectedSources();
            float desiredLegendHeight = 30f + plottedSources.Count * 22f;
            float maxLegendHeight = Mathf.Max(72f, _window.height - 410f);
            float legendHeight = Mathf.Clamp(desiredLegendHeight, 72f, Mathf.Min(240f, maxLegendHeight));

            int graphWidth = Math.Max(320, (int)_window.width - 28);
            // Keep enough vertical room for the two export-path/footer rows. The resize
            // grip is an absolute GUI overlay and does not need its own GUILayout row.
            // Returning the old 24-pixel spacer to the graph makes the final footer/status
            // line the bottom layout row instead of leaving a blank line beneath it.
            int graphHeight = Math.Max(180, (int)(_window.height - 263f - legendHeight));
            EnsureGraphTexture(graphWidth, graphHeight);
            GUILayout.Box(_graphTexture, GUILayout.Width(graphWidth), GUILayout.Height(graphHeight));
            Rect graphRect = GUILayoutUtility.GetLastRect();
            DrawStageMarkerLabels(graphRect);
            DrawElapsedTimeLabels(graphRect);
            GUILayout.Space(20f);

            DrawLegend(plottedSources, legendHeight);
            if (_manager.IsRecording && _manager.IsGamePaused)
                GUILayout.Label("Game paused - plotting is suspended.");
            if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status);
            GUILayout.Label("CSV export: " + GetCsvExportFolder());
            GUILayout.Label("PNG export: " + GetPngExportFolder());

            // The resize grip is drawn as an absolute overlay at the lower-right corner.
            // Do not reserve a GUILayout row for it; the final footer/status line should
            // be the bottom row of the flight-data window.
            DrawResizeHandle(_window, ref _resizing, ref _pendingResizeDelta);
            GUI.DragWindow(new Rect(0f, 0f, _window.width, _window.height));
        }

        private void AdjustSampleDelay(float deltaSeconds)
        {
            float updated = Mathf.Clamp(_manager.SampleDelaySeconds + deltaSeconds, 0.05f, 60f);
            // Avoid floating-point display noise after repeated +/- clicks.
            updated = (float)Math.Round(updated, 2, MidpointRounding.AwayFromZero);
            _manager.SampleDelaySeconds = updated;
            _sampleDelayText = updated.ToString("0.##", CultureInfo.InvariantCulture);
            _status = "Sample delay updated.";
        }

        private void DrawLegend(List<FlightSensorSource> selected, float height)
        {
            GUILayout.Label("Plotted sensors");

            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            GUILayout.Label("Sensor", GUILayout.Width(230));
            GUILayout.Label("Current", GUILayout.Width(110));
            GUILayout.Label("Max", GUILayout.Width(110));
            GUILayout.Label("Units", GUILayout.Width(70));
            GUILayout.EndHorizontal();

            _legendScroll = GUILayout.BeginScrollView(_legendScroll, GUILayout.Height(height));
            if (selected.Count == 0)
            {
                GUILayout.Label("No sensors selected. Open Settings to choose data sources.");
            }
            else
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    FlightSensorSource source = selected[i];
                    GUILayout.BeginHorizontal();
                    DrawColorSwatch(Palette[i % Palette.Length]);
                    GUILayout.Label(source.DisplayName, GUILayout.Width(230));
                    GUILayout.Label(FormatCurrentValue(source), GUILayout.Width(110));
                    GUILayout.Label(FormatMaximumValue(source), GUILayout.Width(110));
                    GUILayout.Label(string.IsNullOrEmpty(source.Units) ? "" : source.Units, GUILayout.Width(70));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawSettingsWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Select the telemetry series to plot. Data is collected for all available sources while plotting.");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(30))) SettingsVisible = false;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Altitude chart top", GUILayout.Width(145));
            string newTopText = GUILayout.TextField(_chartTopText, GUILayout.Width(90));
            if (!string.Equals(newTopText, _chartTopText, StringComparison.Ordinal))
            {
                _chartTopText = newTopText;
                double parsedTop;
                if (double.TryParse(_chartTopText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedTop) && parsedTop > 0.0)
                {
                    _chartTopAltitudeMeters = parsedTop;
                    _graphDirty = true;
                    _status = "Altitude chart top updated.";
                }
            }
            GUILayout.Label("m", GUILayout.Width(20));
            if (GUILayout.Button("Body Default", GUILayout.Width(100)))
            {
                SetChartTopFromCurrentBody(true);
                _graphDirty = true;
                _status = "Altitude chart top reset to the body default.";
            }
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_chartTopBodyName))
                GUILayout.Label("Body default source: " + _chartTopBodyName);

            GUILayout.Space(8);
            GUILayout.Label("CSV export folder");
            GUILayout.BeginHorizontal();
            string newCsvExportDirectory = GUILayout.TextField(_csvExportDirectory, GUILayout.MinWidth(300));
            if (!string.Equals(newCsvExportDirectory, _csvExportDirectory, StringComparison.Ordinal))
            {
                _csvExportDirectory = newCsvExportDirectory;
                SaveExportDirectories();
            }
            if (GUILayout.Button("Default", GUILayout.Width(70)))
            {
                _csvExportDirectory = DefaultCsvExportDirectory;
                SaveExportDirectories();
                _status = "CSV export folder reset to default.";
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Relative CSV paths are resolved under GameData. Default: " + DefaultCsvExportDirectory);

            GUILayout.Space(6);
            GUILayout.Label("PNG export folder");
            GUILayout.BeginHorizontal();
            string newPngExportDirectory = GUILayout.TextField(_pngExportDirectory, GUILayout.MinWidth(300));
            if (!string.Equals(newPngExportDirectory, _pngExportDirectory, StringComparison.Ordinal))
            {
                _pngExportDirectory = newPngExportDirectory;
                SaveExportDirectories();
            }
            if (GUILayout.Button("Default", GUILayout.Width(70)))
            {
                _pngExportDirectory = DefaultPngExportDirectory;
                SaveExportDirectories();
                _status = "PNG export folder reset to default.";
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Relative PNG paths are resolved under the KSP root. Default: " + DefaultPngExportDirectory);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh available resources/sensors", GUILayout.Height(26)))
            {
                _manager.RefreshNow();
                _graphDirty = true;
            }
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            string activeVesselName = activeVessel != null && !string.IsNullOrEmpty(activeVessel.vesselName)
                ? activeVessel.vesselName
                : "current vessel";
            if (GUILayout.Button("Clear Max Values for Vessel", GUILayout.Width(190), GUILayout.Height(26)))
            {
                bool cleared = _manager.ClearMaximaForCurrentVessel();
                _graphDirty = true;
                _status = cleared
                    ? "Cleared stored sensor maxima for " + activeVesselName + "."
                    : "No stored sensor maxima to clear for " + activeVesselName + ".";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Plot", GUILayout.Width(38));
            GUILayout.Label("Sensor", GUILayout.Width(200));
            GUILayout.Label("Max", GUILayout.Width(110));
            GUILayout.Label("Units", GUILayout.Width(70));
            GUILayout.Label("Axis", GUILayout.Width(60));
            GUILayout.EndHorizontal();
            GUILayout.Label("Axis marks a series' scale on the left or right edge of the graph, so the plot shows what it is displaying. The button cycles Off, Left, and Right.");

            _settingsScroll = GUILayout.BeginScrollView(_settingsScroll);
            string[] groups = { "Flight data", "Ship resources", "Sensor outputs" };
            foreach (string group in groups)
            {
                List<FlightSensorSource> groupSources = _manager.Sources.Where(s => string.Equals(s.Group, group, StringComparison.Ordinal)).ToList();
                if (groupSources.Count == 0) continue;

                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Label(group);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("All", GUILayout.Width(45))) { _manager.SetGroupSelected(group, true); _graphDirty = true; }
                if (GUILayout.Button("None", GUILayout.Width(50))) { _manager.SetGroupSelected(group, false); _graphDirty = true; }
                GUILayout.EndHorizontal();

                foreach (FlightSensorSource source in groupSources)
                {
                    GUILayout.BeginHorizontal();
                    bool selected = GUILayout.Toggle(source.IsSelected, "", GUILayout.Width(38));
                    if (selected != source.IsSelected)
                    {
                        _manager.SetSelected(source.Key, selected);
                        _graphDirty = true;
                    }
                    GUILayout.Label(source.DisplayName, GUILayout.Width(200));
                    GUILayout.Label(FormatMaximumValue(source), GUILayout.Width(110));
                    GUILayout.Label(string.IsNullOrEmpty(source.Units) ? "" : source.Units, GUILayout.Width(70));

                    GraphAxisSide axisSide = GetAxisSide(source.Key);
                    string axisLabel = axisSide == GraphAxisSide.Left ? "Left" : axisSide == GraphAxisSide.Right ? "Right" : "Off";
                    if (GUILayout.Button(axisLabel, GUILayout.Width(60)))
                    {
                        GraphAxisSide next = axisSide == GraphAxisSide.None
                            ? GraphAxisSide.Left
                            : axisSide == GraphAxisSide.Left ? GraphAxisSide.Right : GraphAxisSide.None;
                        SetAxisSide(source.Key, next);
                        _status = source.DisplayName + " axis: " + (next == GraphAxisSide.None ? "off" : next.ToString().ToLowerInvariant()) + ".";
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(24f);
            DrawResizeHandle(_settingsWindow, ref _settingsResizing, ref _pendingSettingsResizeDelta);
            GUI.DragWindow(new Rect(0f, 0f, _settingsWindow.width, _settingsWindow.height));
        }

        private enum GraphAxisSide
        {
            None,
            Left,
            Right
        }

        // One series' scale, recorded while the graph is rendered so the annotation can be
        // drawn with the same numbers the plot was scaled by.
        private struct AxisAnnotation
        {
            public GraphAxisSide Side;
            public string Name;
            public string Units;
            public double Minimum;
            public double Maximum;
            public Color32 Color;
        }

        // A 5x7 pixel font, one byte per column with the low bit at the top row. The graph
        // is drawn into a Texture2D rather than with GUI calls, so text baked into it shows
        // up in the PNG export as well as on screen. Unknown characters draw as a space.
        private const string GlyphCharacters = " 0123456789.:+-/()%ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly byte[] GlyphColumns =
        {
            0x00, 0x00, 0x00, 0x00, 0x00, // space
            0x3E, 0x51, 0x49, 0x45, 0x3E, // 0
            0x00, 0x42, 0x7F, 0x40, 0x00, // 1
            0x42, 0x61, 0x51, 0x49, 0x46, // 2
            0x21, 0x41, 0x45, 0x4B, 0x31, // 3
            0x18, 0x14, 0x12, 0x7F, 0x10, // 4
            0x27, 0x45, 0x45, 0x45, 0x39, // 5
            0x3C, 0x4A, 0x49, 0x49, 0x30, // 6
            0x01, 0x71, 0x09, 0x05, 0x03, // 7
            0x36, 0x49, 0x49, 0x49, 0x36, // 8
            0x06, 0x49, 0x49, 0x29, 0x1E, // 9
            0x00, 0x60, 0x60, 0x00, 0x00, // .
            0x00, 0x36, 0x36, 0x00, 0x00, // :
            0x08, 0x08, 0x3E, 0x08, 0x08, // +
            0x08, 0x08, 0x08, 0x08, 0x08, // -
            0x20, 0x10, 0x08, 0x04, 0x02, // /
            0x00, 0x1C, 0x22, 0x41, 0x00, // (
            0x00, 0x41, 0x22, 0x1C, 0x00, // )
            0x23, 0x13, 0x08, 0x64, 0x62, // %
            0x7E, 0x11, 0x11, 0x11, 0x7E, // A
            0x7F, 0x49, 0x49, 0x49, 0x36, // B
            0x3E, 0x41, 0x41, 0x41, 0x22, // C
            0x7F, 0x41, 0x41, 0x22, 0x1C, // D
            0x7F, 0x49, 0x49, 0x49, 0x41, // E
            0x7F, 0x09, 0x09, 0x01, 0x01, // F
            0x3E, 0x41, 0x49, 0x49, 0x7A, // G
            0x7F, 0x08, 0x08, 0x08, 0x7F, // H
            0x00, 0x41, 0x7F, 0x41, 0x00, // I
            0x20, 0x40, 0x41, 0x3F, 0x01, // J
            0x7F, 0x08, 0x14, 0x22, 0x41, // K
            0x7F, 0x40, 0x40, 0x40, 0x40, // L
            0x7F, 0x02, 0x04, 0x02, 0x7F, // M
            0x7F, 0x04, 0x08, 0x10, 0x7F, // N
            0x3E, 0x41, 0x41, 0x41, 0x3E, // O
            0x7F, 0x09, 0x09, 0x09, 0x06, // P
            0x3E, 0x41, 0x51, 0x21, 0x5E, // Q
            0x7F, 0x09, 0x19, 0x29, 0x46, // R
            0x46, 0x49, 0x49, 0x49, 0x31, // S
            0x01, 0x01, 0x7F, 0x01, 0x01, // T
            0x3F, 0x40, 0x40, 0x40, 0x3F, // U
            0x1F, 0x20, 0x40, 0x20, 0x1F, // V
            0x7F, 0x20, 0x18, 0x20, 0x7F, // W
            0x63, 0x14, 0x08, 0x14, 0x63, // X
            0x07, 0x08, 0x70, 0x08, 0x07, // Y
            0x61, 0x51, 0x49, 0x45, 0x43  // Z
        };

        private static int MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return text.Length * (GlyphWidth + GlyphSpacing) - GlyphSpacing;
        }

        // x and y are measured from the top-left of the image; the pixel array itself is
        // bottom-up, which is why the row index is flipped here.
        private static void DrawText(Color32[] pixels, int width, int height, int x, int y, string text, Color32 color)
        {
            if (string.IsNullOrEmpty(text)) return;

            int penX = x;
            for (int i = 0; i < text.Length; i++)
            {
                int glyph = GlyphCharacters.IndexOf(char.ToUpperInvariant(text[i]));
                if (glyph >= 0)
                {
                    for (int column = 0; column < GlyphWidth; column++)
                    {
                        byte bits = GlyphColumns[glyph * GlyphWidth + column];
                        for (int row = 0; row < GlyphHeight; row++)
                        {
                            if ((bits & (1 << row)) == 0) continue;
                            int px = penX + column;
                            int py = height - 1 - (y + row);
                            if (px < 0 || px >= width || py < 0 || py >= height) continue;
                            pixels[py * width + px] = color;
                        }
                    }
                }
                penX += GlyphWidth + GlyphSpacing;
            }
        }

        private static string FormatAxisNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "n/a";
            double magnitude = Math.Abs(value);
            if (magnitude >= 1000000.0 || (magnitude > 0.0 && magnitude < 0.001))
                return value.ToString("0.##e+0", CultureInfo.InvariantCulture);
            if (magnitude >= 1000.0) return value.ToString("0", CultureInfo.InvariantCulture);
            if (magnitude >= 1.0) return value.ToString("0.##", CultureInfo.InvariantCulture);
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private GraphAxisSide GetAxisSide(string key)
        {
            GraphAxisSide side;
            return !string.IsNullOrEmpty(key) && _axisSides.TryGetValue(key, out side) ? side : GraphAxisSide.None;
        }

        private void SetAxisSide(string key, GraphAxisSide side)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (side == GraphAxisSide.None) _axisSides.Remove(key);
            else _axisSides[key] = side;
            _graphDirty = true;
            SaveAxisSides();
        }

        // Draws a value scale down the inside edge of the graph for every annotated series,
        // in the series colour. Ticks sit on the horizontal gridlines the graph already
        // draws, so the numbers line up with the grid rather than floating between it. The
        // series name heads its column. Several series can share a side, in which case each
        // gets its own column working inward from the edge.
        private void DrawAxisAnnotations(Color32[] pixels, int width, int height)
        {
            if (_axisAnnotations.Count == 0) return;

            int leftColumn = 3;
            int rightColumn = width - 3;
            int maximumCharacters = Math.Max(4, (width / 3) / (GlyphWidth + GlyphSpacing));

            for (int i = 0; i < _axisAnnotations.Count; i++)
            {
                AxisAnnotation annotation = _axisAnnotations[i];
                if (annotation.Side == GraphAxisSide.None) continue;

                string units = string.IsNullOrEmpty(annotation.Units) ? "" : " " + annotation.Units;
                string heading = (annotation.Name ?? "") + units;
                if (heading.Length > maximumCharacters) heading = heading.Substring(0, maximumCharacters);

                // One tick per gridline where they fit, thinning to every second or fifth
                // line on a short graph so the numbers cannot run into each other.
                int step = 1;
                if (height * 2 < GridDivisions * GlyphHeight * 5) step = 2;
                if (height * 5 < GridDivisions * GlyphHeight * 5) step = 5;

                var ticks = new List<string>();
                var tickRows = new List<int>();
                int columnWidth = MeasureText(heading);
                for (int division = 0; division <= GridDivisions; division += step)
                {
                    // Fractions run from the bottom of the scale upward, matching how the
                    // series itself was plotted.
                    double fraction = 1.0 - (division / (double)GridDivisions);
                    string text = FormatAxisNumber(annotation.Minimum + (annotation.Maximum - annotation.Minimum) * fraction);

                    int row = (int)Math.Round((height - 1) * (division / (double)GridDivisions)) - (GlyphHeight / 2);
                    // Keep the top tick clear of the heading and the bottom one inside the
                    // graph; only the outermost ticks are ever nudged off their gridline.
                    row = Mathf.Clamp(row, GlyphHeight + 3, height - GlyphHeight - 2);

                    ticks.Add(text);
                    tickRows.Add(row);
                    columnWidth = Math.Max(columnWidth, MeasureText(text));
                }

                if (annotation.Side == GraphAxisSide.Left)
                {
                    if (leftColumn + columnWidth > width / 2) continue;
                    DrawText(pixels, width, height, leftColumn, 2, heading, annotation.Color);
                    for (int tick = 0; tick < ticks.Count; tick++)
                        DrawText(pixels, width, height, leftColumn, tickRows[tick], ticks[tick], annotation.Color);
                    leftColumn += columnWidth + 6;
                }
                else
                {
                    if (rightColumn - columnWidth < width / 2) continue;
                    DrawText(pixels, width, height, rightColumn - MeasureText(heading), 2, heading, annotation.Color);
                    for (int tick = 0; tick < ticks.Count; tick++)
                        DrawText(pixels, width, height, rightColumn - MeasureText(ticks[tick]), tickRows[tick], ticks[tick], annotation.Color);
                    rightColumn -= columnWidth + 6;
                }
            }
        }

        private void LoadAxisSides()
        {
            _axisSides.Clear();
            try
            {
                if (!File.Exists(SettingsPath)) return;
                ConfigNode root = ConfigNode.Load(SettingsPath);
                if (root == null) return;
                ConfigNode settings = root.GetNode("ENGINE_STAGE_PLANNER_SETTINGS") ?? root;
                ReadAxisSideList(settings, "GraphAxisLeft", GraphAxisSide.Left);
                ReadAxisSideList(settings, "GraphAxisRight", GraphAxisSide.Right);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[EngineStagePlanner] Unable to load graph axis sides: " + ex.Message);
            }
        }

        private void ReadAxisSideList(ConfigNode settings, string key, GraphAxisSide side)
        {
            if (settings == null || !settings.HasValue(key)) return;
            string value = settings.GetValue(key);
            if (string.IsNullOrEmpty(value)) return;
            foreach (string entry in value.Split(','))
            {
                string trimmed = entry.Trim();
                if (trimmed.Length > 0) _axisSides[trimmed] = side;
            }
        }

        private void SaveAxisSides()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                ConfigNode root = File.Exists(SettingsPath) ? ConfigNode.Load(SettingsPath) : null;
                if (root == null) root = new ConfigNode();
                ConfigNode settings = root.GetNode("ENGINE_STAGE_PLANNER_SETTINGS");
                if (settings == null) settings = root.AddNode("ENGINE_STAGE_PLANNER_SETTINGS");
                settings.SetValue("GraphAxisLeft", JoinAxisKeys(GraphAxisSide.Left), true);
                settings.SetValue("GraphAxisRight", JoinAxisKeys(GraphAxisSide.Right), true);
                root.Save(SettingsPath);
            }
            catch (Exception ex)
            {
                _status = "Unable to save graph axis sides: " + ex.Message;
                Debug.LogWarning("[EngineStagePlanner] Unable to save graph axis sides: " + ex.Message);
            }
        }

        private string JoinAxisKeys(GraphAxisSide side)
        {
            var keys = new List<string>();
            foreach (KeyValuePair<string, GraphAxisSide> pair in _axisSides)
                if (pair.Value == side) keys.Add(pair.Key);
            keys.Sort(StringComparer.Ordinal);
            return string.Join(",", keys.ToArray());
        }

        private void EnsureGraphTexture(int width, int height)
        {
            if (!_graphDirty && _graphTexture != null && _graphWidth == width && _graphHeight == height &&
                _graphSampleCount == _manager.Samples.Count && _graphSelectionRevision == _manager.SelectionRevision &&
                _graphMaximaRevision == _manager.MaximaRevision && _graphStageMarkerRevision == _manager.StageMarkerRevision)
                return;

            _graphWidth = width;
            _graphHeight = height;
            _graphSampleCount = _manager.Samples.Count;
            _graphSelectionRevision = _manager.SelectionRevision;
            _graphMaximaRevision = _manager.MaximaRevision;
            _graphStageMarkerRevision = _manager.StageMarkerRevision;
            _graphDirty = false;

            if (_graphTexture == null || _graphTexture.width != width || _graphTexture.height != height)
            {
                if (_graphTexture != null) UnityEngine.Object.Destroy(_graphTexture);
                _graphTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
                _graphTexture.wrapMode = TextureWrapMode.Clamp;
                _graphTexture.filterMode = FilterMode.Bilinear;
            }

            RenderGraph(_graphTexture, _manager.Samples, _manager.StageMarkers, _manager.SelectedSources(), _chartTopAltitudeMeters);
        }

        private void RenderGraph(Texture2D texture, IList<FlightDataSample> samples, IList<FlightStageMarker> stageMarkers, IList<FlightSensorSource> selected, double altitudeChartTop)
        {
            int width = texture.width;
            int height = texture.height;
            var pixels = new Color32[width * height];
            Color32 background = new Color32(20, 22, 26, 255);
            Color32 grid = new Color32(55, 58, 64, 255);
            Color32 border = new Color32(115, 120, 130, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = background;

            for (int gx = 0; gx <= GridDivisions; gx++)
            {
                int x = (int)Math.Round((width - 1) * gx / (double)GridDivisions);
                DrawVertical(pixels, width, height, x, gx == 0 || gx == GridDivisions ? border : grid);
            }
            for (int gy = 0; gy <= GridDivisions; gy++)
            {
                int y = (int)Math.Round((height - 1) * gy / (double)GridDivisions);
                DrawHorizontal(pixels, width, height, y, gy == 0 || gy == GridDivisions ? border : grid);
            }

            _axisAnnotations.Clear();
            if (samples.Count > 0 && selected.Count > 0)
            {
                // Keep a fixed horizontal spacing between samples. The graph initially fills from
                // the left edge; once full, the visible sample window advances and the lines scroll left.
                int visibleCapacity = Math.Max(2, ((width - 3) / PixelsPerSample) + 1);
                int firstVisible = Math.Max(0, samples.Count - visibleCapacity);
                int lastVisible = samples.Count - 1;

                for (int seriesIndex = 0; seriesIndex < selected.Count; seriesIndex++)
                {
                    FlightSensorSource source = selected[seriesIndex];
                    bool altitudeSeries = string.Equals(source.Key, "flight.altitudeASL", StringComparison.Ordinal) ||
                                          string.Equals(source.Key, "flight.altitudeSurface", StringComparison.Ordinal);

                    double min;
                    double max;
                    if (altitudeSeries && altitudeChartTop > 0.0)
                    {
                        min = 0.0;
                        max = altitudeChartTop;
                    }
                    else
                    {
                        min = double.PositiveInfinity;
                        double visibleMax = double.NegativeInfinity;
                        for (int i = firstVisible; i <= lastVisible; i++)
                        {
                            double value;
                            if (!samples[i].Values.TryGetValue(source.Key, out value) || double.IsNaN(value) || double.IsInfinity(value)) continue;
                            if (value < min) min = value;
                            if (value > visibleMax) visibleMax = value;
                        }
                        if (double.IsInfinity(min) || double.IsInfinity(visibleMax)) continue;

                        double recordedMaximum;
                        max = _manager.TryGetMaximum(source.Key, out recordedMaximum) ? recordedMaximum : visibleMax;
                        // CaptureSample updates maxima before the graph is rendered, but retain this
                        // guard for legacy maxima files or unusual dynamic-source changes.
                        if (visibleMax > max) max = visibleMax;

                        // Preserve the historical maximum as the exact top of the scale. If the
                        // currently visible range collapses to that value, expand only downward.
                        if (Math.Abs(max - min) < 1e-12 || min > max)
                        {
                            double pad = Math.Max(1.0, Math.Abs(max) * 0.05);
                            min = max - pad;
                        }
                    }

                    bool hasPrevious = false;
                    int previousX = 0;
                    int previousY = 0;
                    Color32 color = Palette[seriesIndex % Palette.Length];

                    // Record the scale this series was drawn against, so the annotation
                    // reports the same numbers the line was plotted from.
                    GraphAxisSide side = GetAxisSide(source.Key);
                    if (side != GraphAxisSide.None)
                    {
                        _axisAnnotations.Add(new AxisAnnotation
                        {
                            Side = side,
                            Name = source.DisplayName,
                            Units = source.Units,
                            Minimum = min,
                            Maximum = max,
                            Color = color
                        });
                    }
                    for (int i = firstVisible; i <= lastVisible; i++)
                    {
                        double value;
                        if (!samples[i].Values.TryGetValue(source.Key, out value) || double.IsNaN(value) || double.IsInfinity(value))
                        {
                            hasPrevious = false;
                            continue;
                        }

                        int visibleIndex = i - firstVisible;
                        int x = 1 + visibleIndex * PixelsPerSample;
                        x = Math.Max(1, Math.Min(width - 2, x));
                        double normalized = (value - min) / (max - min);
                        normalized = Math.Max(0.0, Math.Min(1.0, normalized));
                        int y = 1 + (int)Math.Round((height - 3) * normalized);
                        y = Math.Max(1, Math.Min(height - 2, y));
                        if (hasPrevious) DrawLine(pixels, width, height, previousX, previousY, x, y, color);
                        else SetPixel(pixels, width, height, x, y, color);
                        previousX = x;
                        previousY = y;
                        hasPrevious = true;
                    }
                }
            }

            DrawStageMarkerLines(pixels, width, height, samples, stageMarkers);
            DrawAxisAnnotations(pixels, width, height);

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private static void DrawStageMarkerLines(Color32[] pixels, int width, int height, IList<FlightDataSample> samples, IList<FlightStageMarker> stageMarkers)
        {
            if (samples == null || samples.Count == 0 || stageMarkers == null || stageMarkers.Count == 0) return;

            int visibleCapacity = Math.Max(2, ((width - 3) / PixelsPerSample) + 1);
            int firstVisible = Math.Max(0, samples.Count - visibleCapacity);
            long firstSequence = samples[firstVisible].SequenceNumber;
            long lastSequence = firstSequence + visibleCapacity - 1;
            Color32 markerColor = new Color32(255, 150, 32, 255);

            foreach (FlightStageMarker marker in stageMarkers)
            {
                if (marker.SampleSequence < firstSequence || marker.SampleSequence > lastSequence) continue;
                int x = 1 + (int)((marker.SampleSequence - firstSequence) * PixelsPerSample);
                x = Math.Max(1, Math.Min(width - 2, x));
                for (int y = 1; y < height - 1; y++)
                {
                    // Dashed vertical marker so sensor traces remain readable underneath it.
                    if (((y / 4) & 1) == 0) SetPixel(pixels, width, height, x, y, markerColor);
                }
            }
        }

        static private GUIStyle style;
        static private int _styleSkinRevision = -1;

        static internal void InitStyles()
        {
            Debug.Log("[EngineStagePlanner] InitStyles");
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.LowerCenter
            };
            style.normal.textColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        }

        private void DrawElapsedTimeLabels(Rect graphRect)
        {
            IList<FlightDataSample> samples = _manager.Samples;
            if (samples == null || samples.Count == 0) return;

            int width = Math.Max(2, (int)graphRect.width);
            int visibleCapacity = Math.Max(2, ((width - 3) / PixelsPerSample) + 1);
            int firstVisible = Math.Max(0, samples.Count - visibleCapacity);
            int lastVisible = samples.Count - 1;
            if (lastVisible < firstVisible) return;

            // The labels sit at fixed positions spread across the full width of the graph,
            // one per fifth of the plot area. Spacing them by sample position instead put
            // them a couple of pixels apart while the graph was still filling, so they were
            // drawn on top of each other.
            //
            // The axis therefore always spans the same number of samples as the plot area
            // holds. Positions past the newest sample have no sample to read, so their time
            // is projected from the newest one using the average interval between the
            // samples on screen, falling back to the configured sample delay when there is
            // only one. Those slots fill in with real data as it arrives.
            double interval = _manager.SampleDelaySeconds;
            if (lastVisible > firstVisible)
            {
                double span = samples[lastVisible].ElapsedSeconds - samples[firstVisible].ElapsedSeconds;
                if (span > 0.0) interval = span / (lastVisible - firstVisible);
            }

            // Drop to fewer labels on a narrow graph rather than letting them touch.
            int labelCount = Mathf.Clamp((int)(graphRect.width / (ElapsedTimeLabelWidth + 8f)) + 1, 2, ElapsedTimeLabelCount);

            for (int label = 0; label < labelCount; label++)
            {
                int offset = (int)Math.Round((visibleCapacity - 1) * label / (double)(labelCount - 1));
                int sampleIndex = firstVisible + offset;

                double elapsed = sampleIndex <= lastVisible
                    ? samples[sampleIndex].ElapsedSeconds
                    : samples[lastVisible].ElapsedSeconds + (sampleIndex - lastVisible) * interval;

                // Centre the label on its position, then keep the outermost two inside the
                // graph so neither is clipped by the window edge.
                float x = graphRect.x + 1f + offset * PixelsPerSample;
                float labelX = Mathf.Clamp(x - (ElapsedTimeLabelWidth * 0.5f), graphRect.x, graphRect.xMax - ElapsedTimeLabelWidth);
                GUI.Label(new Rect(labelX, graphRect.yMax + 1f, ElapsedTimeLabelWidth, 18f), FormatElapsedTime(elapsed), style);
            }
        }

        private static string FormatElapsedTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0.0) seconds = 0.0;
            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours >= 1.0)
                return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", (int)span.TotalHours, span.Minutes, span.Seconds);
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);
        }

        private void DrawStageMarkerLabels(Rect graphRect)
        {
            IList<FlightDataSample> samples = _manager.Samples;
            IList<FlightStageMarker> markers = _manager.StageMarkers;
            if (samples == null || samples.Count == 0 || markers == null || markers.Count == 0) return;

            int width = Math.Max(2, (int)graphRect.width);
            int visibleCapacity = Math.Max(2, ((width - 3) / PixelsPerSample) + 1);
            int firstVisible = Math.Max(0, samples.Count - visibleCapacity);
            long firstSequence = samples[firstVisible].SequenceNumber;
            long lastSequence = firstSequence + visibleCapacity - 1;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.UpperLeft
            };
            style.normal.textColor = new Color(1f, 0.65f, 0.18f, 1f);

            foreach (FlightStageMarker marker in markers)
            {
                if (marker.SampleSequence < firstSequence || marker.SampleSequence > lastSequence) continue;
                float x = graphRect.x + 3f + (float)(marker.SampleSequence - firstSequence) * PixelsPerSample;
                GUI.Label(new Rect(x + 2f, graphRect.y + 2f, 64f, 18f), "Stage " + marker.StageNumber.ToString(CultureInfo.InvariantCulture), style);
            }
        }

        private string FormatCurrentValue(FlightSensorSource source)
        {
            for (int i = _manager.Samples.Count - 1; i >= 0; i--)
            {
                double value;
                if (_manager.Samples[i].Values.TryGetValue(source.Key, out value) && !double.IsNaN(value) && !double.IsInfinity(value))
                    return value.ToString("0.###", CultureInfo.InvariantCulture);
            }
            return "n/a";
        }

        private string FormatMaximumValue(FlightSensorSource source)
        {
            if (source == null) return "n/a";
            if (string.Equals(source.Key, "flight.altitudeASL", StringComparison.Ordinal) ||
                string.Equals(source.Key, "flight.altitudeSurface", StringComparison.Ordinal))
                return "—";

            double maximum;
            return _manager.TryGetMaximum(source.Key, out maximum)
                ? maximum.ToString("0.###", CultureInfo.InvariantCulture)
                : "n/a";
        }

        private void ExportCsv()
        {
            try
            {
                List<FlightSensorSource> selected = _manager.SelectedSources();
                if (_manager.Samples.Count == 0) { _status = "No plot data to export."; return; }
                if (selected.Count == 0) { _status = "Select at least one sensor before exporting."; return; }

                string path = BuildExportPath("csv");
                using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    var header = new List<string> { "ElapsedSeconds", "UniversalTime" };
                    header.AddRange(selected.Select(s => CsvEscape(s.DisplayName + (string.IsNullOrEmpty(s.Units) ? "" : " (" + s.Units + ")"))));
                    writer.WriteLine(string.Join(",", header.ToArray()));

                    foreach (FlightDataSample sample in _manager.Samples)
                    {
                        var fields = new List<string>
                        {
                            sample.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                            sample.UniversalTime.ToString("0.###", CultureInfo.InvariantCulture)
                        };
                        foreach (FlightSensorSource source in selected)
                        {
                            double value;
                            fields.Add(sample.Values.TryGetValue(source.Key, out value) && !double.IsNaN(value)
                                ? value.ToString("R", CultureInfo.InvariantCulture)
                                : string.Empty);
                        }
                        writer.WriteLine(string.Join(",", fields.ToArray()));
                    }
                }
                _status = "CSV saved: " + path;
            }
            catch (Exception ex)
            {
                _status = "CSV export failed: " + ex.Message;
            }
        }


        // The stage-marker and elapsed-time labels are GUI overlays drawn beside the graph
        // texture, so they are absent from the texture itself. The export copies the graph
        // into a taller image and bakes both sets of labels in with the pixel font, leaving
        // a strip along the bottom for the times.
        private Texture2D BuildExportTexture()
        {
            if (_graphTexture == null) return null;

            int width = _graphTexture.width;
            int graphHeight = _graphTexture.height;
            int height = graphHeight + ExportLabelStripHeight;

            Color32[] graphPixels = _graphTexture.GetPixels32();
            var pixels = new Color32[width * height];
            Color32 background = new Color32(20, 22, 26, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = background;

            // The pixel array is bottom-up, so the graph occupies the rows above the strip.
            Array.Copy(graphPixels, 0, pixels, width * ExportLabelStripHeight, graphPixels.Length);

            IList<FlightDataSample> samples = _manager.Samples;
            if (samples != null && samples.Count > 0)
            {
                DrawExportStageMarkerLabels(pixels, width, height, samples);
                DrawExportElapsedTimeLabels(pixels, width, height, samples);
            }

            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private void DrawExportElapsedTimeLabels(Color32[] pixels, int width, int height, IList<FlightDataSample> samples)
        {
            int visibleCapacity = Math.Max(2, ((width - 3) / PixelsPerSample) + 1);
            int firstVisible = Math.Max(0, samples.Count - visibleCapacity);
            int lastVisible = samples.Count - 1;
            if (lastVisible < firstVisible) return;

            // Same axis as the on-screen labels: fixed positions across the width, with
            // times past the newest sample projected from the average interval on screen.
            double interval = _manager.SampleDelaySeconds;
            if (lastVisible > firstVisible)
            {
                double span = samples[lastVisible].ElapsedSeconds - samples[firstVisible].ElapsedSeconds;
                if (span > 0.0) interval = span / (lastVisible - firstVisible);
            }

            Color32 color = new Color32(224, 224, 224, 255);
            int labelCount = Mathf.Clamp((width / 60) + 1, 2, ElapsedTimeLabelCount);
            int labelY = height - ExportLabelStripHeight + 3;

            for (int label = 0; label < labelCount; label++)
            {
                int offset = (int)Math.Round((visibleCapacity - 1) * label / (double)(labelCount - 1));
                int sampleIndex = firstVisible + offset;
                double elapsed = sampleIndex <= lastVisible
                    ? samples[sampleIndex].ElapsedSeconds
                    : samples[lastVisible].ElapsedSeconds + (sampleIndex - lastVisible) * interval;

                string text = FormatElapsedTime(elapsed);
                int x = Mathf.Clamp(1 + offset * PixelsPerSample - MeasureText(text) / 2, 1, width - 1 - MeasureText(text));
                DrawText(pixels, width, height, x, labelY, text, color);
            }
        }

        private void DrawExportStageMarkerLabels(Color32[] pixels, int width, int height, IList<FlightDataSample> samples)
        {
            IList<FlightStageMarker> markers = _manager.StageMarkers;
            if (markers == null || markers.Count == 0) return;

            int visibleCapacity = Math.Max(2, ((width - 3) / PixelsPerSample) + 1);
            int firstVisible = Math.Max(0, samples.Count - visibleCapacity);
            long firstSequence = samples[firstVisible].SequenceNumber;
            long lastSequence = firstSequence + visibleCapacity - 1;
            Color32 color = new Color32(255, 166, 46, 255);

            foreach (FlightStageMarker marker in markers)
            {
                if (marker.SampleSequence < firstSequence || marker.SampleSequence > lastSequence) continue;
                string text = "STAGE " + marker.StageNumber.ToString(CultureInfo.InvariantCulture);
                int x = (int)(3 + (marker.SampleSequence - firstSequence) * PixelsPerSample) + 2;
                x = Mathf.Clamp(x, 1, Math.Max(1, width - 1 - MeasureText(text)));
                DrawText(pixels, width, height, x, 3, text, color);
            }
        }

        private void ExportPng()
        {
            try
            {
                if (_manager.Samples.Count == 0) { _status = "No plot data to export."; return; }
                int plottedCount = _manager.SelectedSources().Count;
                float desiredLegendHeight = 30f + plottedCount * 22f;
                float maxLegendHeight = Mathf.Max(72f, _window.height - 410f);
                float legendHeight = Mathf.Clamp(desiredLegendHeight, 72f, Mathf.Min(240f, maxLegendHeight));
                EnsureGraphTexture(Math.Max(320, (int)_window.width - 28),
                    Math.Max(180, (int)(_window.height - 265f - legendHeight)));
                Texture2D export = BuildExportTexture();
                if (export == null) { _status = "No graph to export yet."; return; }

                string path = BuildExportPath("png");
                try
                {
                    File.WriteAllBytes(path, export.EncodeToPNG());
                }
                finally
                {
                    if (!ReferenceEquals(export, _graphTexture)) UnityEngine.Object.Destroy(export);
                }
                _status = "PNG saved: " + path;
            }
            catch (Exception ex)
            {
                _status = "PNG export failed: " + ex.Message;
            }
        }

        private void SetChartTopFromCurrentBody(bool force)
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            CelestialBody body = vessel != null ? vessel.mainBody : FlightGlobals.currentMainBody;
            if (body == null) return;
            if (!force && _chartTopAltitudeMeters > 0.0) return;

            double defaultTop = GetDefaultChartTop(body);
            if (defaultTop <= 0.0) return;

            _chartTopAltitudeMeters = defaultTop;
            _chartTopText = defaultTop.ToString("0.###", CultureInfo.InvariantCulture);
            _chartTopBodyName = body.bodyName + (body.atmosphere && body.atmosphereDepth > 0.0
                ? " atmosphere height"
                : " 10% of body diameter");
            _graphDirty = true;
        }

        private static double GetDefaultChartTop(CelestialBody body)
        {
            if (body == null) return 0.0;
            if (body.atmosphere && body.atmosphereDepth > 0.0)
                return body.atmosphereDepth;

            // 10% of diameter = 20% of radius. CelestialBody.Radius is in meters.
            return Math.Max(1.0, body.Radius * 0.2);
        }

        private static string CsvEscape(string value)
        {
            if (value == null) return string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private string BuildExportPath(string extension)
        {
            string folder = string.Equals(extension, "csv", StringComparison.OrdinalIgnoreCase)
                ? GetCsvExportFolder()
                : GetPngExportFolder();
            Directory.CreateDirectory(folder);
            string save = HighLogic.CurrentGame != null ? HighLogic.CurrentGame.Title : "KSP";
            Vessel vessel = FlightGlobals.ActiveVessel;
            string vesselName = vessel != null ? vessel.vesselName : "Vessel";
            string filename = SanitizeFilename(save) + "_" + SanitizeFilename(vesselName) + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "." + extension;
            return Path.Combine(folder, filename);
        }

        private string GetCsvExportFolder()
        {
            string configured = string.IsNullOrWhiteSpace(_csvExportDirectory)
                ? DefaultCsvExportDirectory
                : _csvExportDirectory.Trim();

            if (Path.IsPathRooted(configured))
                return configured;

            configured = configured.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", configured);
        }

        private string GetPngExportFolder()
        {
            string configured = string.IsNullOrWhiteSpace(_pngExportDirectory)
                ? DefaultPngExportDirectory
                : _pngExportDirectory.Trim();

            if (Path.IsPathRooted(configured))
                return configured;

            configured = configured.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            return Path.Combine(KSPUtil.ApplicationRootPath, configured);
        }

        private static string SettingsPath
        {
            get
            {
                return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "EngineStagePlanner", "PluginData", "EngineStagePlannerSettings.cfg");
            }
        }

        private void LoadExportDirectories()
        {
            _csvExportDirectory = DefaultCsvExportDirectory;
            _pngExportDirectory = DefaultPngExportDirectory;
            try
            {
                if (!File.Exists(SettingsPath)) return;
                ConfigNode root = ConfigNode.Load(SettingsPath);
                if (root == null) return;
                ConfigNode settings = root.GetNode("ENGINE_STAGE_PLANNER_SETTINGS") ?? root;
                if (settings.HasValue("CsvExportDirectory"))
                {
                    string value = settings.GetValue("CsvExportDirectory");
                    if (!string.IsNullOrWhiteSpace(value)) _csvExportDirectory = value.Trim();
                }
                if (settings.HasValue("PngExportDirectory"))
                {
                    string value = settings.GetValue("PngExportDirectory");
                    if (!string.IsNullOrWhiteSpace(value)) _pngExportDirectory = value.Trim();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[EngineStagePlanner] Unable to load export directories: " + ex.Message);
            }
        }

        private void SaveExportDirectories()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                ConfigNode root = File.Exists(SettingsPath) ? ConfigNode.Load(SettingsPath) : null;
                if (root == null) root = new ConfigNode();
                ConfigNode settings = root.GetNode("ENGINE_STAGE_PLANNER_SETTINGS");
                if (settings == null) settings = root.AddNode("ENGINE_STAGE_PLANNER_SETTINGS");
                settings.SetValue("CsvExportDirectory", string.IsNullOrWhiteSpace(_csvExportDirectory) ? DefaultCsvExportDirectory : _csvExportDirectory.Trim(), true);
                settings.SetValue("PngExportDirectory", string.IsNullOrWhiteSpace(_pngExportDirectory) ? DefaultPngExportDirectory : _pngExportDirectory.Trim(), true);
                root.Save(SettingsPath);
            }
            catch (Exception ex)
            {
                _status = "Unable to save export folders: " + ex.Message;
                Debug.LogWarning("[EngineStagePlanner] Unable to save export directories: " + ex.Message);
            }
        }

        private static string SanitizeFilename(string value)
        {
            if (string.IsNullOrEmpty(value)) return "KSP";
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
            return value;
        }

        private static void DrawColorSwatch(Color32 color)
        {
            Rect rect = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f), GUILayout.Height(14f));
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void DrawHorizontal(Color32[] pixels, int width, int height, int y, Color32 color)
        {
            if (y < 0 || y >= height) return;
            int offset = y * width;
            for (int x = 0; x < width; x++) pixels[offset + x] = color;
        }

        private static void DrawVertical(Color32[] pixels, int width, int height, int x, Color32 color)
        {
            if (x < 0 || x >= width) return;
            for (int y = 0; y < height; y++) pixels[y * width + x] = color;
        }

        private static void DrawLine(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                SetPixel(pixels, width, height, x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private static void SetPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            pixels[y * width + x] = color;
        }

        private static void DrawResizeHandle(Rect window, ref bool resizing, ref Vector2 pendingDelta)
        {
            Rect handle = new Rect(Math.Max(0f, window.width - 20f), Math.Max(0f, window.height - 20f), 18f, 18f);
            GUI.Box(handle, "//");
            Event e = Event.current;
            if (e == null) return;
            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition))
            {
                resizing = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && resizing)
            {
                pendingDelta += e.delta;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && resizing)
            {
                resizing = false;
                e.Use();
            }
        }

        private static void ApplyResize(ref Rect window, ref Vector2 pendingDelta, float minWidth, float minHeight)
        {
            if (pendingDelta.sqrMagnitude <= 0f) return;
            float maxWidth = Mathf.Max(minWidth, Screen.width - ScreenMargin);
            float maxHeight = Mathf.Max(minHeight, Screen.height - ScreenMargin);
            window.width = Mathf.Clamp(window.width + pendingDelta.x, minWidth, maxWidth);
            window.height = Mathf.Clamp(window.height + pendingDelta.y, minHeight, maxHeight);
            pendingDelta = Vector2.zero;
        }

        private static void KeepSizeOnScreen(ref Rect window, float minWidth, float minHeight)
        {
            float maxWidth = Mathf.Max(minWidth, Screen.width - ScreenMargin);
            float maxHeight = Mathf.Max(minHeight, Screen.height - ScreenMargin);
            window.width = Mathf.Clamp(window.width, minWidth, maxWidth);
            window.height = Mathf.Clamp(window.height, minHeight, maxHeight);
        }

        private static void ClampWindow(ref Rect window)
        {
            window.x = Mathf.Clamp(window.x, -window.width + 40f, Screen.width - 40f);
            window.y = Mathf.Clamp(window.y, 0f, Screen.height - 30f);
        }
    }
}
