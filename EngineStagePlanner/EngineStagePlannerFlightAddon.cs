using EngineStagePlanner.UI;
using KSP.UI.Screens;
using ToolbarControl_NS;
using UnityEngine;


namespace EngineStagePlanner
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class EngineStagePlannerFlightAddon : UnityEngine.MonoBehaviour
    {
        private EngineStagePlanner.UI.FlightGraphWindow _window;
        private ToolbarControl_NS.ToolbarControl _toolbarControl;
        private bool _lastVisible;

        private void Start()
        {
            _window = new EngineStagePlanner.UI.FlightGraphWindow();
            _window.Initialize();
            CreateToolbarButton();
            _lastVisible = !_window.Visible;
            SyncToolbarButton();
            UnityEngine.Debug.Log("[EngineStagePlanner] Flight telemetry loaded");

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
            _toolbarControl = gameObject.AddComponent<ToolbarControl_NS.ToolbarControl>();
            _toolbarControl.AddToAllToolbars(
                ShowWindow,
                HideWindow,
                ApplicationLauncher.AppScenes.FLIGHT,
                EngineStagePlannerEditorAddon.MODID,
                "engineStagePlannerButton",
                "EngineStagePlanner/PluginData/Textures/icon_38",
                "EngineStagePlanner/PluginData/Textures/icon_24",
                EngineStagePlannerEditorAddon.MODNAME);
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
