using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EngineStagePlanner.Flight
{
    public sealed class FlightSensorSource
    {
        public string Key;
        public string Group;
        public string DisplayName;
        public string Units;
        public bool IsSelected;
        public Func<Vessel, double> ReadValue;
    }

    public sealed class FlightDataSample
    {
        public long SequenceNumber;
        public double ElapsedSeconds;
        public double UniversalTime;
        public readonly Dictionary<string, double> Values = new Dictionary<string, double>(StringComparer.Ordinal);
    }

    public sealed class FlightStageMarker
    {
        public long SampleSequence;
        public double ElapsedSeconds;
        public double UniversalTime;
        public int StageNumber;
    }

    public sealed class FlightSensorManager
    {
        private readonly List<FlightSensorSource> _sources = new List<FlightSensorSource>();
        private readonly List<FlightDataSample> _samples = new List<FlightDataSample>();
        private readonly List<FlightStageMarker> _stageMarkers = new List<FlightStageMarker>();
        private readonly Dictionary<string, bool> _selectionMemory = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, double>> _maximaByVessel = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
        private Guid _lastVesselId = Guid.Empty;
        private float _lastDynamicRefreshRealtime = -1000f;
        private float _lastSampleRealtime = -1000f;
        private double _startUniversalTime = double.NaN;
        private Guid _launchTrackingVesselId = Guid.Empty;
        private Vessel.Situations _lastSituation = Vessel.Situations.PRELAUNCH;
        private bool _haveLastSituation;
        private bool _wasGamePaused;
        private bool _pauseEventActive;
        private bool _pauseEventsRegistered;
        private bool _stageEventRegistered;
        private string _currentVesselKey = string.Empty;
        private bool _maximaDirty;
        private float _lastMaximaSaveRealtime = -1000f;
        private int _lastCurrentStage;
        private bool _haveLastCurrentStage;
        private long _nextSampleSequence;
        private bool _hasPendingLaunchStage;
        private int _pendingLaunchStageNumber = -1;

        public bool IsRecording { get; set; }
        public bool StartOnLaunchArmed { get; set; }
        public bool IsGamePaused { get; private set; }
        public bool AutoStartedThisUpdate { get; private set; }
        public float SampleDelaySeconds { get; set; } = 0.25f;
        public int MaxSamples { get; set; } = 5000;
        public int SelectionRevision { get; private set; }
        public int MaximaRevision { get; private set; }
        public int StageMarkerRevision { get; private set; }
        public bool CanArmStartOnLaunch
        {
            get
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                return vessel != null && vessel.situation == Vessel.Situations.PRELAUNCH && !IsRecording;
            }
        }

        public IList<FlightSensorSource> Sources { get { return _sources; } }
        public IList<FlightDataSample> Samples { get { return _samples; } }
        public IList<FlightStageMarker> StageMarkers { get { return _stageMarkers; } }

        public void Initialize()
        {
            LoadMaxima();
            RegisterPauseEvents();
            RegisterStageEvent();
            Vessel vessel = FlightGlobals.ActiveVessel;
            RefreshSources(vessel, true);
            InitializeLaunchTracking(vessel);
            IsGamePaused = DetectGamePaused();
            _wasGamePaused = IsGamePaused;
        }

        public void Dispose()
        {
            SaveMaxima();
            if (_pauseEventsRegistered)
            {
                GameEvents.onGamePause.Remove(OnGamePause);
                GameEvents.onGameUnpause.Remove(OnGameUnpause);
                _pauseEventsRegistered = false;
            }
            if (_stageEventRegistered)
            {
                GameEvents.onStageActivate.Remove(OnStageActivate);
                _stageEventRegistered = false;
            }
        }

        private void RegisterPauseEvents()
        {
            if (_pauseEventsRegistered) return;
            GameEvents.onGamePause.Add(OnGamePause);
            GameEvents.onGameUnpause.Add(OnGameUnpause);
            _pauseEventsRegistered = true;
        }

        private void RegisterStageEvent()
        {
            if (_stageEventRegistered) return;
            GameEvents.onStageActivate.Add(OnStageActivate);
            _stageEventRegistered = true;
        }

        private void OnStageActivate(int stageNumber)
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null) return;

            // KSP's onStageActivate event directly supplies the activated inverse-stage number.
            // Prefer this over polling currentStage because currentStage can update at a different
            // point in KSP's staging/update sequence.
            if (IsRecording)
            {
                AddStageMarker(stageNumber);
            }
            else if (StartOnLaunchArmed)
            {
                // Launch staging can fire before the vessel situation changes out of PRELAUNCH.
                // Preserve it until UpdateLaunchTracking starts the recording and resets the plot.
                _pendingLaunchStageNumber = stageNumber;
                _hasPendingLaunchStage = true;
            }

            _lastCurrentStage = stageNumber;
            _haveLastCurrentStage = true;
        }

        private void OnGamePause()
        {
            _pauseEventActive = true;
            IsGamePaused = true;
            _wasGamePaused = true;
            _lastSampleRealtime = Time.realtimeSinceStartup;
        }

        private void OnGameUnpause()
        {
            _pauseEventActive = false;
            // Keep _wasGamePaused set until Update so the configured delay starts again from resume.
            _wasGamePaused = true;
        }

        private bool DetectGamePaused()
        {
            return _pauseEventActive || Planetarium.Pause || Time.timeScale <= 0.0001f;
        }

        public void Update()
        {
            AutoStartedThisUpdate = false;

            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null) return;

            UpdateLaunchTracking(vessel);

            float realtime = Time.realtimeSinceStartup;
            if (vessel.id != _lastVesselId || realtime - _lastDynamicRefreshRealtime >= 2f)
                RefreshSources(vessel, false);

            IsGamePaused = DetectGamePaused();
            if (IsGamePaused)
            {
                _wasGamePaused = true;
                // Update runs on real time even while KSP is paused. Keep moving the sample clock
                // forward so paused real time can never satisfy the inter-sample delay.
                _lastSampleRealtime = realtime;
                return;
            }

            // Do not take an immediate catch-up sample after leaving the pause menu.
            if (_wasGamePaused)
            {
                _wasGamePaused = false;
                _lastSampleRealtime = realtime;
            }

            DetectStageTransition(vessel);

            if (!IsRecording) return;
            float delay = Mathf.Clamp(SampleDelaySeconds, 0.05f, 60f);
            if (realtime - _lastSampleRealtime < delay) return;

            _lastSampleRealtime = realtime;
            CaptureSample(vessel);
            SaveMaximaIfDue(realtime);
        }

        private void InitializeLaunchTracking(Vessel vessel)
        {
            if (vessel == null)
            {
                _launchTrackingVesselId = Guid.Empty;
                _haveLastSituation = false;
                return;
            }

            _launchTrackingVesselId = vessel.id;
            _lastSituation = vessel.situation;
            _haveLastSituation = true;
            _lastCurrentStage = vessel.currentStage;
            _haveLastCurrentStage = true;
        }

        private void UpdateLaunchTracking(Vessel vessel)
        {
            if (vessel == null) return;

            if (!_haveLastSituation || vessel.id != _launchTrackingVesselId)
            {
                InitializeLaunchTracking(vessel);
                return;
            }

            Vessel.Situations current = vessel.situation;
            if (StartOnLaunchArmed &&
                _lastSituation == Vessel.Situations.PRELAUNCH &&
                current != Vessel.Situations.PRELAUNCH)
            {
                // Preserve the pre-launch stage number before Reset() updates the stage baseline.
                // Launch commonly occurs on the same frame as the first staging activation.
                int stageBeforeLaunch = _lastCurrentStage;
                int stageAfterLaunch = vessel.currentStage;
                bool hadPendingLaunchStage = _hasPendingLaunchStage;
                int pendingLaunchStage = _pendingLaunchStageNumber;

                Reset();
                IsRecording = true;
                StartOnLaunchArmed = false;
                AutoStartedThisUpdate = true;

                if (hadPendingLaunchStage)
                {
                    AddStageMarker(pendingLaunchStage);
                }
                else if (stageAfterLaunch < stageBeforeLaunch)
                {
                    // Fallback for staging paths that do not fire onStageActivate.
                    for (int stage = stageBeforeLaunch - 1; stage >= stageAfterLaunch; stage--)
                        AddStageMarker(stage);
                }
            }

            _lastSituation = current;
        }

        public void Reset()
        {
            _samples.Clear();
            _stageMarkers.Clear();
            _nextSampleSequence = 0;
            _hasPendingLaunchStage = false;
            _pendingLaunchStageNumber = -1;
            StageMarkerRevision++;
            _startUniversalTime = double.NaN;
            _lastSampleRealtime = -1000f;
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel != null)
            {
                _lastCurrentStage = vessel.currentStage;
                _haveLastCurrentStage = true;
            }
            else
            {
                _haveLastCurrentStage = false;
            }
        }

        private void DetectStageTransition(Vessel vessel)
        {
            if (vessel == null) return;

            if (!_haveLastCurrentStage || vessel.id != _launchTrackingVesselId)
            {
                _lastCurrentStage = vessel.currentStage;
                _haveLastCurrentStage = true;
                return;
            }

            int currentStage = vessel.currentStage;
            if (currentStage < _lastCurrentStage && IsRecording)
            {
                // Fallback for unusual staging code that bypasses GameEvents.onStageActivate.
                // AddStageMarker de-duplicates the normal event + polling path.
                for (int stage = _lastCurrentStage - 1; stage >= currentStage; stage--)
                    AddStageMarker(stage);
            }

            _lastCurrentStage = currentStage;
        }

        private void AddStageMarker(int stageNumber)
        {
            double ut = Planetarium.GetUniversalTime();

            // The event path and the currentStage polling fallback can both observe the same
            // staging operation. Suppress only a near-simultaneous duplicate of the same stage.
            if (_stageMarkers.Count > 0)
            {
                FlightStageMarker last = _stageMarkers[_stageMarkers.Count - 1];
                if (last.StageNumber == stageNumber && Math.Abs(ut - last.UniversalTime) < 0.25)
                    return;
            }

            double elapsed = double.IsNaN(_startUniversalTime) ? 0.0 : Math.Max(0.0, ut - _startUniversalTime);
            _stageMarkers.Add(new FlightStageMarker
            {
                SampleSequence = _nextSampleSequence,
                UniversalTime = ut,
                ElapsedSeconds = elapsed,
                StageNumber = stageNumber
            });
            StageMarkerRevision++;
        }

        public void RefreshNow()
        {
            RefreshSources(FlightGlobals.ActiveVessel, false);
        }

        public void SetSelected(string key, bool selected)
        {
            FlightSensorSource source = _sources.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.Ordinal));
            if (source == null || source.IsSelected == selected) return;
            source.IsSelected = selected;
            _selectionMemory[key] = selected;
            SelectionRevision++;
        }

        public void SetGroupSelected(string group, bool selected)
        {
            bool changed = false;
            foreach (FlightSensorSource source in _sources.Where(s => string.Equals(s.Group, group, StringComparison.Ordinal)))
            {
                if (source.IsSelected == selected) continue;
                source.IsSelected = selected;
                _selectionMemory[source.Key] = selected;
                changed = true;
            }
            if (changed) SelectionRevision++;
        }

        public List<FlightSensorSource> SelectedSources()
        {
            return _sources.Where(s => s.IsSelected).ToList();
        }

        private void RefreshSources(Vessel vessel, bool initial)
        {
            SetCurrentVessel(vessel);
            foreach (FlightSensorSource source in _sources)
                _selectionMemory[source.Key] = source.IsSelected;

            _sources.Clear();
            AddBuiltInSources();

            if (vessel != null)
            {
                AddResourceSources(vessel);
                AddEnvironmentSensorSources(vessel);
                _lastVesselId = vessel.id;
            }

            _lastDynamicRefreshRealtime = Time.realtimeSinceStartup;
            SelectionRevision++;
        }

        private void AddBuiltInSources()
        {
            AddSource("flight.surfaceVelocity", "Flight data", "Velocity (surface)", "m/s", true,
                v => v.srf_velocity.magnitude);
            AddSource("flight.orbitVelocity", "Flight data", "Velocity (orbit)", "m/s", false,
                v => v.obt_velocity.magnitude);
            AddSource("flight.verticalSpeed", "Flight data", "Vertical speed", "m/s", true,
                v => v.verticalSpeed);
            AddSource("flight.acceleration", "Flight data", "Acceleration", "m/s²", false,
                v => v.acceleration.magnitude);
            AddSource("flight.gforce", "Flight data", "G-Force", "g", false,
                v => v.geeForce_immediate);
            AddSource("flight.altitudeASL", "Flight data", "Altitude (ASL)", "m", true,
                v => v.altitude);
            AddSource("flight.altitudeSurface", "Flight data", "Altitude (surface)", "m", false,
                v => v.heightFromTerrain);
            AddSource("flight.dynamicPressure", "Flight data", "Dynamic pressure (q)", "Pa", true,
                v => 0.5 * v.atmDensity * v.srf_velocity.sqrMagnitude);
            AddSource("flight.mass", "Flight data", "Vehicle mass", "t", false,
                v => v.GetTotalMass());
            AddSource("flight.aoa", "Flight data", "Angle of attack", "deg", false,
                v => v.srf_velocity.sqrMagnitude > 1e-8 ? Vector3.Angle(v.transform.up, v.srf_velocity) : 0.0);
        }

        private void AddResourceSources(Vessel vessel)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Part part in vessel.parts)
            {
                if (part == null || part.Resources == null) continue;
                for (int i = 0; i < part.Resources.Count; i++)
                {
                    PartResource resource = part.Resources[i];
                    if (resource != null && !string.IsNullOrEmpty(resource.resourceName))
                        names.Add(resource.resourceName);
                }
            }

            foreach (string resourceName in names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                string capturedName = resourceName;
                AddSource("resource." + capturedName, "Ship resources", capturedName, "units", false,
                    v => SumResource(v, capturedName));
            }
        }

        private void AddEnvironmentSensorSources(Vessel vessel)
        {
            // Use a stable key based on the part type and sensor index rather than flightID.
            // flightID can change after a revert/relaunch, which would otherwise lose the
            // historical maximum for that sensor. Identical sensor parts are intentionally
            // collapsed to one source because they report the same vessel environment value.
            var addedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Part part in vessel.parts)
            {
                if (part == null || part.Modules == null) continue;
                int sensorIndex = 0;
                foreach (ModuleEnviroSensor sensor in part.Modules.OfType<ModuleEnviroSensor>())
                {
                    ModuleEnviroSensor capturedSensor = sensor;
                    string partName = string.IsNullOrEmpty(part.name) ? "Part" : part.name;
                    string partTitle = part.partInfo != null ? part.partInfo.title : part.name;
                    string key = "sensor." + partName + "." + sensorIndex.ToString(CultureInfo.InvariantCulture);
                    sensorIndex++;
                    if (!addedKeys.Add(key)) continue;

                    AddSource(key, "Sensor outputs", partTitle, "", false,
                        v => ParseSensorReadout(capturedSensor));
                }
            }
        }

        private void AddSource(string key, string group, string name, string units, bool defaultSelected, Func<Vessel, double> reader)
        {
            bool selected;
            if (!_selectionMemory.TryGetValue(key, out selected)) selected = defaultSelected;
            _sources.Add(new FlightSensorSource
            {
                Key = key,
                Group = group,
                DisplayName = name,
                Units = units,
                IsSelected = selected,
                ReadValue = reader
            });
            _selectionMemory[key] = selected;
        }

        private void CaptureSample(Vessel vessel)
        {
            double ut = Planetarium.GetUniversalTime();
            if (double.IsNaN(_startUniversalTime)) _startUniversalTime = ut;

            var sample = new FlightDataSample
            {
                SequenceNumber = _nextSampleSequence++,
                UniversalTime = ut,
                ElapsedSeconds = ut - _startUniversalTime
            };

            foreach (FlightSensorSource source in _sources)
            {
                try
                {
                    double value = source.ReadValue != null ? source.ReadValue(vessel) : double.NaN;
                    if (!double.IsInfinity(value))
                    {
                        sample.Values[source.Key] = value;
                        if (!double.IsNaN(value) && !IsAltitudeSource(source.Key))
                            UpdateMaximum(source.Key, value);
                    }
                }
                catch
                {
                    sample.Values[source.Key] = double.NaN;
                }
            }

            _samples.Add(sample);
            int max = Math.Max(100, MaxSamples);
            if (_samples.Count > max)
                _samples.RemoveRange(0, _samples.Count - max);
        }

        public bool TryGetMaximum(string sourceKey, out double maximum)
        {
            maximum = 0.0;
            if (string.IsNullOrEmpty(_currentVesselKey)) return false;
            Dictionary<string, double> vesselMaxima;
            return _maximaByVessel.TryGetValue(_currentVesselKey, out vesselMaxima) &&
                   vesselMaxima.TryGetValue(sourceKey, out maximum);
        }

        public bool ClearMaximaForCurrentVessel()
        {
            if (string.IsNullOrEmpty(_currentVesselKey)) return false;

            bool removed = _maximaByVessel.Remove(_currentVesselKey);
            if (!removed) return false;

            _maximaDirty = true;
            MaximaRevision++;
            // A user-requested clear should be durable immediately rather than waiting for
            // the normal periodic save used while recording.
            SaveMaxima();
            return true;
        }

        private void SetCurrentVessel(Vessel vessel)
        {
            string newKey = GetVesselKey(vessel);
            if (string.Equals(newKey, _currentVesselKey, StringComparison.Ordinal)) return;
            _currentVesselKey = newKey;
            MaximaRevision++;
        }

        private static string GetVesselKey(Vessel vessel)
        {
            if (vessel == null) return string.Empty;
            string saveName = HighLogic.CurrentGame != null && !string.IsNullOrEmpty(HighLogic.CurrentGame.Title)
                ? HighLogic.CurrentGame.Title
                : "KSP";
            string vesselName = string.IsNullOrEmpty(vessel.vesselName) ? "Unnamed Vessel" : vessel.vesselName.Trim();
            return saveName + "|" + vesselName;
        }

        private static bool IsAltitudeSource(string sourceKey)
        {
            return string.Equals(sourceKey, "flight.altitudeASL", StringComparison.Ordinal) ||
                   string.Equals(sourceKey, "flight.altitudeSurface", StringComparison.Ordinal);
        }

        private void UpdateMaximum(string sourceKey, double value)
        {
            if (string.IsNullOrEmpty(_currentVesselKey) || string.IsNullOrEmpty(sourceKey)) return;

            Dictionary<string, double> vesselMaxima;
            if (!_maximaByVessel.TryGetValue(_currentVesselKey, out vesselMaxima))
            {
                vesselMaxima = new Dictionary<string, double>(StringComparer.Ordinal);
                _maximaByVessel[_currentVesselKey] = vesselMaxima;
            }

            double oldMaximum;
            if (vesselMaxima.TryGetValue(sourceKey, out oldMaximum) && value <= oldMaximum) return;

            vesselMaxima[sourceKey] = value;
            _maximaDirty = true;
            MaximaRevision++;
        }

        private void SaveMaximaIfDue(float realtime)
        {
            if (!_maximaDirty || realtime - _lastMaximaSaveRealtime < 5f) return;
            SaveMaxima();
        }

        private static string GetMaximaPath()
        {
            return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "EngineStagePlanner", "PluginData", "FlightSensorMaxima.tsv");
        }

        private void LoadMaxima()
        {
            _maximaByVessel.Clear();
            string path = GetMaximaPath();
            try
            {
                if (!File.Exists(path)) return;
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith("#", StringComparison.Ordinal)) continue;
                    string[] fields = rawLine.Split('	');
                    if (fields.Length != 3) continue;

                    string vesselKey;
                    string sensorKey;
                    try
                    {
                        vesselKey = Encoding.UTF8.GetString(Convert.FromBase64String(fields[0]));
                        sensorKey = Encoding.UTF8.GetString(Convert.FromBase64String(fields[1]));
                    }
                    catch
                    {
                        continue;
                    }

                    double value;
                    if (!double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                        double.IsNaN(value) || double.IsInfinity(value)) continue;

                    Dictionary<string, double> vesselMaxima;
                    if (!_maximaByVessel.TryGetValue(vesselKey, out vesselMaxima))
                    {
                        vesselMaxima = new Dictionary<string, double>(StringComparer.Ordinal);
                        _maximaByVessel[vesselKey] = vesselMaxima;
                    }
                    vesselMaxima[sensorKey] = value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[EngineStagePlanner] Unable to load flight sensor maxima: " + ex.Message);
            }
            finally
            {
                _maximaDirty = false;
                _lastMaximaSaveRealtime = Time.realtimeSinceStartup;
                MaximaRevision++;
            }
        }

        private void SaveMaxima()
        {
            if (!_maximaDirty) return;
            string path = GetMaximaPath();
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    writer.WriteLine("# Engine Stage Planner per-vessel sensor maxima");
                    foreach (KeyValuePair<string, Dictionary<string, double>> vesselEntry in _maximaByVessel.OrderBy(k => k.Key, StringComparer.Ordinal))
                    {
                        string encodedVessel = Convert.ToBase64String(Encoding.UTF8.GetBytes(vesselEntry.Key));
                        foreach (KeyValuePair<string, double> sensorEntry in vesselEntry.Value.OrderBy(k => k.Key, StringComparer.Ordinal))
                        {
                            string encodedSensor = Convert.ToBase64String(Encoding.UTF8.GetBytes(sensorEntry.Key));
                            writer.Write(encodedVessel);
                            writer.Write('	');
                            writer.Write(encodedSensor);
                            writer.Write('	');
                            writer.WriteLine(sensorEntry.Value.ToString("R", CultureInfo.InvariantCulture));
                        }
                    }
                }
                _maximaDirty = false;
                _lastMaximaSaveRealtime = Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[EngineStagePlanner] Unable to save flight sensor maxima: " + ex.Message);
            }
        }

        private static double SumResource(Vessel vessel, string resourceName)
        {
            double total = 0.0;
            if (vessel == null) return total;
            foreach (Part part in vessel.parts)
            {
                if (part == null || part.Resources == null) continue;
                for (int i = 0; i < part.Resources.Count; i++)
                {
                    PartResource resource = part.Resources[i];
                    if (resource != null && string.Equals(resource.resourceName, resourceName, StringComparison.OrdinalIgnoreCase))
                        total += resource.amount;
                }
            }
            return total;
        }

        private static double ParseSensorReadout(ModuleEnviroSensor sensor)
        {
            if (sensor == null || string.IsNullOrEmpty(sensor.readoutInfo)) return double.NaN;
            Match match = Regex.Match(sensor.readoutInfo, @"[-+]?\d+(?:[\.,]\d+)?(?:[eE][-+]?\d+)?");
            if (!match.Success) return double.NaN;

            string text = match.Value.Replace(',', '.');
            double value;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : double.NaN;
        }
    }
}
