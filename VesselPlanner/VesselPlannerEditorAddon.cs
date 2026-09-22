using VesselPlanner.UI;
using KSP.UI.Screens;
using ToolbarControl_NS;
using UnityEngine;

namespace VesselPlanner
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public sealed class VesselPlannerEditorAddon : MonoBehaviour
    {
        internal const string MODID = "VesselPlanner_NS";
        internal const string MODNAME = "VesselPlanner";

        private PlannerWindow _window;
        private ToolbarControl _toolbarControl;
        private bool _lastVisible;

        private void Start()
        {
            _window = new PlannerWindow();
            _window.Initialize();
            // Windows always start closed.  This also prevents a saved/previous toolbar
            // toggle state from making the window appear as the scene initializes.
            _window.Visible = false;
            _lastVisible = false;
            CreateToolbarButton();
            ForceHideWindow();
            Debug.Log("[VesselPlanner] Loaded");

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
            _toolbarControl = gameObject.AddComponent<ToolbarControl>();
            _toolbarControl.AddToAllToolbars(
                ShowWindow,
                HideWindow,
                ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                MODID,
                "vesselPlannerButton",
                "VesselPlanner/PluginData/Textures/icon_38",
                "VesselPlanner/PluginData/Textures/icon_24",
                MODNAME);
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
            if (_window != null) _window.Update();

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

