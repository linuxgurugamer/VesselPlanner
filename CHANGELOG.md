## 0.7.75
- Added `Manual/VesselPlanner-Analyze-Existing-Tutorial.docx`, covering stage selection, current-resource simulation, environment/candidate filters, result columns, Selected Engine details, part placement, layout controls, examples, and troubleshooting.
- Added `Manual/VesselPlanner-Planning-Tutorial.docx`, covering payload and delta-v basis, optimization modes, body/altitude behavior, candidate results, actual tank-set selection, placement actions, Delta-V Table/Mission Planner integration, examples, and troubleshooting.
- Updated `VesselPlanner.version` and assembly version to 0.7.75.0.

## 0.7.74
- Added `Manual/VesselPlanner-Mission-Planner-Tutorial.docx`, a dedicated walkthrough of mission step types, automatic delta-v suggestions, transfer-planner clipboard import, subassemblies, list editing, save/load, and Stage-by-Stage integration.
- Reworked `Manual/VesselPlanner-Stage-by-Stage-Tutorial.docx` for the current mission-linked workflow, current engine/tank selection behavior, subassemblies, part previews, cumulative mass, finalization, and standalone/mission-driven planning.
- Updated `VesselPlanner.version` and assembly version to 0.7.74.0.

## 0.7.73
- Added a one-page Table of Contents to the main Word manual, listing all major sections with verified page numbers.
- Updated the manual title/version and release metadata to 0.7.73.0.

## 0.7.72
- Reworked the user manual as a current-capabilities guide rather than relying on accumulated historical notes.
- Corrected Planning documentation for selectable Vacuum/Atmosphere target delta-v behavior and current engine/tank filtering.
- Documented current Mission Planner, Stage-By-Stage, Delta-V Table, part-image, Settings, and Flight Data behavior through 0.7.71.
- Corrected Flight Data documentation for the 1000 px minimum width, Sample Interval controls, bottom-row CSV/PNG exports, configurable folders, axes, and persistent maxima.
- Regenerated `Manual/VesselPlanner-Manual.docx` from the refreshed manual and updated release/version metadata to 0.7.72.0.

## 0.7.71
- Moved the **Delta-V Table** mode button to immediately right of **Planning**, separated by an 18-pixel gap.
- Updated `VesselPlanner.version` and assembly version to 0.7.71.0.

## 0.7.70
- Replaced Delta-V Table metric buttons with clickable numeric values under dedicated Ejection, Capture, Plane Change, Total, Landing, and Ascent column headers.
- Removed the Selected Value column; clicking any displayed value copies the numeric delta-v to the clipboard and reports it in the bottom status line.
- Reworked Delta-V Table rows to use absolute fixed column rectangles so moon indentation affects only the Body cell and all value columns align exactly with planet and home-body rows.
- Updated `VesselPlanner.version` and assembly version to 0.7.70.0.

## 0.7.69
- Moved Delta-V Table clipboard confirmations into a fixed status line at the bottom of the page.
- Reworked Delta-V Table row layout into fixed-width Body, route, and surface column groups so planets, moons, and the home-body row stay aligned.
- Made the always-visible **dV to low orbit** value clickable; clicking it copies the numeric value to the clipboard and selects Low Orbit for that row.
- Updated `VesselPlanner.version` and assembly version to 0.7.69.0.

## 0.7.68
- Delta-V Table value buttons now copy the selected numeric delta-v value to the system clipboard.
- Added an on-page confirmation showing the metric, body, and copied value in m/s.
- Updated `VesselPlanner.version` and assembly version to 0.7.68.0.

## 0.7.67
- Removed the **Transfer** button from the Delta-V Table page.
- Delta-V Table route and surface buttons are now omitted when their value is zero or unavailable.
- Expandable body names now use label styling while remaining clickable to expand/collapse moons.
- Updated `VesselPlanner.version` and assembly version to 0.7.67.0.

## 0.7.66
- Added a new **Delta-V Table** editor page alongside Mission Planner, Stage-By-Stage, Analyze Existing, and Planning.
- The page displays solar-orbiting bodies as a tree and expands each body's moons (including nested moons such as Wal -> Tal).
- Every body row always shows `dV_to_low_orbit`. Non-home bodies provide **Ejection**, **Transfer**, **Capture**, **Plane Change**, and **Total** buttons; bodies with a surface landing value also provide **Landing** and **Ascent**.
- Clicking a value button displays the corresponding delta-v from the active planet-pack CSV. Planet routes use home-body -> planet rows; moon routes use parent -> moon rows.
- Updated `VesselPlanner.version` and assembly version to 0.7.66.0.

## 0.7.65
- Added `GameData/VesselPlanner/PluginData/DeltaVTables/OPM.csv`, starting from the Stock table and adding Outer Planets Mod bodies/routes derived from the supplied OPM delta-v map.
- Updated Eeloo metadata for OPM so it is a moon of Sarnus rather than a solar-orbiting planet.
- Added Sarnus, Urlum, Neidon, Plock, their mapped moons, and Tal as a subsatellite of Wal.
- Plock uses the lower/ideal end of the map's 1900-2700 m/s outer-transfer range.
- Updated `VesselPlanner.version` and assembly version to 0.7.65.0.

## 0.7.64
- Widened the Flight Data **Sample Interval** label from 96 px to 120 px.

# Changelog

## 0.7.63

- Moved **Export CSV** from the Flight Data top toolbar to the CSV export-path row at the bottom of the window.
- Moved **Export PNG** from the Flight Data top toolbar to the PNG export-path row at the bottom of the window.
- Kept **Settings** in the top-right toolbar immediately before the close button.
- Updated `VesselPlanner.version` and assembly version to 0.7.63.0.

## 0.7.62

- Increased the minimum width of the Flight Data window from 935 px to **1000 px**.
- Updated `VesselPlanner.version` and assembly version to 0.7.62.0.

## 0.7.61

- Moved the Flight Data **Settings** button to the right of **Export CSV** and **Export PNG**, immediately before the close button.
- Renamed the Flight Data sampling control from **Sample** to **Sample Interval** and changed the displayed suffix from `s` to `/sec`.
- Lowered the Plotted Sensors color swatch by half the active label line height for better vertical alignment.
- Updated `VesselPlanner.version` and assembly version to 0.7.61.0.

## 0.7.60

- Removed the `VerboseLogging` flag and per-render verbose diagnostic logging from `PartIconRenderer.cs`.
- Removed the temporary `PartIconRendererDebugTool`, F9 cache dump support, disk-image diagnostic helpers, and related debug-only code.
- Removed the remaining thumbnail diagnostic log/disk-write calls from `PartThumbnailCache.cs`.
- Updated `VesselPlanner.version` and assembly version to 0.7.60.0.

## 0.7.59

- Removed the obsolete `RenderRotatingPreview` method from `PartThumbnailCache.cs`.
- Removed the legacy live-preview camera, pivot, light, render-texture, clone, and timing fields now superseded by `PartIconRenderer`.
- Removed legacy helper members used only by that obsolete rendering path.
- Updated `VesselPlanner.version` and assembly version to 0.7.59.0.

## 0.7.58

- Made the Settings section headers **Planning mode behavior**, **Filter persistence**, **Editor window appearance**, **Part images**, and **Detail pane sizes** bold.
- Increased the Settings window height by about one button height when using the Alternate skin.
- Kept the KSP skin's existing four-line height allowance and added one button height plus two more label lines.
- Moved the Flight Data **Settings**, **Export CSV**, and **Export PNG** buttons to the far-right toolbar group immediately before the close button.
- Updated `VesselPlanner.version` and assembly version to 0.7.58.0.

## 0.7.57

- Consolidated the duplicate Settings/Mission Planner tooltip drawing logic into `CommonRoutines.DrawTooltip()`.
- Settings and Mission Planner now use the same common tooltip renderer.
- Removed the local `DrawSettingsTooltip()` and `DrawMissionTooltip()` methods.
- Updated `VesselPlanner.version` and assembly version to 0.7.57.0.

## 0.7.56

- Made the editor Settings window four KSP label-line heights taller whenever the KSP skin is active; the Alternate skin keeps the existing 720-pixel height.
- Added a saved **Show tooltips** setting under Settings → Appearance, enabled by default.
- Disabling tooltips suppresses the Part Images field/slider hover help and Mission Planner ASL/VAC plus insert/order/delete button hover help immediately.
- Updated `VesselPlanner.version` and assembly version to 0.7.56.0.

## 0.7.55

- Updated both Part Images zoom tooltips to explain that lower zoom-factor values make the rendered part image larger.
- Increased the VesselPlanner Settings window from 600×680 to 640×720 pixels.
- Added visible hover tooltips for the Mission Planner ASL/VAC basis buttons and the insert/order controls in the mission-step list.
- Added `CommonRoutines.AddSpacesToString()` for consistently inserting spaces into PascalCase/camelCase labels.
- Changed `FormatManeuver()` to use the new common string-spacing routine.
- Removed the local `ModeLabel()` helper and now formats optimization-mode labels with `CommonRoutines.AddSpacesToString()`.
- Updated `VesselPlanner.version` and assembly version to 0.7.55.0.

## 0.7.54

- Added hover tooltips to the text-entry fields and sliders for all numeric Part Images settings in Settings → Appearance.
- Tooltips explain icon/rotating zoom, camera yaw/pitch, rotating-preview resolution, and rotating-preview speed behavior.
- Updated `VesselPlanner.version` and assembly version to 0.7.54.0.

## 0.7.53

- Changed Camera Yaw Degrees range to 0–180°.
- Changed Camera Pitch Degrees range to 0–90°.
- Applied the same limits to sliders, typed/persisted settings, and `PartThumbnailCache.Configure()`.
- Updated `VesselPlanner.version` and assembly version to 0.7.53.0.

## 0.7.52
- Lowered all numeric part-image setting sliders by 5 pixels within their rows for better vertical alignment with the text fields and labels.
- Updated `VesselPlanner.version` and assembly version to 0.7.52.0.

## 0.7.51
- Changed the **Rotating Image Background** Appearance setting from a row of buttons to a combo box.
- Added a slider beside every numeric part-image setting while retaining the editable numeric text field.
- Added saved **RotatingPreviewSize** control, default `100`, with a `32`–`256` pixel range; it controls the resolution used to pre-render each rotating hover-preview frame.
- Added saved **Degrees per frame** control, range `1`–`180`, default `60`, replacing the hard-coded `60f` value passed by `PartThumbnailCache` to rotating-preview playback.
- Reset image settings now also restores RotatingPreviewSize to `100` and Degrees per frame to `60`.
- Updated `VesselPlanner.version` and assembly version to 0.7.51.0.

## 0.7.50
- Removed the user-facing `Alpha Channel` part-image setting.
- Added `Rotating Image Background` in Settings → Appearance with Transparent, Black, Dark Gray, Gray, and White choices.
- The background setting applies only to enlarged rotating hover previews; normal list thumbnails remain transparent.
- Kept the Camera Pitch Degrees default/reset value at 20°.
- Updated `VesselPlanner.version` and assembly version to 0.7.50.0.

## 0.7.49
- Changed the default Camera Pitch Degrees value from 40° to 20° for new/reset settings.
- Added an `Alpha Channel` Appearance setting (0.0–1.0, default 1.0) controlling opacity of both static part icons and rotating hover-preview frames.
- Alpha changes immediately invalidate the part-image caches so rendered images refresh without restarting KSP.
- Updated `VesselPlanner.version` and assembly version to 0.7.49.0.

## 0.7.48
- Rebases the thumbnail/rotating-preview work on the user-provided 0.7.47 source.
- Added saved Appearance settings for `ZoomFactor for icons`, `ZoomFactor for Rotating Images`, `Camera Yaw Degrees`, and `Camera Pitch Degrees`.
- Static list thumbnails now use the configured icon zoom/yaw/pitch values; rotating hover frames use the configured rotating-image zoom/yaw/pitch values.
- Changing any part-image setting immediately clears the thumbnail and rotating-preview caches so the new view is used without restarting KSP.
- Added a Reset image settings button restoring 0.8 icon zoom, 1.0 rotating zoom, 45° yaw, and 40° pitch.
- Updated `VesselPlanner.version` and assembly version to 0.7.48.0.

