# VesselPlanner - User Manual

For Kerbal Space Program 1.12.x  
VesselPlanner 0.7.73

VesselPlanner is a planning, staging, mission-design, delta-v reference, and flight-telemetry mod for Kerbal Space Program 1. It can analyze a stage that already exists, size a future stage, build and save a complete vessel plan, create mission-step plans, browse planet-pack delta-v tables, place selected parts on the editor cursor, and record/export flight data.

> **[IMAGE PLACEHOLDER: Main VesselPlanner window in the VAB/SPH]**

## Table of Contents

1. [Requirements and installation](#1-requirements-and-installation)
2. [Opening VesselPlanner and window behavior](#2-opening-vesselplanner-and-window-behavior)
3. [Editor mode overview](#3-editor-mode-overview)
4. [Planning mode](#4-planning-mode)
5. [Analyze Existing mode](#5-analyze-existing-mode)
6. [Candidate engine table and part images](#6-candidate-engine-table-and-part-images)
7. [Adding parts to the editor](#7-adding-parts-to-the-editor)
8. [Mission Planner](#8-mission-planner)
9. [Stage-By-Stage planning](#9-stage-by-stage-planning)
10. [Delta-V Table page](#10-delta-v-table-page)
11. [Delta-v tables and planet-pack detection](#11-delta-v-tables-and-planet-pack-detection)
12. [Settings](#12-settings)
13. [Flight Data](#13-flight-data)
14. [How calculations are performed](#14-how-calculations-are-performed)
15. [Important files created or used by VesselPlanner](#15-important-files-created-or-used-by-vesselplanner)
16. [Known limits](#16-known-limits)
17. [Troubleshooting](#17-troubleshooting)
18. [Current release notes](#18-current-release-notes)

---

## 1. Requirements and installation

### 1.1 Requirements

VesselPlanner requires:

- Kerbal Space Program 1.12.x
- ToolbarController
- ClickThroughBlocker

The dependency folders are normally:

```text
GameData/001_ToolbarControl/
GameData/000_ClickThroughBlocker/
```

### 1.2 Install VesselPlanner

1. Remove any older `GameData/EngineStagePlanner` installation so KSP does not load both assemblies.
2. Copy the VesselPlanner folder into:

```text
KSP_ROOT/GameData/VesselPlanner/
```

3. Verify that the plugin DLL is located at:

```text
KSP_ROOT/GameData/VesselPlanner/Plugins/VesselPlanner.dll
```

4. Verify that `VesselPlanner.version`, `PluginData/Textures`, and `PluginData/DeltaVTables` are present.
5. Start KSP.

VesselPlanner can migrate older EngineStagePlanner settings, Stage-By-Stage plans, and stored flight maxima when the corresponding VesselPlanner files do not already exist.

> **[IMAGE PLACEHOLDER: GameData folder showing VesselPlanner and its dependencies]**

---

## 2. Opening VesselPlanner and window behavior

One ToolbarController button is used in both the editor and flight scenes. Look for the transparent checklist-and-rising-graph icon.

- In the VAB or SPH, the button opens the main VesselPlanner editor window.
- In flight, the same button opens the Flight Data graph.

The editor planner starts closed when you enter the editor. Click the toolbar button to open it.

> **[IMAGE PLACEHOLDER: VesselPlanner toolbar button]**

### Editor window layout

The main editor window can be resized horizontally from **1150 to 1850 pixels** by dragging the grip on its right edge. The chosen width is remembered. Candidate-table headers scroll horizontally with their rows so the headings remain aligned.

The editor mode row is grouped as:

```text
Mission Planner | Stage-By-Stage    Analyze Existing | Planning    Delta-V Table
```

There is a visible gap between the Mission/Stage group and the Analyze/Planning group, and another gap between Planning and Delta-V Table.

All VesselPlanner editor windows use ClickThroughBlocker so clicks do not pass through to parts behind the window. Window skin, solid-background behavior, tooltips, and detail-pane sizes can be changed in Settings.

---

## 3. Editor mode overview

| Mode | Use it when |
|---|---|
| **Analyze Existing** | A stage already exists on the craft and you want to compare replacement engines against its actual mass and loaded propellant. |
| **Planning** | You want to size a new stage from a delta-v target, payload, TWR requirement, engine filters, and tank assumptions. |
| **Stage-By-Stage** | You want to design, save, edit, finalize, and build an entire vessel one stage at a time. |
| **Mission Planner** | You want an ordered mission plan containing propulsion steps and/or saved KSP subassemblies. |
| **Delta-V Table** | You want an expandable tree view of the currently loaded planet-pack delta-v data and one-click clipboard copying. |

---

## 4. Planning mode

Use Planning when the stage does not exist yet or when you want VesselPlanner to size a stage independently of the current craft.

### 4.1 Stage requirements

The Planning Requirements pane includes:

- **Target delta-v** - the required stage delta-v.
- **Delta-v basis** - **Vacuum** or **Atmosphere**.
- **Minimum TWR** - candidates below this initial TWR are rejected.
- **Use craft payload above selected stage** - when enabled, VesselPlanner uses the editor craft above the selected stage instead of the typed payload.
- **Payload mass** - the mass the new stage must carry when the craft-payload option is off.
- **Body** - selected with the shared ComboBox body selector.
- **Altitude** - used for atmospheric pressure, thrust, Isp, and atmospheric TWR.
- **Max engines** - largest engine cluster VesselPlanner will evaluate.
- **Tank dry/fuel mass ratio** - estimated tank dry mass divided by propellant mass until a real tank set is selected.
- **Engine class toggles** - Solid fuel, Air-breathing, and Electric-propellant.
- **Optimization goal** - Lowest Stage Mass, Lowest Propellant Mass, Lowest Cost, Shortest Burn, Highest TWR, or Highest Isp.

Future stage numbers may be higher than the current craft's highest stage.

> **[IMAGE PLACEHOLDER: Planning Requirements pane with Vacuum/Atmosphere basis controls]**

### 4.2 Vacuum versus Atmosphere target basis

Planning now supports two different sizing modes:

- **Vacuum** - the stage is sized to meet the target delta-v in vacuum. Changing body or altitude does not resize the stage; it only changes atmospheric delta-v, thrust, and TWR shown for that already-sized stage.
- **Atmosphere** - the stage is sized to meet the target at the selected body and altitude. Changing the body or altitude can therefore change propellant, tank mass, wet mass, engine count, and the resulting stage solution.

For an airless body, atmospheric delta-v equals vacuum delta-v.

### 4.3 Body and altitude

The candidate area contains the environment controls used by both Planning and Analyze Existing:

- **Planet** - shared ComboBox selector using the loaded KSP body list.
- **Pressure** - calculated from KSP's atmosphere model at the selected altitude.
- **Altitude** - slider from zero to the body's atmosphere depth.

Changing the selected body automatically clamps altitude to the valid atmosphere range and recalculates environment-dependent results.

### 4.4 Engine filtering

Above the candidate table are:

- **Filter** - comma-separated include terms. Any matching term includes the engine.
- **Exclude** - comma-separated exclusion terms. Any matching term removes the engine.
- **Match stage bulkhead size** - when applicable, restricts engines to the detected stage attach-node size.

Filter text applies immediately. Persistence of each include/exclude field is controlled independently on the Settings -> Filters page.

### 4.5 Calculate and choose an engine

Click **Calculate** to solve the stage. Candidate rows can then be sorted by clicking column headers. Click an engine name to show its details in the Selected Engine pane.

Planning uses a resizable layout:

- Drag the horizontal grip above the lower detail panes to give more or less height to the engine list.
- Drag the vertical grip between Selected Engine and Tanks to change their relative widths.
- The pane sizes are saved between sessions and can be reset from Settings -> Appearance.

### 4.6 Tanks needed

After selecting an engine, the Tanks pane finds compatible storage parts for the required propellant.

Tank suggestions can contain **one, two, or three different tank types**. Every type in a mixed set must share the same KSP bulkhead profile used by that suggestion. Storage parts must have both `top` and `bottom` attach nodes; radial or one-ended tanks are excluded from this list.

The tank pane has its own **Filter** and **Exclude** fields. Each accepts comma-separated display-name or internal-name terms.

Each tank suggestion shows:

- a thumbnail for every tank type in the set
- each tank type and count
- total tank count
- total dry mass
- excess capacity
- supplied resource capacity
- shared bulkhead profile

Outside Stage-By-Stage, the tank action places a part on the editor cursor. While a Stage-By-Stage Engines & Tanks stage is open, the action is labeled **Add to Stage** and records the complete selected tank set in the stage plan.

---

## 5. Analyze Existing mode

Use Analyze Existing when you already have a craft and want to compare alternative engines for a real stage.

### 5.1 Select a stage

You can select the stage in three ways:

1. Type the stage number.
2. Click **Pick Stage**, then click a part on the craft.
3. Click the stage header in KSP's normal staging list.

Analyze Existing clamps the stage number to the actual stage range of the current vessel. While Pick Stage is armed, VesselPlanner temporarily locks normal editor grabbing so the click selects a part instead of detaching it.

### 5.2 Simulation environment

The Simulation environment pane includes:

- Minimum TWR
- TWR gravity from the selected body's surface gravity
- Max engines
- Ignore monopropellant
- Include solid fuel
- Include air-breathing
- Include electric-propellant
- Simulate all engines

### 5.3 Existing-stage information

The Existing stage pane can show:

- craft wet and dry mass
- payload above the stage
- current and full stage propellant
- KSP stage wet, dry, and fuel mass
- current engine mass
- vehicle start and end mass
- current-stage burn time from KSP data when available
- tank capacity
- known tank volume
- current engines
- stage resources

The Current engines table shows part thumbnails, ASL thrust, vacuum thrust, and ASL/vacuum Isp. Which informational lines are shown is configurable on Settings -> Analyze Existing.

### 5.4 How replacement candidates are evaluated

Analyze Existing prefers KSP's `VesselDeltaV` / `DeltaVStageInfo` mass boundaries when they are available. Candidate engines are evaluated against the propellant **currently loaded** in the stage rather than assuming every tank is full. Resources the candidate engine cannot burn remain as carried mass.

Installed engine dry mass is removed before the candidate engine mass is added, preventing the old and new engines from being counted together.

Analyze Existing honors installed engine thrust limiters when describing the current engines.

### 5.5 Resizable Analyze layout

The candidate list appears on the right, with Selected Engine below it. Drag the grip above Selected Engine to trade height between the candidate table and the detail pane. The setting is remembered between sessions.

---

## 6. Candidate engine table and part images

Planning and Analyze Existing use the same sortable candidate table.

| Column | Meaning |
|---|---|
| **Engine** | Localized KSP part title with part thumbnail |
| **#** | Engine count in the candidate cluster |
| **Mass t/eng** | Dry mass of one engine part |
| **Stage Wet t** | Wet mass of the stage itself |
| **Start Mass t** | Full mass accelerated when the stage starts |
| **Atm delta-v** | Delta-v at the selected body and altitude |
| **Vac delta-v** | Vacuum delta-v |
| **ASL kN/eng** | Sea-level thrust of one engine |
| **Vac kN/eng** | Vacuum thrust of one engine |
| **Cost Eff.** | Atmospheric delta-v per Fund of total engine cost |
| **TWR** | Initial atmospheric TWR using total cluster thrust |
| **Max TWR** | Vacuum-thrust TWR |
| **Isp ASL/Vac** | Sea-level and vacuum specific impulse |
| **Burn** | Estimated burn duration |
| **Fuel** | Propellant requirement summary |
| **Bulkhead** | Engine KSP bulkhead profile(s) |
| **Add** | Selects the engine for normal KSP editor placement |

The candidate list has a fixed-height scroll area and a synchronized horizontal header/row scrollbar. Settings -> Engine Columns can show or hide every column.

### 6.1 Part thumbnails and rotating hover previews

VesselPlanner displays KSP part images in:

- Planning and Analyze Existing candidate engine rows
- Analyze Existing Current engines
- Planning tank-set rows
- Stage-By-Stage engine/tank build-list rows

The thumbnail cache resolves the exact loaded `AvailablePart.partUrl` when available, avoiding collisions between mod parts that share an internal name.

Hovering over a part thumbnail shows a larger **rotating preview** generated from KSP's editor icon prefab. Static list thumbnails remain cached for performance. Camera/zoom/background/render-size controls are available on Settings -> Appearance.

---

## 7. Adding parts to the editor

VesselPlanner uses KSP's normal editor part-spawn path.

- **Add** on a candidate row selects that engine for placement.
- **Add Engine** in Selected Engine does the same for the currently selected result.
- **Add Tank** / **Add Set** selects a tank part in normal Planning.
- **Add to Stage** records the selected tank set while Stage-By-Stage is capturing an Engines & Tanks stage.
- A finalized Stage-By-Stage build list provides **Add** buttons beside planned engine/tank parts.

For multimode engines, both listed engine modes point to the same underlying KSP part.

Analyze Existing and Planning have separate settings controlling whether VesselPlanner closes after a successful Add action. The window closes only when KSP actually accepts the part for placement.

---

## 8. Mission Planner

Mission Planner builds an ordered list of mission steps. Each step is one of two mutually exclusive types:

- **Engines & Tanks** - maneuver/body/delta-v/ASL-VAC data
- **Subassemblies** - one or more saved KSP subassemblies with Count and Decoupler options

A step never contains both propulsion data and subassemblies.

> **[IMAGE PLACEHOLDER: Mission Planner with several ordered steps]**

### 8.1 Mission list controls

Mission Planner begins with the name **Unnamed Mission**. Enter a different mission name if desired.

Rows can be edited and reordered with:

- double-click row - edit the step
- insert above
- insert below
- move up
- move down
- delete
- **Add New Step** - append a new step

The ASL/VAC controls and insert/order buttons have hover tooltips when tooltips are enabled in Settings.

The bottom of the Mission Planner page shows total delta-v for Engines & Tanks steps.

### 8.2 Supported maneuvers

The Engines & Tanks step editor supports:

- Launch
- Sub Orbital Launch
- Orbit
- Reentry
- Landing
- Splashdown
- Resource Transfer
- Impact Asteroid
- Transfer To Another Planet
- Change Apoapsis
- Change Both Pe And Ap
- Change Inclination
- Change Periapsis
- Change Semi Major Axis
- Fine Tune Closest Approach
- Intercept Asteroid
- Intercept Vessel
- Match Planes With Vessel
- Match Velocities With Vessel
- Return From A Moon

Maneuver labels are displayed with readable spaces while saved enum values remain compatible with existing plans.

### 8.3 Body history and automatic delta-v suggestions

Mission Planner uses the active delta-v CSV to suggest values where a matching row exists.

- Launch/Sub Orbital Launch use the selected body's `dV_to_low_orbit`.
- Landing/Splashdown use `dV_low_orbit_to_surface`.
- Transfer To Another Planet uses the matching route's `total_capture_dV`.
- Return From A Moon uses the moon ascent value plus the reverse parent/moon escape leg derived from the parent-to-moon route.

Launch body defaults from mission history: the KSP home body is used when no earlier Landing exists; otherwise the most recent landed body is used. The launch body remains editable.

For Transfer To Another Planet, the source is taken from the immediately preceding body-bearing maneuver. The transfer cannot be saved if the preceding mission history does not provide a valid source body.

### 8.4 Transfer-planner clipboard import

While a Transfer To Another Planet step is open, VesselPlanner checks the system clipboard for matching transfer-planner text. When compatible data is found, the **Clipboard delta-v** selector can import:

- Ejection
- Insertion
- Total

Clipboard data for a different route is ignored. The selector disables itself when no matching transfer is available.

### 8.5 Subassemblies steps

Mission Planner scans saved KSP subassemblies from the current save. Add one or more entries and set:

- **Count** - whole number greater than zero
- **Decoupler** - independently enabled per subassembly line

Adding the same subassembly again increases its count. VesselPlanner calculates subassembly wet mass from saved parts, `modMass` adjustments, and stored resources. Per-copy decoupler reference mass comes from:

```text
GameData/VesselPlanner/decouplerMasses.cfg
```

using the subassembly root part's bulkhead profile(s).

### 8.6 Saving and loading missions

Mission plans are stored in:

```text
GameData/VesselPlanner/PluginData/MissionPlans/
```

The filename is derived from the mission name with invalid filename characters replaced. Loading restores the ordered steps, step types, subassembly entries/counts/decouplers, unit mass data, and propulsion settings. Older mission files without explicit step types load as Engines & Tanks steps.

---

## 9. Stage-By-Stage planning

Stage-By-Stage lets you design a complete craft from the payload downward before building it in the editor.

A separate detailed tutorial is included as:

```text
Manual/VesselPlanner-Stage-by-Stage-Tutorial.docx
```

### 9.1 Starting a plan

When a new plan starts:

- If an editor vessel exists, Payload is initialized from its full wet mass.
- If the editor is empty, Payload defaults to **5.0 t**.
- Existing saved plans keep their stored payload.

The Stage-By-Stage Plan window opens below and left-aligned with the main VesselPlanner mode buttons. **New Stage** opens a modal immediately to the right of the plan window. With solid editor backgrounds enabled, the Stage-By-Stage windows use a lighter 0.36 gray underlay so they are distinct from the main planner.

### 9.2 Stage types

Each stage is either:

- **Engines & Tanks**
- **Subassemblies**

Changing the stage type replaces the previous contents when the stage is saved/recalculated.

### 9.3 Engines & Tanks stages

An Engines & Tanks stage includes:

- target delta-v
- Vacuum or Atmosphere basis
- minimum TWR
- max engines
- additional cargo mass
- optional stage decoupler mass
- bulkhead profile selection
- Side Boosters layout flag
- Core burns too layout flag
- Radial decouplers flag when Side Boosters is enabled

Click **Calculate** to open/use the normal Planning solver for that stage. Select an engine and tank set, then use **Add Engine & Tanks** / **Add to Stage** to capture the solution. Click **Done** when the stage contents are complete.

Booster-layout flags are saved as annotations. Radial-decoupler mass is not inferred automatically because the number of booster stacks cannot be known from the engine/tank solution alone.

### 9.4 Bulkhead profiles

The New/Edit Stage dialog can restrict engine and tank candidates by KSP `bulkheadProfiles`.

- Click the profile summary to expand/collapse the inline multi-select list.
- **Clear selection** removes all bulkhead restrictions.
- `srf` is shown first; stack profiles such as `size0`, `size1`, `size1p5`, `size2`, and larger sizes follow.
- A profile inherited from the craft or previous stage is a preset. Selecting a different profile replaces that inherited single choice; additional manual selections can create a multi-profile set.

For Stage 1, VesselPlanner attempts to inherit the open lower node profile of the existing editor craft. For later stages, it attempts to inherit the previous stage engine's lower attachment profile.

### 9.5 Subassemblies stages

A Subassemblies stage uses the same two-list workflow as Mission Planner:

- choose one or more saved subassemblies
- set Count per line
- optionally enable Decoupler per line
- review unit and total mass

A decoupler is counted once per subassembly copy. The total completed subassembly-stage mass is carried automatically by every lower stage.

### 9.6 Cumulative mass

Stage-By-Stage works from the payload downward. Stage 2 carries the payload plus Stage 1 wet mass; Stage 3 carries the payload plus Stages 1 and 2, and so on. **Previous stages mass** is shown for lower stages.

This cumulative mass includes propulsion hardware/resources, additional cargo, stage decoupler mass, mission-linked subassemblies, Subassemblies-stage contents, and their selected per-copy decouplers.

### 9.7 Mission Planner integration

Click **Select Mission Plan** to attach a saved mission. The mission appears in a left-hand sidebar with the last mission step first. The Stage-By-Stage plan name is initialized from the selected mission name and remains editable.

Clicking a mission step:

- opens an Engines & Tanks stage with target delta-v and ASL/VAC basis copied from an Engines & Tanks mission step, or
- opens a Subassemblies stage with all selected subassemblies, counts, and decoupler flags copied from a Subassemblies step.

Clicking the same linked mission step later edits the existing linked stage instead of creating a duplicate.

When Calculate is used on a mission-linked stage, VesselPlanner sets the Planning body/environment from the last body referenced in that mission history. If that body comes from a transfer, the transfer destination is used.

### 9.8 Save, load, finalize, and build

The plan window provides:

- New Stage
- Edit/Delete stage
- Save
- Load
- Clear/Confirm
- Finalize
- Reopen

Finalizing turns the plan into a build list. Engine and tank lines display part thumbnails and an **Add** action for normal KSP editor placement. Hovering over thumbnails uses the same rotating part preview as the main Planning/Analyze lists.

Plans are stored under VesselPlanner's PluginData plan directory. Older plans are migrated where possible; plans saved before stage types were introduced load as Engines & Tanks stages.

---

## 10. Delta-V Table page

Delta-V Table is immediately to the right of Planning in the editor mode row.

The page reads the currently active planet-pack CSV and displays it as an expandable tree:

- solar-orbiting bodies are top-level rows
- clicking a body with moons expands/collapses its children
- nested moon systems are supported, such as Urlum -> Wal -> Tal in OPM

The fixed columns are:

- Body
- dV to low orbit
- Ejection
- Capture
- Plane Change
- Total
- Landing
- Ascent

Values equal to zero or unavailable values are left blank. The home body does not show route values that do not apply. Moon indentation is confined to the Body cell so all numeric columns remain aligned.

Every displayed numeric value is clickable. Clicking it copies the **numeric value only** to the system clipboard and writes a confirmation message in the fixed status line at the bottom of the page.

For route columns:

- a planet uses the home-body -> planet route
- a moon uses its immediate parent -> moon route
- a nested moon uses its immediate parent -> child route

The dV-to-low-orbit, Landing, and Ascent values come from the body's self row.

> **[IMAGE PLACEHOLDER: Delta-V Table with an expanded moon system and status line]**

---

## 11. Delta-v tables and planet-pack detection

Mission Planner and Delta-V Table share CSV files in:

```text
GameData/VesselPlanner/PluginData/DeltaVTables/
```

The current CSV schema is:

```text
Origin,Destination,dV_to_low_orbit,ejection_dV,capture_dV,transfer_to_low_orbit_dV,total_capture_dV,dV_low_orbit_to_surface,ascent_dV,plane_change_dV,parent,isMoon,order
```

VesselPlanner detects the active planet pack through `PlanetPackHeuristics`. Known packs use the built-in pack name; a single unknown custom pack can use its detected GameData folder name.

Bundled starter tables include:

- `Stock.csv`
- `OPM.csv`
- `JNSQ.csv`
- `GPP.csv`
- `RSS.csv`

`OPM.csv` begins with the stock system, moves Eeloo under Sarnus, and adds Sarnus, Urlum, Neidon, Plock, their mapped moons, and the Wal -> Tal nested moon relationship. Plock uses the lower/ideal value from the supplied OPM transfer range.

You can replace or extend CSV rows for your preferred delta-v map as long as the schema and body hierarchy are preserved.

---

## 12. Settings

Open **Settings** from the main editor planner. Editor settings are saved in:

```text
GameData/VesselPlanner/PluginData/VesselPlannerSettings.cfg
```

The Settings window can be dragged from unused background area. It is intentionally taller than earlier releases: Alternate skin receives roughly one button-height of extra vertical room, while KSP skin receives that allowance plus six measured text-line heights so the controls fit without crowding.

> **[IMAGE PLACEHOLDER: VesselPlanner Settings window]**

### 12.1 Engine Columns

Show or hide individual candidate-table columns:

- Engine
- Count
- engine mass
- Stage Wet
- Start Mass
- atmospheric delta-v
- vacuum delta-v
- ASL thrust
- vacuum thrust
- Cost Efficiency
- TWR
- Max TWR
- Isp
- Burn
- Fuel
- Bulkhead
- Add

**Show All** and **Hide All** provide quick presets.

### 12.2 Analyze Existing

Choose which lines appear in the Existing stage pane and whether Analyze Existing closes after a successful Add operation.

### 12.3 Planning

Controls the independent **Close planner after Add selects a part for placement** preference for Planning. It applies to engine Add, Add Engine, and tank Add actions.

### 12.4 Filters

Four independent persistence switches control whether the following text is restored next session:

- engine include Filter
- engine Exclude
- tank include Filter
- tank Exclude

Turning one persistence option off clears only that field's saved value; it does not clear the current-session text or the paired field.

### 12.5 Appearance

#### Window skin

Choose:

- **KSP skin**
- **Alternate skin** - Unity's stock GUI skin

The change applies immediately to VesselPlanner editor and flight windows.

#### Solid editor backgrounds

**Use solid backgrounds for all editor windows** adds an opaque dark underlay to the main planner, Settings, and Stage-By-Stage windows while retaining normal borders/title styling. Flight Data has a separate solid-background setting.

#### Tooltips

**Show tooltips** is enabled by default. It controls VesselPlanner hover help, including:

- Part Images numeric fields and sliders
- Mission Planner ASL/VAC controls
- Mission Planner insert/order/delete controls

#### Part images

Every numeric Part Images setting has both an editable field and a slider. The sliders are vertically offset for better visual alignment with their text fields.

| Setting | Range | Default | Effect |
|---|---:|---:|---|
| **ZoomFactor for icons** | 0.1-5.0 | 0.8 | Static list-thumbnail camera zoom. Lower values make the part appear larger. |
| **ZoomFactor for Rotating Images** | 0.1-5.0 | 1.0 | Enlarged rotating-preview camera zoom. Lower values make the part appear larger. |
| **Camera Yaw Degrees** | 0-180 | 45 | Horizontal viewing angle. |
| **Camera Pitch Degrees** | 0-90 | 20 | Vertical viewing angle. |
| **RotatingPreviewSize** | 32-256 px | 100 | Render resolution for each rotating-preview frame. Higher values are sharper but cost more memory/render time. |
| **Degrees per frame** | 1-180 | 60 | Controls how quickly the rotating preview advances through its generated orientations. |

**Rotating Image Background** is a combo box with:

- Transparent
- Black
- Dark Gray
- Gray
- White

The background setting affects only the enlarged rotating hover preview. Static list icons remain transparent.

Changing any part-image setting invalidates the relevant thumbnail/preview caches so new images use the updated configuration immediately. **Reset image settings** restores the defaults above.

#### Detail pane sizes

Planning and Analyze Existing splitter positions are remembered. **Reset pane sizes** restores the default Planning engine/tank width split and list heights.

---

## 13. Flight Data

Click the VesselPlanner toolbar button in flight to open the Flight Data window.

The Flight Data window has a fixed height and a **1000-pixel minimum width**. Resize it horizontally using the centered grab handle on the right edge. Vertical resizing is intentionally disabled. The separate Flight Settings window can be resized in both directions.

> **[IMAGE PLACEHOLDER: Flight Data graph during ascent]**

### 13.1 Top toolbar

The top row contains:

- **Start Plotting / Stop Plotting**
- **Start at Launch / Launch Armed**
- **Reset**
- **Sample Interval**
- minus button
- editable interval value
- plus button
- `/sec`
- sample count
- **Settings**
- close X

**Settings** is at the extreme right immediately before the X button.

### 13.2 Start at Launch and Reset

**Start at Launch** can be armed while the vessel is in PRELAUNCH. When the vessel leaves PRELAUNCH, VesselPlanner clears the existing graph and starts recording automatically. Once the vessel is already flying, the Start-at-Launch control is disabled.

**Reset** clears current samples and stage markers but intentionally preserves stored historical maxima.

Sampling pauses while KSP is paused.

### 13.3 Sample Interval

Default sample interval is **0.25 seconds**. The minus/plus buttons change it by 0.25 seconds, or you can type a value from **0.05 to 60 seconds**. The field is labeled **Sample Interval** and the displayed suffix is `/sec`.

### 13.4 Built-in flight data sources

VesselPlanner can record:

- Velocity (surface)
- Velocity (orbit)
- Vertical speed
- Acceleration
- G-Force
- Altitude ASL
- Altitude above surface
- Dynamic pressure (q)
- Vehicle mass
- Angle of attack
- every resource currently carried by the active vessel
- every stock `ModuleEnviroSensor` output found on the vessel

All available sources are recorded while plotting, even if they are not currently visible, so changing Plot selections can redraw already-collected data.

### 13.5 Legend and graph behavior

The Plotted sensors area shows each visible series with:

- color swatch
- Sensor name
- Current value
- Max value
- Units

The graph fills left to right with fixed horizontal spacing, then scrolls as new samples arrive. Vertical grid positions remain fixed while the window is resized; widening adds grid lines only on the right and narrowing removes lines that no longer fit.

Elapsed-time labels can be shown on every grid line, every other line, or every third line.

Stage activations are drawn as orange dashed vertical markers labeled `Stage N`.

### 13.6 Scaling and stored maxima

Altitude uses a configurable **Altitude chart top** and a zero baseline. **Body Default** restores the launch/body-derived default.

Other series use per-vessel historical maxima stored across launches and reverts in the current save. Stored maxima are kept in:

```text
GameData/VesselPlanner/PluginData/FlightSensorMaxima.tsv
```

Use **Clear Max Values for Vessel** in Flight Settings to remove stored non-altitude maxima for the current vessel.

### 13.7 Axis annotations

In Flight Settings, each series has an **Axis** button that cycles:

```text
Off -> Left -> Right -> Off
```

This draws that series' scale on the selected side of the graph. Axis text is rendered into the graph texture, so it is included in PNG exports.

### 13.8 Flight Settings

Open Settings from the graph to configure:

- Altitude chart top and Body Default
- solid Flight Data background
- time-label frequency: Every line / Every other / Every third
- CSV export folder
- PNG export folder
- Clear Max Values for Vessel
- Plot selection
- Max display
- Units
- Axis side

Sources are grouped into:

- Flight data
- Ship resources
- Sensor outputs

Each group has **All** and **None** controls. Available resources and sensor outputs refresh automatically.

### 13.9 Export CSV and PNG

The export controls are at the **bottom** of the Flight Data window:

- The **CSV export** path is shown on one line with **Export CSV** on the right.
- The **PNG export** path is shown on the next line with **Export PNG** on the right.

Defaults:

```text
CSV: GameData/VesselPlanner/PluginData/CSV
PNG: KSP_ROOT/Screenshots
```

Relative CSV paths are resolved under `GameData`. Relative PNG paths are resolved from the KSP root. Both folders can be changed from Flight Settings.

**Export CSV** writes the selected plotted series as comma-separated data. **Export PNG** saves the currently visible graph, including graph-rendered time labels/axes.

---

## 14. How calculations are performed

This section explains the important assumptions behind the numbers.

### 14.1 Candidate engines

Candidates come from KSP's loaded part database after the editor's `ExcludeFilters` are applied. If another mod hides an engine through the editor filter API, VesselPlanner hides it too. The currently selected editor category is not used as a candidate filter.

### 14.2 Thrust, Isp, and TWR

- TWR gravity comes from the selected body's surface gravity.
- Vacuum thrust is based on the loaded `ModuleEngines` / `ModuleEnginesFX` configuration.
- Atmospheric thrust follows the engine atmosphere curve; engines with atmosphere/velocity-sensitive flow can use KSP flow calculations with a fallback.
- Candidate TWR uses total cluster thrust.
- The ASL/Vac thrust columns show thrust **per engine**.
- Analyze Existing honors the installed engine's editor thrust limiter when describing the current configuration.

### 14.3 Stage mass boundaries

Analyze Existing prefers KSP's own `VesselDeltaV` / `DeltaVStageInfo` values:

- Stage Wet uses KSP `stageMass`.
- Start Mass uses full-vehicle `startMass`.
- Retained payload is `startMass - stageMass`.

The branch scanner still identifies resources, tanks, volume, and engine details and provides a fallback when stock delta-v data is not ready.

### 14.4 Planning tank estimate

Before a real tank set is selected, Planning estimates tank dry mass from:

```text
tank dry mass = propellant mass x tank dry/fuel mass ratio
```

The stage solver iterates because propellant mass affects tank mass and tank mass affects the total mass the propellant must accelerate.

### 14.5 Burn time

Mass flow is derived from thrust and Isp:

```text
mdot = F / (Isp x g0)
t    = propellant mass x Isp x g0 / F
```

The rocket equation uses KSP standard gravity, **9.80665 m/s^2**.

In Analyze Existing, burn duration uses only the propellant mixture the candidate can actually consume from the stage. Unrelated resources do not inflate burn time.

### 14.6 Resources and volume

The complete propellant list is read from each loaded engine module. VesselPlanner does not assume LiquidFuel/Oxidizer.

- resources with positive engine ratios are kept in the requirement set
- mass-bearing resources that affect Isp drive the rocket equation
- `ignoreForIsp` resources can remain visible as auxiliary requirements
- physical volume uses `PartResourceDefinition.volume`

This allows stock and many modded propellant mixtures to work without a hard-coded resource table.

---

## 15. Important files created or used by VesselPlanner

Typical persistent data lives under:

```text
GameData/VesselPlanner/PluginData/
```

Important files/folders include:

- `VesselPlannerSettings.cfg` - editor and Flight Data settings
- `DeltaVTables/*.csv` - Mission Planner and Delta-V Table data
- `MissionPlans/*.cfg` - saved Mission Planner missions
- `Plans/*.cfg` - saved Stage-By-Stage vessel plans
- `FlightSensorMaxima.tsv` - per-vessel historical graph maxima
- `CSV/` - default Flight Data CSV export directory
- `decouplerMasses.cfg` - reference decoupler masses used by Stage-By-Stage/subassemblies

PNG exports default to:

```text
KSP_ROOT/Screenshots
```

---

## 16. Known limits

VesselPlanner is a planning tool and heuristic stage solver, not a full replacement for KSP's in-flight fuel-flow simulation.

The following are not fully modeled:

- dynamic RealFuels/B9/MFT-style tank reconfiguration after prefab loading
- complete jet intake/velocity-curve flight-state simulation beyond the loaded engine/pressure behavior available to the solver
- arbitrary third-party engine-variant systems outside loaded `ModuleEngines` / `ModuleEnginesFX`
- full MechJeb-style crossfeed/asparagus/drop-tank flow simulation for every modded arrangement

For unusual modded fuel-flow systems, Planning with a manually entered payload is often more reliable than depending on a complex editor branch scan.

---

## 17. Troubleshooting

### VesselPlanner does not appear

Check that:

- ToolbarController is installed.
- ClickThroughBlocker is installed.
- `VesselPlanner.dll` is under `GameData/VesselPlanner/Plugins`.
- an old `GameData/EngineStagePlanner` copy is not also installed.

### Expected engines are missing

Check:

- Filter and Exclude text
- Include solid/air/electric class toggles
- Ignore monopropellant
- Match stage bulkhead size
- Stage-By-Stage selected bulkhead profiles
- KSP/mod editor ExcludeFilters

### Expected tanks are missing

Check:

- tank Filter and Exclude text
- required propellant type
- bulkhead profile restriction
- whether the storage part has both top and bottom attach nodes

### Wrong or missing delta-v values

Verify that the detected planet-pack CSV exists in:

```text
GameData/VesselPlanner/PluginData/DeltaVTables/
```

and that body `parent`, `isMoon`, and route rows are consistent with the active planet pack.

### Part image looks wrong

Part thumbnails prefer KSP `partUrl` to distinguish duplicate internal part names. If the view angle/size is undesirable, adjust the Part Images settings under Appearance and hover again to regenerate the preview.

---

## 18. Current release notes

This manual reflects **VesselPlanner 0.7.73** and includes the capabilities through 0.7.72, including:

- configurable Vacuum/Atmosphere Planning targets
- mixed one-to-three-type tank sets
- Mission Planner Engines & Tanks and Subassemblies step types
- Stage-By-Stage mission integration and finalized build lists
- exact-part thumbnails and rotating hover previews
- configurable part-image camera, preview size, speed, and background
- tooltip enable/disable setting
- Stock, OPM, JNSQ, GPP, and RSS delta-v tables
- expandable clickable Delta-V Table page
- 1000-pixel minimum-width Flight Data window
- bottom-row CSV/PNG export controls and configurable export folders
- selectable graph axis annotations and persistent per-vessel maxima

For detailed change history, see `CHANGELOG.md` in the source package.
