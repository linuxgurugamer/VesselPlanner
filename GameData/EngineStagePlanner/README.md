# Engine Stage Planner for KSP1

Engine Stage Planner is a Kerbal Space Program 1.x mod intended for KSP 1.12.x.

### Editor part-filter integration
Engine candidates are taken from the loaded KSP part database **after** the editor's `ExcludeFilters` are applied. This allows other mods that hide parts through KSP's editor filter API to hide those engines from Engine Stage Planner as well. The currently selected editor category is not applied.


## Features

- Runs in the VAB/SPH. The planner window starts closed and is opened with the ToolbarController button.
- Select an editor stage using an editable numeric Stage field. In **Analyze Existing**, the stage is clamped to the vessel's current maximum stage, including when staging changes; in **Planning**, manually entered future stages may exceed the current craft maximum. Analyze Existing provides **Pick Stage** to click a part on the craft and immediately select/recalculate that part's stage. Separately, clicking the orange header/tab for any stage in KSP's stock editor staging list selects that stage in the planner automatically. Pick mode uses a private KSP `InputLockManager` lock and temporarily disables stock `EditorLogic` so the selection click cannot detach/grab the vessel part. The guards remain active through mouse-up and are then restored.
- Scans all currently available `ModuleEngines` / `ModuleEnginesFX` parts.
- Reads engine thrust, atmosphere curve, propellant ratios and resource densities from KSP at runtime.
- Planning inputs: target vacuum delta-v, minimum TWR, payload, selected planet/body, altitude, max engine count and tank structural ratio. TWR gravity comes from the selected body automatically.
- Planet/altitude selection uses KSP's atmospheric pressure model and each engine's actual atmosphere curve. The body selector opens a vertical overlay list directly below the current-body button, with every body choice using the same button size. The list is rendered inside the main planner window in the button's own GUI coordinate system, so it remains correctly anchored as the planner moves. It is drawn on top of the following controls/data rather than participating in the layout, so opening it does not push the rest of the planner downward or activate controls underneath it.
- Candidate results show atmospheric delta-v at the selected altitude and vacuum delta-v side-by-side, plus atmospheric TWR and sortable **Max TWR** based on vacuum thrust. The **Stage Wet t**, **Start Mass t**, **ASL kN/eng**, and **Vac kN/eng** columns use expanded widths for easier reading.
- Automatically tests engine counts and ranks valid configurations.
- Sorting modes: lowest wet stage mass, lowest propellant, lowest engine cost, shortest burn, highest TWR, highest ISP.
- Shows required propellant by KSP resource units and mass; known stock resources also show estimated physical tank volume.
- Analyze Existing mode summarizes the selected stage and simulates all compatible engines against the propellant amounts currently loaded in the selected stage.
- Analyze Existing mass columns use KSP stock `DeltaVStageInfo` boundaries directly: **Stage Wet t** uses `stageMass`, while **Start Mass t** uses full vehicle `startMass`; retained payload is `startMass - stageMass`.
- Installed engine mass is removed using KSP `DeltaVPartInfo.dryMass` before candidate engine mass is added, preventing the current engine from being counted twice.
- Window is resizable from the bottom-right grip. The Settings window can be dragged from any unused/background area.
- ToolbarController provides the window button in both VAB/SPH and Flight.
- Editor and flight windows automatically close when KSP requests a scene load or scene switch, preventing a visible planner/telemetry window from carrying into a scene transition.
- ClickThroughBlocker prevents clicks on the planner window from passing through to editor parts/UI.

## Planner settings and UI customization

Open **Settings** from the editor planner to configure the candidate table, mode-specific UI behavior, and editor-window appearance. Settings are saved to `GameData/EngineStagePlanner/PluginData/EngineStagePlannerSettings.cfg` and are restored the next time KSP runs.

The **Engine Columns** page independently enables or disables every candidate-engine-list column, including Engine, count, Stage Wet mass, Start Mass, atmospheric/vacuum delta-v, atmospheric/vacuum thrust, cost efficiency, TWR, Max TWR, Isp, Burn, Fuel, and the row Add button. **Show All** and **Hide All** provide quick presets.

