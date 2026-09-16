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
        private static float IconZoomFactor = 0.8f;
        private static float RotatingImageZoomFactor = 1.0f;
        private static float CameraYawDegrees = 45f;
        private static float CameraPitchDegrees = 20f;
        private static int RotatingImageBackground = 0;
        private static int RotatingPreviewSize = 100;
        private static int RotatingPreviewDegreesPerFrame = 60;
        private static readonly Dictionary<string, Texture2D> Thumbnails =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> FailedParts =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // The enlarged hover image is intentionally not cached as a static texture.  KSP's
        // editor part list presents a slowly rotating 3D icon, so VesselPlanner keeps one
        // lightweight off-screen preview scene alive for whichever part is currently hovered.
        // The normal 64 px list thumbnails remain static and cached.
        private static string RotatingPreviewPartKey;
        private static Texture2D RotatingPreviewTexture;

        public static void Configure(float iconZoomFactor, float rotatingImageZoomFactor, float cameraYawDegrees, float cameraPitchDegrees, int rotatingImageBackground, int rotatingPreviewSize, int rotatingPreviewDegreesPerFrame)
        {
            iconZoomFactor = Mathf.Clamp(iconZoomFactor, 0.1f, 5f);
            rotatingImageZoomFactor = Mathf.Clamp(rotatingImageZoomFactor, 0.1f, 5f);
            cameraYawDegrees = Mathf.Clamp(cameraYawDegrees, 0f, 180f);
            cameraPitchDegrees = Mathf.Clamp(cameraPitchDegrees, 0f, 90f);
            rotatingImageBackground = Mathf.Clamp(rotatingImageBackground, 0, 4);
            rotatingPreviewSize = Mathf.Clamp(rotatingPreviewSize, 32, 256);
            rotatingPreviewDegreesPerFrame = Mathf.Clamp(rotatingPreviewDegreesPerFrame, 1, 180);

            bool changed = Math.Abs(IconZoomFactor - iconZoomFactor) > 0.0001f ||
                           Math.Abs(RotatingImageZoomFactor - rotatingImageZoomFactor) > 0.0001f ||
                           Math.Abs(CameraYawDegrees - cameraYawDegrees) > 0.0001f ||
                           Math.Abs(CameraPitchDegrees - cameraPitchDegrees) > 0.0001f ||
                           RotatingImageBackground != rotatingImageBackground ||
                           RotatingPreviewSize != rotatingPreviewSize ||
                           RotatingPreviewDegreesPerFrame != rotatingPreviewDegreesPerFrame;

            IconZoomFactor = iconZoomFactor;
            RotatingImageZoomFactor = rotatingImageZoomFactor;
            CameraYawDegrees = cameraYawDegrees;
            CameraPitchDegrees = cameraPitchDegrees;
            RotatingImageBackground = rotatingImageBackground;
            RotatingPreviewSize = rotatingPreviewSize;
            RotatingPreviewDegreesPerFrame = rotatingPreviewDegreesPerFrame;

            if (changed) Clear();
        }

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

        private static void GetRotatingBackground(out bool transparentBackground, out Color backgroundColor)
        {
            transparentBackground = RotatingImageBackground == 0;
            switch (RotatingImageBackground)
            {
                case 1: backgroundColor = Color.black; break;
                case 2: backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f); break;
                case 3: backgroundColor = Color.gray; break;
                case 4: backgroundColor = Color.white; break;
                default: backgroundColor = Color.black; break;
            }
        }

        public static Texture2D GetRotatingPreview(string plannedPartName, string partUrl)
        {
            string partName = BasePartName(plannedPartName);
            if (string.IsNullOrEmpty(partName) && string.IsNullOrEmpty(partUrl)) return null;

            AvailablePart available = ResolveAvailablePart(partName, partUrl);
            if (available == null || available.iconPrefab == null) return null;

            string partKey = !string.IsNullOrEmpty(partUrl) ? "url:" + partUrl : "name:" + partName;
            if (!string.Equals(RotatingPreviewPartKey, partKey, StringComparison.OrdinalIgnoreCase) ||
                RotatingPreviewTexture == null ||
                !VesselPlanner.Core.PartIconRenderer.HasRotatingPreview(partKey))
            {
                //if (!BuildRotatingPreview(available, partKey))
                //    return null;
                bool transparentBackground;
                Color backgroundColor;
                GetRotatingBackground(out transparentBackground, out backgroundColor);
                VesselPlanner.Core.PartIconRenderer.StartRotatingPreview(
                    available,
                    partKey,
                    transparentBackground: transparentBackground,
                    backgroundColor: backgroundColor,
                    zoomFactor: RotatingImageZoomFactor,
                    cameraPitchDegrees: CameraPitchDegrees,
                    cameraYawDegrees: CameraYawDegrees,
                    rotatingPreviewSize: RotatingPreviewSize);

            }

            RotatingPreviewTexture = VesselPlanner.Core.PartIconRenderer.GetRotatingPreviewFrameForTime(partKey, Time.time, RotatingPreviewDegreesPerFrame);
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
            if (available == null)
            {
                FailedParts.Add(cacheKey);
                return null;
            }
            try
            {
                Texture2D thumbnail = RenderThumbnail(available, renderPixels);
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
            // Static thumbnails returned by RenderIcon are owned by PartIconRenderer's
            // cache as well, so clear our references first and let that renderer destroy
            // each Texture2D exactly once.
            Thumbnails.Clear();
            FailedParts.Clear();
            VesselPlanner.Core.PartIconRenderer.ClearCache();
            VesselPlanner.Core.PartIconRenderer.DisposeRotatingPreview();
            RotatingPreviewPartKey = null;
            RotatingPreviewTexture = null;
        }

        private static Texture2D RenderThumbnail(AvailablePart available, int renderPixels)
        {
            if (available == null) return null;

            Texture2D a = VesselPlanner.Core.PartIconRenderer.RenderIcon(
                available,
                renderPixels,
                zoomFactor: IconZoomFactor,
                cameraPitchDegrees: CameraPitchDegrees,
                cameraYawDegrees: CameraYawDegrees);
            return a;
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
