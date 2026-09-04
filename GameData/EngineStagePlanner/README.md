# Engine Stage Planner for KSP1

### Editor part-filter integration
Engine candidates are taken from the loaded KSP part database **after** the editor's `ExcludeFilters` are applied. This allows other mods that hide parts through KSP's editor filter API to hide those engines from Engine Stage Planner as well. The currently selected editor category is not applied.


## Features

- Runs in the VAB/SPH.
- Select an editor stage using an editable numeric Stage field; `+` increments the value and manually entered planning stages may exceed the craft's current max stage. **Pick Stage** lets you click a part on the current craft to select the stage associated with that part and immediately recalculate. Separately, clicking the orange header/tab for any stage in KSP's stock editor staging list selects that stage in the planner automatically. Pick mode uses a private KSP `InputLockManager` lock and temporarily disables stock `EditorLogic` so the selection click cannot detach/grab the vessel part. The guards remain active through mouse-up and are then restored.
- Scans all currently available `ModuleEngines` / `ModuleEnginesFX` parts.
- Reads engine thrust, atmosphere curve, propellant ratios and resource densities from KSP at runtime.
- Planning inputs: target delta-v, minimum TWR, payload, selected planet/body, altitude, max engine count and tank structural ratio. TWR gravity comes from the selected body automatically.
- Planet/altitude selection uses KSP's atmospheric pressure model and each engine's actual atmosphere curve.
- Candidate results show atmospheric delta-v at the selected altitude and vacuum delta-v side-by-side, plus atmospheric TWR and sortable **Max TWR** based on vacuum thrust.
- Automatically tests engine counts and ranks valid configurations.
- Sorting modes: lowest wet stage mass, lowest propellant, lowest engine cost, shortest burn, highest TWR, highest ISP.
- Shows required propellant by KSP resource units and mass; known stock resources also show estimated physical tank volume.
- Analyze Existing mode summarizes the selected stage and simulates all compatible engines against the propellant amounts currently loaded in the selected stage.
- Window is resizable from the bottom-right grip.
- ToolbarController provides the same toolbar button in VAB/SPH and Flight; Alt+P toggles the scene-appropriate window.
- ClickThroughBlocker prevents clicks on the planner window from passing through to editor parts/UI.


## Flight telemetry plotter

The same ToolbarController button is available in **Flight**. In the editor it opens Engine Stage Planner; in flight it opens the telemetry graph. Alt+P also toggles the scene-appropriate window.

The graph window keeps a fixed user-selected size instead of expanding to fit GUILayout content, while the bottom-right grip still allows manual resizing. Telemetry sampling is suspended using KSP pause/unpause events (with additional pause-state safeguards) and resumes only after unpausing and waiting the configured sample delay. **Start at Launch** can be armed while the vessel is PRELAUNCH; when the vessel leaves PRELAUNCH, existing plot data is cleared and recording starts automatically. Once the vessel is already in flight, **Start at Launch** is disabled.

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

Open **Settings** from the flight graph to select the series to plot. Flight data, resources, and sensor outputs are grouped separately and each group has All/None controls. The settings table shows every available sensor with **Plot**, **Sensor**, **Max**, and **Units** columns. The main plotted-sensor list also shows **Current** and **Max** values for each plotted series. Altitude intentionally shows no stored Max because its scale is controlled by the separate altitude-chart-top setting. **Clear Max Values for Vessel** removes the active vessel/craft's persisted non-altitude maxima without clearing the current plot samples. The **Delay between samples** setting defaults to **1.0 second**, is directly editable from 0.05 to 60 seconds, and controls the minimum real-time delay before the next telemetry sample is recorded. Data for all currently available sources is retained while plotting, so changing the visible series can redraw previously collected samples when those values were present.

The graph fills from left to right using fixed horizontal sample spacing. Once the graph reaches the right edge, each new sample advances the visible window and older data scrolls left. Stage activations are recorded as orange dashed vertical markers labeled `Stage N`; the markers scroll with the telemetry and are cleared with the current plot by Reset or Start at Launch. Altitude ASL and surface-altitude series use a configurable **Altitude chart top** with a zero baseline. By default the chart top is the launch body's `atmosphereDepth`; for an airless body it is 10% of the body's diameter. For every non-altitude telemetry source, the mod records a per-vessel historical maximum from all launches/reverts in the current save and uses that recorded maximum as the top of that source's independent Y scale. These maxima are persisted in `GameData/EngineStagePlanner/PluginData/FlightSensorMaxima.tsv`; Reset clears plot samples but deliberately preserves the maxima. The lower bound still follows the currently visible data, so values with very different units can share the graph. The graph is resizable, draggable, and protected by ClickThroughBlocker. **Export CSV** writes selected series as comma-separated text, and **Export PNG** writes the current visible graph image. Both are saved under `Screenshots/EngineStagePlanner`.

## TWR calculation