## 0.7.46
- Changed enlarged engine/tank hover previews from static images to slowly rotating 3D part previews, matching the behavior of KSP's editor part list.
- The rotating preview uses the same exact `AvailablePart`/`partUrl` resolution as the list thumbnail, rotates around the part's visual bounds center, and is rendered off-screen without affecting the editor scene.
- Kept normal list thumbnails static and cached; only the currently hovered enlarged preview is animated.
- Updated `VesselPlanner.version` and assembly version to 0.7.46.0.

## 0.7.45
- Added a larger high-resolution part image preview when hovering over engine and tank thumbnails in Planning, Analyze Existing, and Stage-By-Stage lists.
- Hover previews render separately from the normal 64 px thumbnail cache so list drawing remains lightweight.
- Updated `VesselPlanner.version` and assembly version to 0.7.45.0.

## 0.7.44

- Fixed incorrect engine and tank thumbnails when more than one loaded KSP part shares the same internal part name.
- Engine candidates, tank candidates, and Analyze Existing installed-engine rows now carry the exact `AvailablePart.partUrl`; the thumbnail cache resolves and keys by that URL first, falling back to the legacy internal-name lookup only when no URL is available.
- Stage-By-Stage saved plans remain compatible and continue to use internal-name lookup because older plan files do not store part URLs.

## 0.7.43

- Added cached KSP part thumbnails to the shared candidate-engine table used by both Planning and Analyze Existing.
- Added up to three tank-type thumbnails to each mixed tank-set row in Planning.
- Added part thumbnails to Analyze Existing's Current engines table; the stage scanner now carries each installed engine's internal KSP part name for thumbnail lookup.
- Reused the existing `PartThumbnailCache`; no second thumbnail-rendering path was added.

## 0.7.42

- Fixed Stage-By-Stage part thumbnails rendering invisible because KSP editor-icon shaders do not reliably write alpha when rendered by VesselPlanner's off-screen camera.
- Thumbnail generation now renders over an opaque chroma-key background and explicitly constructs the output alpha channel before caching the texture.

## 0.7.41

- Added cached KSP part thumbnails to engine and tank rows in the Stage-By-Stage stage contents list.
- Thumbnails are rendered from each part's `AvailablePart.iconPrefab`, cached as 64×64 textures, and displayed at 40×40 beside the corresponding row.
- Thumbnail rendering is lazy and repaint-only so it does not disturb IMGUI layout; cached textures are released when the planner is disposed.

## 0.7.40

- Added a bundled `RSS.csv` Mission Planner delta-v table under `GameData/VesselPlanner/PluginData/DeltaVTables`.
- Normalized the supplied Real Solar System data into VesselPlanner body self rows plus exact route rows with integer sort orders.
- Replaced malformed capture-node origins with their actual parent bodies (Jupiter, Saturn, Uranus, and Neptune), corrected `Amelthea` to `Amalthea` and `MakeMake` to `Makemake`, and omitted Geostationary/L1/L2 pseudo-destinations so they do not appear as celestial bodies.
- Interplanetary `total_capture_dV` values now include departure, capture, and low-orbit insertion so Mission Planner receives the complete route requirement.
- Parent-to-moon rows keep the supplied full moon-to-parent reverse insertion value in `capture_dV` so `Return From A Moon` works with the RSS hierarchy.
- Added explicit self-row ascent/landing data, including a 29,705 m/s Venus ascent value from the public Solar System delta-v map while retaining the supplied 270 m/s landing allowance.

## 0.7.39

- Added a bundled `GPP.csv` Mission Planner delta-v table under `GameData/VesselPlanner/PluginData/DeltaVTables`.
- Restructured the supplied Galileo's Planet Pack data to use body self rows plus exact route rows with integer sort orders and the current GPP body hierarchy.
- Corrected map-derived values where needed, including Ceti low-orbit insertion (225 m/s), Argo surface/low-orbit (205 m/s), Julia surface/low-orbit (85 m/s), and the Gauss capture path (230 + 1930 m/s).
- Interplanetary `total_capture_dV` values now represent the complete low-Gael-orbit to low-destination-orbit transfer total used by Mission Planner.
- Parent-to-moon route rows retain the full reverse low-orbit return value in `capture_dV` so `Return From A Moon` works with GPP's multi-leg moon transfers.

## 0.7.38

- Added a bundled `JNSQ.csv` Mission Planner delta-v table under `GameData/VesselPlanner/PluginData/DeltaVTables`.
- Restructured the supplied JNSQ data to match VesselPlanner's current CSV schema: body launch/landing metadata now uses self rows, sort orders are integers, and moon parent metadata matches the JNSQ hierarchy.
- Corrected JNSQ values against the official map where needed, including Minmus transfer (1570 m/s), Huygen ascent (1875 m/s), and Moho plane change (2810 m/s).
- Interplanetary `total_capture_dV` values use the lower/ideal JNSQ map Total so Mission Planner's Transfer To Another Planet suggestion reads a complete route value rather than only destination insertion.

## 0.7.37

- Fixed a compile error in `EditorStageScanner` left by the 0.7.36 CommonRoutines refactor: the final engine-mass fallback now calls `CommonRoutines.GetDryPartMass`.
- Re-scanned all moved CommonRoutines helpers for stale unqualified call sites; only intentional local compatibility/window wrappers remain.

## 0.7.36

- Expanded `VesselPlanner.Core.CommonRoutines` and removed duplicated reusable helpers across the project.
- Centralized ConfigNode string/double/bool reads, invariant double parsing, case-insensitive unique string/bulkhead-profile handling, player-part availability checks, dry part-mass calculation, window clamping, stage-solution failure handling, and plan filename sanitizing.
- Updated Mission Planner, Stage-By-Stage, Planning/Analyze Existing, engine/tank databases, saved-subassembly scanning, stage scanning, and both stage solvers to use the shared routines.
- Kept Unity lifecycle callbacks and cache-specific reload wrappers local because they depend on class instance/static state rather than representing reusable helpers.

## 0.7.35

- Added `VesselPlanner.Core.CommonRoutines` for shared helper methods.
- Moved `FormatManeuver` from `MissionPlannerPage` into `CommonRoutines` and updated both Mission Planner and Stage-By-Stage to use the shared formatter.

## 0.7.34

- Moved the tank **Filter** and **Exclude** text fields onto the same row, matching the engine-filter layout.
- While a Stage-By-Stage stage is being built, tank suggestion action buttons now read **Add to Stage** for both single-type and mixed tank sets.

## 0.7.33

- Split filter persistence into four independent settings: engine include Filter, engine Exclude, tank include Filter, and tank Exclude.
- Each filter field now autosaves/restores only when its own persistence setting is enabled. Disabling one clears only that field's saved value and leaves the other filter in the same category unchanged.
- Existing 0.7.32 combined engine/tank persistence settings migrate to the corresponding two new per-field settings on first load.

## 0.7.32

- Renamed the engine-name text field label to **Filter**.
- Added separate **Exclude** fields for engines and tank suggestions.
- Engine and tank Filter/Exclude fields now accept multiple comma-separated terms. Filter terms use OR matching; any matching Exclude term removes the candidate.
- Filter values are saved automatically while persistence is enabled.
- Added **Settings → Filters** with independent options to save/restore the engine Filter/Exclude pair and the tank Filter/Exclude pair. Both options default to enabled.

## 0.7.31

- Set the Add/Edit Mission Step **Source body** display to `GUILayout.Height(30)`.
- Selecting a saved Mission Planner plan in Stage-By-Stage now initializes the Stage-By-Stage plan name from the mission plan name; the name remains editable afterward.

## 0.7.30

- Increased the Stage-By-Stage New/Edit Stage dialog height slightly in both Engines & Tanks and Subassemblies modes.
- Mission Planner now starts with the mission name **Unnamed Mission**.
- The Mission Planner row delete **X** is now drawn in red.
- **Return From A Moon** now derives its automatic Needed Δv from the active delta-v CSV: moon ascent/surface-to-orbit Δv plus the parent-to-moon row's capture Δv, which is used as the reverse moon-escape leg.
- Maneuver dropdown entries now use readable spaced labels while keeping the existing enum values and saved-file compatibility.

## 0.7.29

- Moved the Stage-By-Stage **Core burns too** booster-layout toggle onto its own line below **Side Boosters**. **Radial decouplers** remains on the following conditional line when Side Boosters is enabled.

## 0.7.28

- Added Stage-By-Stage Engines & Tanks booster-layout options: **Side Boosters**, **Core burns too**, and a conditional **Radial decouplers** toggle when Side Boosters is enabled. The layout choices are saved with the stage and shown in the stage summary.
- ComboBox popup entries now use the active button font, size, and style so dropdown text matches normal buttons.
- Tank analysis now evaluates tank sets containing up to three different tank types. Every tank type in a suggested set must share the same KSP bulkhead profile, and Stage-By-Stage captures the complete selected set.
- Engine display names now pass through KSP's `Localizer`, including engines detected in Analyze Existing, so localized part titles are shown when available.

## 0.7.27

- Mission Planner steps are now mutually exclusive **Engines & Tanks** or **Subassemblies** steps, matching the Stage-By-Stage stage-type model.
- Engines & Tanks steps contain maneuver/body/route/delta-v data only; Subassemblies steps contain one or more saved subassemblies with Count and per-copy Decoupler options only.
- Clicking a Subassemblies mission step in Stage-By-Stage now opens a Subassemblies stage directly instead of mixing its mass into an engine/tank stage.
- Renamed Mission Planner entry actions from maneuver-centric wording to **Mission Step** where appropriate and updated documentation.

## 0.7.26

- Changed the Mission Planner **Add Subassemblies** editor to match the Stage-By-Stage Subassemblies workflow: the same available/selected list layout, per-line Count and Decoupler controls, duplicate-add count behavior, Remove placement, mass details, and validation.
- Increased the expanded Add/Edit Mission Maneuver height to accommodate the Stage-By-Stage-sized subassembly lists.
- Updated README and manuals for the unified subassembly workflow.

## 0.7.25

- Renamed user-facing Mission Planner and Stage-By-Stage **Assembly/Assemblies** labels to **Subassembly/Subassemblies** to match KSP terminology.
- Moved **Mission Planner** and **Stage-By-Stage** to the left side of the main mode-button row, followed by an 18 px gap before **Analyze Existing** and **Planning**.
- Updated README and manuals for the terminology and mode-button layout changes.

## 0.7.24

- Mission Planner assembly rows now use flexible spacing before **Remove**, keeping the remove button aligned at the far right of the row.
- Increased the horizontal gap after the Mission Planner Assembly **Add** toggle from 10 px to 20 px.
- Add/Edit Mission Maneuver can now be dragged from anywhere in the window, not only the title strip.
- Increased the Stage-By-Stage Subassemblies New/Edit Stage dialog height and reserved more bottom space so the validation/status line remains fully visible when no subassembly has been selected.
- Updated README and manuals for these UI refinements.

## 0.7.23

- Increased the Stage-By-Stage Subassemblies New/Edit Stage dialog height so the stage-mass and Add/Save/Cancel controls are no longer clipped at the bottom.
- Mission Planner Assembly **Add** now supports one or more saved KSP subassemblies instead of a single selection. Each selected assembly has its own whole-number **Count** and independent **Decoupler** toggle.
- Mission-plan files now save each assembly's file/name, stored unit mass, count, decoupler selection, and per-copy decoupler mass. Mission files from 0.7.20/0.7.21 with a single assembly are converted automatically when loaded.
- Mission-linked Engines & Tanks stages preserve all counted mission assemblies and their per-copy decouplers, show them in the stage list, include their combined mass in the stage solve, and propagate that mass into every lower stage.
- Updated README and manuals for the expanded Mission Planner assembly workflow and the taller Subassemblies stage dialog.

## 0.7.22

- Stage-By-Stage **New Stage/Edit Stage** now has two mutually exclusive stage types: **Engines & Tanks** and **Subassemblies**.
- **Subassemblies** stages can contain one or more saved KSP subassemblies. Each selected subassembly has its own quantity field and independent **Decoupler** toggle.
- When a subassembly's Decoupler option is enabled, the decoupler quantity follows the subassembly count. The reference mass is chosen from the subassembly root part's bulkhead profile(s) using `decouplerMasses.cfg`.
- Subassembly and per-copy decoupler mass are included in the stage mass and therefore in the payload carried by every lower stage.
- Subassembly-stage contents, counts, decoupler selections, and masses are saved in Stage-By-Stage plan files. Plans from earlier versions load as **Engines & Tanks** stages for backward compatibility.
- Updated README and manuals for the new Stage-By-Stage stage types and subassembly-stage workflow.

## 0.7.21

