using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VesselPlanner.KSP
{
    /// <summary>
    /// Creates and caches small thumbnails from KSP's AvailablePart icon prefabs.
    /// The cache is shared by VesselPlanner's IMGUI part lists (Planning, Analyze Existing,
    /// and Stage-By-Stage), where rendering a fresh 3D icon every repaint would be unnecessarily expensive.
    /// </summary>
    internal static class PartThumbnailCache
    {
        private const int ThumbnailPixels = 64;
        private const int PreviewPixels = 192;
        private const int ThumbnailLayer = 31;
        private const int RotatingPreviewLayer = 30;
        private const float RotatingPreviewDegreesPerSecond = 18f;
        private static readonly Color32 ChromaKey = new Color32(3, 251, 113, 255);
        private static readonly Dictionary<string, Texture2D> Thumbnails =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> FailedParts =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // The enlarged hover image is intentionally not cached as a static texture.  KSP's
        // editor part list presents a slowly rotating 3D icon, so VesselPlanner keeps one
        // lightweight off-screen preview scene alive for whichever part is currently hovered.
        // The normal 64 px list thumbnails remain static and cached.
        private static string RotatingPreviewPartKey;
        private static GameObject RotatingPreviewClone;
        private static GameObject RotatingPreviewPivot;
        private static GameObject RotatingPreviewCameraObject;
        private static GameObject RotatingPreviewKeyLightObject;
        private static GameObject RotatingPreviewFillLightObject;
        private static Camera RotatingPreviewCamera;
        private static RenderTexture RotatingPreviewRenderTexture;
        private static Texture2D RotatingPreviewTexture;
        private static float RotatingPreviewStartTime;
        private static float RotatingPreviewLastAngle = float.NaN;

        public static Texture2D Get(string plannedPartName)
        {
            return Get(plannedPartName, null);
        }

        public static Texture2D Get(string plannedPartName, string partUrl)
        {
            return Get(plannedPartName, partUrl, ThumbnailPixels);
        }

        public static Texture2D GetPreview(string plannedPartName, string partUrl)
        {
            return Get(plannedPartName, partUrl, PreviewPixels);
        }

        public static Texture2D GetRotatingPreview(string plannedPartName, string partUrl)
        {
            string partName = BasePartName(plannedPartName);
            if (string.IsNullOrEmpty(partName) && string.IsNullOrEmpty(partUrl)) return null;

            AvailablePart available = ResolveAvailablePart(partName, partUrl);
            if (available == null || available.iconPrefab == null) return null;

            string partKey = !string.IsNullOrEmpty(partUrl) ? "url:" + partUrl : "name:" + partName;
            if (!string.Equals(RotatingPreviewPartKey, partKey, StringComparison.OrdinalIgnoreCase) ||
                /* RotatingPreviewClone == null || */ RotatingPreviewTexture == null)
            {
                //if (!BuildRotatingPreview(available, partKey))
                //    return null;
                VesselPlanner.Core.PartIconRenderer.StartRotatingPreview(available, partKey);

            }

            RenderRotatingPreview();
            RotatingPreviewTexture = VesselPlanner.Core.PartIconRenderer.GetRotatingPreviewFrameForTime(partKey, Time.time, 60f);
            RotatingPreviewPartKey = partKey;

            return RotatingPreviewTexture;
        }

        private static Texture2D Get(string plannedPartName, string partUrl, int renderPixels)
        {
            string partName = BasePartName(plannedPartName);
            if (string.IsNullOrEmpty(partName) && string.IsNullOrEmpty(partUrl)) return null;

            // partUrl identifies one exact AvailablePart in GameData.  Internal part names are
            // normally unique, but mod packs can contain replacements or duplicate names; using
            // FirstOrDefault(name) in that case rendered a perfectly valid thumbnail for the
            // wrong part.  URL-backed rows therefore cache and resolve by URL first.  Include
            // render size in the cache key so the hover preview can stay sharp without making
            // every small list thumbnail expensive.
            string partKey = !string.IsNullOrEmpty(partUrl) ? "url:" + partUrl : "name:" + partName;
            string cacheKey = renderPixels.ToString() + ":" + partKey;

            Texture2D cached;
            if (Thumbnails.TryGetValue(cacheKey, out cached)) return cached;
            if (FailedParts.Contains(cacheKey)) return null;

            AvailablePart available = ResolveAvailablePart(partName, partUrl);
            Debug.Log("[PartIconRenderer] Get: partName=" + partName + ", partUrl=" + partUrl + ", available=" + (available != null ? available.name : "null"));

            if (available == null)
            {
                FailedParts.Add(cacheKey);
                return null;
            }
            try
            {
                Texture2D thumbnail = RenderThumbnail(available, renderPixels);
                VesselPlanner.Core.PartIconRenderer.WriteImageToDisk(available, thumbnail);
                if (thumbnail == null)
                {
                    FailedParts.Add(cacheKey);
                    return null;
                }

                Thumbnails[cacheKey] = thumbnail;
                return thumbnail;
            }
            catch (Exception ex)
            {
                FailedParts.Add(cacheKey);
                Debug.LogWarning("[VesselPlanner] Unable to render thumbnail for " + (string.IsNullOrEmpty(partUrl) ? partName : partUrl) + ": " + ex.Message);
                return null;
            }
        }

        private static AvailablePart ResolveAvailablePart(string partName, string partUrl)
        {
            if (PartLoader.LoadedPartsList == null) return null;

            if (!string.IsNullOrEmpty(partUrl))
            {
                AvailablePart byUrl = PartLoader.LoadedPartsList.FirstOrDefault(p => p != null &&
                    string.Equals(p.partUrl, partUrl, StringComparison.OrdinalIgnoreCase));
                if (byUrl != null) return byUrl;
            }

            if (string.IsNullOrEmpty(partName)) return null;
            return PartLoader.LoadedPartsList.FirstOrDefault(p => p != null &&
                string.Equals(p.name, partName, StringComparison.OrdinalIgnoreCase));
        }

        public static void Clear()
        {
            foreach (Texture2D texture in Thumbnails.Values)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
            Thumbnails.Clear();
            FailedParts.Clear();
            VesselPlanner.Core.PartIconRenderer.DisposeRotatingPreview();
        }

        private static void RenderRotatingPreview()
        {
            if (RotatingPreviewPivot == null || RotatingPreviewCamera == null ||
                RotatingPreviewRenderTexture == null || RotatingPreviewTexture == null) return;

            float angle = Mathf.Repeat(
                (Time.realtimeSinceStartup - RotatingPreviewStartTime) * RotatingPreviewDegreesPerSecond, 360f);

            // OnGUI can issue more than one repaint at effectively the same instant.  Avoid a
            // redundant GPU readback unless the visible angle has actually changed.
            if (!float.IsNaN(RotatingPreviewLastAngle) &&
                Mathf.Abs(Mathf.DeltaAngle(RotatingPreviewLastAngle, angle)) < 0.75f)
                return;

            RotatingPreviewPivot.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            RotatingPreviewCamera.targetTexture = RotatingPreviewRenderTexture;
            RotatingPreviewCamera.Render();
            RotatingPreviewCamera.targetTexture = null;

            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = RotatingPreviewRenderTexture;
                RotatingPreviewTexture.ReadPixels(
                    new Rect(0f, 0f, PreviewPixels, PreviewPixels), 0, 0, false);

                Color32[] pixels = RotatingPreviewTexture.GetPixels32();
                Color32 renderedChromaKey = pixels.Length > 0 ? pixels[0] : ChromaKey;
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    pixel.a = IsChromaKey(pixel, renderedChromaKey) ? (byte)0 : (byte)255;
                    pixels[i] = pixel;
                }

                RotatingPreviewTexture.SetPixels32(pixels);
                // Keep the texture readable because the same allocation is refreshed while hovered.
                RotatingPreviewTexture.Apply(false, false);
                RotatingPreviewLastAngle = angle;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static Texture2D RenderThumbnail(AvailablePart available, int renderPixels)
        {
            if (available == null) return null;

            Texture2D a = VesselPlanner.Core.PartIconRenderer.RenderIcon(available, renderPixels);
            return a;
        }


        private static bool IsChromaKey(Color32 color, Color32 renderedKey)
        {
            // Allow a small tolerance for antialiasing/color conversion on the render target.
            return Math.Abs(color.r - renderedKey.r) <= 3 &&
                   Math.Abs(color.g - renderedKey.g) <= 3 &&
                   Math.Abs(color.b - renderedKey.b) <= 3;
        }

        private static GameObject CreateDirectionalLight(string name, Quaternion rotation, float intensity)
        {
            return CreateDirectionalLight(name, rotation, intensity, ThumbnailLayer);
        }

        private static GameObject CreateDirectionalLight(string name, Quaternion rotation, float intensity, int layer)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.layer = layer;
            lightObject.transform.rotation = rotation;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.cullingMask = 1 << layer;
            return lightObject;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null) return;
            root.layer = layer;
            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                    SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static string BasePartName(string plannedPartName)
        {
            if (string.IsNullOrEmpty(plannedPartName)) return plannedPartName;
            int modeSeparator = plannedPartName.LastIndexOf(':');
            if (modeSeparator > 0 && modeSeparator < plannedPartName.Length - 1)
            {
                int ignored;
                if (int.TryParse(plannedPartName.Substring(modeSeparator + 1), out ignored))
                    return plannedPartName.Substring(0, modeSeparator);
            }
            return plannedPartName;
        }
    }
}