Switching to **Analyze Existing** rescans the selected stage and runs the engine simulation automatically, so the candidate list is ready without pressing **Simulate all engines** first. The button remains available for re-running the simulation after changing the craft or the simulation inputs.

In **Analyze Existing**, the left column is split into two panes. A dedicated **Simulation environment** pane is always at the top and contains Minimum TWR, TWR gravity, Max engines, the simulation filters, **Simulate all engines**, and simulation status. The separate **Existing stage** pane below it contains the selected-stage informational lines and sections. Within that pane, the **Current engines** list is drawn in a darker box so the table stands apart from the informational rows above it, and scrolls on its own with its column header fixed above the rows. The pane itself scrolls in a fixed viewport, so enabling more informational lines or selecting a stage with many engines or resources does not stretch the planner window.

The **Analyze Existing** settings page controls which informational lines and sections appear in the **Existing stage** pane. This includes craft wet/dry mass, payload above stage, current/full stage propellant, KSP stage wet/dry/fuel masses, current engine mass, vehicle start/end mass, current burn, tank capacity/known tank volume, the Current Engines section, and stage-resource lines. Simulation controls remain available regardless of those visibility settings. Analyze Existing also has an option to close the planner after a successful engine **Add** / **Add Engine** selection.

The **Planning** page has an independent option to close the planner after a successful candidate **Add**, selected **Add Engine**, or **Add Tank** selection. The planner only closes after KSP successfully puts the requested part on the editor cursor. Planning's top stage row is intentionally simpler and omits Analyze Existing-only **Pick Stage**, **Craft max**, and **Craft wet** controls.

The **Appearance** page provides a **Window skin** choice of **KSP skin** or **Alternate skin**. The alternate skin is Unity's stock GUI skin; the KSP skin is the default. The choice applies to the editor planner, its Settings window, and the Flight telemetry windows, takes effect immediately, and is saved as `UseAltSkin` in `EngineStagePlannerSettings.cfg`.

The **Appearance** page also provides **Use solid backgrounds for all editor windows**. When enabled, both the main VAB/SPH planner and the editor Settings window receive an opaque dark underlay while retaining the normal KSP window border/title styling. The option is on by default for new configurations and does not affect the Flight telemetry windows. An existing saved setting continues to override the default.

### Selected Engine and Tanks pane sizing (0.5.38)

In Planning, the **Selected Engine** and **Tanks** panes can be resized horizontally by dragging the grip between them. That grip is the planner's only pane grip: the detail-list heights are fixed, and Analyze Existing has no pane grip at all.

- In **Planning**, the engine list box is sized so its bottom lines up with the bottom of the **Requirements** box on its left. The two bottom edges are measured each frame, so a longer status line or rewrapped help text re-levels the list automatically.
- In **Planning**, the horizontal grip above **Selected Engine** and **Tanks** resizes the **Requirements** pane, and the candidate engine list follows it because the list is aligned to the bottom of that box. Both panes scroll in fixed viewports, so the drag works in either direction without stretching the window.
- In **Planning**, the vertical grip between **Selected Engine** and **Tanks** moves the split between the two panes. Widening one pane narrows the other; neither can be dragged below 220 pixels.
- In **Analyze Existing**, the horizontal grip above **Selected Engine** moves the boundary between the candidate engine list and the pane below it. Dragging down gives the list more rows and shrinks the pane; dragging up does the reverse. The bottom of the column stays level with the **Existing stage** box either way, and the list stops growing once the pane reaches its minimum height. The pane has no grip on its left or right edge.
- In **Planning** the two panes are drawn at a matching height: the **Tanks** table uses a fixed 145 pixels and the **Selected Engine** detail list is sized so both panes finish at the same point. Because the panes have different headers and the Tanks footer line can rewrap at narrow widths, the match is computed from the measured pane heights each frame rather than from a fixed offset.
- In **Analyze Existing** the **Selected Engine** detail list is sized so the bottom of its pane lines up with the bottom of the left **Existing stage** box. The two bottom edges are measured each frame, so hiding analysis lines on the settings page, a stage with no KSP stock stage masses, or a longer Current Engines list all re-level the pane automatically. Both lists scroll when their content is longer.
- The Planning pane width is stored as a fraction of the width the row has available, and the Analyze Existing list height is stored as an offset from its automatic height, so both keep up as the window content changes size.
- Both are saved to `EngineStagePlannerSettings.cfg` and restored on the next run. The **Appearance** page provides **Reset pane sizes** to return them to the 0.5.32 defaults.


