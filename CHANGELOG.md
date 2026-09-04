## 0.4.13

- Changed the default Flight telemetry sample interval from 1.0 second back to **0.25 seconds**.
- The existing `-` / `+` buttons still adjust the interval by 0.25 seconds and the numeric field remains directly editable from 0.05 to 60 seconds.

# Changelog

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
- Per-vessel maxima are keyed by save name plus vessel/craft name and stored in `GameData/EngineStagePlanner/PluginData/FlightSensorMaxima.tsv`, so they survive reverts, scene reloads, and repeated launches.
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
- This addresses the KSP update-order race where Engine Stage Planner handled the click before stock `EditorLogic`, then released the lock soon enough for stock EditorLogic to process the same click.

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
- CSV exports selected plotted series with elapsed time and universal time; PNG exports the current graph. Exports are written to `Screenshots/EngineStagePlanner`.

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
- Third-party mods that hide/filter parts through the editor exclusion-filter pipeline can now remove those engines from Engine Stage Planner candidates.
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
