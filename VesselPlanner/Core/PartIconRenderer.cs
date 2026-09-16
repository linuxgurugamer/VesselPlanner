using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace VesselPlanner.Core
{
    /// <summary>
    /// Renders an isolated thumbnail image of a KSP part on demand.
    ///
    /// Usage:
    ///     Texture2D icon = PartIconRenderer.RenderIcon(avPart);
    ///     someRawImage.texture = icon;
    ///
    /// or, if you just want to drive a UI RawImage/Image without ever reading
    /// pixels back to the CPU:
    ///     RenderTexture rt = PartIconRenderer.RenderIconToRenderTexture(avPart);
    ///     someRawImage.texture = rt;
    ///
    /// Results are cached per part + every parameter that affects the render
    /// (size, angle, zoom, lighting, background) so repeated calls (e.g. a
    /// scrolling parts list) don't re-render every frame, but changing any of
    /// those parameters always produces a fresh render rather than a stale
    /// cached one. Cache identity is by AvailablePart.partUrl (falling back to
    /// .name only if partUrl is somehow empty) rather than name alone, since mod
    /// packs can ship multiple parts that intentionally share the same internal
    /// name with different partUrls - keying on name alone made two such parts
    /// collide on one cache entry, so whichever rendered first got reused for
    /// both. Call ClearCache() if you need to free the memory (e.g. on scene
    /// change) or InvalidateCache(avPart) if a specific part's appearance
    /// changed (texture switch, variant, etc.) while every argument stayed the
    /// same.
    ///
    /// For a rotating/turntable preview, see BuildRotatingPreview,
    /// GetRotatingPreviewFrameForTime, and DisposeRotatingPreview further down -
    /// that pre-renders a full spin as a set of still frames once, then playback
    /// is just picking which already-rendered frame to display.
    /// </summary>
    public static class PartIconRenderer
    {
        const float LIGHT_INTENSITY = 0.1f; // Default light intensity for icon rendering

        const float ZOOM_FACTOR_ICON = 0.8f; // 0.6f; // Default zoom factor for icon rendering
        const float ZOOM_FACTOR_ROTATION = 1f; // 0.6f; // Default zoom factor for icon rendering

        const float CAMERA_PITCH_DEGREES = 40f; // Default camera pitch for icon rendering
        const float CAMERA_YAW_DEGREES = 45f; // Default camera yaw for icon rendering

        /// <summary>
        /// Unity layer reserved for icon rendering. Layer 31 is unused by stock KSP;
        /// double check against any other mods you know clash with it (Kopernicus,
        /// EVE, etc. sometimes claim high layer numbers) and change this constant if needed.
        /// </summary>
        public const int IconLayer = 31;

        /// <summary>Set true temporarily if you need the per-render diagnostic log line back.</summary>
        private const bool VerboseLogging = false;

        /// <summary>How many still frames make up one full rotation in a rotating preview.</summary>
        public const int RotatingPreviewFrameCount = 60;

        /// <summary>Resolution (width/height) of each rotating preview frame. Kept modest since
        /// RotatingPreviewFrameCount frames are held in memory at once per part.</summary>
        public const int RotatingPreviewSize = 100; // 200;

        private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D[]> _rotatingPreviewCache = new Dictionary<string, Texture2D[]>();

        /// <summary>
        /// Renders a part's thumbnail and returns it as a Texture2D (safe to store,
        /// assign to a Sprite, or save to disk with EncodeToPNG).
        /// </summary>
        /// <param name="avPart">The part to render.</param>
        /// <param name="size">Width/height of the square output texture, in pixels.</param>
        /// <param name="transparentBackground">If true, background alpha is 0; otherwise backgroundColor is used opaque.</param>
        /// <param name="backgroundColor">Background color (alpha ignored unless transparentBackground is false).</param>
        /// <param name="zoomFactor">Smaller = camera pulled back further (part appears smaller in frame).</param>
        /// <param name="cameraPitchDegrees">Downward viewing angle, in degrees above the horizon. 0 = looking straight along the horizontal, 90 = straight down. Defaults to a 40 degree "looking down at the part" angle.</param>
        /// <param name="cameraYawDegrees">Horizontal rotation around the part, in degrees, so the view isn't a flat front-on shot.</param>
        /// <param name="lightIntensity">Brightness of the light illuminating the part. 1.0 is Unity's default directional-light intensity; lower it (e.g. 0.3-0.6) to darken the render, or pass 0 to rely only on Unity's ambient light.</param>
        /// <param name="useCache">If true, reuses a previously rendered icon for the same part+size.</param>
        public static Texture2D RenderIcon(
            AvailablePart avPart,
            int size = 256,
            bool transparentBackground = true,
            Color? backgroundColor = null,
            float zoomFactor = ZOOM_FACTOR_ICON,
            float cameraPitchDegrees = CAMERA_PITCH_DEGREES,
            float cameraYawDegrees = CAMERA_YAW_DEGREES,
            float lightIntensity = LIGHT_INTENSITY,
            bool useCache = true)
        {
            if (avPart == null)
            {
                Debug.LogError("[PartIconRenderer] RenderIcon called with a null AvailablePart.");
                return null;
            }

            // IMPORTANT: the cache key must include every parameter that affects the
            // rendered pixels. Earlier this was just "name_size", so changing
            // cameraPitchDegrees (or yaw, zoom, lighting, background) on a part that
            // had already been rendered once silently returned the old cached image
            // instead of re-rendering - that's almost certainly why pitch appeared to
            // have no effect.
            string cacheKey = BuildCacheKey(avPart, size, transparentBackground, backgroundColor, zoomFactor, cameraPitchDegrees, cameraYawDegrees, lightIntensity);
            if (useCache && _cache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
                return cached;

            RenderTexture rt = null;
            Texture2D result = null;

            try
            {
                rt = RenderIconInternal(avPart, size, transparentBackground, backgroundColor, zoomFactor, cameraPitchDegrees, cameraYawDegrees, lightIntensity);
                if (rt == null)
                    return null;

                result = new Texture2D(size, size, TextureFormat.ARGB32, false, false);
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = rt;
                result.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                result.Apply(false, false);

                RenderTexture.active = previousActive;

                if (useCache)
                    _cache[cacheKey] = result;
            }
            catch (Exception ex)
            {
                Debug.LogError("[PartIconRenderer] Failed to render icon for " + avPart.name + ": " + ex);
                if (result != null)
                    UnityEngine.Object.Destroy(result);
                result = null;
            }
            finally
            {
                if (rt != null)
                {
                    // rt is allocated in RenderIconInternal with "new RenderTexture(...)",
                    // never with RenderTexture.GetTemporary(...) - so it was never checked
                    // out of Unity's shared temporary-RT pool. Calling ReleaseTemporary on
                    // it anyway (as this used to) is invalid: it decrements/frees pool
                    // bookkeeping for an RT the pool never tracked, which can corrupt that
                    // pool for whatever next legitimately calls GetTemporary with a
                    // matching width/height/format - including KSP's own rendering or
                    // another mod. Since every icon render requests the same-shaped RT,
                    // that corruption could hand back aliased/stale GPU memory completely
                    // unpredictably, for entirely unrelated parts - independent of anything
                    // about cache keys or part identity, which is why the partUrl fixes
                    // didn't touch this. Plain Release()+Destroy() is the correct cleanup
                    // for a manually "new"-ed RenderTexture.
                    RenderTexture.active = null;
                    rt.Release();
                    UnityEngine.Object.Destroy(rt);
                }
            }

            return result;
        }

#if false
        /// <summary>
        /// Renders a part's thumbnail directly into a RenderTexture you own and can
        /// assign straight to a RawImage.texture, skipping the CPU readback. Caller
        /// is responsible for releasing/destroying the returned RenderTexture when done.
        /// Not cached, since RenderTextures are typically bound to a live UI element.
        /// </summary>
        public static RenderTexture RenderIconToRenderTexture(
            AvailablePart avPart,
            int size = 256,
            bool transparentBackground = true,
            Color? backgroundColor = null,
            float zoomFactor = ZOOM_FACTOR,
            float cameraPitchDegrees = CAMERA_PITCH_DEGREES,
            float cameraYawDegrees = CAMERA_YAW_DEGREES,
            float lightIntensity = LIGHT_INTENSITY)
        {
            if (avPart == null)
            {
                Debug.LogError("[PartIconRenderer] RenderIconToRenderTexture called with a null AvailablePart.");
                return null;
            }

            return RenderIconInternal(avPart, size, transparentBackground, backgroundColor, zoomFactor, cameraPitchDegrees, cameraYawDegrees, lightIntensity);
        }
#endif


        /// <summary>
        /// Pre-renders a full turntable rotation of a part (RotatingPreviewFrameCount
        /// frames, spaced evenly around 360 degrees of yaw at a fixed pitch) and
        /// stores them under partKey for later playback. This is the "flipbook"
        /// approach used for rotating previews - cheap to display (just swap which
        /// already-rendered texture a RawImage points at) since nothing is rendered
        /// per-frame at display time.
        ///
        /// Typical usage:
        ///     PartIconRenderer.BuildRotatingPreview(avPart, avPart.name);
        ///     // ...then, every Update() while the preview is visible:
        ///     rawImage.texture = PartIconRenderer.GetRotatingPreviewFrameForTime(avPart.name, Time.time, 60f);
        ///     // ...and when the preview is closed:
        ///     PartIconRenderer.DisposeRotatingPreview(avPart.name);
        ///
        /// Building is relatively expensive (RotatingPreviewFrameCount separate
        /// renders happen synchronously in this one call), so call it once when a
        /// preview panel opens or a part is selected, not every frame.
        /// </summary>
        /// <param name="available">The part to render.</param>
        /// <param name="partKey">Cache key frames are stored under (e.g. avPart.name, or something
        /// more specific if the same part can look different, like a variant-qualified string).
        /// Call DisposeRotatingPreview(partKey) with this same key when you're done with it.</param>
        /// <returns>True if every frame rendered successfully and the preview is ready to play; false
        /// (with nothing left cached under partKey) if available was null/invalid or any frame failed.</returns>
        private static bool BuildRotatingPreview(
            AvailablePart available,
            string partKey,
            bool transparentBackground = true,
            Color? backgroundColor = null,
            float zoomFactor = ZOOM_FACTOR_ROTATION,
            float cameraPitchDegrees = CAMERA_PITCH_DEGREES,
            float cameraYawDegrees = CAMERA_YAW_DEGREES,
            float lightIntensity = LIGHT_INTENSITY)
        {
            if (available == null)
            {
                Debug.LogError("[PartIconRenderer] BuildRotatingPreview called with a null AvailablePart.");
                return false;
            }

            if (string.IsNullOrEmpty(partKey))
            {
                Debug.LogError("[PartIconRenderer] BuildRotatingPreview called with a null/empty partKey for part " + available.name + ".");
                return false;
            }

            // Replace any previous preview under this key rather than leaking it.
            DisposeRotatingPreview(partKey);

            var frames = new Texture2D[RotatingPreviewFrameCount];
            float yawStep = 360f / RotatingPreviewFrameCount;

            for (int i = 0; i < RotatingPreviewFrameCount; i++)
            {
                float yaw = cameraYawDegrees + i * yawStep;

                // useCache:false - these frames live in _rotatingPreviewCache instead,
                // and each yaw is a one-off value that would just bloat the icon cache.
                Texture2D frame = RenderIcon(
                    available,
                    RotatingPreviewSize,
                    transparentBackground,
                    backgroundColor,
                    zoomFactor,
                    cameraPitchDegrees,
                    yaw,
                    lightIntensity,
                    useCache: false);

                if (frame == null)
                {
                    Debug.LogError("[PartIconRenderer] BuildRotatingPreview failed on frame " + i + "/" + RotatingPreviewFrameCount + " for " + available.name + " - aborting.");
                    for (int j = 0; j < i; j++)
                    {
                        if (frames[j] != null)
                            UnityEngine.Object.Destroy(frames[j]);
                    }
                    return false;
                }

                frames[i] = frame;
            }

            _rotatingPreviewCache[partKey] = frames;
            return true;
        }

        /// <summary>
        /// Public entry point for BuildRotatingPreview. BuildRotatingPreview itself is
        /// private (as specified), so this is what external code calls to kick off
        /// building a preview; delete this wrapper if you already have your own public
        /// method elsewhere that calls BuildRotatingPreview directly (e.g. this file
        /// ends up merged into a larger class where it's already reachable).
        /// </summary>
        public static bool StartRotatingPreview(
            AvailablePart available,
            string partKey,
            bool transparentBackground = true,
            Color? backgroundColor = null,
            float zoomFactor = ZOOM_FACTOR_ROTATION,
            float cameraPitchDegrees = CAMERA_PITCH_DEGREES,
            float cameraYawDegrees = CAMERA_YAW_DEGREES,
            float lightIntensity = LIGHT_INTENSITY)
        {
            return BuildRotatingPreview(available, partKey, transparentBackground, backgroundColor, zoomFactor, cameraPitchDegrees, cameraYawDegrees, lightIntensity);
        }

        /// <summary>Returns true if BuildRotatingPreview has already built frames for this key.</summary>
        public static bool HasRotatingPreview(string partKey)
        {
            return partKey != null && _rotatingPreviewCache.ContainsKey(partKey);
        }

        /// <summary>Number of frames built for partKey, or 0 if none have been built.</summary>
        public static int GetRotatingPreviewFrameCount(string partKey)
        {
            return partKey != null && _rotatingPreviewCache.TryGetValue(partKey, out var frames) ? frames.Length : 0;
        }

        /// <summary>
        /// Returns one specific frame by index (wraps around for indexes outside
        /// 0..frameCount-1, including negative ones), or null if BuildRotatingPreview
        /// hasn't been called for partKey (or it was disposed).
        /// </summary>
        public static Texture2D GetRotatingPreviewFrame(string partKey, int frameIndex)
        {
            if (partKey == null || !_rotatingPreviewCache.TryGetValue(partKey, out var frames) || frames.Length == 0)
                return null;

            int wrapped = frameIndex % frames.Length;
            if (wrapped < 0)
                wrapped += frames.Length;
            return frames[wrapped];
        }

        /// <summary>
        /// Convenience for driving playback straight from elapsed time (e.g. Time.time
        /// in a MonoBehaviour's Update()), so callers don't have to track a frame index
        /// or a timer themselves. degreesPerSecond controls spin speed.
        /// </summary>
        public static Texture2D GetRotatingPreviewFrameForTime(string partKey, float elapsedSeconds, float degreesPerSecond = 60f)
        {
            if (partKey == null || !_rotatingPreviewCache.TryGetValue(partKey, out var frames) || frames.Length == 0)
                return null;

            float totalDegrees = elapsedSeconds * degreesPerSecond;
            int frameIndex = Mathf.FloorToInt((totalDegrees / 360f) * frames.Length);
            return GetRotatingPreviewFrame(partKey, frameIndex);
        }

        /// <summary>Destroys and removes the rotating preview frames stored under partKey, if any.</summary>
        public static void DisposeRotatingPreview(string partKey)
        {
            if (partKey == null || !_rotatingPreviewCache.TryGetValue(partKey, out var frames))
                return;

            foreach (var frame in frames)
            {
                if (frame != null)
                    UnityEngine.Object.Destroy(frame);
            }
            _rotatingPreviewCache.Remove(partKey);
        }
        public static void DisposeRotatingPreview()
        {
            foreach (var frames in _rotatingPreviewCache.Values)
            {
                {
                    foreach (var frame in frames)
                    {
                        if (frame != null)
                            UnityEngine.Object.Destroy(frame);
                    }
                }
            }
            _rotatingPreviewCache.Clear();
        }

        /// <summary>Destroys and clears every cached rotating preview (all parts/keys). Call on scene unload.</summary>
        public static void ClearRotatingPreviewCache()
        {
            foreach (var frames in _rotatingPreviewCache.Values)
            {
                foreach (var frame in frames)
                {
                    if (frame != null)
                        UnityEngine.Object.Destroy(frame);
                }
            }
            _rotatingPreviewCache.Clear();
        }

        private static RenderTexture RenderIconInternal(
            AvailablePart avPart,
            int size,
            bool transparentBackground,
            Color? backgroundColor,
            float zoomFactor,
            float cameraPitchDegrees,
            float cameraYawDegrees,
            float lightIntensity)
        {
            GameObject sourcePrefab = avPart.iconPrefab != null ? avPart.iconPrefab : avPart.partPrefab.gameObject;
            if (sourcePrefab == null)
            {
                Debug.LogError("[PartIconRenderer] Part " + avPart.name + " has no iconPrefab or partPrefab to render.");
                return null;
            }

            GameObject iconObj = null;
            GameObject camObj = null;
            Debug.Log("[PartIconRenderer] RenderIconInternal: Rendering thumbnail for " + avPart.partUrl);

            try
            {
                // Clone the model well away from the active scene so nothing else can
                // ever collide with or see it outside our dedicated camera.
                Vector3 stagingPosition = new Vector3(0f, -9000f, 0f);

                iconObj = UnityEngine.Object.Instantiate(sourcePrefab, stagingPosition, Quaternion.identity);

                // KSP stores iconPrefab/partPrefab as INACTIVE template objects sitting
                // dormant in the scene. Instantiate() clones the active state along with
                // everything else, so without this the clone is inactive and renders
                // nothing at all - which looks like a flat, unchanging background no
                // matter what the camera or lighting is doing, exactly like solid gray
                // output regardless of pitch/light settings.
                iconObj.SetActive(true);

                SetLayerRecursive(iconObj, IconLayer);
                StripUnwantedComponents(iconObj);

                // Belt-and-suspenders: some part models disable individual mesh
                // renderers (LOD swaps, variant meshes) rather than whole GameObjects.
                // Force them all on so the clone we render is guaranteed visible.
                foreach (var renderer in iconObj.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.gameObject.SetActive(true);
                    renderer.enabled = true;
                }

                // AvailablePart.iconScale is a single uniform-scale float, not a Vector3.
                if (avPart.iconScale > 0f)
                    iconObj.transform.localScale = Vector3.one * avPart.iconScale;

                Bounds bounds = GetRendererBounds(iconObj);

                var rt = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 2,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                camObj = new GameObject("PartIconRenderer_Camera");
                Camera cam = camObj.AddComponent<Camera>();
                cam.cullingMask = 1 << IconLayer;
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = transparentBackground
                    ? new Color(0f, 0f, 0f, 0f)
                    : (backgroundColor ?? Color.black);
                cam.orthographic = true;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = bounds.size.magnitude * 4f + 10f;
                cam.targetTexture = rt;
                cam.renderingPath = RenderingPath.Forward;
                cam.allowHDR = false;
                cam.allowMSAA = true;

                float distance = bounds.extents.magnitude + cam.nearClipPlane + 1f;

                // Spherical offset from the part's center: cameraPitchDegrees is how far
                // down the camera looks below the horizon (40 = a clear "looking down at
                // it from above" 3/4 view, 0 = dead-on from the side, 90 = straight down),
                // cameraYawDegrees spins that view around the part so it isn't a flat
                // front-on shot.
                float pitchRad = cameraPitchDegrees * Mathf.Deg2Rad;
                float yawRad = cameraYawDegrees * Mathf.Deg2Rad;
                Vector3 camDirection = new Vector3(
                    Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
                    Mathf.Sin(pitchRad),
                    Mathf.Cos(pitchRad) * Mathf.Cos(yawRad));

                camObj.transform.position = bounds.center + camDirection * distance;
                camObj.transform.LookAt(bounds.center);

                cam.orthographicSize = Mathf.Max(bounds.extents.magnitude * zoomFactor, 0.05f);

                // Simple headlight so the part isn't rendered flat-lit/black if the
                // staging area has no scene lighting reaching it. Skipped entirely when
                // lightIntensity <= 0, leaving the part lit only by Unity's ambient light.
                if (lightIntensity > 0f)
                {
                    Light light = camObj.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.color = Color.white;
                    light.intensity = lightIntensity;
                    light.cullingMask = 1 << IconLayer;
                }

                // Diagnostic log, off by default now that pitch/light are confirmed working -
                // flip VerboseLogging on if a rendering issue needs investigating again.
                // Left off here since BuildRotatingPreview calls this once per frame and
                // would otherwise spam the log.
                if (VerboseLogging)
                {
                    Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "[PartIconRenderer] part={0} requestedPitch={1:F1} requestedYaw={2:F1} requestedLight={3:F2} " +
                        "layer={4} rendererCount={5} bounds.center={6} bounds.extents={7} camPos={8} camEuler={9} " +
                        "camDist={10:F2} orthoSize={11:F2} lightAdded={12}",
                        avPart.name, cameraPitchDegrees, cameraYawDegrees, lightIntensity,
                        IconLayer, iconObj.GetComponentsInChildren<Renderer>(true).Length,
                        bounds.center, bounds.extents, camObj.transform.position, camObj.transform.eulerAngles,
                        distance, cam.orthographicSize, lightIntensity > 0f));
                }

                cam.Render();

                cam.targetTexture = null;
                return rt;
            }
            finally
            {
                // DestroyImmediate, not Destroy: Destroy() only marks these for cleanup at
                // end of frame, but every call clones its model to the SAME fixed staging
                // position (0, -9000, 0) on the SAME fixed layer. An IMGUI parts list draws
                // every visible row's thumbnail in one repaint pass, so a cache-miss list
                // can trigger many RenderIconInternal calls within a single frame - with
                // plain Destroy(), each prior call's clone is still alive and sitting right
                // there when the next call's camera (same culling mask) renders, so the
                // image comes out as an overlapping composite of whichever parts were
                // rendered earlier that same frame instead of a clean shot of just the
                // current one. These objects are created and used entirely within this one
                // call with nothing else ever referencing them, so destroying immediately
                // is safe here and removes the overlap.
                if (camObj != null)
                    UnityEngine.Object.DestroyImmediate(camObj);
                if (iconObj != null)
                    UnityEngine.Object.DestroyImmediate(iconObj);
            }
        }

        /// <summary>
        /// Recursively sets every GameObject in the hierarchy to the given layer so
        /// the icon camera (and nothing else) picks it up.
        /// </summary>
        private static void SetLayerRecursive(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        /// <summary>
        /// Disables components on the cloned model that could interfere with an
        /// isolated icon render or waste time doing physics/effects nobody will see:
        /// colliders, rigidbodies, particle systems, and any Part/PartModule scripts
        /// left over if you cloned partPrefab instead of iconPrefab.
        /// </summary>
        private static void StripUnwantedComponents(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.Destroy(rb);

            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
                ps.gameObject.SetActive(false);

            var part = root.GetComponent<Part>();
            if (part != null)
                part.enabled = false;
        }

        /// <summary>
        /// Combines the bounds of every renderer in the hierarchy into one Bounds,
        /// in world space, so the camera can be framed to fit the whole model.
        /// </summary>
        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        /// <summary>
        /// Returns the identifier that uniquely picks out this exact loaded part config.
        /// avPart.name is only SUPPOSED to be unique - mod packs frequently ship
        /// replacement/compat parts that intentionally reuse another part's internal
        /// name with a different partUrl (its GameData path), which is the one KSP
        /// guarantees is unique per loaded AvailablePart. Caching or invalidating by
        /// name alone means two different parts that happen to share a name collide
        /// on the same cache entry, and whichever rendered first "wins" for both -
        /// which is exactly the "thumbnails reusing some images" symptom.
        /// </summary>
        private static string GetPartIdentifier(AvailablePart avPart)
        {
            return !string.IsNullOrEmpty(avPart.partUrl) ? avPart.partUrl : avPart.name;
        }

        /// <summary>
        /// Builds a cache key that captures every parameter which changes the
        /// rendered pixels. Always starts with "partIdentifier_size" so
        /// InvalidateCache's prefix match keeps working.
        /// </summary>
        private static string BuildCacheKey(
            AvailablePart avPart,
            int size,
            bool transparentBackground,
            Color? backgroundColor,
            float zoomFactor,
            float cameraPitchDegrees,
            float cameraYawDegrees,
            float lightIntensity)
        {
            Color bg = transparentBackground ? new Color(0f, 0f, 0f, 0f) : (backgroundColor ?? Color.black);
            var str = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}_{1}_{2}_{3:F2}_{4:F2}_{5:F2}_{6:F2}_{7}",
                GetPartIdentifier(avPart), size, transparentBackground, zoomFactor, cameraPitchDegrees, cameraYawDegrees, lightIntensity,
                transparentBackground ? "t" : bg.r.ToString("F2") + "," + bg.g.ToString("F2") + "," + bg.b.ToString("F2"));
            //Debug.Log("PartIconRendererBuildCacheKey: " + str);
            return str;
        }

        /// <summary>Removes one part's cached icon(s) so the next RenderIcon call regenerates it.</summary>
        public static void InvalidateCache(AvailablePart avPart)
        {
            if (avPart == null)
                return;

            string prefix = GetPartIdentifier(avPart) + "_";
            List<string> keysToRemove = new List<string>();
            foreach (var key in _cache.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
            {
                if (_cache[key] != null)
                    UnityEngine.Object.Destroy(_cache[key]);
                _cache.Remove(key);
            }
        }

        /// <summary>Destroys and clears every cached icon texture. Call on scene unload if you cache a lot.</summary>
        public static void ClearCache()
        {
            foreach (var tex in _cache.Values)
            {
                if (tex != null)
                    UnityEngine.Object.Destroy(tex);
            }
            _cache.Clear();
        }

        public static void WriteImageToDisk(AvailablePart avPart, Texture2D tex)
        {
            string dir = Path.Combine(KSPUtil.ApplicationRootPath, "PartIconRendererTest");
            if (avPart == null)
            {
                Debug.LogError("[PartIconRendererDebugTool] WriteImageToDisk called with a null AvailablePart.");
                return;
            }
            if (tex == null)
            {
                // Was previously called unconditionally by PartThumbnailCache.Get even
                // when RenderThumbnail failed and returned null, which threw a
                // NullReferenceException on tex.EncodeToPNG() below and masked whatever
                // actually went wrong behind a generic caught-exception log line.
                Debug.LogWarning("[PartIconRendererDebugTool] WriteImageToDisk called with a null Texture2D for " + avPart.name + " - nothing to write.");
                return;
            }
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                Debug.LogError("[PartIconRendererDebugTool] Could not create directory " + dir + ": " + ex);
                return;
            }
            // Same duplicate-name hazard as the render cache: use the unique partUrl
            // (sanitized for the filesystem) so two different parts sharing a .name
            // don't overwrite each other's debug PNG.
            string label = SanitizeFileName(GetPartIdentifier(avPart));
            File.WriteAllBytes(Path.Combine(dir, label + ".png"), tex.EncodeToPNG());

        }

        /// Writes every icon currently sitting in the RenderIcon cache to its own PNG
        /// file in the given directory, named after its cache key (the same key
        /// BuildCacheKey produces - part name, size, and every render setting baked
        /// in, so e.g. two renders of the same part at different pitches land in two
        /// distinctly-named files rather than overwriting each other). Useful for
        /// bulk-inspecting or debugging exactly what's been rendered/cached so far.
        /// Does not touch the rotating preview cache - see BuildRotatingPreview's
        /// frames for that.
        /// </summary>
        /// <param name="directory">Folder to write into; created if it doesn't already exist.</param>
        /// <returns>How many files were written successfully.</returns>
        public static int DumpCacheToDisk(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                Debug.LogError("[PartIconRenderer] DumpCacheToDisk called with a null/empty directory.");
                return 0;
            }

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                Debug.LogError("[PartIconRenderer] DumpCacheToDisk could not create directory " + directory + ": " + ex);
                return 0;
            }

            int written = 0;
            foreach (var entry in _cache)
            {
                string cacheKey = entry.Key;
                Texture2D tex = entry.Value;
                if (tex == null)
                    continue;

                string fileName = SanitizeFileName(cacheKey) + ".png";
                string path = Path.Combine(directory, fileName);

                try
                {
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    written++;
                }
                catch (Exception ex)
                {
                    Debug.LogError("[PartIconRenderer] DumpCacheToDisk failed to write " + path + ": " + ex);
                }
            }

            Debug.Log("[PartIconRenderer] DumpCacheToDisk wrote " + written + "/" + _cache.Count + " cached icon(s) to " + directory);
            return written;
        }

        /// <summary>Replaces any character that isn't legal in a filename with an underscore.</summary>
        private static string SanitizeFileName(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);
            return sb.ToString();
        }
    }
}