## Flight telemetry plotter

The same ToolbarController button is available in **Flight**. In the editor it opens Engine Stage Planner; in flight it opens the telemetry graph.

The graph window keeps a fixed user-selected size instead of expanding to fit GUILayout content, while the bottom-right grip still allows manual resizing. The layout reserves space for both export-path footer rows so the lower-right resize grip stays at the true bottom edge of the window. Telemetry sampling is suspended using KSP pause/unpause events (with additional pause-state safeguards) and resumes only after unpausing and waiting the configured sample delay. **Start at Launch** can be armed while the vessel is PRELAUNCH; when the vessel leaves PRELAUNCH, existing plot data is cleared and recording starts automatically. Once the vessel is already in flight, **Start at Launch** is disabled.

The flight graph can record and plot:

- Surface velocity and orbital velocity
- Vertical speed
- Acceleration
- G-force
- Altitude ASL and altitude above the surface
- Dynamic pressure (q)
- Vehicle mass
- Angle of attack
- Every resource currently carried by the active vessel
- Outputs from stock `ModuleEnviroSensor` modules on the active vessel

Open **Settings** from the flight graph to select the series to plot and configure the CSV and PNG export folders. The **CSV export folder** setting defaults to `EngineStagePlanner/PluginData/CSV`; relative CSV paths are resolved under `GameData`, so the default on disk is `GameData/EngineStagePlanner/PluginData/CSV`. The **PNG export folder** setting defaults to `Screenshots`; relative PNG paths are resolved under the KSP root, so the default on disk is `KSP_ROOT/Screenshots`. Absolute paths are accepted for either export type. Both values are saved with the other Engine Stage Planner settings in `GameData/EngineStagePlanner/PluginData/EngineStagePlannerSettings.cfg`. Flight data, resources, and sensor outputs are grouped separately and each group has All/None controls. The settings table shows every available sensor with **Plot**, **Sensor**, **Max**, and **Units** columns. The main plotted-sensor list also shows **Current** and **Max** values for each plotted series. Altitude intentionally shows no stored Max because its scale is controlled by the separate altitude-chart-top setting. **Clear Max Values for Vessel** removes the active vessel/craft's persisted non-altitude maxima without clearing the current plot samples. The **Sample** interval defaults to **0.25 seconds** and is shown at the top of the flight plotter with `-` / `+` buttons that adjust it by 0.25 seconds. The value remains directly editable from 0.05 to 60 seconds and controls the minimum real-time delay before the next telemetry sample is recorded. Data for all currently available sources is retained while plotting, so changing the visible series can redraw previously collected samples when those values were present.

The graph fills from left to right using fixed horizontal sample spacing. Elapsed time since plotting started is labeled along the bottom of the visible graph (`m:ss`, or `h:mm:ss` for longer runs). Once the graph reaches the right edge, each new sample advances the visible window and older data scrolls left. Stage activations are recorded as orange dashed vertical markers labeled `Stage N`; the markers scroll with the telemetry and are cleared with the current plot by Reset or Start at Launch. Altitude ASL and surface-altitude series use a configurable **Altitude chart top** with a zero baseline. By default the chart top is the launch body's `atmosphereDepth`; for an airless body it is 10% of the body's diameter. For every non-altitude telemetry source, the mod records a per-vessel historical maximum from all launches/reverts in the current save and uses that recorded maximum as the top of that source's independent Y scale. These maxima are persisted in `GameData/EngineStagePlanner/PluginData/FlightSensorMaxima.tsv`; Reset clears plot samples but deliberately preserves the maxima. The lower bound still follows the currently visible data, so values with very different units can share the graph. The graph is resizable, draggable, and protected by ClickThroughBlocker. **Export CSV** writes selected series as comma-separated text to the configured CSV export folder (default `GameData/EngineStagePlanner/PluginData/CSV`). **Export PNG** writes the current visible graph image to its independently configured PNG export folder (default `KSP_ROOT/Screenshots`).

