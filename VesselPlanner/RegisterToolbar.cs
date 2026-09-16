using ToolbarControl_NS;
using UnityEngine;
using VesselPlanner.UI;

namespace VesselPlanner
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class RegisterToolbar : MonoBehaviour
    {
        internal static Texture2D texDarker, texLighter;
        internal static GUIStyle winDarker, winLighter;
        bool initialized = false;
        internal static GUIStyle styleXButtonSettings;

        void InitWinTextures()
        {
            float shade = 0.12f;
            Color color = new Color(shade, shade, shade, 1f);
            winDarker = new GUIStyle(HighLogic.Skin.window);
            texDarker = winDarker.normal.background;
            var pixels = texDarker.GetPixels32();
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i].a = 255;
            texDarker.SetPixels32(pixels); texDarker.Apply();
            winDarker.active.background =
            winDarker.focused.background =
            winDarker.normal.background = texDarker;

            // --------------------------

            shade = 0.36f;
            color = new Color(shade, shade, shade, 1f);
            winLighter = new GUIStyle(HighLogic.Skin.window);
            texLighter = winLighter.normal.background;
            pixels = texLighter.GetPixels32();
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i].a = 255;
            texLighter.SetPixels32(pixels); texLighter.Apply();
            winLighter.active.background =
            winLighter.focused.background =
            winLighter.normal.background = texLighter;

            winLighter.active.background =
            winLighter.focused.background =
            winLighter.normal.background = texLighter;

            styleXButtonSettings = new GUIStyle(GUI.skin.button);
            styleXButtonSettings.normal.textColor = GUI.skin.button.normal.textColor;
            styleXButtonSettings.hover.textColor = GUI.skin.button.hover.textColor;

            styleXButtonSettings.name = "ButtonSettings";
            styleXButtonSettings.padding = new RectOffset(1, 1, 1, 1);
            styleXButtonSettings.onNormal.background = styleXButtonSettings.active.background;
            styleXButtonSettings.alignment = TextAnchor.MiddleCenter;
            styleXButtonSettings.normal.textColor = new Color32(177, 193, 205, 255);
            styleXButtonSettings.fontStyle = FontStyle.Bold;

        }

        private void Start()
        {
            ToolbarControl.RegisterMod(VesselPlannerEditorAddon.MODID, VesselPlannerEditorAddon.MODNAME);
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

                InitWinTextures();
            }
        }
    }
}
