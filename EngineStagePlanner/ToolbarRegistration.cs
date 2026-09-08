using EngineStagePlanner.UI;
using KSP.UI.Screens;
using ToolbarControl_NS;
using UnityEngine;

namespace EngineStagePlanner
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class ToolbarRegistration : MonoBehaviour
    {
        bool initialized = false;
        private void Start()
        {
            ToolbarControl.RegisterMod(EngineStagePlannerEditorAddon.MODID, EngineStagePlannerEditorAddon.MODNAME);
        }

        private void OnGUI()
        {
            if (!initialized)
            {
                // Capture the stock Unity skin before anything overrides it, then apply
                // whichever skin the settings ask for.
                WindowSkin.CaptureAltSkin();
                WindowSkin.Load();
                WindowSkin.Apply();

                PlannerWindow.EnsureStyles();
                FlightGraphWindow.InitStyles();
                initialized = true;
            }
        }
    }
}