namespace VesselPlanner.Core
{
    /// <summary>
    /// TEMPORARY debugging aid, not something to ship. Press F9 while in the
    /// VAB/SPH with at least one part placed on the ship: it renders the first
    /// part at several pitch/light combinations and writes each as a PNG to
    /// GameData's parent folder (KSP root)/PartIconRendererTest/. Compare the
    /// files directly in an image viewer:
    ///
    ///   - If the files differ from each other (different angle, different
    ///     brightness) then PartIconRenderer itself is working correctly, and
    ///     the problem is in how your own code displays/caches the texture it
    ///     returns (e.g. a UI element only having its texture assigned once).
    ///   - If the files all look identical, the bug is inside PartIconRenderer,
    ///     and the log line each render prints (search Player.log /
    ///     KSP.log for "[PartIconRenderer]") will show the actual computed
    ///     camera position/rotation and light intensity used, which is the
    ///     next thing to inspect.
    ///
    /// Delete this file once the real bug is found.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class PartIconRendererDebugTool : MonoBehaviour
    {
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F9))
                return;
            string dir = Path.Combine(KSPUtil.ApplicationRootPath, "PartIconRendererTest");
            VesselPlanner.Core.PartIconRenderer.DumpCacheToDisk(dir);
        }



        private static void SaveTest(AvailablePart avPart, string dir, string label, float pitch, float yaw, float lightIntensity)
        {
            // useCache:false so this test never hands back a stale image regardless
            // of what your own game code has already cached for this part.
            Texture2D tex = PartIconRenderer.RenderIcon(
                avPart,
                size: 256,
                transparentBackground: false,
                backgroundColor: Color.gray,
                zoomFactor: 0.6f,
                cameraPitchDegrees: pitch,
                cameraYawDegrees: yaw,
                lightIntensity: lightIntensity,
                useCache: false);

            if (tex == null)
            {
                Debug.LogError("[PartIconRendererDebugTool] Render returned null for " + label);
                return;
            }

            File.WriteAllBytes(Path.Combine(dir, label + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }
    }
}