TWR uses the selected celestial body's surface gravity automatically. Atmospheric engine thrust is evaluated through KSP's engine thrust API using pressure, temperature, and density at the selected altitude. Analyze Existing also carries the mass of loaded stage resources that the candidate engine cannot burn, so those resources still reduce TWR and delta-v as they do in KSP.


## Dependencies

- **ToolbarController** (`GameData/001_ToolbarControl/Plugins/ToolbarControl.dll`)
- **ClickThroughBlocker** (`GameData/000_ClickThroughBlocker/Plugins/ClickThroughBlocker.dll`)

Both must be installed in the KSP instance referenced by `KSP_ROOT` when building and must also be installed in the game when running Engine Stage Planner.


## Important stage-analysis note

KSP assigns activation stages to engines and decouplers, but fuel tanks themselves do not have a reliable "this tank belongs to stage N" semantic. `EditorStageScanner` therefore uses a practical editor heuristic: it starts at engines activated in the selected `inverseStage`, follows their parent branches until a decoupler boundary, and treats mass-bearing resources on those parts as stage resources. For unusual radial/crossfeed/modded designs, use Planning mode with a manual payload mass when the automatic stage snapshot is not representative.

## Tank mass model

Planning needs a dry tank mass before an actual tank part has been selected. The solver therefore uses a configurable **tank dry mass / propellant mass** ratio. `0.125` is a useful stock-like starting point. Selecting a stage causes the UI to infer a ratio from resource-bearing parts found in the current stage when possible.

The solver iterates because propellant mass requires tank dry mass, and tank dry mass itself depends on propellant mass.

## Resource volume

KSP's authoritative engine mixture data is in resource *units*. Exact physical liters are not universally defined for modded resources. The project therefore always reports resource units and mass. It also includes a small stock volume table in `KspResourceVolume.cs`; unknown resources show no liters rather than inventing a conversion.

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

- RealFuels/B9/MFT-specific physical-volume APIs.
- Jet-engine velocity curves and intake-air flight-state modeling beyond static pressure-based engine ISP/thrust.
- Engine variants/configurations controlled by arbitrary third-party modules beyond loaded `ModuleEngines` modules.
- Full MechJeb-style fuel-flow simulation for complex asparagus/crossfeed/drop-tank arrangements.

## 0.2 editor placement features

Planning results now include a **Tanks needed** table for the selected engine configuration. Each row shows an available tank type, the number of copies required, total tank dry mass, excess capacity, and supplied resource capacity. **Add Tank** calls KSP's normal editor part spawning path and places one copy of that tank on the editor cursor; repeat it for the count shown.

Engine candidate rows contain **Add** buttons, and the selected result contains **Add Engine**. These similarly place the engine part on the editor cursor. For multimode engines, both candidate modes reference the same underlying editor part.

Analyze Existing includes **Ignore monopropellant**. When checked, candidates whose mass-bearing propellant includes `MonoPropellant` are excluded from the simulation.


### Bulkhead-size filtering
The Candidates area also has a live **Engine name** text filter (case-insensitive).

When **Match stage bulkhead size** is enabled, Engine Stage Planner compares the KSP stack-node size of each candidate engine's `top` attach node with the `top` attach-node size of the engine(s) currently assigned to the selected stage.


## Delta-v calculation notes (0.3.6)
Existing-stage simulation uses the current editor resource amounts, not max tank capacity. Stage parts found by the propulsion-branch scan are excluded from the payload-above-stage sum so their dry mass and fuel are not double-counted.

The planet/body selector and altitude slider determine atmospheric pressure using KSP's own atmospheric model. The selected pressure is fed into each engine's actual atmosphere curve. **Atm Δv** uses that ISP; **Vac Δv** uses the same wet/dry mass ratio with vacuum ISP. Planning solves the requested target Δv using the selected atmospheric condition and then reports the vacuum value for that same stage design. The rocket equation uses KSP's standard gravity constant, 9.80665 m/s².

KSP/MechJeb perform a more complete fuel-flow/staging simulation for complex crossfeed, asparagus, drop-tank and unusual decoupler arrangements. The planner's automatic stage scan remains a heuristic for those layouts.


## Candidate interaction (0.3.7)

The Candidates list is capped at a maximum height of 300 pixels and remains scrollable, so enlarging the main planner window does not make the engine list dominate the UI.
- Candidate filters recalculate immediately when changed.
- Optimization uses a vertical mutually exclusive toggle group.
- **Δv/Fund** is atmospheric Δv at the selected body/altitude divided by the total cost of the candidate engine count.
- The window can be dragged from any non-control area and resized with the lower-right grip.

### Flight plotting updates in 0.4.11
- The sample interval is edited directly on the main flight plotter toolbar (`Sample ... s`) instead of in Settings.
- Staging markers are driven by KSP's `GameEvents.onStageActivate` event and drawn as orange dashed `Stage N` lines on the scrolling graph. A current-stage polling fallback remains for nonstandard staging code.
