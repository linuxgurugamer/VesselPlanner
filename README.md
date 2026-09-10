# VesselPlanner for KSP1

VesselPlanner is a planning and telemetry mod for **Kerbal Space Program 1.12.x**. It helps you analyze an existing stage, design a future stage, build a vessel one stage at a time, and record flight telemetry.

An illustrated Word version of this instruction manual, including UI screenshots, is included at `Manual/VesselPlanner-Manual.docx`.

A detailed Stage-By-Stage walkthrough is included at `Manual/VesselPlanner-Stage-by-Stage-Tutorial.docx`.

> **[IMAGE PLACEHOLDER: Main VesselPlanner window in the VAB/SPH]**

---

## 1. Requirements

VesselPlanner requires:

- **Kerbal Space Program 1.12.x**
- **ToolbarController**
- **ClickThroughBlocker**

The required mod files are normally installed under:

```text
GameData/001_ToolbarControl/
GameData/000_ClickThroughBlocker/
```

---

## 2. Installation

1. Remove any older `GameData/EngineStagePlanner` folder so KSP does not load both the old and new assemblies.
2. Copy VesselPlanner into:

```text
KSP_ROOT/GameData/VesselPlanner/
```

3. Make sure the plugin DLL is located at:

```text
KSP_ROOT/GameData/VesselPlanner/Plugins/VesselPlanner.dll
```

4. Start KSP.

VesselPlanner can read older EngineStagePlanner settings, Stage-By-Stage plans, and flight maxima if the new VesselPlanner copies do not already exist.

> **[IMAGE PLACEHOLDER: GameData folder showing VesselPlanner and its dependencies]**

---

## 3. Opening VesselPlanner

### In the VAB or SPH

Click the **VesselPlanner** ToolbarController button. Look for the transparent checklist-and-rising-graph icon.

The editor window has three operating modes:

- **Planning** — design a stage that does not yet exist.
- **Analyze Existing** — inspect a stage already on the vessel and compare replacement engines.
- **Stage-By-Stage** — build and save a complete vessel plan one stage at a time.

### In Flight

Click the same toolbar button to open the **Flight Telemetry** graph.

> **[IMAGE PLACEHOLDER: VesselPlanner toolbar button]**

---

# Editor Instructions

## 4. Planning Mode

Use **Planning** when you want VesselPlanner to size a future stage.

### Step 1 — Select Planning

Choose **Planning** at the top of the window.

### Step 2 — Enter the stage requirements

Set the values in the **Requirements** pane:

- **Target Δv** — required delta-v for the stage.
- **Δv basis** — Vacuum or Atmosphere.
- **Minimum TWR** — minimum acceptable thrust-to-weight ratio.
- **Payload** — mass carried above the new stage.
- **Body** — celestial body used for gravity and atmospheric calculations.
- **Altitude** — altitude used for atmospheric thrust and Isp.
- **Max engines** — largest engine cluster VesselPlanner should test.
- **Tank structural ratio** — estimated tank dry mass divided by propellant mass.

Future stage numbers may be higher than the craft's current maximum stage.

> **[IMAGE PLACEHOLDER: Planning Requirements pane with sample values]**

### Step 3 — Calculate

Click **Calculate**.

VesselPlanner tests compatible engines and engine counts and fills the candidate engine list.

### Step 4 — Select an engine

Click an engine row in the candidate list.

The **Selected Engine** pane shows detailed information for that solution, including thrust, TWR, mass, delta-v, burn time, and propellant requirements.

> **[IMAGE PLACEHOLDER: Candidate engine list and Selected Engine pane]**

### Step 5 — Select a tank

The **Tanks** pane shows tank combinations that can hold the required propellant.

Each row shows information such as:

- tank name
- number required
- dry mass
- excess capacity
- total capacity
- KSP bulkhead profile

Click a tank row to select it. The selected tank is highlighted and is also shown beneath the selected engine name.

> **[IMAGE PLACEHOLDER: Tanks pane with one tank row selected]**

### Step 6 — Add parts

Depending on what you are doing, use:

- **Add** — selects the engine part for placement in the editor.
- **Add Engine** — selects the currently highlighted engine for placement.
- **Add Tank** — selects one copy of that tank for placement.

When a Stage-By-Stage stage is open, these buttons add the selected parts to the plan instead of placing them directly on the editor cursor.

---

## 5. Analyze Existing Mode

Use **Analyze Existing** to inspect a stage that already exists on the vessel.

### Step 1 — Select the stage

Enter a stage number or use **Pick Stage** and click a part on the vessel.

You can also click a stage header in KSP's stock staging list.

In Analyze Existing mode, the selected stage cannot be higher than the vessel's current maximum stage.

### Step 2 — Review the Simulation Environment

The upper-left **Simulation Environment** pane contains the controls used for the engine comparison, including:

- Minimum TWR
- TWR gravity
- Max engines
- engine filters
- propellant filters
- **Simulate all engines**

### Step 3 — Review Existing Stage information

The lower-left **Existing Stage** pane summarizes the selected stage. Depending on your Settings choices, it can show:

- craft wet and dry mass
- payload above the stage
- stage wet, dry, and propellant mass
- current engine mass
- vehicle start and end mass
- current burn time
- current engines
- stage resources
- tank capacity and known tank volume

> **[IMAGE PLACEHOLDER: Analyze Existing mode showing Simulation Environment and Existing Stage panes]**

### Step 4 — Compare candidate engines

The right side lists engines that can be simulated against the current stage.

Select an engine row to see its detailed result below the list.

Useful filters include:

- engine-name search
- **Match stage bulkhead size**
- **Ignore monopropellant**

Click **Add** or **Add Engine** to select a replacement engine for placement in the editor.

---

## 6. Reading the Engine List

The candidate engine table can include the following columns:

| Column | Meaning |
|---|---|
| Engine | Engine part name |
| # | Number of engines in the tested cluster |
| Mass t/eng | Dry mass of one engine part |
| Stage Wet t | Wet mass of the stage itself |
| Start Mass t | Full vehicle mass accelerated at ignition |
| Atm Δv | Delta-v at the selected body and altitude |
| Vac Δv | Vacuum delta-v |
| ASL kN/eng | Sea-level thrust for one engine |
| Vac kN/eng | Vacuum thrust for one engine |
| TWR | Atmospheric thrust-to-weight ratio |
| Max TWR | Vacuum-thrust TWR |
| Isp | Specific impulse |
| Burn | Estimated burn time |
| Fuel | Required propellant |
| Δv/Fund | Delta-v divided by engine cost |
| Bulkhead | Engine bulkhead profile where available |
| Add | Select the engine for placement or add it to an open plan stage |

The visible columns can be changed in **Settings → Engine Columns**.

### Sorting

You can sort engine solutions by goals such as:

- lowest wet stage mass
- lowest propellant
- lowest engine cost
- shortest burn
- highest TWR
- highest Isp

---

# Stage-By-Stage Instructions

## 7. Creating a Stage-By-Stage Plan

Use **Stage-By-Stage** to design and save an entire vessel before building it.

> **[IMAGE PLACEHOLDER: Stage-By-Stage Plan window]**

### Step 1 — Open Stage-By-Stage mode

Choose **Stage-By-Stage** from the top of the editor planner.

The **Stage-By-Stage Plan** window opens with its left edge aligned to the main VesselPlanner window and its top edge immediately below the main mode-button row.
When solid editor backgrounds are enabled, the Stage-By-Stage Plan uses a lighter **0.36** gray background shade than the main planner so the planning window is easier to distinguish.

The plan window contains:

- vessel/craft name
- payload mass
- starting body
- list of planned stages

When you start a new Stage-By-Stage plan, **Payload (t)** is initialized automatically:

- if a vessel is already in the editor, VesselPlanner uses that vessel's current full wet mass;
- if the editor is empty, VesselPlanner uses **5.0 t**.

A plan that already contains stages keeps its existing payload mass when you return to Stage-By-Stage mode.

### Step 2 — Create a stage

Click **New Stage**.

The **New Stage** dialog opens immediately to the right of the Stage-By-Stage Plan window, with both top edges aligned and the dialog's left edge adjacent to the plan window's right edge.
The New Stage/Edit Stage dialog uses the same **0.36** gray solid background shade as the Stage-By-Stage Plan.

The New Stage dialog is modal. Other VesselPlanner windows cannot be used until you click **Calculate** or **Cancel**.

Enter:

- **Target Δv**
- **Δv basis**
- **Minimum TWR**
- **Max engines**
- **Additional Cargo Mass**
- **Add decoupler mass**
- optional **Bulkhead profiles**

**Additional Cargo Mass** is non-propellant dry mass carried by that stage. It is separate from the vessel payload.

When **Add decoupler mass** is enabled, VesselPlanner adds the decoupler mass for the selected stack profile to the stage's fixed dry mass. The values come from `GameData/VesselPlanner/decouplerMasses.cfg`. If several matching stack profiles are selected, the largest matching decoupler mass is used. `srf` has no decoupler mass entry.

For Stage 2 and later, the dialog also shows **Previous stages mass**. This is the original plan payload plus the full wet mass of all previously planned stages, and it is automatically used as the payload for the new stage.

Tab moves through the numeric entry fields; Shift+Tab moves backward.

> **[IMAGE PLACEHOLDER: New Stage dialog with all entry fields visible]**

### Step 3 — Choose bulkhead profiles if needed