- Added 10 px of horizontal spacing to the right of the Mission Planner Assembly **Add** toggle so the assembly selector no longer sits directly against the toggle.
- Updated README and manuals for the Mission Planner Assembly control layout.

## 0.7.20

- Mission Planner maneuver entries now include an optional **Assembly** control. Enabling **Add** shows the saved KSP subassemblies from the current save and displays each assembly's calculated wet mass.
- Mission plans now save the selected assembly file/name and mass for each mission step. Older mission plans load unchanged with no assembly selected.
- Mission-linked Stage-By-Stage stages copy the selected assembly from the mission step, show it in the stage requirements/list, and include its mass as fixed stage dry mass during engine/tank sizing.
- Assembly mass is also included when completed upper stages become payload for lower stages, so the added mass propagates through the rest of the vehicle plan.
- Added saved-subassembly mass scanning from `saves/<current save>/Subassemblies/*.craft`, including loaded resource mass and craft `modMass` adjustments.
- Updated README and manuals for Mission Planner assembly selection and Stage-By-Stage assembly mass handling.

## 0.7.19

- Stage-By-Stage stage part lists now display tanks first and engines second. Saved part order and placement behavior are unchanged.
- When **Add decoupler mass** is enabled and the calculated decoupler mass is greater than zero, the stage list now shows a **decoupler** item between the tanks and engines, including its reference mass.
- Updated README and manuals for the stage-list display order and decoupler item.

## 0.7.18

- When **Calculate** is clicked while editing an existing Stage-By-Stage stage, Planning now switches its body/environment to the last body referenced in the selected or linked mission plan before solving the stage.
- For a final interplanetary transfer, the transfer destination is treated as the mission plan's last body.
- Updated README and manuals for mission-aware body selection during stage editing.

## 0.7.17

- Stage-By-Stage Mission Plan maneuver buttons are taller for easier reading and selection.
- Added a live text filter above the Planning **Tanks** list; it matches both the displayed tank name and internal part name.
- Tank candidates are now limited to storage parts that have both `top` and `bottom` attach nodes, excluding radial/one-ended storage parts from tank selection.
- Updated README and manuals for the taller mission buttons, tank filtering, and stack-node requirement.

## 0.7.16

- Mission Planner **Landing** now defaults its body to the most recently specified body in earlier mission steps, including a previous transfer destination; the user can still choose a different body.
- Stage-By-Stage mission sidebar text is now left-justified.
- Each Stage-By-Stage stage now shows its linked **Mission step N: maneuver** line. Mission-plan name, step number, and maneuver are saved with the stage plan.
- Clicking a mission step that already has a linked stage now opens that stage in **Edit Stage** instead of creating another stage.
- Fixed a single manually selected Bulkhead profile being combined with an automatically preset previous profile: the first manual profile choice now replaces the automatic preset, while later choices can still intentionally make a multi-profile selection.
- Updated README and manuals for mission-linked stages, Landing body defaults, and Bulkhead profile selection behavior.

## 0.7.15

- Mission Planner now has a **Mission name** field plus **Save** and **Load** buttons. Mission plans are saved under `GameData/VesselPlanner/PluginData/MissionPlans` using the mission name as the `.cfg` filename.
- Added shared mission-plan persistence for maneuver kind, body/route, required delta-v, and ASL/VAC basis.
- Stage-By-Stage now has a **Select Mission Plan** button that loads one of the saved Mission Planner files without replacing the current stage plan.
- When a mission plan is selected, its maneuvers are shown in reverse order in a list along the left side of the Stage-By-Stage window.
- Clicking a mission maneuver opens a new stage with that maneuver's target delta-v and delta-v basis prefilled.
- Updated README and manuals for mission-plan save/load and Stage-By-Stage mission integration.

## 0.7.14

- Restored the New/Edit Stage **Bulkhead profiles** control to the original inline multi-select dropdown/list used before 0.7.11.
- Removed the Bulkhead-specific shared-ComboBox, modal-popup, and late/topmost-popup workaround code; New/Edit Stage remains modal while its inline profile list is open.
- Removed all embedded images from the main Word instruction manual and replaced each former figure with a descriptive `[IMAGE PLACEHOLDER: ...]` tag.
- Updated README and manuals for the restored Bulkhead profile selector and image-placeholder manual format.

## 0.7.13

- Fixed the New/Edit Stage Bulkhead profiles ComboBox failing to appear after the 0.7.12 modal-popup change.
- Removed the second-modal-window approach. While the bulkhead ComboBox is open, New/Edit Stage temporarily renders as a normal click-through-protected window and the shared ComboBox popup is drawn in a late/topmost pass.
- When the popup closes, New/Edit Stage immediately returns to modal behavior. The existing multi-profile toggle behavior is unchanged.
- Updated README and manuals for the corrected Bulkhead ComboBox layering behavior.

## 0.7.12

- Fixed the New/Edit Stage Bulkhead profiles ComboBox appearing behind the modal stage dialog and not receiving mouse input.
- Added modal-popup support to the shared `ComboBox` implementation.
- The Stage-By-Stage Bulkhead profiles selector now opens its ComboBox as a modal popup drawn after the New/Edit Stage window, keeping it above the dialog and responsive while preserving the existing multi-profile toggle behavior.
- Updated README and manuals for the modal Bulkhead ComboBox behavior.

## 0.7.11

- Replaced the Analyze Existing Planet/body dropdown with the shared `ComboBox` implementation used by Mission Planner and Planning.
- Analyze Existing body changes continue to clamp altitude to the selected body's atmosphere and immediately recalculate environment-dependent results.
- Replaced the New/Edit Stage Bulkhead profiles inline dropdown/list with the shared `ComboBox` implementation.
- The Bulkhead profiles ComboBox preserves multi-profile selection by toggling one profile per selection, marks selected profiles with `[x]`, and includes a `Clear selection` entry.
- Updated README and manuals for the shared ComboBox selectors.

## 0.7.10

- Replaced the Planning screen Planet dropdown with the shared `ComboBox` implementation used by Mission Planner.
- Planning body changes continue to clamp altitude to the selected body and immediately recalculate environment-dependent results.
- Analyze Existing retains its existing body-selector overlay; the change is scoped to Planning.
- Updated README and manuals for the Planning ComboBox selector.

## 0.7.9

- Added automatic planet-pack detection for Mission Planner using `PlanetPackHeuristics`.
- `DeltaVTable.planetPack` is now a runtime string instead of the hard-coded `Stock` constant.
- Mission Planner detects the active pack before loading delta-v data; known packs use the `PlanetPackKind` name and a custom single pack uses its detected GameData folder name.
- Delta-v tables are loaded from `GameData/VesselPlanner/PluginData/DeltaVTables/<packName>.csv`.
- Development builds now copy all CSV files in `DeltaVTables`, not only `Stock.csv`.
- Updated README and manuals for planet-pack-aware delta-v table selection.

## 0.7.8

- Fixed CS0117 in Mission Planner: clipboard-selector disabling no longer accesses the private ComboBox backing dictionary.
- Added `ComboBox.Close(id)` as the supported way to close an open popup when its control becomes disabled.
- Retained the 0.7.7 clipboard availability behavior.

## 0.7.7

- Mission Planner now disables the **Clipboard Δv** selector when the clipboard does not contain usable transfer data for the currently selected destination/route.
- The selector automatically becomes available again when matching clipboard transfer data is present.
- Updated README and manuals for clipboard-selector availability.

## 0.7.6

- Fixed Mission Planner clipboard delta-v selection so **Ejection** reads only the `Ejection Δv:` value and **Insertion** reads only the `Insertion Δv:` value from the matching clipboard transfer block.
- Clipboard delta-v selection changes now force the newly selected field to be reapplied immediately to Needed Δv.
- Updated README and manuals for the corrected clipboard field mapping.

## 0.7.5

- Restored the bottom Mission Planner append control to a full **Add New Maneuver** text button instead of the plus icon.
- Fixed the **Clipboard Δv** selector so changing between **Ejection**, **Insertion**, and **Total** immediately reloads the corresponding value from the matching clipboard transfer block into Needed Δv.
- Updated README and manuals for the restored append button and clipboard-selector refresh behavior.

## 0.7.4

- Launch and Sub-Orbital Launch now use the inferred home-body/most-recent-Landing body only as the initial body selection; the body remains an editable dropdown so the user can choose a different launch body.
- Mission Planner row insertion controls now use `+▲` for Add Above and `-▼` for Add Below.
- Transfer To Another Planet now shows a **Clipboard Δv** dropdown with **Ejection**, **Insertion**, and **Total** choices.
- Clipboard transfer parsing now reads `Ejection Δv`, `Insertion Δv`, or `Total Δv` from the matching transfer-planner block according to the selected Clipboard Δv choice. The clipboard route must still match the derived source and selected destination.
- Updated README and manuals for the revised launch-body selection, row controls, and clipboard delta-v selector.

## 0.7.3

- Mission Planner Launch and Sub-Orbital Launch body selection is now derived from mission history instead of using an editable body selector.
- A launch uses KSP's home body when there is no earlier Landing maneuver; after a Landing, it uses the body from the most recent earlier Landing. Derived launch bodies refresh after insert, edit, delete, and reorder operations.
- Replaced Mission Planner row action text buttons with the requested GUIContent icons: ▲ Move up, ▼ Move down, bold + Add child, and ✖ Delete. The bottom Add New Maneuver control also uses the + icon.
- Removed the **Reload Delta-v Table** button from Mission Planner. The table continues to load automatically when Mission Planner initializes.
- Updated README and manuals for the launch-body inference and revised Mission Planner controls.

## 0.7.2

- Mission Planner now checks the system clipboard while entering or editing **Transfer To Another Planet** maneuvers.
- Transfer-planner text containing a `Total Δv:` line automatically fills the maneuver's needed delta-v field.
- Clipboard transfer routes such as `Kerbin (@100km) -> Moho (@100km)` are matched against the maneuver's derived source body and selected destination before the value is applied, preventing an unrelated clipboard transfer from overwriting the entry.
- Clipboard `Total Δv` takes precedence over the CSV transfer suggestion; if no matching clipboard transfer is found, the existing Stock.csv behavior remains unchanged.
- Updated README and manuals for clipboard-assisted transfer delta-v entry.

## 0.7.1