## TWR calculation

TWR uses the selected celestial body's surface gravity automatically. For conventional rocket engines, vacuum thrust uses the configured `ModuleEngines.maxThrust` and atmospheric thrust follows the engine's atmosphere/Isp curve. Engines whose fuel flow genuinely depends on atmosphere or velocity still use KSP's engine-flow calculation with a sanity fallback. Analyze Existing also carries the mass of loaded stage resources that the candidate engine cannot burn, so those resources still reduce TWR and delta-v as they do in KSP.


## Dependencies

The mod now has hard dependencies on:

- **ToolbarController** (`GameData/001_ToolbarControl/Plugins/ToolbarControl.dll`)
- **ClickThroughBlocker** (`GameData/000_ClickThroughBlocker/Plugins/ClickThroughBlocker.dll`)

Both must be installed in the game when running Engine Stage Planner.

## Installation

Install the DLL and toolbar textures under `GameData/EngineStagePlanner`:

- `EngineStagePlanner/bin/Release/EngineStagePlanner.dll` -> `GameData/EngineStagePlanner/Plugins/EngineStagePlanner.dll`
- `EngineStagePlanner/PluginData/Textures/icon_38.png` -> `GameData/EngineStagePlanner/PluginData/Textures/icon_38.png`
- `EngineStagePlanner/PluginData/Textures/icon_24.png` -> `GameData/EngineStagePlanner/PluginData/Textures/icon_24.png`
- `EngineStagePlanner.version` -> `GameData/EngineStagePlanner/EngineStagePlanner.version`


## Important stage-analysis note

For stage start/end mass, Analyze Existing now prefers KSP's own `VesselDeltaV` / `DeltaVStageInfo` results, which account for the stock staging and fuel-flow model. The branch scanner is still used to identify the selected stage's resource inventory, tank volume, engine list, and as a fallback when the stock delta-v simulation is not ready. For especially unusual modded fuel-flow systems, Planning mode with a manual payload mass remains available.

## Tank mass model

Planning needs a dry tank mass before an actual tank part has been selected. The solver therefore uses a configurable **tank dry mass / propellant mass** ratio. `0.125` is a useful stock-like starting point. Selecting a stage causes the UI to infer a ratio from resource-bearing parts found in the current stage when possible.

The solver iterates because propellant mass requires tank dry mass, and tank dry mass itself depends on propellant mass. The requested planning target is interpreted as vacuum Δv so the solved stage geometry remains fixed while the altitude slider is moved. Burn time is calculated from the engine's vacuum thrust and vacuum Isp using `mdot = F / (Isp * g0)` and `t = mPropellant * Isp * g0 / F`, so the burn-rate calculation is independent of selected planet and altitude. In Analyze Existing, `mPropellant` is the mass of the candidate engine's actually usable propellant mixture, limited by the resources currently loaded in the selected stage; KSP's whole-stage `fuelMass` is not substituted into candidate burn time.

## Engine resources and resource volume

Engine Stage Planner reads the complete propellant/resource list from each loaded `ModuleEngines` / `ModuleEnginesFX` module instead of assuming LiquidFuel/Oxidizer or any other stock mixture. Every resource with a positive engine ratio is retained in the requirement set, including auxiliary resources such as intake or electrical resources. Resources which participate in Isp and have mass are used for rocket-equation propellant mass; `ignoreForIsp` resources remain visible as auxiliary requirements.

Physical resource volume is taken directly from KSP's `PartResourceDefinition.volume` for that resource. The planner no longer contains a hard-coded LiquidFuel/Oxidizer/MonoPropellant/Xenon volume table. Resource requirements, existing-stage capacity, and storage-part suggestions therefore work with stock and mod-defined resources using the definitions actually loaded by the current KSP installation.

