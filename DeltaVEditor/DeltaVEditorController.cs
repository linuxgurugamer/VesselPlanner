using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using static VesselPlanner.ToolbarRegistration;

namespace DeltaVEditor
{
    // The editor is started this way so that the MissionPlanner doesn't have a dependency on the editor
    // so that the DLL can be removed if desired

    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class DeltaVEditorController : MonoBehaviour
    {

        static internal string packName = "";
        static internal bool usePack = false;

        private void FixedUpdate()
        {
            if (VesselPlanner.Core.openDeltaVEditor)
            {
                DeltaVEditorWindow.Toggle();
                VesselPlanner.Core.openDeltaVEditor = false;
            }
        }

        private void Start()
        {
            var packInfo = PlanetPackHeuristics.GetPlanetPackInfo();

            Log.Info("Planet pack detected: " + packInfo);
            if (packInfo.Kind == PlanetPackKind.CustomSinglePack)
                packName = packInfo.FolderName;
            else
                packName = packInfo.Kind.ToString();

            Log.Info("Final DeltaV Planet Pack: " + packName);

        }
    }
}
