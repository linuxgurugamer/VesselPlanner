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
            CreateToolbarButton();
            _lastVisible = !_window.Visible;
            SyncToolbarButton();
            Debug.Log("[VesselPlanner] Loaded");

            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);
            GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);
        }

        void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> ed)
        {
            _window.Visible = _lastVisible = false;
        }

        void onGameSceneLoadRequested(GameScenes ed)
        {
            _window.Visible = _lastVisible = false;
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

        private void ShowWindow()
        {
            if (_window != null) _window.Visible = true;
            _lastVisible = true;
        }

        private void HideWindow()
        {
            if (_window != null) _window.Visible = false;
            _lastVisible = false;
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
            GameEvents.onGameSceneLoadRequested.Remove(onGameSceneLoadRequested);
            GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);

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