- Added double-click editing for Mission Planner rows; double-click the row data to reopen the maneuver entry window with the existing values, then save the edited entry.
- Transfer To Another Planet no longer has an independent source-body selector. Its source body is derived from the immediately preceding maneuver (or the preceding transfer's destination).
- Transfer source bodies are refreshed after insert, edit, delete, and reorder operations so displayed routes stay consistent with mission order.
- A transfer cannot be saved without a preceding maneuver that provides a body.
- Updated README and manuals for Mission Planner editing and transfer-source behavior.

## 0.7.0

- Added a fourth editor mode, **Mission Planner**, for building an ordered list of mission maneuvers.
- Mission rows show maneuver, body/route, required delta-v, ASL/VAC basis, **Add Above**, **Add Below**, **Up**, **Down**, and **Delete** controls.
- Added a maneuver-entry window using the supplied drop-down ComboBox behavior and the requested maneuver enumeration.
- Body selectors are shown for launch/orbit/landing and orbital-change maneuvers; transfers select source and destination bodies; Return From A Moon selects a moon.
- Added CSV delta-v table loading from `GameData/VesselPlanner/PluginData/DeltaVTables/Stock.csv` using the requested 13-column format.
- Launch/Sub-Orbital Launch automatically use `dV_to_low_orbit`, planetary transfers use `total_capture_dV`, and Landing/Splashdown use `dV_low_orbit_to_surface` when a matching table row is available; the value remains manually editable.
- Added a starter Stock delta-v table and a **Reload Delta-v Table** control for editing/testing custom values without restarting KSP.
- Mission Planner uses the same main-window background and skin as the rest of VesselPlanner.
- Updated README and manuals for the new Mission Planner.

## 0.6.43

- Added a **Time labels** setting to Flight Plot Settings with **Every line**, **Every other**, and **Every third** choices.
- The selected time-label frequency is saved as `ElapsedTimeLabelGridInterval` in `VesselPlannerSettings.cfg`; the default remains every third vertical grid line.
- Time labels are now positioned directly from the fixed vertical-grid coordinates, so all three frequency choices remain stationary while the Flight Data window is resized.
- Elapsed values on intermediate grid lines are interpolated from the visible samples, with the existing sample interval used for extrapolation beyond the newest visible sample.
- PNG export uses the same selected time-label frequency and alignment as the on-screen graph.
- Updated README and manuals for the configurable time-label spacing.

## 0.6.42

- Removed the **Refresh available resources/sensors** button from Flight Plot Settings; resources and sensor availability continue to refresh automatically.
- Fixed elapsed-time label placement so labels after the minimum-width boundary are no longer shifted left by the right-edge clamp.
- Elapsed-time labels now remain centered on their fixed tick positions, keeping every interval exactly three vertical grid columns apart.
- PNG export uses the same corrected elapsed-time label alignment as the on-screen graph.
- Updated README and manuals for the Flight Data changes.

## 0.6.41

- Changed the Flight Data vertical grid spacing so each elapsed-time tick interval is divided into three equal fixed grid intervals.
- Every third vertical grid line now aligns with an elapsed-time marker at the bottom of the graph.
- Grid lines and elapsed-time markers remain stationary during horizontal resizing; wider graphs only append additional grid lines and time markers on the right.
- Updated README and manuals for the new grid/time-marker alignment.

## 0.6.40

- Changed the Flight Data graph vertical grid lines to fixed horizontal positions based on the 935 px minimum-width layout.
- Resizing the Flight Data window no longer redistributes existing vertical grid lines; widening adds additional grid lines only on the right, while narrowing removes only lines that no longer fit.
- The graph's moving right border remains tied to the current graph width.
- Normalized release metadata from the supplied 0.6.39 rebase baseline to version 0.6.40.
- Updated README and manuals for the fixed vertical-grid behavior.

## 0.6.36

- Changed the Flight Data window to horizontal-only resizing.
- Replaced the lower-right corner resize control with a centered grab handle on the right edge; dragging it changes only the window width.
- The Flight Data window height is no longer user-resizable, eliminating vertical resize movement and making the graph layout more stable.
- The separate Flight Plot Settings window retains its existing two-axis resize handle.
- Updated README and manuals for the new Flight Data resize behavior.

## 0.6.35

- Fixed Flight Data window flicker while resizing with the solid background enabled.
- The opaque Flight Data background is now drawn once behind the real KSP window instead of drawing a second full window skin inside the Flight Data window. This preserves the selected skin, title, and border while eliminating the resize-time ghost/second-window effect.
- Retained the 0.6.34 resize-grip mouse ownership and bottom-edge anchoring behavior.
- Updated README and manuals for the revised solid-background rendering behavior.

## 0.6.34

- Fixed the Flight Data resize grip so it owns the mouse drag for the full resize operation instead of allowing the window drag handler to take over part of the movement.
- When the Flight Data window starts a resize while its bottom edge is aligned with the bottom of the screen, finishing the resize preserves that bottom alignment.
- Changed the default **Use solid background for Flight Data window** setting to enabled for new configurations. Existing saved `SolidFlightWindowBackground` values are still honored.
- Updated README and manuals for the new Flight Data resize behavior and solid-background default.

## 0.6.33

- Replaced `Manual/VesselPlanner-Manual.docx` with the user-supplied illustrated manual containing the added VesselPlanner UI screenshots.
- Preserved all embedded manual images while bringing the illustrated document forward to version **0.6.33**.
- Added the current **Use solid background for Flight Data window** option to the illustrated manual so Flight Settings documentation matches the 0.6.32 functionality.

## 0.6.32

- Added a persistent **Use solid background for Flight Data window** option to **Flight Plot Settings**.
- When enabled, the Flight Data graph window uses an opaque dark background while retaining the selected KSP/alternate window skin and normal border/title styling.
- The new setting is stored in `VesselPlannerSettings.cfg` as `SolidFlightWindowBackground` and is off by default.

## 0.6.31

- Replaced `jenkins.txt` with the VesselPlanner-specific Jenkins/build configuration.
- The build configuration now targets `VesselPlanner`, copies `VesselPlanner.version`, `License.md`, and `README.md` into `GameData/VesselPlanner`, and includes the `Manual` folder in release packages.

## 0.6.30

- Increased the maximum horizontal resize width of the main VesselPlanner editor window from **1280 px** to **1850 px**.
- The minimum width remains **1150 px**, and the selected width continues to be remembered.

## 0.6.29

- Increased the synchronized candidate-engine column-header area from **28 px to 34 px**.
- Made the main VesselPlanner editor window horizontally resizable with a **1150 px minimum** and **1280 px maximum** width.
- Added a right-edge horizontal resize grip and persisted the selected main-window width in `VesselPlannerSettings.cfg`.
- Updated README and manuals for the new window-width range and 34 px engine-list header area.

## 0.6.28

- Changed the lighter solid background used by the **Stage-By-Stage Plan** and **New/Edit Stage** windows to a shade value of **0.36** for stronger visual separation from the main planner.
- Increased the synchronized candidate-engine column-header scroll area by **4 pixels** (24 px to 28 px) so the heading buttons have more vertical room.

## 0.6.27

- Limited the main VesselPlanner editor window to a maximum width of **1150 pixels**.
- Synchronized the candidate-engine column-heading row with the engine list's horizontal scroll position, so the sortable heading buttons move with their columns.
- Updated README and manuals for the maximum editor width and synchronized engine-list headings.

## 0.6.26

- Made the solid background of the **Stage-By-Stage Plan** window slightly lighter than the main VesselPlanner window.
- Applied the same slightly lighter solid background to the **New Stage/Edit Stage** dialog for clearer visual separation.
- Updated README and manuals to document the Stage-By-Stage window appearance.

## 0.6.25

- Stage-By-Stage Plan now opens left-aligned with the main VesselPlanner window, with its top edge directly below the main mode-button row.
- New Stage now opens immediately to the right of the Stage-By-Stage Plan window, with the two windows top-aligned and adjacent.
- Updated README and manuals for the new Stage-By-Stage window-placement behavior.

## 0.6.24

- Added `Manual/VesselPlanner-Stage-by-Stage-Tutorial.docx`, a detailed Word tutorial covering the complete Stage-By-Stage planning workflow.
- Updated `Manual/VesselPlanner-Manual.docx` to point users to the dedicated Stage-By-Stage tutorial.
- Updated `README.md` to list both Word manuals included with the source package.

## 0.6.23

- Constrained the Stage-By-Stage New/Edit Stage **Add** decoupler-mass toggle to `GUILayout.Width(75)` so the control uses a fixed 75-pixel width.
- Replaced the VesselPlanner ToolbarController icon with the newly supplied transparent checklist-and-graph artwork, updating both the 24 px and 38 px textures.

## 0.6.22

- Replaced the VesselPlanner ToolbarController icon with the new checklist-and-graph artwork supplied for this release.
- Updated both the 24 px and 38 px toolbar textures used by the editor and flight toolbar buttons.

## 0.6.21

- Added `GameData/VesselPlanner/decouplerMasses.cfg` with configurable decoupler and stack-separator masses for `size0`, `size1`, `size1p5`, `size2`, `size3`, `size4`, `size5`, and `size6`, including the supplied Stock/SpaceY source labels.
- Added a sortable/configurable **Mass t/eng** column to the candidate engine list, showing the dry mass of one engine part.
- Added an **Add decoupler mass** option to the Stage-By-Stage New/Edit Stage dialog. When enabled, VesselPlanner adds the matching decoupler mass to the stage fixed dry mass; if several stack profiles are selected, the largest matching configured mass is used.
- For Stage-By-Stage stages after the first, the New/Edit Stage dialog now shows **Previous stages mass** and uses that cumulative wet mass as the payload for the current stage. The cumulative value starts with the original plan payload and adds the full wet mass of every previously selected planned part plus stage cargo and optional decoupler mass.
- New Stage now presets its bulkhead profile automatically. For the first stage, an existing editor vessel uses the open lower stack node (bottom, falling back to top). For later stages, the previous stage engine's bottom node is used, falling back to its top node.
- Added `size1p5` (1.875 m) to the bulkhead-profile table so the Making History profile can be selected and can resolve the corresponding decoupler mass.

## 0.6.20

- When a new **Stage-By-Stage** plan is started, its starting/payload mass is initialized from the current editor vessel's full wet mass when a vessel is present. If the editor is empty, the default remains **5.0 t**. Existing plans with stages keep their saved mass.

## 0.6.19

- Replaced `Manual/VesselPlanner-Manual.docx` with the formatted VesselPlanner instruction manual, including the screenshot placeholders from the rewritten README.
- Added a README note pointing users to the packaged Word manual.

## 0.6.18

- Rewrote `README.md` as a simple user instruction manual organized around installation, the three editor modes, Stage-By-Stage planning, settings, flight telemetry, exports, saved files, and troubleshooting.
- Added clearly marked image placeholders throughout the README for future screenshots.

## 0.6.17

- Fixed New/Edit Stage Tab navigation so it no longer depends on the modal IMGUI wrapper delivering `KeyCode.Tab`. Tab/Shift+Tab are now polled from Unity's normal `Update()` loop, queued into the modal, and still accept IMGUI tab-character events as a fallback.

## 0.6.16

- Added keyboard Tab navigation to the Stage-By-Stage **New Stage** and **Edit Stage** modal. **Tab** moves through Target Δv, Minimum TWR, Max engines, and Additional Cargo Mass; **Shift+Tab** moves through the same fields in reverse.

## 0.6.15

- The selected tank is now displayed on its own line directly below the **Selected Engine** line instead of being appended to the engine header. The tank line remains hidden until a tank suggestion is selected.

## 0.6.14

- The **Selected Engine** pane header now shows the explicitly selected tank and the number of tank copies required, immediately after the selected engine information. The tank text is omitted until a tank suggestion is selected.

## 0.6.13

- After **Calculate** in the Stage-By-Stage **New Stage** or **Edit Stage** modal, the main VesselPlanner window is now brought to the front so the engine and tank choices are immediately accessible.

## 0.6.12

- After **Add Engine & Tanks** successfully adds the selected engine and tank to the open Stage-By-Stage stage, the **Stage-By-Stage Plan** window is brought to the front.
- The `srf` (Surface attach) bulkhead profile is now shown at the top of the New/Edit Stage bulkhead-profile list, ahead of the numeric stack-size profiles.

## 0.6.11

- Added a **Bulkhead** column to the Planning **Tanks** list, showing each tank part's KSP `bulkheadProfiles` values. The column is sortable.
- Stage-By-Stage bulkhead selection now uses KSP profile tokens rather than only numeric stack sizes, and **srf (Surface attach)** is available in the New/Edit Stage bulkhead-profile dropdown.
- Tank suggestions now obey the Stage-By-Stage bulkhead-profile selection: a tank is listed only when at least one of its KSP bulkhead profiles matches one of the profiles selected for that stage.
- Candidate engines use the same Stage-By-Stage profile selection, including `srf`; Analyze Existing keeps its existing top-node-size matching behavior.
- Saved plans now store `BulkheadProfile` values. Older plans containing only `BulkheadSize` entries remain compatible and are translated to `sizeN` profiles when loaded; numeric size entries are also still written for downgrade compatibility.

## 0.6.10

- Tank suggestions in the Planning **Tanks** pane are now selectable by clicking their row; the selected tank is highlighted.
- **Add Engine & Tanks** now uses the explicitly selected tank instead of automatically using the first tank suggestion. The button is disabled until both a Stage-By-Stage stage is open and a tank is selected.
- Changing the selected engine, recalculating, simulating, or refreshing the part databases clears the tank selection so a stale tank cannot be reused accidentally.

## 0.6.9

- Widened all New/Edit Stage entry-field labels to the same width as **Additional Cargo Mass**, aligning Target Δv, Δv basis, Minimum TWR, Max engines, Additional Cargo Mass, and Bulkhead sizes in one consistent input column.

## 0.6.8

- The Stage-By-Stage **New Stage** dialog is now a true modal window, so it stays above the other GUI windows and blocks interaction with them until **Calculate** or **Cancel** closes it. The shared **Edit Stage** dialog uses the same modal behavior.
- Renamed the New/Edit Stage field label from **Cargo mass (t)** to **Additional Cargo Mass**. The value is still stored and calculated as per-stage cargo mass in metric tons.

## 0.6.7

- Fixed the Stage-By-Stage solid-background rendering when its windows overlap the main planner. The plan, New/Edit Stage, and Load Plan windows now paint their opaque fill inside their own GUI window layer and then repaint the active KSP window skin over it, preventing the main planner from bleeding through even when the Stage-By-Stage window is above it.

## 0.6.6

- Renamed the mod from **EngineStagePlanner** to **VesselPlanner**. The project/assembly, namespaces, toolbar identifiers, GameData folder, version file, settings paths, build/deploy scripts, window titles, and documentation now use `VesselPlanner`.
- To ease the rename, VesselPlanner reads the legacy `GameData/EngineStagePlanner/PluginData/EngineStagePlannerSettings.cfg`, saved plans, and flight maxima when the new VesselPlanner equivalents do not yet exist. New saves are written under `GameData/VesselPlanner`; the old default CSV path is translated to the new VesselPlanner default.
- Stage-By-Stage windows are now drawn after the main editor planner/settings windows. When **Use solid backgrounds for all editor windows** is enabled, the plan, New/Edit Stage, and Load Plan windows therefore remain fully solid even where they overlap the main planner.
- Added **Cargo mass (t)** to New Stage and Edit Stage in Stage-By-Stage mode. Cargo is stored per stage, shown in the stage summary, saved/loaded with plan files, and included as non-propellant dry mass in that stage's engine/tank solve. Existing plans without the new value load with zero cargo.

## 0.6.5

- Added a **Delete** button to each row of the Load Plan window. It takes two presses, the first arming that row, and removes the file only; the plan open in the window is left alone.
- The Stage-By-Stage plan, stage and load windows now use the same solid background as the planner and its settings window, following the same Appearance setting.
- **Stage-By-Stage** is now exclusive with **Planning** and **Analyze Existing**: picking it deselects the other two, and picking either of them leaves the mode, putting the plan window away and closing any stage being built. The plan itself is kept. Closing the plan window from its own × leaves the mode as well.
- Widened the **Match stage bulkhead size** toggle from 190 to 250 pixels.

## 0.6.4

- `BulkheadProfiles.cfg` now names sizes 5 to 10 as 7.5, 10, 12.5, 15, 17.5 and 20 m instead of leaving them as placeholders.
- The fallback table in code was updated to match, so a missing or unreadable config file gives the same names the shipped one does.

## 0.6.3

- **Finalize** now closes the planner window, leaves the plan window open as the build list, and saves the plan in its finalised state, so reloading it comes back finalised.
- Loading a finalised plan closes the planner window too. An unfinished plan loads with the planner still available.
- **Reopen** brings the planner window back.
- Added a **Clear** button. It takes two presses, the first arming it, so a stray click cannot discard a plan.
- The plan window is now drawn independently of the planner window rather than from inside it, which is what lets one close while the other stays open.

## 0.6.2

- Plans are now filed under the vessel name; the separate plan-name field is gone, so there is no second name to keep in step.
- **New Stage** is disabled while the stage dialog is open, and so are the per-stage **Edit** buttons.
- Added an **Edit** button to each stage. It reopens the dialog on that stage's requirements and recalculates it, keeping the parts already chosen for it.
- The **Bulkhead** column is now sortable. It sorts on the node size itself rather than its label, so the diameters come out in order instead of alphabetically.
- **Match stage bulkhead size** is hidden while a Stage-By-Stage stage is open, since the stage's own sizes replace it there; the sizes in use are shown in its place.
- Bulkhead descriptions now come from `BulkheadProfiles.cfg` instead of being hard-coded, so a part pack can describe stack sizes this mod does not know about. The file ships with the stock sizes 0 to 4 described and entries through size10 for larger part packs to fill in; a PROFILE for the same size in another file replaces the entry here.
- The bulkhead size selector in the stage dialog is a dropdown rather than a row of buttons, and lists whatever the config file defines.

## 0.6.1

- Fixed two CS0165 build errors in the New Stage dialog. The three inputs were parsed in one `&&` chain, so short circuiting left the later `out` parameters unassigned as far as the compiler was concerned; each is now parsed in its own statement.
- **Finalize** is disabled until the plan has at least one stage.
- The vessel name is now an entry field, seeded from the craft in the editor but owned by the plan thereafter, and it is what gets saved.
- Added **Add Engine & Tanks** to the Selected Engine pane. It adds the selected engine and the tank at the top of the suggestion list to the open plan stage in one press, and is disabled unless a Stage-By-Stage stage is open.
- Added a **Bulkhead** column to the candidate engine list, showing the stack diameter the engine's top node mates with. It can be switched off with the other columns in Settings.
- The New Stage dialog can restrict candidate engines to chosen bulkhead sizes, which stands in for **Match stage bulkhead size** while that stage is open. A plan is built before the craft exists, so there is no stage bulkhead to match against; selecting no sizes considers them all.

## 0.6.0

- Added a **Stage-By-Stage** button, which opens a plan window holding the craft name, payload mass, starting body and a list of stages with the engines and tanks chosen for each.
- **New Stage** prompts for target Δv (vacuum or atmospheric), minimum TWR and maximum engines. **Calculate** is disabled until all three are filled in, then loads them into the planner window and solves the stage.
- While a stage is being built, the planner's **Add**, **Add Engine** and **Add Tank** buttons add the part and its quantity to that stage instead of placing it in the editor. Adding the same part twice accumulates into one line.
- Each stage has a **Delete** button, and each part a **Remove** button, until the plan is finalised.
- **Finalize** ends stage building: the New Stage button goes away and each part gains an **Add** button that places it in the editor. **Reopen** goes back to editing.
- **Save** writes the plan to `PluginData/Plans`, and **Load** lists what is there and opens the selected one.
- Deleting, loading and starting a stage are all deferred until both windows have been drawn, since each changes how many controls the windows draw.

## 0.5.62

- Added a **Δv basis** selector to the Planning **Requirements** pane: **Vacuum** or **Atmosphere**. The target field is labelled to match, and the choice is saved as `TargetDeltaVBasis`.
- On the vacuum basis the stage is sized from vacuum Isp as before, so the design does not change with the selected body or altitude.
- On the atmospheric basis the stage is sized from the Isp at the selected pressure, so the target is met where the craft actually flies. The altitude then does resize the stage: the same target needs more propellant the lower and thicker the air.
- The results table still reports both Atm Δv and Vac Δv, so the figure that was not targeted is still visible.

## 0.5.61

- Fixed the planner window flickering until a grip was dragged.
- The candidate engine list's base height was `_window.height` minus a fixed reserve, but the window is sized by its content, so the list fed its own height back through the window and the two never settled. Dragging a grip stopped it only because the height then sat against one of its clamps, where the window no longer changed it.
- The base height now comes from the screen, in one place shared by the list, the Planning alignment, and the Analyze Existing grip, which previously each derived it separately.

## 0.5.60

- Fixed the bottom of the Planning **Requirements** pane carrying on upwards after the grip had stopped.
- The candidate engine list is aligned to the bottom of the Requirements box but can only shrink so far. Below that the results box became the taller of the two and set the row height, so the grip stopped with the row while the Requirements box kept shrinking and its bottom edge pulled away.
- The upward stop is now the viewport at which the two boxes are the same height, worked out from the measured box heights each frame rather than a fixed figure, so it holds whatever the pane's chrome and the list's contents come to.

## 0.5.59

- Fixed the Planning panes continuing to move once a grip had reached its limit and the mouse kept going.
- The grip now takes the IMGUI hot control for the duration of the drag, so every mouse event goes to it and nothing else under the cursor reacts. Previously the movement past the stop was free to be picked up by whatever lay beneath, which dragged the window or scrolled the pane being resized.
- The window drag is also skipped outright while a grip drag is in progress.

## 0.5.58

- Dragging the Planning grip down no longer expands the window. What the **Requirements** viewport gains, the **Tanks** table gives up, so the two rows always add up to the same total.
- That only holds while both panes are within their limits, so the grip now stops where either would clamp. Past that point one row would grow with nothing given back, and the window had to take the difference.
- A saved offset from an earlier version is brought into the same range when it is read, so an old setting cannot reopen the window oversized.

## 0.5.57

- Dragging the Planning grip up now enlarges the **Selected Engine** and **Tanks** panes as well as shrinking the **Requirements** pane above it, so the grip trades height between the two rows instead of only resizing the top one.
- The Tanks table takes the negative of the grip offset; the Selected Engine list follows because its pane is already matched to the Tanks pane.
- Pane height is clamped to the same 60 to 900 pixel range as the other matched panes.

## 0.5.56

- The Planning **Requirements** pane now scrolls in a fixed viewport instead of stretching the planner window, and the grip above the detail panes resizes it. **Calculate** and the status line stay pinned below the scroll view.
- The grip can now be dragged in both directions. It previously stopped at the aligned position, because the pane stretched to fit its content and had nothing to give back; now that it scrolls, dragging up shrinks it.
- The candidate engine list still aligns its bottom to the Requirements box, so the drag moves both together. The drag offset was removed from the list's own height and from the alignment target, since the Requirements box now carries it.
- Widened the Requirements pane from 310 to 350 pixels so its widest row still fits once the scroll view takes its scrollbar out of the available width.
- The viewport height comes from the screen, not the planner window, for the same reason as the Analyze Existing panes: the window is sized by its content.

## 0.5.55

- Narrowed the Sensor column in the flight plot settings from 285 to 200 pixels.
- The Analyze Existing **Existing stage** pane now scrolls in a fixed viewport instead of stretching the planner window as lines, engines, and stage resources are added.
- The **Current engines** table scrolls on its own inside that pane, with its column header staying put above the rows.
- Both viewport heights come from the screen and the row count, never from the planner window: the window is sized by its content, so measuring it there would let the pane and the window grow off each other.
- Trimmed the engine table's column widths to leave room for the two scrollbars the list now sits inside.

## 0.5.54

- Axis-annotated series now get a full value scale down their side of the graph rather than just the two ends of the range.
- Ticks sit on the horizontal gridlines the graph already draws, so the numbers line up with the grid. The series name and units head the column.
- On a short graph the ticks thin to every second or fifth gridline so they cannot run into each other, and the topmost and bottommost ticks are nudged to stay clear of the heading and the graph edge.
- Several series on the same side each get their own column, working inward from the edge; a column that would reach the middle of the graph is skipped.
- The grid divisions are now a shared constant, so the ticks cannot drift away from the gridlines they label.

## 0.5.53

- The axis annotation at the top of the graph now states the full scale of its series, not just the top of it: name, then bottom and top of the scale, then units, on the assigned side.
- The two ends are separated by `..` rather than a dash, which would run into the sign of a negative minimum.
- The figure at the bottom edge is unchanged and still marks where the bottom of that scale sits on the graph.

## 0.5.52

- The flight plot **Export PNG** now includes the labels. The elapsed-time and stage-marker labels are GUI overlays drawn beside the graph texture, so they were absent from the exported image; the export now copies the graph into a taller image and bakes both sets in, with a strip along the bottom for the times.
- Added an **Axis** column to the flight plot settings. Each sensor's button cycles Off, Left, and Right, marking that series' scale on the chosen edge of the graph so the plot shows what it is displaying.
- Axis annotations name the series and print the top and bottom of the scale it was actually plotted against, in the series colour. Several series can share a side and stack.
- Annotations are drawn into the graph texture itself, so they appear both on screen and in the PNG export.
- The assignments are saved as `GraphAxisLeft` and `GraphAxisRight` in `VesselPlannerSettings.cfg`.

## 0.5.51

- Widened the flight plot **Start at Launch** button from 105 to 130 pixels.
- Fixed the elapsed-time labels along the bottom of the flight graph overwriting each other while the graph was still filling. They were positioned by sample, and with only a few samples recorded they sat a couple of pixels apart.
- The labels now sit at fixed positions spread across the full width of the graph. The time axis always spans as many samples as the plot area holds, so positions past the newest sample are projected from it using the average interval between the samples on screen, and fill in with real data as it arrives.
- The outermost labels are kept inside the graph so neither is clipped, and a narrow graph draws fewer labels rather than letting them touch.

## 0.5.50

- In Analyze Existing, the **Current engines** list in the **Existing stage** pane is drawn in a darker box, so it reads as a table rather than as more of the rows above it.
- The box uses a flat dark fill with the skin box's border and overflow cleared, since those are sliced from a bordered texture and would smear the corners across a plain fill.
- Its horizontal padding is deliberately small: the engine table's fixed column widths nearly fill the 350 pixel left column, and wider padding would push the Isp column past the pane edge.

## 0.5.49

- Added a **Stage Resources** heading above the stage resource lines in the **Existing stage** pane.
- Added a **Fuel** heading above the propellant lines in the **Selected Engine** pane.
- Widened the value column of the Analyze Existing informational rows from 90 to 120 pixels.
- Repository cleanup: removed the stale duplicate source tree and the outdated project-level CHANGELOG, README, .version, .gitignore, .sln, and BuildRelease.bat copies, and moved GameData to the repository root.

## 0.5.48

- Added a **Window skin** setting on the Settings > Appearance page: **KSP skin** (the default) or **Alternate skin**, which is Unity's stock GUI skin.
- The choice applies to every VesselPlanner window, in the editor and in flight, and takes effect immediately.
- The stock Unity skin is captured at the main menu before anything assigns `HighLogic.Skin`, since Unity resets `GUI.skin` to its default at the start of each OnGUI.
- Cached GUIStyles are copied from `GUI.skin`, so both windows now rebuild theirs when the setting changes.
- Stored as `UseAltSkin` in `VesselPlannerSettings.cfg`. The flight windows read the key directly from that file, so the skin applies without the editor planner having been opened.

## 0.5.47
- Moved each addon section into it's own file
- Renamed VesselPlannerAddon to ToolbarRegistration 
- Made GUIStyles static
- Moved initialization of GUIStyles into ToolbarRegistration

## 0.5.46

- Removed the planner window resize grip and all of its supporting code. The window is sized by GUILayout from its content again, with MinWidth/MinHeight as the floor.
- The Planning **Selected Engine** and **Tanks** panes no longer stretch toward the window bottom; the Tanks table is back to a fixed height with the engine pane matched to it. With no user-set window height there is nothing to stretch to, and measuring the drawn window would have let the panes and the window grow off each other.
- The pane grips are unaffected: the Planning width grip, the Planning grip above the detail panes, and the Analyze Existing grip above the Selected Engine pane all still work, as do the column alignments.

## 0.5.45

- Fixed the window resize grip again: the window still snapped back to its previous size after a drag.
- GUILayout sizes a window as Clamp(passed rect, content minimum, content maximum). With nothing stretchy inside the window, the content minimum and maximum are both the content size, so the clamp discarded the dragged size. The MinWidth/MinHeight options added in 0.5.44 could not win that clamp, because Mathf.Clamp applies its maximum last.
- The window is now given explicit Width/Height options from the requested size, which sets the group maximum as well. The window still grows past the request when the content needs more room.

## 0.5.44

- Fixed the window resize grip, which could not make the planner window larger. The window is drawn with GUILayout, which sizes it to its content and overwrote the dragged size on the next frame; the requested size is now kept separately and passed as the window's minimum, so the window follows the grip and can still grow further when the content needs more room.
- In Planning, the **Selected Engine** and **Tanks** panes now stretch down to the bottom of the window as it is resized, instead of staying at a fixed height.
- The Planning grip above the panes now genuinely trades height: growing the engine list shortens the panes below rather than making the window taller.

## 0.5.43

- Added a drag bar in Planning between the engine list and the **Selected Engine** / **Tanks** panes below it.
- Dragging it down gives the engine list more rows past the bottom of the **Requirements** box. Dragging up stops at the aligned position from 0.5.42, since the Requirements box beside the list cannot shrink further.
- The offset is saved and restored, and clears with **Reset pane sizes** on the Settings > Appearance page.

## 0.5.42

- In Planning, the bottom of the engine list box now lines up with the bottom of the **Requirements** box beside it.
- The list height is corrected from the measured bottom edges of the two boxes each frame, so a longer status line or rewrapped help text re-levels the list automatically.
- The Planning list can run past the 300-pixel cap that applied before, in either direction.

## 0.5.41

- Added a drag bar in Analyze Existing between the candidate engine list and the **Selected Engine** pane below it.
- Dragging it trades height between the two: the list grows or shrinks and the pane takes up the slack, so the column still ends level with the **Existing stage** box.
- The list can now be taller than the 300-pixel cap that applies without a drag, and stops growing once the pane below reaches its minimum height.
- The position is stored as an offset from the automatic height, so the list still tracks the planner window size. Settings > Appearance offers **Reset pane sizes**.

## 0.5.40

- Selecting **Analyze Existing** now rescans the selected stage and runs **Simulate all engines** automatically, so the candidate list is populated without a separate button press.
- Only the switch into the mode triggers the run; staying in Analyze Existing does not re-simulate every frame, and the **Simulate all engines** button still works as before.
- The run happens after the window has finished drawing, since replacing the candidate list mid-layout would change how many controls the window draws.

## 0.5.39

- Corrected the Analyze Existing **Selected Engine** pane height, which sat roughly half a line below the bottom of the **Existing stage** box.
- The reference edge is now the Existing stage box itself rather than the column group wrapping it. A layout group's rect can extend past its last child by that child's margin, which was being absorbed into the pane height.

## 0.5.38

- In Analyze Existing, the **Selected Engine** pane is now taller so its bottom lines up with the bottom of the left **Existing stage** column.
- The height is derived from the measured bottom edges of the two columns each frame, so hidden analysis lines, missing KSP stock stage mass rows, and a longer or shorter Current Engines list are all accounted for automatically.
- Changing which lines and sections are shown on the **Analyze Existing** settings page re-levels the pane immediately.

## 0.5.37

- Removed the width grip from the right edge of the **Selected Engine** pane in Analyze Existing.
- The Analyze Existing **Selected Engine** pane fills the right-hand column beneath the candidate engine list again, as it did before 0.5.33.
- Dropped the `AnalysisEnginePaneFraction` setting. Planning keeps its grip between **Selected Engine** and **Tanks**, and Settings > Appearance now offers **Reset pane width**.

## 0.5.36

- In Planning, the **Selected Engine** pane is now the same height as the **Tanks** pane.
- The Selected Engine detail list is sized from the measured height of both panes rather than a fixed offset, so the match holds when the Tanks footer line rewraps as the width grip is dragged.
- The width grip between the panes now spans the full height of the pane row.
- Analyze Existing is unchanged: its Selected Engine detail list keeps the fixed 130-pixel height.

## 0.5.35

- Removed the drag bar at the bottom of the **Selected Engine** pane. Neither detail pane carries a bottom grip now.
- The **Selected Engine** detail list and the **Tanks** table return to their fixed pre-0.5.33 heights of 130 and 145 pixels.
- The panes remain resizable horizontally: the Planning grip between the two panes and the Analyze Existing grip on the right edge of **Selected Engine** are unchanged.
- Dropped the `SelectedEngineScrollHeight` setting. Settings > Appearance now offers **Reset pane widths**.

## 0.5.34

- Removed the drag bar at the bottom of the **Tanks** pane.
- The **Tanks** table now follows the height set by the grip below the **Selected Engine** list, so a single grip sizes both Planning detail panes and keeps them the same height.
- Dropped the separate `TankListScrollHeight` setting; the tank table height is no longer stored independently.

## 0.5.33

- The **Selected Engine** and **Tanks** panes are now resizable both horizontally and vertically.
- Planning: drag the grip between **Selected Engine** and **Tanks** to move the split between the two panes.
- Analyze Existing: drag the grip on the right edge of **Selected Engine** to narrow or widen the pane.
- Both panes have a grip along the bottom of their list area that changes the pane height.
- Pane widths are stored as a fraction of the available row width, so they keep their proportions when the planner window is resized.
- Pane sizes persist in the settings file and can be restored with **Reset pane sizes** on the Settings > Appearance page.

## 0.5.32

- Added 10 pixels of spacing below the **Selected Engine** header before its detail list.
- Moved **Tanks needed for the selected propellant requirement** onto the Tanks pane header row, aligned to the right of **Tanks**.
- Gave the Planning Tanks pane more horizontal space so the full **Add Tank** buttons remain visible.
- Expanded the Tanks table **Tank**, **Excess**, and **Capacity** columns.

## 0.5.31
- Reworked the editor selected-engine layout by mode.
- In Planning mode, the selected engine details and tank suggestions now appear in two side-by-side panes, with **Selected Engine** on the left and **Tanks** on the right.
- Planning selected-engine details use a compact single-column scroll layout so the engine pane remains readable while sharing the row with the tank pane.
- In Analyze Existing, the selected-engine pane now appears directly below the candidate engine list in the right-hand column, while the Simulation environment / Existing stage panes remain on the left.
- Compact tank-table column widths keep the Planning two-pane layout inside the planner window.

## 0.5.30
- Removed the extra 24-pixel layout spacer below the flight-data status/export footer.
- The final status/footer line now occupies the bottom layout row of the flight data window instead of sitting approximately one text line above the bottom edge.
- The resize grip remains an absolute lower-right overlay and no longer consumes a separate layout row.

## 0.5.29
- Fixed the flight graph layout so the lower-right resize grip and bottom edge stay aligned with the actual bottom of the window.
- Increased the graph window's reserved vertical footer space to account for both CSV and PNG export-location rows, preventing `GUILayoutWindow` from expanding the outer window by approximately one text line.

## 0.5.28
- Added a persistent **PNG export folder** setting to the Flight Plot Settings window.
- The PNG export folder defaults to `Screenshots`, resolved relative to the KSP root, so PNG files default to `KSP_ROOT/Screenshots`.
- Relative PNG export paths are resolved under the KSP root; absolute paths are also accepted.
- The PNG directory is saved with the other settings in `GameData/VesselPlanner/PluginData/VesselPlannerSettings.cfg` as `PngExportDirectory`.
- PNG export creates the configured directory automatically when needed; CSV and PNG export locations remain independently configurable.
- Fixed the flight graph footer to show the CSV and PNG export paths separately instead of calling the obsolete `GetExportFolder()` helper.

## 0.5.27
- Added a persistent **CSV export folder** setting to the Flight Plot Settings window.
- The default setting is `VesselPlanner/PluginData/CSV`, resolved relative to `GameData`, so CSV files default to `GameData/VesselPlanner/PluginData/CSV`.
- Relative CSV export paths are resolved under `GameData`; absolute paths are also accepted.
- The configured directory is saved in `GameData/VesselPlanner/PluginData/VesselPlannerSettings.cfg` as `CsvExportDirectory`.
- CSV export creates the configured directory automatically when needed. PNG export remains under `Screenshots/VesselPlanner`.

## 0.5.26
- Changed the default editor appearance so **Use solid backgrounds for all editor windows** is enabled when no saved setting exists.
- Existing `VesselPlannerSettings.cfg` values are still honored, so users who previously saved the option as disabled keep their preference.
- The default applies to the editor planner and editor Settings window only; flight windows remain unchanged.

## 0.5.25
- Fixed KSP1 scene-switch event handler signatures to use the nested `GameEvents.FromToAction<GameScenes, GameScenes>` type required by `GameEvents.onGameSceneSwitchRequested`.
- This resolves the CS0246 and CS1503 build errors introduced by the 0.5.24 scene-transition handling.
- Scene-load/switch behavior is unchanged: editor and flight windows still close immediately during scene transitions and event handlers are unregistered in `OnDestroy()`.

## 0.5.24
- Added KSP scene-load and scene-switch event handlers to the editor and flight addon entry points.
- The editor planner and flight telemetry windows are forced closed as soon as KSP requests a scene load or scene switch, preventing a visible planner window from carrying into scene transitions.
- Scene event handlers are unregistered in `OnDestroy()` to avoid stale subscriptions when the addon instance is destroyed.

## 0.5.23
- Fixed the body-selector dropdown still appearing at the planner window's upper-left edge.
- Removed the separate ClickThroughBlocker dropdown window and now render the body list directly inside the main planner window using the body button's local GUI rectangle.
- The body list remains an overlay outside the GUILayout flow, so it opens directly below the current-body button without moving the controls below it.
- Dropdown mouse/scroll input is processed before the covered planner controls, preventing clicks on body choices from activating controls underneath the overlay.

## 0.5.22
- Fixed the planet/body overlay dropdown appearing near the upper-left of the planner window instead of directly below the current-body button.
- The dropdown anchor is now stored in planner-window-local coordinates and converted using the planner window's current position when the overlay is drawn, so it remains attached to the body selector even after the planner is moved.

## 0.5.21
- Changed the planet/body selector dropdown to a true overlay drawn on top of the following planner data instead of participating in the GUILayout flow.
- Opening the body list no longer pushes the altitude controls, engine filters, candidate list, or other content downward.
- The overlay still opens directly below the current-body button, keeps every body-choice button the same size as that button, and remains scrollable when the body list is long.

## 0.5.20
- Changed the planet/body selector so the body choices open as a vertically aligned list directly below the current body button.
- Body-selection buttons now use the same width and height as the current body button for a consistent dropdown appearance.

## 0.5.19
- Added a persistent **Appearance** settings page for editor UI options.
- Added **Use solid backgrounds for all editor windows** to make the VAB/SPH planner and its Settings window fully opaque instead of translucent.
- The solid-background option is disabled by default and does not change the Flight telemetry or Flight Plot Settings windows.

## 0.5.18
- Removed the Alt+P keyboard shortcut for opening/toggling VesselPlanner in both the editor and flight scenes; window access now uses the ToolbarController button.
- Reworked the Analyze Existing left column so **Simulation environment** is a separate pane at the top.
- Moved the full Analyze Existing simulation-control block into that top pane, including Minimum TWR, TWR gravity, Max engines, engine-class/resource filters, **Simulate all engines**, and simulation status.
- Kept the selected-stage diagnostics, current engines, and stage resources together in a separate **Existing stage** pane below the simulation controls.

## 0.5.17
- Added a separate persistent Planning setting to close the planner after KSP successfully selects a part for editor placement.
- The Planning close-after-Add option applies to candidate-row **Add**, selected-result **Add Engine**, and **Add Tank**.
- Analyze Existing and Planning retain independent close-after-Add preferences.

## 0.5.16
- Made the VesselPlanner Settings window draggable from any unused/background area of the window instead of only the title strip.
- Added a persistent Analyze Existing option to close the planner after an engine is successfully selected for editor placement.
- The Analyze Existing close-after-Add option applies to both candidate-row **Add** and selected-result **Add Engine** and does not close the planner when part selection fails.

## 0.5.15
- Simplified the Planning-mode top row by removing **Pick Stage**, **Craft max**, and **Craft wet** while leaving those Analyze Existing aids available where applicable.
- Added the VesselPlanner **Settings** window.
- Added per-column visibility controls for the candidate engine list, including Show All / Hide All controls.
- Added per-line/section visibility controls for informational content in the Analyze Existing left pane, including craft/stage mass diagnostics, burn information, tank data, current engines, and stage resources.
- Planner UI settings are persisted in `GameData/VesselPlanner/PluginData/VesselPlannerSettings.cfg`.

## 0.5.14
- Analyze Existing stage selection is capped at the vessel's current maximum stage; typed values and the `+` control cannot advance past `EditorStageScanner.MaxStage`.
- If staging changes and the vessel maximum stage decreases, Analyze Existing automatically clamps the selected stage back into range.
- Planning mode may still use manually entered future stages beyond the current vessel maximum.
- Widened the candidate columns **Stage Wet t**, **Start Mass t**, **ASL kN/eng**, and **Vac kN/eng** for improved readability.
- Widened the Analyze Existing current-engine **ASL kN** and **Vac kN** fields to match the expanded thrust display.

## 0.5.13
- Rebased on 0.5.12; the discarded 0.5.13 installed-engine-count change is not included.
- Corrected Analyze Existing burn time to use only the propellant mass actually usable by each candidate engine's own resource mixture.
- `DeltaVStageInfo.fuelMass` is no longer substituted into the candidate burn-time calculation, because it describes the stock stage/current propulsion fuel mass and can include resources a replacement candidate does not consume.
- Burn time remains altitude-independent and uses `t = mPropellant * IspVac * g0 / FVac`, with total vacuum thrust for the candidate engine count.
- KSP stock start/stage/end masses remain unchanged as the source for delta-v and TWR mass boundaries.

## 0.5.12
- Fixed the Analyze Existing row for the engine configuration already installed on the selected stage.
- The exact current engine part type/count now uses KSP's stock `stageMass`, `startMass`, and `endMass` unchanged instead of subtracting the KSP engine mass and then adding the candidate prefab mass back.
- The exact current-engine row also reports KSP's installed engine mass, preserving variant/module mass modifiers that may differ from `partPrefab.mass`.
- Replacement-engine rows continue to use `stock mass - installed engine mass + candidate engine mass`.

## 0.5.11
- Fixed Analyze Existing candidate-row mass accounting when the current stage engine was effectively being counted twice.
- Installed-engine mass is now taken first from `DeltaVStageInfo.enginesInStage`, the exact KSP stage model that supplied `stageMass`, `startMass`, and `endMass`.
- Each physical engine part is counted once even when it contains multiple `ModuleEngines` modules.
- Candidate mass boundaries remain `stock mass - installed engine mass + candidate engine mass`, but the subtraction now uses the authoritative engine set for that stock stage.
- The previous scanner/`VesselDeltaV.PartInfo` path is retained only as a fallback if KSP has not populated `enginesInStage` yet.

## 0.5.9
- Corrected Analyze Existing mass accounting to use KSP `DeltaVStageInfo.stageMass`, `dryMass`, and `fuelMass` directly instead of rebuilding the selected-stage mass from scanned tank parts/resources.
- **Stage Wet t** now comes from KSP's selected-stage mass plus only the candidate-vs-installed engine mass difference, preventing tank structure/resources from being counted twice.
- **Start Mass t** remains the full KSP vehicle start mass plus only the engine-mass difference.
- Retained payload mass is now derived as `startMass - stageMass`, so a part can belong to the stage or payload boundary, never both.
- Burnable stage fuel now uses KSP's explicit `fuelMass`; it is no longer inferred from `startMass - endMass`, which can include staging/jettisoned mass.
- Analyze Existing diagnostics now show KSP stage wet, dry, fuel, vehicle start, and vehicle end masses separately.

## 0.5.8
- Replaced the ambiguous Candidate **Wet t** column with two independently sortable mass columns:
  - **Stage Wet t** = selected-stage hardware + candidate engines + tank structure/resources/propellant, excluding payload and upper stages.
  - **Start Mass t** = total vehicle mass at the beginning of the stage burn, including payload/upper stages.
- Delta-v and initial/max TWR continue to use **Start Mass t** as the rocket-equation/TWR start mass.
- Selected-candidate details now show Stage wet mass, Start mass, and End mass separately.

## 0.5.7
- Corrected Analyze Existing stage mass accounting by using KSP stock `DeltaVStageInfo.startMass` and `endMass` when available, with the previous scanner retained as a fallback.
- Candidate engine replacement now adjusts KSP's stock stage start/end masses only for the engine dry-mass difference instead of rebuilding the whole retained vessel from `inverseStage` heuristics.
- Burnable propellant mass for Analyze Existing now follows the stock stage start/end mass difference when available; the displayed resource mixture is scaled to the same total mass.
- Conventional rocket vacuum thrust now uses the configured `ModuleEngines.maxThrust` directly; atmospheric thrust uses the Isp ratio. Prefab runtime flow calculations are reserved for atmosphere/velocity-flow engines and sanity-bounded.
- Burn time continues to use `t = mPropellant * IspVac * g0 / Fvac`, but now shares the corrected stage propellant mass and corrected vacuum thrust inputs.
- Analyze Existing now displays KSP stock stage start mass, end mass, burned mass, and current-engine burn time when the stock simulator data is available, making comparison/debugging straightforward.

## 0.5.6
- Fixed Planning altitude behavior: target Δv is now the vacuum design target. Moving the altitude slider no longer changes the solved propellant load, wet/dry mass, vacuum Δv, tank requirements, or burn time.
- Atmospheric Δv is recalculated from the selected body/altitude using the same fixed stage mass ratio.
- Atmospheric thrust and atmospheric TWR continue to respond to altitude.
- Updated the Planning UI text to make the vacuum-target behavior explicit.

## 0.5.5
- Removed build instructions/build configuration information from `README.md`.
- Removed use of `ModuleEngines.getMaxFuelFlow`.
- Burn mass flow is calculated from total vacuum thrust and vacuum Isp using `mdot = F / (Isp * g0)`.
- Burn time uses `t = mPropellant * Isp * g0 / F`, making burn rate independent of selected planet or altitude.

## 0.5.4

- Burn time is calculated from KSP's configured per-propellant maximum flow (`getMaxFuelFlow`) and resource densities, rather than indirectly from thrust/Isp.
- Added elapsed-time labels to the bottom of the scrolling flight graph.
- Editor planner window now starts closed.


## 0.5.3

- Replaced prefab `MaxThrustOutputVac()` / `MaxThrustOutputAtm()` calls with the editor-safe `ModuleEngines.GetEngineThrust(isp, throttle)` calculation.
- Isp is read directly from `atmosphereCurve` without manually multiplying `multIsp`.
- Candidate thrust columns are explicitly per-engine (`ASL kN/eng`, `Vac kN/eng`) while TWR/Max TWR continue to use total thrust for all engines in the candidate configuration.
- Existing installed-engine thrust uses the part's current thrust limiter.
- Static velocity-curve engines are evaluated at Mach 0.


## 0.5.2

- Corrected editor TWR thrust evaluation for ordinary rocket engines. Static atmospheric thrust is now derived from configured vacuum thrust and the engine atmosphere/Isp curve instead of calling the runtime `MaxThrustOutputAtm()` evaluator on a part prefab.
- Air-breathing engines continue to use KSP's atmosphere/density-aware thrust evaluator, with the static Isp-ratio calculation as a fallback.
- Added explicit ASL and vacuum thrust values to every candidate configuration; both columns are sortable.
- Analyze Existing now shows the engines currently assigned to the selected stage in an **Engine / ASL kN / Vac kN** table so candidate TWR inputs can be compared directly with the installed engines.
- Selected candidate details now show ASL and vacuum thrust together.

## 0.5.0

- Reworked engine-resource handling to support arbitrary KSP/mod resources instead of relying on stock propellant names.
- Engine candidates now retain every `ModuleEngines` propellant/resource with a positive ratio, including auxiliary/`ignoreForIsp` resources.
- Mass-bearing non-`ignoreForIsp` resources continue to define the rocket-equation mass flow; auxiliary resources are reported separately in the requirements rather than changing the Isp mass calculation.
- Removed the hard-coded stock resource-volume table. Physical volume now comes directly from each `PartResourceDefinition.volume`.
- Existing-stage resource scans and storage-part scans now include zero-density resources such as ElectricCharge when present.
- Tank/storage suggestions now use resource-definition volume for provided capacity and volume-based excess calculations when all required resource volumes are defined.

## 0.4.13

- Changed the default Flight telemetry sample interval from 1.0 second back to **0.25 seconds**.
- The existing `-` / `+` buttons still adjust the interval by 0.25 seconds and the numeric field remains directly editable from 0.05 to 60 seconds.

## 0.4.12
- Added `-` and `+` buttons beside the main Flight plotter's **Sample** interval field.
- Each button changes the delay by 0.25 seconds, clamps to the existing 0.05-60 second range, and applies immediately.
- The Sample text field remains directly editable for exact interval values.

## 0.4.11
- Moved the editable flight sample interval from Flight Plot Settings to the main plotter toolbar. It is shown as `Sample [seconds] s` and remains clamped to 0.05-60 seconds.
- Fixed flight stage-marker detection by subscribing directly to `GameEvents.onStageActivate`, which supplies the activated stage number.
- Retained `vessel.currentStage` transition detection only as a fallback for unusual staging implementations that bypass the stock stage event.
- Added duplicate suppression so the event and fallback paths cannot draw the same stage marker twice.
- Preserved the first launch-stage marker when Start at Launch is armed even when the staging event fires before the vessel leaves PRELAUNCH.

## 0.4.10
- Added flight graph staging markers. Each detected KSP stage activation is recorded at its position in the telemetry timeline and drawn as an orange dashed vertical marker labeled `Stage N`.
- Stage markers use vessel `currentStage` transitions, so staging performed by other mods is captured as well as normal space-bar staging.
- Markers participate in the fixed-spacing scrolling window: they move left with the telemetry and disappear only after leaving the visible graph area.
- Reset and Start at Launch clear old stage markers together with the current plot samples.

## 0.4.9
- Reserved the bottom strip of the Flight plotter and Settings windows for the resize grip, keeping status/footer text above the lower-right handle.
- Expanded the plotted-sensor legend into a table with **Sensor**, **Current**, **Max**, and **Units** columns. Every selected/plotted sensor remains in the list; the list scrolls only when the current window height cannot display all rows simultaneously.
- Reworked Flight Plot Settings into an all-sensor table with **Plot**, **Sensor**, **Max**, and **Units** columns across Flight data, Ship resources, and Sensor outputs.
- Added **Clear Max Values for Vessel**. It removes all persisted non-altitude maxima for the active vessel/craft, updates the graph immediately, and writes the change to `FlightSensorMaxima.tsv` immediately. Plot samples are not cleared.
- Altitude rows display an em dash in the Max column because altitude intentionally uses the configurable/body-derived chart ceiling instead of a stored vessel maximum.

## 0.4.8
- Changed the Flight telemetry default delay between samples from 0.25 seconds to **1.0 second**.
- **Start at Launch** is now disabled once the active vessel has left `PRELAUNCH` (and while plotting is already active). It becomes available again after a revert/reload that returns the vessel to PRELAUNCH.
- Added persistent per-vessel maximum tracking for every recorded telemetry source except ASL/surface altitude. Maxima are updated from all available sources while plotting, even if a source is not currently selected for display.
- Per-vessel maxima are keyed by save name plus vessel/craft name and stored in `GameData/VesselPlanner/PluginData/FlightSensorMaxima.tsv`, so they survive reverts, scene reloads, and repeated launches.
- Resource and `ModuleEnviroSensor` maxima are persisted along with built-in flight sensors. Environment-sensor keys no longer depend on transient `flightID`, allowing their maxima to survive reverts.
- Non-altitude graph series now use the vessel's recorded historical maximum as the top of their independent Y scale. The lower bound continues to follow the visible data. Altitude retains its separate configurable/body-default chart top.
- Resetting plot samples or starting a new launch does not clear the vessel's historical maxima.

## 0.4.7
- Renamed the editor **Pick Part** button to **Pick Stage**. The one-shot craft-part picker behavior is unchanged.
- Added passive stage selection from the stock editor staging list: clicking the orange header/tab for a stage sets the planner Stage field to that `StageGroup.inverseStageIndex` and immediately recalculates.
- Stock staging part icons are explicitly ignored, so clicking an engine/decoupler icon in the staging list does not change the planner stage through this new path.
- Orange-tab detection uses the stock `StageGroup` drag/header UI handle, with a conservative prefab-name fallback for compatible KSP UI variants.

## 0.4.6
- Fixed **Pick Part** so the stock editor no longer grabs/detaches the clicked part and its attached branch.
- The picker now keeps its KSP input lock active through the entire mouse-down frame and releases it only after the left mouse button is released.
- While Pick Part is armed, the stock `EditorLogic` behaviour is temporarily disabled as an additional guard; its previous enabled state is restored on pick, cancel, window close, or addon unload.
- This addresses the KSP update-order race where VesselPlanner handled the click before stock `EditorLogic`, then released the lock soon enough for stock EditorLogic to process the same click.

## 0.4.5
- Fixed KSP 1.12 compile errors in the **Pick Part** stage selector.
- Replaced unavailable `EditorLogic.softLock` / `EditorLogic.SetSoftLock()` calls with a private `InputLockManager` control lock while pick mode is armed.
- Removed the unavailable `EditorLogic.fetch.mouseOverGUI` check; the planner still ignores clicks inside its own window.
- Replaced the incompatible `EditorLogic.GetComponentUpwards<T>()` call with `FlightGlobals.GetPartUpwardsCached()`.
- Pick mode still cancels with Escape/right-click, **Cancel Pick**, closing the planner, or unloading the editor, and always removes its input lock when cancelled.

## 0.4.4
- Added **Pick Part** beside the editable Stage field in the VAB/SPH. Arm it, then click a part on the current craft to select that part's stage and immediately recalculate.
- Engine and decoupler clicks use their KSP staging-display `inverseStage` directly. Tanks and structural parts are resolved through the same engine-to-decoupler branch logic used by the stage analyzer, then fall back to their own `inverseStage`.
- Pick mode applies a temporary KSP input lock while armed so the selection click does not intentionally become a normal part-placement operation.
- Escape, right-click, **Cancel Pick**, closing the planner, or unloading the editor cancels pick mode cleanly.

## 0.4.3
- Reworked pause detection to use KSP `GameEvents.onGamePause` / `onGameUnpause`, with `Planetarium.Pause` and `Time.timeScale == 0` as safeguards. No telemetry samples are recorded while paused, and the sample-delay clock restarts on resume.
- Changed the flight graph X axis to fixed sample spacing. New plots fill from left to right; after the visible area fills, older samples scroll left as new samples arrive.
- Added a configurable **Altitude chart top** in Flight Plot Settings.
- Atmospheric bodies default the altitude chart top to `CelestialBody.atmosphereDepth`.
- Airless bodies default the altitude chart top to 10% of body diameter (`0.2 * Radius`).
- Altitude ASL and surface-altitude series use the fixed 0-to-chart-top scale; other sensor series continue to autoscale independently within the visible scrolling window.

## 0.4.2
- Added an explicit **Delay between samples** setting to the Flight Plot Settings window.
- The delay is entered directly in seconds and supports values from 0.05 to 60 seconds.
- Flight telemetry sampling uses that delay as the minimum real-time spacing between recorded samples.
- Pause handling and Start at Launch honor the configured sample delay.

## 0.4.1
- Prevented the Flight graph window from automatically growing to satisfy GUILayout content. Its height/width stay at the user-selected size until the resize grip is dragged.
- Constrained manually resized Flight and Settings windows to the current screen bounds.
- Suspended telemetry sampling while KSP is paused; recording remains armed and resumes after unpausing without adding pause-time samples.
- Added a **Start at Launch** button. When armed on a PRELAUNCH vessel, it clears old samples and starts plotting when the vessel leaves PRELAUNCH, then disarms itself.


## 0.4.0
- Shortened the Candidates cost-efficiency column heading to **Cost Eff.**
- **Refresh** now reloads engine/tank/stage data and immediately runs the current Planning calculation or Analyze Existing simulation.
- Added a Flight telemetry/plotting mode using the same ToolbarController icon/button used by the editor planner.
- Added flight sources for surface/orbital velocity, vertical speed, acceleration, G-force, ASL/surface altitude, dynamic pressure, vehicle mass, and angle of attack.
- Added dynamic vessel-resource sources and stock `ModuleEnviroSensor` output sources.
- Added a ClickThroughBlocker-protected, resizable flight graph window with Start/Stop, Reset, Settings, CSV export, and PNG export.
- Added a sensor Settings window with grouped toggles for Flight data, Ship resources, and Sensor outputs, plus All/None group controls and an adjustable sample interval.
- CSV exports selected plotted series with elapsed time and universal time; PNG exports the current graph. Exports are written to `Screenshots/VesselPlanner`.

## 0.3.10
- Added sortable **Max TWR** to Candidates. Max TWR uses the engine's vacuum thrust with the candidate wet mass and the selected body's surface gravity.
- Changed Stage from a fixed label to an editable numeric text field. The `+` increment button is immediately adjacent to the field; `-` remains available after it.
- Manually entered non-negative stage numbers are accepted, including planning stages above the craft's current maximum stage.
- Added a ToolbarController button in the VAB/SPH. ToolbarController registration is performed at Main Menu and Alt+P remains available.
- Added ClickThroughBlocker support by routing the planner through `ClickThruBlocker.GUILayoutWindow`.
- Added ToolbarController and ClickThroughBlocker as explicit KSP assembly/build dependencies.
- Added 38 px and 24 px toolbar icons and deployment support for those assets.

## 0.3.9
- Corrected TWR gravity to use the selected celestial body's surface gravity automatically, matching the convention used by MechJeb stage Start TWR.
- Removed the obsolete manual Gravity input; the selected planet now drives TWR gravity.
- Existing-stage mass accounting now keeps loaded mass-bearing resources that the candidate engine does not consume aboard for both initial and final mass.
- Atmospheric thrust now uses KSP's `ModuleEngines.MaxThrustOutputAtm` / `MaxThrustOutputVac` APIs when available instead of assuming thrust scales only with Isp.
- Atmospheric thrust evaluation now supplies KSP pressure, temperature, and density for the selected body/altitude.
- Engine Isp evaluation now honors `ModuleEngines.multIsp`.
- Planning tank dry/fuel ratio inference now uses full resource capacity rather than current fill level, avoiding artificially heavy tank estimates on partially filled stages.

## 0.3.8
- Engine discovery now honors `KSP.UI.Screens.EditorPartList.Instance.ExcludeFilters` before scanning for engine modules.
- Third-party mods that hide/filter parts through the editor exclusion-filter pipeline can now remove those engines from VesselPlanner candidates.
- The currently selected stock editor category is intentionally not applied, so planner candidates are not restricted by whichever part category is open.
- Editor exclusion filters are re-evaluated whenever Planning or Analyze Existing recalculates, allowing dynamic filter changes to be picked up.

## 0.3.7
- Limited the Candidates engine-list viewport to a maximum height of 300 pixels.
- The engine list remains scrollable when there are more candidates than fit in the capped viewport.
- Resizing the main planner window no longer allows the Candidates list to consume most of the window.

## 0.3.6
- The planner window can now be dragged from anywhere in its background/content area, while controls continue to receive their own clicks.
- Candidate results recalculate immediately when engine-class filters, Ignore monopropellant, bulkhead-size matching, engine-name text, selected stage, planet, or altitude change.
- Replaced the cycling Optimize button with a vertical set of mutually exclusive toggles; exactly one optimization mode remains active.
- Added sortable **Δv/Fund** Cost Efficiency to Candidates, defined as atmospheric Δv at the selected body/altitude divided by total engine cost in Funds.
- Added Cost Efficiency to the selected-candidate detail panel.

## 0.3.5
- Removed the Vacuum / Sea Level selector buttons.
- Added a planet/body dropdown populated from KSP's loaded celestial bodies; Kerbin is selected by default when available.
- Added an altitude slider from 0 m through the selected body's `atmosphereDepth`. Airless bodies use 0 m and 0 atm.
- Atmospheric pressure is read from KSP for the selected body/altitude and converted to atmospheres before evaluating engines.
- Engine ISP now evaluates the actual KSP atmosphere curve at the selected pressure rather than linearly interpolating only between vacuum and 1 atm.
- Candidate results now show separate **Atm Δv** and **Vac Δv** sortable columns for the same stage configuration.
- Planning target Δv is solved at the selected atmospheric condition; the corresponding vacuum Δv is shown alongside it.
- Added a bottom-right resize grip and dynamic Candidate table height so the editor window can be resized.

## 0.3.4
- Added a live engine-name text filter to the Candidates section.
- Corrected existing-stage delta-v calculations to use current resource amounts instead of tank capacities.
- Corrected stage payload accounting so stage parts/resources are not counted a second time through the inverse-stage payload heuristic.
- These corrections bring sea-level/vacuum stage mass ratios much closer to KSP/MechJeb for conventional staged rockets.

## 0.3.3
- Changed candidate bulkhead-size matching to use the engine's **top stack attach node size** (`top`) instead of the part-wide `bulkheadProfiles` list.
- The selected stage's matching size is taken from the top node of the engine(s) currently assigned to that stage.
- Candidate engines without a top stack node are excluded while the bulkhead-size filter is enabled.

## 0.3.2
- Added clickable sorting to all Candidate engine columns except the Add action column.
- Clicking the active Candidate column reverses ascending/descending order and shows a direction indicator.
- Added the same clickable sorting behavior to the Planning tank suggestion table.
- The selected sort is reapplied after recalculation/simulation.

## 0.3.1
- Moved Δv basis selector directly into the Candidate engine filters area.
- Added explicit Vacuum / Sea Level selector buttons.
- Moved bulkhead-size filtering into the Candidate engine filters area and displays detected stage profiles.

## 0.1.0
- Initial Visual Studio project.
- Existing-stage engine simulation using selected-stage resource capacities.
- Planning mode for target delta-v and minimum TWR.
- Iterative propellant/tank dry-mass solver.
- Runtime engine/propellant discovery.
- Vacuum/custom-pressure and custom-gravity calculations.
- Multiple optimization/ranking modes.

## 0.2.0
- Planning mode now lists available tank types and the number of copies needed to provide the selected engine solution's propellants.
- Added **Add Tank** buttons that place the selected tank on the KSP editor cursor.
- Added **Add** engine buttons to every candidate row and an **Add Engine** button in selected-result details.
- Analyze Existing now has an **Ignore monopropellant** toggle; when enabled, monopropellant-powered engine candidates are omitted.
- Added tank database scanning and editor part spawning helpers.

## 0.3.0
- Rebased on the user-provided 0.2.0 source tree.
- Added optional candidate-engine filtering by the selected stage's detected KSP bulkhead profile(s).
- Added sea-level/vacuum Δv toggle; off uses vacuum Isp/thrust, on uses sea-level Isp/thrust.
