using EngineStagePlanner.UI;
using KSP.UI.Screens;
using ToolbarControl_NS;
using UnityEngine;

namespace EngineStagePlanner
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class EngineStagePlannerToolbarRegistration : MonoBehaviour
    {
        private void Start()
        {
            ToolbarControl.RegisterMod(EngineStagePlannerAddon.MODID, EngineStagePlannerAddon.MODNAME);
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public sealed class EngineStagePlannerAddon : MonoBehaviour
    {
        internal const string MODID = "EngineStagePlanner_NS";
        internal const string MODNAME = "Engine Stage Planner";

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
            Debug.Log("[EngineStagePlanner] Loaded");
        }

        private void CreateToolbarButton()
        {
            _toolbarControl = gameObject.AddComponent<ToolbarControl>();
            _toolbarControl.AddToAllToolbars(
                ShowWindow,
                HideWindow,
                ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                MODID,
                "engineStagePlannerButton",
                "EngineStagePlanner/PluginData/Textures/icon_38",
                "EngineStagePlanner/PluginData/Textures/icon_24",
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

            // Alt+P remains available as a keyboard toggle in addition to ToolbarController.
            if (_window != null &&
                (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) &&
                Input.GetKeyDown(KeyCode.P))
            {
                _window.Visible = !_window.Visible;
            }

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
        }

        private void CreateToolbarButton()
        {
            _toolbarControl = gameObject.AddComponent<ToolbarControl_NS.ToolbarControl>();
            _toolbarControl.AddToAllToolbars(
                ShowWindow,
                HideWindow,
                ApplicationLauncher.AppScenes.FLIGHT,
                EngineStagePlannerAddon.MODID,
                "engineStagePlannerButton",
                "EngineStagePlanner/PluginData/Textures/icon_38",
                "EngineStagePlanner/PluginData/Textures/icon_24",
                EngineStagePlannerAddon.MODNAME);
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
            {
                _window.Update();
                if ((UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftAlt) || UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightAlt)) &&
                    UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.P))
                    _window.Visible = !_window.Visible;
            }
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
