using VesselPlanner.UI;
using KSP.UI.Screens;
using ToolbarControl_NS;
using UnityEngine;


namespace VesselPlanner
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class VesselPlannerFlightAddon : UnityEngine.MonoBehaviour
    {
        private VesselPlanner.UI.FlightGraphWindow _window;
        private ToolbarControl_NS.ToolbarControl _toolbarControl;
        private bool _lastVisible;

        private void Start()
        {
            _window = new VesselPlanner.UI.FlightGraphWindow();
            _window.Initialize();
            // Windows always start closed.  This also prevents a saved/previous toolbar
            // toggle state from making the window appear as the scene initializes.
            _window.Visible = false;
            _lastVisible = false;
            CreateToolbarButton();
            ForceHideWindow();
            UnityEngine.Debug.Log("[VesselPlanner] Flight telemetry loaded");

            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);
        }

        private void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> scenes)
        {
            ForceHideWindow();
        }

        private void OnGameSceneLoadRequested(GameScenes scene)
        {
            // Hide as soon as KSP requests a scene load.  Both the editor and flight
            // addons do this so no VesselPlanner window carries an open state across
            // scene transitions.
            ForceHideWindow();
        }

        private void CreateToolbarButton()
        {
            _toolbarControl = gameObject.AddComponent<ToolbarControl_NS.ToolbarControl>();
            _toolbarControl.AddToAllToolbars(
                ShowWindow,
                HideWindow,
                ApplicationLauncher.AppScenes.FLIGHT,
                VesselPlannerEditorAddon.MODID,
                "vesselPlannerButton",
                "VesselPlanner/PluginData/Textures/icon_38",
                "VesselPlanner/PluginData/Textures/icon_24",
                VesselPlannerEditorAddon.MODNAME);
        }

        private void ForceHideWindow()
        {
            if (_window != null) _window.Visible = false;
            if (_toolbarControl != null) _toolbarControl.SetFalse(false);
            _lastVisible = false;
        }

        private void ShowWindow()
        {
            if (_window != null) _window.Visible = true;
            _lastVisible = true;
        }

        private void HideWindow()
        {
            ForceHideWindow();
        }

        private void Update()
        {
            if (_window != null)
                _window.Update();
            SyncToolbarButton();
        }

        private void SyncToolbarButton()
        {
            if (_window == null || _toolbarControl == null) return;
            if (_window.Visible == _lastVisible) return;
            if (_window.Visible) _toolbarControl.SetTrue(false);
            else _toolbarControl.SetFalse(false);
            _lastVisible = _window.Visible;
        }

        private void OnGUI()
        {
            if (_window != null) _window.Draw();
        }

        private void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);

            if (_window != null) _window.Dispose();
            if (_toolbarControl != null)
            {
                _toolbarControl.OnDestroy();
                Destroy(_toolbarControl);
                _toolbarControl = null;
            }
        }
    }
}