## Files

- `Core/StageSolver.cs` - rocket equation, iterative tank/fuel solution, burn and TWR calculations.
- `Core/PlannerEngine.cs` - simulates all engines/counts and ranks results.
- `KSP/EngineDatabase.cs` - scans loaded engine parts and propellants.
- `KSP/EditorStageScanner.cs` - editor craft/stage snapshot.
- `UI/PlannerWindow.cs` - IMGUI editor UI.
- `Flight/FlightSensorManager.cs` - flight telemetry acquisition, resources, and sensor outputs.
- `UI/FlightGraphWindow.cs` - flight graph, sensor settings, PNG/CSV export.
- `EngineStagePlannerAddon.cs` - editor and flight addon entry points plus ToolbarController integration.

## Current scope / intentionally not yet included

- Dynamic tank-type/configuration switching performed by arbitrary RealFuels/B9/MFT-style modules after the part prefab has loaded.
- Jet-engine velocity curves and intake-air flight-state modeling beyond static pressure-based engine ISP/thrust.
- Engine variants/configurations controlled by arbitrary third-party modules beyond loaded `ModuleEngines` modules.
- Full MechJeb-style fuel-flow simulation for complex asparagus/crossfeed/drop-tank arrangements.

## 0.2 editor placement features

Planning results include a **Tanks** pane beside **Selected Engine**. The pane header shows **Tanks needed for the selected propellant requirement** on the right. Each row shows an available tank type, the number of copies required, total tank dry mass, excess capacity, and supplied resource capacity. The Tank, Excess, and Capacity columns are widened for readability, and the pane is allocated extra width so the full **Add Tank** buttons remain visible. Since 0.5.33 the pane can also be resized directly: drag the grip between **Selected Engine** and **Tanks** to change its width. **Add Tank** calls KSP's normal editor part spawning path and places one copy of that tank on the editor cursor; repeat it for the count shown.

Engine candidate rows contain **Add** buttons, and the selected result contains **Add Engine**. These similarly place the engine part on the editor cursor. For multimode engines, both candidate modes reference the same underlying editor part. Settings can optionally close the planner after a successful placement selection, with separate preferences for Analyze Existing and Planning.

Analyze Existing includes **Ignore monopropellant**. When checked, candidates whose mass-bearing propellant includes `MonoPropellant` are excluded from the simulation.


### Bulkhead-size filtering
The Candidates area also has a live **Engine name** text filter (case-insensitive).

When **Match stage bulkhead size** is enabled, Engine Stage Planner compares the KSP stack-node size of each candidate engine's `top` attach node with the `top` attach-node size of the engine(s) currently assigned to the selected stage.


## Delta-v calculation notes (0.3.6)
Existing-stage simulation uses the current editor resource amounts, not max tank capacity. Stage parts found by the propulsion-branch scan are excluded from the payload-above-stage sum so their dry mass and fuel are not double-counted.

The planet/body selector and altitude slider determine atmospheric pressure using KSP's own atmospheric model. The selected pressure is fed into each engine's actual atmosphere curve. **Planning sizes the stage from the requested vacuum Δv**, so its propellant load, wet/dry mass, **Vac Δv**, tank requirements, and burn time do not change when altitude changes. **Atm Δv**, atmospheric thrust, and atmospheric TWR are then evaluated for that fixed stage at the selected altitude. The rocket equation uses KSP's standard gravity constant, 9.80665 m/s².

KSP/MechJeb perform a more complete fuel-flow/staging simulation for complex crossfeed, asparagus, drop-tank and unusual decoupler arrangements. The planner's automatic stage scan remains a heuristic for those layouts.


## Candidate interaction (0.3.7)

The Candidates list is capped at a maximum height of 300 pixels and remains scrollable, so enlarging the main planner window does not make the engine list dominate the UI.
- Candidate filters recalculate immediately when changed.
- Optimization uses a vertical mutually exclusive toggle group.
- **Δv/Fund** is atmospheric Δv at the selected body/altitude divided by the total cost of the candidate engine count.
- The window can be dragged from any non-control area. It has no resize grip: GUILayout sizes it to its content. In Planning, the grip between **Selected Engine** and **Tanks** splits those panes, and the grip above them gives the engine list more room.

