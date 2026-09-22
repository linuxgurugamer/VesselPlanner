# VesselPlanner for KSP1

VesselPlanner is a planning, staging, mission-design, delta-v reference, part-selection, and flight-telemetry mod for **Kerbal Space Program 1.12.x**.

It can analyze an existing stage, size a new stage, compare engine choices, find compatible tank combinations, build and save complete multi-stage vessel plans, create mission plans, browse planet-pack delta-v data, place selected parts into the KSP editor, and record/export flight telemetry.

For detailed change history, see [`CHANGELOG.md`](CHANGELOG.md).

## Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Editor modes](#editor-modes)
  - [Analyze Existing](#analyze-existing)
  - [Planning](#planning)
  - [Mission Planner](#mission-planner)
  - [Stage-By-Stage](#stage-by-stage)
  - [Delta-V Table](#delta-v-table)
- [Engine and tank planning](#engine-and-tank-planning)
- [Part images and editor placement](#part-images-and-editor-placement)
- [Delta-v tables and planet packs](#delta-v-tables-and-planet-packs)
- [Settings](#settings)
- [Flight Data](#flight-data)
- [Saved data and exports](#saved-data-and-exports)
- [Documentation](#documentation)
- [Calculation notes](#calculation-notes)
- [Known limits](#known-limits)
- [Building from source](#building-from-source)
- [License](#license)

## Requirements

VesselPlanner requires:

- **Kerbal Space Program 1.12.x**
- **ToolbarController**
- **ClickThroughBlocker**

The dependency folders are normally:

```text
GameData/001_ToolbarControl/
GameData/000_ClickThroughBlocker/
```

## Installation

1. Remove any older `GameData/EngineStagePlanner` installation so KSP does not load both assemblies.
2. Copy the VesselPlanner folder into:

   ```text
   KSP_ROOT/GameData/VesselPlanner/
   ```

3. Verify that the plugin DLL is located at:

   ```text
   KSP_ROOT/GameData/VesselPlanner/Plugins/VesselPlanner.dll
   ```

4. Verify that the following are present:

   ```text
   GameData/VesselPlanner/VesselPlanner.version
   GameData/VesselPlanner/PluginData/Textures/
   GameData/VesselPlanner/PluginData/DeltaVTables/
   ```

5. Start KSP.

VesselPlanner can migrate older EngineStagePlanner settings, Stage-By-Stage plans, and stored flight maxima when equivalent VesselPlanner files do not already exist.

## Opening VesselPlanner

VesselPlanner uses one ToolbarController button in both the editor and flight scenes.

- In the **VAB/SPH**, the button opens the main VesselPlanner editor window.
- In **flight**, the button opens the Flight Data window.

The main editor window is horizontally resizable from approximately **1150 to 1850 pixels**, and the selected width is remembered. Candidate-table headers scroll horizontally with the rows so columns stay aligned.

The editor modes are grouped approximately as:

```text
Mission Planner | Stage-By-Stage    Analyze Existing | Planning    Delta-V Table
```

## Editor modes

### Analyze Existing

Use **Analyze Existing** when a stage already exists on the current craft and you want to understand it or compare replacement engines against the real stage.

Features include:

- Select a stage by number, **Pick Stage**, or KSP's staging list.
- Read current stage mass, payload, propellant, tank capacity, resources, engine mass, burn time, and current engines.
- Use KSP `VesselDeltaV` / `DeltaVStageInfo` mass boundaries when available.
- Evaluate replacement engines using the propellant **currently loaded** in the stage.
- Remove installed engine dry mass before adding candidate engine mass.
- Preserve unrelated resources as carried mass when a candidate cannot burn them.
- Honor installed engine thrust limiters when describing the current configuration.
- Simulate the selected body and altitude.
- Set minimum TWR and maximum engine count.
- Include or exclude solid-fuel, air-breathing, and electric-propellant engines.
- Ignore monopropellant when desired.
- Simulate all engines when appropriate.
- Optionally match the stage bulkhead size.
- Filter and exclude engines by comma-separated text terms.
- Sort the candidate engine table by any displayed column.
- View a Selected Engine detail pane.
- Add a selected engine directly to the KSP editor cursor.
- Resize the candidate/detail panes and save the pane size.

### Planning

Use **Planning** to design a stage that does not exist yet or to size a stage independently from the current craft.

Planning supports:

- Target delta-v.
- **Vacuum** or **Atmosphere** target basis.
- Minimum TWR.
- Typed payload mass or payload taken from the craft above the selected stage.
- Body and atmospheric altitude selection.
- Maximum engine count.
- Configurable estimated tank dry/fuel mass ratio.
- Solid-fuel, air-breathing, and electric-propellant engine-class controls.
- Engine include/exclude text filters.
- Optional bulkhead-size matching.
- Sortable candidate engine results.
- Resizable engine, Selected Engine, and tank panes.
- Actual tank-set selection after an engine is chosen.

Optimization goals include:

- Lowest Stage Mass
- Lowest Propellant Mass
- Lowest Cost
- Shortest Burn
- Highest TWR
- Highest Isp

Planning's **Vacuum** and **Atmosphere** target modes behave differently:

- **Vacuum** sizes the stage to the requested vacuum delta-v. Body/altitude changes affect atmospheric performance displays but do not resize the already-sized stage.
- **Atmosphere** sizes the stage at the selected body and altitude, so changing the environment can change propellant, tank mass, wet mass, engine count, and the resulting stage solution.

### Mission Planner

Mission Planner builds an ordered sequence of mission steps. A step can be one of two types:

- **Engines & Tanks** — maneuver/body/delta-v/ASL-VAC information.
- **Subassemblies** — one or more saved KSP subassemblies with Count and Decoupler options.

Mission Planner supports:

- Named mission plans.
- Add, edit, insert, delete, and reorder operations.
- Human-readable maneuver names.
- ASL/VAC basis selection.
- Body history tracking across mission steps.
- Automatic delta-v suggestions from the active delta-v table.
- Transfer-source handling for interplanetary steps.
- Transfer-planner clipboard import where supported.
- Return-from-moon calculations using the active table.
- Saved KSP subassembly selection.
- Per-subassembly count and decoupler settings.
- Save/load of mission plans.
- Direct integration with Stage-By-Stage planning.

Mission steps are intentionally either propulsion steps or subassembly steps; a single step does not mix both types.

### Stage-By-Stage

Stage-By-Stage lets you design an entire vessel from the payload downward before assembling it in the KSP editor.

Capabilities include:

- Create a named vessel plan.
- Start from the current craft wet mass or a typed payload.
- Design upper stages first and automatically carry their mass into later/lower stages.
- Create **Engines & Tanks** stages using the normal delta-v/TWR solver.
- Create **Subassemblies** stages from saved KSP subassemblies.
- Link a Stage-By-Stage plan to a saved Mission Planner mission.
- Click mission steps to create or reopen matching stages.
- Carry mission-step body/environment information into stage calculations.
- Set target delta-v, ASL/VAC basis, TWR, engine limits, cargo mass, and stage decouplers.
- Restrict engine and tank choices by KSP bulkhead profile.
- Add mixed tank sets to a stage.
- Track counted subassemblies and per-copy decouplers.
- Edit completed stages and automatically update mass carried by later stages.
- Save and load work-in-progress plans.
- Finalize a plan into a practical build list.
- Use **Add** actions in the finalized build list to select planned engines/tanks for normal KSP editor placement.

### Delta-V Table

The **Delta-V Table** page reads the active planet-pack CSV and shows it as an expandable body tree.

Features include:

- Solar-orbiting bodies as top-level rows.
- Expand/collapse moon systems by clicking the parent body.
- Support for nested moon systems such as `Urlum -> Wal -> Tal` in OPM.
- Fixed columns for:
  - Body
  - dV to low orbit
  - Ejection
  - Capture
  - Plane Change
  - Total
  - Landing
  - Ascent
- Zero or unavailable values are left blank.
- Moon indentation is confined to the Body column so value columns remain aligned.
- Every displayed numeric value is clickable.
- Clicking a value copies the **numeric value only** to the system clipboard.
- A status line at the bottom confirms the copied value.

For route values:

- planets use the home-body -> planet route
- moons use the immediate parent -> moon route
- nested moons use the immediate parent -> child route

## Engine and tank planning

### Candidate engine table

Planning and Analyze Existing share a sortable candidate engine table. Available columns include:

- Engine
- Engine count
- Engine mass
- Stage Wet mass
- Start Mass
- Atmospheric delta-v
- Vacuum delta-v
- ASL thrust per engine
- Vacuum thrust per engine
- Cost Efficiency
- TWR
- Max TWR
- ASL/Vacuum Isp
- Burn time
- Fuel/propellant summary
- Bulkhead profile
- Add action

Individual columns can be enabled or disabled in Settings.

Candidates come from KSP's loaded part database after editor `ExcludeFilters` are applied. The currently selected KSP editor category is not used as a VesselPlanner candidate filter.

### Engine filtering

Engine lists support separate **Filter** and **Exclude** fields.

- Each field accepts comma-separated terms.
- Filter terms use OR matching.
- Any matching Exclude term removes the engine.
- Persistence of the four engine/tank include/exclude fields can be controlled independently in Settings.

### Tank planning

After an engine is selected, VesselPlanner finds compatible storage parts for its propellant requirements.

Tank planning supports:

- One, two, or three different tank types in one suggested set.
- Counts for each tank type.
- Total tank count.
- Total tank dry mass.
- Excess capacity.
- Supplied resource capacity.
- Shared KSP bulkhead profile.
- Tank include/exclude text filters.
- Part thumbnails for every tank type in a set.
- Direct part placement in normal Planning.
- **Add to Stage** behavior while Stage-By-Stage is capturing an Engines & Tanks stage.

Tank suggestions require storage parts with both `top` and `bottom` attach nodes. Radial or one-ended tanks are intentionally excluded from this list.

## Part images and editor placement

VesselPlanner displays KSP part thumbnails in:

- Planning engine candidates
- Analyze Existing engine candidates
- Analyze Existing Current engines
- Planning tank-set rows
- Stage-By-Stage engine/tank build-list rows

Part images use the exact loaded `AvailablePart.partUrl` when available, which helps distinguish mod parts that share the same internal name.

Hovering over a thumbnail shows a larger rotating preview generated from KSP's editor icon prefab.

Configurable part-image controls include:

- ZoomFactor for icons
- ZoomFactor for Rotating Images
- Camera Yaw Degrees
- Camera Pitch Degrees
- RotatingPreviewSize
- Degrees per frame
- Rotating Image Background

Changing these settings invalidates the relevant image cache so new thumbnails/previews use the new configuration.

VesselPlanner uses KSP's normal editor part-selection path for placement. Engine and tank **Add** actions select the corresponding part on the KSP editor cursor. Analyze Existing and Planning each have independent settings controlling whether the VesselPlanner window closes after a successful Add.

## Delta-v tables and planet packs

Mission Planner and Delta-V Table share CSV files under:

```text
GameData/VesselPlanner/PluginData/DeltaVTables/
```

The schema is:

```text
Origin,Destination,dV_to_low_orbit,ejection_dV,capture_dV,transfer_to_low_orbit_dV,total_capture_dV,dV_low_orbit_to_surface,ascent_dV,plane_change_dV,parent,isMoon,order
```

VesselPlanner uses `PlanetPackHeuristics` to detect the active system and select a matching table where possible.

Bundled starter tables include:

- **Stock**
- **OPM** — Outer Planets Mod
- **JNSQ**
- **GPP** — Galileo's Planet Pack
- **RSS** — Real Solar System

The CSV files are editable. You can replace or extend rows for your preferred delta-v map as long as the schema and body hierarchy are preserved.

The bundled OPM table includes Sarnus, Urlum, Neidon, Plock, their mapped moons, Eeloo under Sarnus, and the nested `Wal -> Tal` relationship.

## Settings

Editor settings are stored in:

```text
GameData/VesselPlanner/PluginData/VesselPlannerSettings.cfg
```

### Engine Columns

Show or hide individual columns in the candidate-engine table. **Show All** and **Hide All** provide quick presets.

### Analyze Existing

Configure which informational lines are visible in the Existing Stage pane and whether the window closes after a successful Add.

### Planning

Configure whether Planning closes after a successful engine/tank Add action.

### Filters

Four independent persistence settings control whether these fields are restored next session:

- engine Filter
- engine Exclude
- tank Filter
- tank Exclude

### Appearance

Appearance settings include:

- KSP skin or Alternate/Unity skin.
- Solid backgrounds for VesselPlanner editor windows.
- Master **Show tooltips** toggle.
- Part-image camera/zoom/render settings.
- Rotating-preview background.
- Reset image settings.
- Saved Planning/Analyze pane sizes.
- Reset pane sizes.

Tooltips include help for the Part Images numeric fields/sliders and Mission Planner controls.

## Flight Data

In flight, the toolbar button opens the Flight Data graph window.

### Window and recording controls

- Minimum Flight Data window width: **1000 pixels**.
- Horizontal resize support.
- Start Plotting / Stop Plotting.
- Start at Launch / Launch Armed.
- Reset samples and stage markers.
- Configurable **Sample Interval**.
- Sample count display.
- Settings button at the top-right next to the close button.
- Sampling pauses while KSP is paused.

**Start at Launch** can be armed while the vessel is in PRELAUNCH. When the vessel leaves PRELAUNCH, VesselPlanner clears the existing graph and begins recording automatically.

### Recorded data

VesselPlanner can record:

- Surface velocity
- Orbital velocity
- Vertical speed
- Acceleration
- G-force
- Altitude ASL
- Altitude above terrain/surface
- Dynamic pressure (q)
- Vehicle mass
- Angle of attack
- Every resource currently carried by the active vessel
- Stock `ModuleEnviroSensor` outputs found on the vessel

All available sources are recorded while plotting even if they are not currently visible, so display selections can be changed without losing already-collected samples.

### Graph behavior

- Plotted-sensor legend with color, current value, maximum value, and units.
- Scrolling time-series graph; after the visible area fills, traces, vertical time-grid lines, elapsed-time labels, and stage markers move left together as new samples arrive.
- Configurable time labels: every line, every other line, or every third line.
- Stage activations shown as dashed vertical markers.
- Configurable altitude chart top with **Body Default**.
- Persistent per-vessel non-altitude maxima across launches/reverts.
- Selectable left/right axis annotations per series.
- Separate Flight Settings window.
- Optional solid Flight Data background.

### Flight Settings

Flight Settings can control:

- Altitude chart top and Body Default.
- Solid Flight Data background.
- Time-label frequency.
- CSV export folder.
- PNG export folder.
- Clear Max Values for Vessel.
- Plot selection.
- Max display.
- Units.
- Axis side.
- All/None controls for Flight data, Ship resources, and Sensor outputs.

### Export

At the bottom of the Flight Data window:

- the CSV path is shown with **Export CSV** on the same row
- the PNG path is shown with **Export PNG** on the same row

Default locations:

```text
CSV: GameData/VesselPlanner/PluginData/CSV
PNG: KSP_ROOT/Screenshots
```

CSV export writes the selected plotted series. PNG export saves the currently visible graph, including graph-rendered time labels and axis annotations.

## Saved data and exports

Typical persistent data lives under:

```text
GameData/VesselPlanner/PluginData/
```

Important files and folders include:

- `VesselPlannerSettings.cfg` — editor and Flight Data settings
- `DeltaVTables/*.csv` — Mission Planner and Delta-V Table data
- `MissionPlans/*.cfg` — saved Mission Planner missions
- `Plans/*.cfg` — saved Stage-By-Stage plans
- `FlightSensorMaxima.tsv` — per-vessel historical graph maxima
- `CSV/` — default CSV export folder

Additional configuration files include:

- `GameData/VesselPlanner/BulkheadProfiles.cfg`
- `GameData/VesselPlanner/decouplerMasses.cfg`
- `GameData/VesselPlanner/VesselPlanner.version`

PNG exports default to:

```text
KSP_ROOT/Screenshots
```

## Documentation

The source package includes a full manual and focused tutorials under `Manual/`:

- `VesselPlanner-Manual.docx`
- `VesselPlanner-Analyze-Existing-Tutorial.docx`
- `VesselPlanner-Planning-Tutorial.docx`
- `VesselPlanner-Mission-Planner-Tutorial.docx`
- `VesselPlanner-Stage-by-Stage-Tutorial.docx`

A Markdown version of the main manual is also included as:

```text
VesselPlanner-Manual.md
```

## Calculation notes

### TWR and engine performance

- TWR uses the selected body's surface gravity.
- Vacuum thrust comes from loaded `ModuleEngines` / `ModuleEnginesFX` data.
- Atmospheric thrust follows the engine atmosphere curve and available KSP flow calculations.
- Candidate TWR uses total cluster thrust.
- ASL/Vac thrust columns show thrust per engine.

### Stage mass

Analyze Existing prefers KSP's own `VesselDeltaV` / `DeltaVStageInfo` stage mass boundaries when available.

Planning solves a new stage iteratively because required propellant affects estimated tank mass, which changes the total mass the propellant must accelerate.

Before a real tank set is selected, estimated tank dry mass is calculated from the configured tank dry/fuel mass ratio.

### Burn time

VesselPlanner derives mass flow from thrust and Isp and uses KSP standard gravity (`9.80665 m/s²`). Analyze Existing calculates burn duration from the propellant mixture the candidate can actually consume.

### Resources

VesselPlanner reads the propellant list from each loaded engine module rather than assuming LiquidFuel/Oxidizer. Mass-bearing propellants drive the rocket equation; auxiliary `ignoreForIsp` resources can still be shown as requirements. Resource volume uses KSP `PartResourceDefinition.volume` where available.

## Known limits

VesselPlanner is a planning and analysis tool, not a complete replacement for KSP's in-flight fuel-flow simulation.

The following are not fully modeled:

- Dynamic RealFuels/B9/MFT-style tank reconfiguration after prefab loading.
- Complete jet intake and velocity-curve flight-state simulation beyond the loaded engine/pressure behavior available to the solver.
- Arbitrary third-party engine-variant systems outside loaded `ModuleEngines` / `ModuleEnginesFX`.
- Full MechJeb-style crossfeed/asparagus/drop-tank fuel-flow simulation for every modded arrangement.

For unusual modded fuel-flow systems, Planning with a manually entered payload can be more reliable than depending on a complex editor branch scan.

## Building from source

The project is a C# library targeting:

- **.NET Framework 4.8**
- **C# 7.3**

The project references KSP's managed assemblies plus ToolbarController and ClickThroughBlocker.

The Visual Studio project is:

```text
VesselPlanner/VesselPlanner.csproj
```

The solution is:

```text
VesselPlanner.sln
```

The project includes build/deploy helpers for copying the DLL, icons, version file, delta-v tables, bulkhead configuration, and decoupler configuration into a KSP development install.

## License

See [`License.md`](License.md) for the project license.