VesselPlanner presets the Bulkhead Profiles selection when a new stage is opened:

- For the first stage, if a vessel already exists in the editor, the profile comes from the vessel's open lower `bottom` node; if that part has no bottom node, its `top` node is used.
- For later stages, the profile comes from the selected engine in the previous stage, using that engine's `bottom` node and falling back to its `top` node.
- If no applicable node can be found, no profile is preselected.

You can then open the Bulkhead Profiles list and change the selection as needed.

- `srf` is listed first for surface-attached parts.
- stack profiles such as `size0`, `size1`, `size1p5`, `size2`, and so on follow it.
- leaving all profiles unselected allows all profiles.

The selected profiles filter **both engine candidates and tank suggestions** for that stage.

### Step 4 — Calculate the stage

Click **Calculate**.

The main VesselPlanner window comes to the front and displays the Planning solution for that stage.

### Step 5 — Select an engine and tank

1. Select an engine from the candidate list.
2. Select a tank from the Tanks pane.
3. Confirm the Selected Engine pane shows both:
   - the selected engine and quantity
   - the selected tank and quantity

### Step 6 — Add the engine and tanks to the stage

Click **Add Engine & Tanks**.

The button is disabled until a tank is selected.

It adds the selected engine and the selected tank quantity to the open Stage-By-Stage stage. After the add completes, the Stage-By-Stage Plan window is brought to the front.

You can also use the individual **Add**, **Add Engine**, and **Add Tank** controls.

> **[IMAGE PLACEHOLDER: Stage-By-Stage stage after engine and tanks have been added]**

### Step 7 — Finish or edit the stage

Use:

- **Done** — close the currently open stage without finalizing the complete plan.
- **Edit** — reopen the stage requirements and recalculate while keeping the selected parts.
- **Delete** — remove a stage from the plan.
- **Remove** — remove an individual planned part.

### Step 8 — Save the plan

Click **Save**.

Plans are stored in:

```text
GameData/VesselPlanner/PluginData/Plans
```

### Step 9 — Finalize the plan

Click **Finalize** when the stage plan is complete.

After finalizing:

- New Stage is no longer available.
- Each planned part receives an **Add** button for placing it in the editor.
- The main planner closes so the plan can be used as a build list.

Use **Reopen** if you need to edit the plan again.

### Loading a saved plan

Click **Load** to open the saved-plan list.

You can:

- load a plan
- delete a saved plan

Deleting requires confirmation and removes only the saved plan file.

---

# Settings

## 8. Editor Settings

Click **Settings** in the editor planner.

Settings are saved in:

```text
GameData/VesselPlanner/PluginData/VesselPlannerSettings.cfg
```

> **[IMAGE PLACEHOLDER: Editor Settings window]**

### Engine Columns

Choose which columns are visible in the engine candidate table.

Use **Show All** or **Hide All** for quick changes.

### Analyze Existing

Choose which informational lines are displayed in the Existing Stage pane.

You can also choose whether the planner closes after a successful engine Add operation.

### Planning

Choose whether the planner closes after a successful Add, Add Engine, or Add Tank operation.

### Appearance

Appearance options include:

- **KSP skin** or **Alternate skin**
- **Use solid backgrounds for all editor windows**

Solid backgrounds are enabled by default for new configurations. The setting applies to the main editor planner, Settings window, and Stage-By-Stage windows. Flight windows are controlled separately from Flight Settings.

---

# Flight Telemetry

## 9. Using the Flight Graph

Click the VesselPlanner toolbar button while in Flight.

> **[IMAGE PLACEHOLDER: Flight telemetry graph during ascent]**

The Flight Data window has a fixed height and is horizontally resizable from the grab handle centered on its right edge. Drag the handle left or right to change only the window width; vertical resizing is disabled. The separate Flight Plot Settings window keeps its normal two-axis resize handle. The graph vertical grid lines keep fixed horizontal positions while the Flight Data window is resized; widening the window adds new grid lines only on the right, and narrowing removes only lines that no longer fit. The vertical grid uses fixed spacing, and the **Time labels** setting can place elapsed-time labels on **Every line**, **Every other**, or **Every third** vertical grid line. The default is **Every third**. Labels stay centered on their selected fixed grid positions; resizing only reveals or removes positions at the right edge.

### Starting a recording

- **Start at Launch** may be armed while the vessel is still PRELAUNCH.
- Recording starts automatically when the vessel leaves PRELAUNCH.
- **Reset** clears the current graph samples and stage markers.
- Sampling pauses when KSP is paused.

### Sample interval

The default sample interval is **0.25 seconds**.

Use the `-` and `+` buttons or type a value directly. Valid values are from **0.05 to 60 seconds**.

### Available telemetry

VesselPlanner can record and plot:

- surface velocity
- orbital velocity
- vertical speed
- acceleration
- G-force
- altitude ASL
- altitude above terrain/surface
- dynamic pressure (q)
- vessel mass
- angle of attack
- vessel resources
- stock `ModuleEnviroSensor` outputs

### Stage markers

Stage activations appear as vertical dashed markers labeled with the stage number.

### Historical maxima

For non-altitude series, VesselPlanner keeps per-vessel maximum values and uses them for graph scaling.

They are stored in:

```text
GameData/VesselPlanner/PluginData/FlightSensorMaxima.tsv
```

**Reset** does not erase these maxima. Use **Clear Max Values for Vessel** in Flight Settings if you want to clear them.

---

## 10. Flight Settings and Export

Open **Settings** from the flight graph.

> **[IMAGE PLACEHOLDER: Flight Settings window showing Plot, Sensor, Max, and Units columns]**

You can choose which flight data, resources, and sensor values are plotted. Available vessel resources and sensor outputs are refreshed automatically; there is no separate manual refresh button.

Use **Time labels** to choose **Every line**, **Every other**, or **Every third**. The setting is saved as `ElapsedTimeLabelGridInterval` in `VesselPlannerSettings.cfg`; **Every third** is the default.

Enable **Use solid background for Flight Data window** to make the flight graph window opaque while keeping the selected KSP/alternate skin, title, and border styling. The opaque underlay is drawn behind the single KSP window, avoiding a duplicate-window flicker during resizing. This setting is saved in `VesselPlannerSettings.cfg` and is enabled by default for new configurations; an existing saved value is preserved.

### CSV export

Default location:

```text
GameData/VesselPlanner/PluginData/CSV
```

The CSV location is configurable. Relative CSV paths are resolved under `GameData`; absolute paths are also accepted.

### PNG export

Default location:

```text
KSP_ROOT/Screenshots
```

The PNG location is configurable. Relative PNG paths are resolved from the KSP root; absolute paths are also accepted.

Use:

- **Export CSV** — export the selected telemetry series.
- **Export PNG** — save an image of the currently visible graph.

---

# Reference

## 11. Important Calculation Notes

### TWR

TWR uses the selected body's gravity. Atmospheric TWR uses thrust at the selected atmosphere/altitude; maximum TWR uses vacuum thrust.

### Delta-v

The results table shows both atmospheric and vacuum delta-v. In Planning, the selected Δv basis determines which value is used to size the stage.

### Stage mass

- **Stage Wet t** is the mass of the stage itself.
- **Start Mass t** is the full vehicle mass accelerated when that stage starts.

### Tank planning

Planning estimates tank dry mass with the **tank structural ratio** until an actual tank choice is made.

### Resources

Propellant requirements are shown in KSP resource units and mass. Where resource volume data is available, VesselPlanner also estimates physical tank volume.

---

## 12. Files Created by VesselPlanner

Typical persistent files are stored under:

```text
GameData/VesselPlanner/PluginData/
```

Important files and folders include:

```text
VesselPlannerSettings.cfg          Editor and flight settings
FlightSensorMaxima.tsv             Per-vessel telemetry maxima
Plans/                             Saved Stage-By-Stage plans
CSV/                               Default CSV export folder
```

The Stage-By-Stage decoupler/stack-separator reference table is stored at:

```text
GameData/VesselPlanner/decouplerMasses.cfg
```

PNG exports default to:

```text
KSP_ROOT/Screenshots
```

---

## 13. Troubleshooting

### VesselPlanner does not appear

Check that:

- ToolbarController is installed.
- ClickThroughBlocker is installed.
- `VesselPlanner.dll` is under `GameData/VesselPlanner/Plugins`.
- the old `GameData/EngineStagePlanner` installation has been removed.

### An engine or tank is missing from the list

Check:

- engine-name filters
- propellant filters
- bulkhead-profile selections
- editor part filters supplied by other mods
- Analyze Existing's **Match stage bulkhead size** option

### Add Engine & Tanks is disabled

In Stage-By-Stage mode, select a tank row first. The combined button requires both an open stage and an explicitly selected tank.

### A saved Stage-By-Stage plan does not contain cargo mass

Plans created before cargo support load with **Additional Cargo Mass = 0**.

---


## Source build configuration

The repository `jenkins.txt` is configured for VesselPlanner release builds. It packages `VesselPlanner.version`, `License.md`, `README.md`, and the `Manual` folder under `GameData/VesselPlanner`.

## 14. License

See `License.md` for license information.

> **Editor layout:** The main VesselPlanner window is horizontally resizable from **1150 px to 1850 px** using the grip on its right edge; the selected width is remembered. When the candidate-engine table is wider than the window, use its horizontal scrollbar; the sortable column headings scroll horizontally with the engine rows so the headings stay aligned. The synchronized header area is **34 px** tall.