### Flight plotting updates in 0.4.13

- Changed the default flight sample interval back to **0.25 seconds**. The `-` / `+` controls still adjust by 0.25 seconds and the field remains directly editable.

### Flight plotting updates in 0.4.12
- The sample interval is edited directly on the main flight plotter toolbar (`Sample ... s`) instead of in Settings.
- `-` and `+` buttons beside the Sample field adjust the interval by 0.25 seconds per click while the text field remains available for exact values.
- Staging markers are driven by KSP's `GameEvents.onStageActivate` event and drawn as orange dashed `Stage N` lines on the scrolling graph. A current-stage polling fallback remains for nonstandard staging code.

### Generic engine-resource support in 0.5.0

- All engine propellant resources are read from the engine module.
- Resource density and physical volume are read from the loaded `PartResourceDefinition`.
- `ignoreForIsp`/auxiliary resources remain part of the displayed resource requirement but do not define rocket-equation mass flow.
- Existing-stage resource inventory and storage-part discovery include zero-density resources.
- Tank/storage capacity summaries include physical liters when the resource definition supplies a positive volume.

### Burn-time and stage-mass updates in 0.5.7

- Analyze Existing uses KSP's stock stage `startMass` / `endMass` boundaries when available, instead of relying only on an `inverseStage` mass heuristic.
- Burn time does not use `ModuleEngines.getMaxFuelFlow`; mass flow is calculated from vacuum thrust and vacuum Isp with `mdot = F / (Isp * g0)`.
- Burn time is calculated with `t = mPropellant * Isp * g0 / F`, using total vacuum thrust for the candidate engine count and the corrected stock stage propellant mass when available.
- The burn-rate calculation is independent of selected body/altitude.
- The flight graph shows elapsed time since plotting started along the bottom of the visible scrolling data.
- The editor planner window starts closed.

### TWR / thrust diagnostics

- Conventional rocket vacuum thrust uses the engine's configured `maxThrust`; atmospheric thrust follows the ratio of atmospheric to vacuum Isp. Atmosphere/velocity-flow engines use KSP's flow-aware calculation with sanity checking.
- Candidate columns **ASL kN/eng** and **Vac kN/eng** show one engine's static thrust rating. The `#` column shows how many engines are in the candidate configuration.
- TWR and Max TWR still use the **total** thrust of all candidate engines. Selected details show both per-engine and total thrust so the multiplication is visible.
- Analyze Existing shows each installed engine's individual ASL/vacuum thrust and honors its editor thrust limiter.



- Candidate mass columns distinguish **Stage Wet t** (selected stage only, excluding payload/upper stages) from **Start Mass t** (the full vehicle mass accelerated at stage ignition and used for Δv/TWR).


### Burn-time correction in 0.5.13

Analyze Existing computes burn duration from the propellant mass actually usable by each candidate engine's own resource mixture. This prevents unrelated stage resources from inflating burn duration. The stock KSP stage mass boundaries are still used for delta-v and TWR.

### Editor UI/settings updates in 0.5.14-0.5.19

