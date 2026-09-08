# Changelog

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
- The assignments are saved as `GraphAxisLeft` and `GraphAxisRight` in `EngineStagePlannerSettings.cfg`.

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
- The choice applies to every Engine Stage Planner window, in the editor and in flight, and takes effect immediately.
- The stock Unity skin is captured at the main menu before anything assigns `HighLogic.Skin`, since Unity resets `GUI.skin` to its default at the start of each OnGUI.
- Cached GUIStyles are copied from `GUI.skin`, so both windows now rebuild theirs when the setting changes.
- Stored as `UseAltSkin` in `EngineStagePlannerSettings.cfg`. The flight windows read the key directly from that file, so the skin applies without the editor planner having been opened.

## 0.5.47
- Moved each addon section into it's own file
- Renamed EngineStagePlannerAddon to ToolbarRegistration 
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
- The PNG directory is saved with the other settings in `GameData/EngineStagePlanner/PluginData/EngineStagePlannerSettings.cfg` as `PngExportDirectory`.
- PNG export creates the configured directory automatically when needed; CSV and PNG export locations remain independently configurable.
- Fixed the flight graph footer to show the CSV and PNG export paths separately instead of calling the obsolete `GetExportFolder()` helper.

## 0.5.27
- Added a persistent **CSV export folder** setting to the Flight Plot Settings window.
- The default setting is `EngineStagePlanner/PluginData/CSV`, resolved relative to `GameData`, so CSV files default to `GameData/EngineStagePlanner/PluginData/CSV`.
- Relative CSV export paths are resolved under `GameData`; absolute paths are also accepted.
- The configured directory is saved in `GameData/EngineStagePlanner/PluginData/EngineStagePlannerSettings.cfg` as `CsvExportDirectory`.
- CSV export creates the configured directory automatically when needed. PNG export remains under `Screenshots/EngineStagePlanner`.

## 0.5.26
- Changed the default editor appearance so **Use solid backgrounds for all editor windows** is enabled when no saved setting exists.
- Existing `EngineStagePlannerSettings.cfg` values are still honored, so users who previously saved the option as disabled keep their preference.
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
- Removed the Alt+P keyboard shortcut for opening/toggling Engine Stage Planner in both the editor and flight scenes; window access now uses the ToolbarController button.
- Reworked the Analyze Existing left column so **Simulation environment** is a separate pane at the top.
- Moved the full Analyze Existing simulation-control block into that top pane, including Minimum TWR, TWR gravity, Max engines, engine-class/resource filters, **Simulate all engines**, and simulation status.
- Kept the selected-stage diagnostics, current engines, and stage resources together in a separate **Existing stage** pane below the simulation controls.

## 0.5.17
- Added a separate persistent Planning setting to close the planner after KSP successfully selects a part for editor placement.
- The Planning close-after-Add option applies to candidate-row **Add**, selected-result **Add Engine**, and **Add Tank**.
- Analyze Existing and Planning retain independent close-after-Add preferences.

## 0.5.16
- Made the Engine Stage Planner Settings window draggable from any unused/background area of the window instead of only the title strip.
- Added a persistent Analyze Existing option to close the planner after an engine is successfully selected for editor placement.
- The Analyze Existing close-after-Add option applies to both candidate-row **Add** and selected-result **Add Engine** and does not close the planner when part selection fails.

## 0.5.15
- Simplified the Planning-mode top row by removing **Pick Stage**, **Craft max**, and **Craft wet** while leaving those Analyze Existing aids available where applicable.
- Added the Engine Stage Planner **Settings** window.
- Added per-column visibility controls for the candidate engine list, including Show All / Hide All controls.
- Added per-line/section visibility controls for informational content in the Analyze Existing left pane, including craft/stage mass diagnostics, burn information, tank data, current engines, and stage resources.
- Planner UI settings are persisted in `GameData/EngineStagePlanner/PluginData/EngineStagePlannerSettings.cfg`.

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
