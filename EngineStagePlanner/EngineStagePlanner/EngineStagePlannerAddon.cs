using EngineStagePlanner.UI;
using UnityEngine;

namespace EngineStagePlanner
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public sealed class EngineStagePlannerAddon : MonoBehaviour
    {
        private PlannerWindow _window;

        private void Start()
        {
            _window = new PlannerWindow();
            _window.Initialize();
            Debug.Log("[EngineStagePlanner] Loaded");
        }

        private void Update()
        {
            // Alt+P is an always-available toggle; no external toolbar dependency is required.
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.P))
                _window.Visible = !_window.Visible;
        }

        private void OnGUI()
        {
            if (_window != null) _window.Draw();
        }
    }
}