- Analyze Existing cannot select a stage above the vessel's current maximum, while Planning continues to permit future-stage numbers.
- Candidate mass/thrust columns were widened for readability.
- Planning omits the Analyze Existing-only Pick Stage / craft-summary controls from its top row.
- Settings can hide/show candidate columns and Analyze Existing informational lines/sections.
- Settings are persistent and the Settings window can be dragged from any unused/background area.
- Separate Analyze Existing and Planning preferences can close the planner after successful Add operations.
- Alt+P window toggling was removed; the ToolbarController button opens the editor planner and flight telemetry window.
- Analyze Existing places its complete Simulation environment controls in a separate pane at the top of the left column, above the Existing stage diagnostics pane.
- 0.5.19 adds an Appearance setting that can make both editor windows fully opaque while leaving Flight windows unchanged.
- 0.5.26 makes the solid editor background the default for new configurations while preserving any previously saved user choice.
- 0.5.27 adds a persistent Flight Settings CSV export folder, defaulting to `GameData/EngineStagePlanner/PluginData/CSV`.
- 0.5.28 adds an independent persistent PNG export folder, defaulting to `KSP_ROOT/Screenshots`, with relative PNG paths resolved from the KSP root.
- 0.5.29 corrects the flight graph bottom layout so the resize grip and lower edge remain flush with the window bottom.
- 0.5.30 removes the extra footer spacer so the final flight-data status/export line is the bottom layout row of the window while the resize grip remains overlaid at the lower-right.
- 0.5.56 makes the Planning Requirements pane scroll in a fixed viewport, resized by the grip above the detail panes.
- 0.5.55 narrows the flight settings Sensor column and makes the Analyze Existing stage pane and its Current engines list scroll.
- 0.5.54 draws a full value scale down the side of the graph for each annotated series, with ticks on the gridlines.
- 0.5.52 bakes the graph labels into the PNG export and adds a per-sensor Axis setting that annotates a series' scale on the left or right edge of the graph.
- 0.5.51 enlarges the flight plot **Start at Launch** button and fixes the graph's elapsed-time labels overwriting each other while the graph fills.
- 0.5.50 draws the Analyze Existing **Current engines** list in a darker box.
- 0.5.49 adds Stage Resources and Fuel headings, widens the Analyze Existing value column, and cleans up the repository layout.
- 0.5.48 adds a Window skin setting (KSP skin or Unity's stock skin) that applies to every Engine Stage Planner window.
- 0.5.46 removes the planner window resize grip and its supporting code; the window is sized from its content again.
- 0.5.45 completes the resize-grip fix; the window was still snapping back to the content size.
- 0.5.44 fixes the window resize grip, which could not enlarge the window, and stretches the Planning detail panes to the bottom of the window.
- 0.5.43 adds a drag bar above the Planning **Selected Engine** and **Tanks** panes for giving the engine list more room.
- 0.5.42 lines the bottom of the Planning engine list up with the bottom of the **Requirements** box.
- 0.5.41 adds a drag bar between the Analyze Existing candidate list and the **Selected Engine** pane, trading height between them while the column bottom stays level.
- 0.5.40 runs Simulate all engines automatically when Analyze Existing is selected.
- 0.5.39 corrects that alignment, which sat about half a line low because the measured column group extended past the Existing stage box by its margin.
- 0.5.38 makes the Analyze Existing **Selected Engine** pane end level with the left Existing stage box, measured from the live layout so hidden analysis lines are accounted for.
- 0.5.37 removes the width grip from the right edge of the Analyze Existing **Selected Engine** pane; that pane fills its column again and Planning keeps the only pane grip.
- 0.5.36 makes the Planning **Selected Engine** pane match the height of the **Tanks** pane, sized from the measured pane heights so the match survives footer rewrapping.
- 0.5.35 removes the drag bar at the bottom of the **Selected Engine** pane; both detail lists use fixed heights again and only the pane widths are draggable.
- 0.5.34 removes the drag bar at the bottom of the **Tanks** pane.
- 0.5.33 makes the **Selected Engine** and **Tanks** panes resizable, stores the pane widths as fractions so they survive window resizing, and persists them.
- 0.5.32 polishes the Planning detail panes: adds spacing below the Selected Engine header, moves the Tanks-needed description onto the Tanks header row, widens the Tank/Excess/Capacity columns, and gives the Tanks pane enough width for the full Add Tank buttons.
- 0.5.31 reorganizes editor selected-engine details: Planning shows **Selected Engine** and **Tanks** as side-by-side panes, while Analyze Existing places the selected engine directly below the candidate engine list in the right-hand column.
- 0.5.25 corrects the KSP1 scene-switch callback signature to `GameEvents.FromToAction<GameScenes, GameScenes>`, preserving the automatic scene-transition window closing while compiling against KSP1 GameEvents.
