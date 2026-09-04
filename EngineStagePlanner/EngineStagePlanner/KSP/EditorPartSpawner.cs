using System;
using System.Linq;

namespace EngineStagePlanner.KSP
{
    public static class EditorPartSpawner
    {
        public static bool Spawn(string partName, out string message)
        {
            message = string.Empty;
            if (EditorLogic.fetch == null)
            {
                message = "The KSP editor is not available.";
                return false;
            }
            if (string.IsNullOrEmpty(partName))
            {
                message = "No part was selected.";
                return false;
            }

            string baseName = partName.Split(':')[0];
            AvailablePart available = PartLoader.LoadedPartsList == null ? null :
                PartLoader.LoadedPartsList.FirstOrDefault(p => p != null &&
                    string.Equals(p.name, baseName, StringComparison.OrdinalIgnoreCase));
            if (available == null)
            {
                message = "Part '" + baseName + "' was not found.";
                return false;
            }

            try
            {
                EditorLogic.fetch.SpawnPart(available);
                message = "Selected " + available.title + " for placement.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Unable to instantiate " + available.title + ": " + ex.Message;
                UnityEngine.Debug.LogError("[EngineStagePlanner] SpawnPart failed: " + ex);
                return false;
            }
        }
    }
}
