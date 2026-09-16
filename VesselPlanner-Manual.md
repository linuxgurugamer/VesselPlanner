# VesselPlanner — User Manual

For Kerbal Space Program 1.12.x

VesselPlanner helps you pick engines and plan missions. In the VAB/SPH it tells you which engines will actually meet a delta-v and TWR target for a given stage, and Mission Planner builds an ordered list of propulsion and subassembly mission steps. In flight it plots telemetry on a scrolling graph.

---

## 1. Installation

### 1.1 Required mods

VesselPlanner will not run without these. Install them first:

| Mod | File it must provide |
|---|---|
| ToolbarController | `GameData/001_ToolbarControl/Plugins/ToolbarControl.dll` |
| ClickThroughBlocker | `GameData/000_ClickThroughBlocker/Plugins/ClickThroughBlocker.dll` |

### 1.2 Copy the files

Place these four files under `GameData/VesselPlanner`:

| From | To |
|---|---|
| `VesselPlanner/bin/Release/VesselPlanner.dll` | `GameData/VesselPlanner/Plugins/VesselPlanner.dll` |
| `VesselPlanner/PluginData/Textures/icon_38.png` | `GameData/VesselPlanner/PluginData/Textures/icon_38.png` |
| `VesselPlanner/PluginData/Textures/icon_24.png` | `GameData/VesselPlanner/PluginData/Textures/icon_24.png` |
| `VesselPlanner.version` | `GameData/VesselPlanner/VesselPlanner.version` |
| `GameData/VesselPlanner/PluginData/DeltaVTables/*.csv` | `GameData/VesselPlanner/PluginData/DeltaVTables/*.csv` |

Start KSP. If the toolbar button appears in the VAB, you're installed.

> **[IMAGE PLACEHOLDER: `images/install-folder-layout.png` — file explorer showing the finished GameData/VesselPlanner folder tree]**

---

## 2. Opening the window

One toolbar button does everything. It appears in the VAB, the SPH, and in Flight. Look for the transparent checklist-and-rising-graph icon.

- **In the editor**, it opens the planner.
- **In flight**, it opens the telemetry graph.

The planner window starts **closed** every time you enter the editor — click the button to open it.

> **[IMAGE PLACEHOLDER: `images/toolbar-button.png` — the VesselPlanner button on the KSP application toolbar]**

Both windows can be dragged from any empty area and resized using the grip in the lower-right corner. Clicks won't leak through to parts behind the window, and the windows close themselves automatically when KSP starts loading a new scene.

---

## 3. The editor modes

| Mode | Use it when |
|---|---|
| **Analyze Existing** | You already have a craft built and want to know which engine to put on a stage that already has tanks and fuel. |
| **Planning** | You have a delta-v target and a payload mass, and you want the mod to size the stage for you from scratch. |
| **Stage-By-Stage** | You want to design and save a complete vessel plan one stage at a time. |
| **Mission Planner** | You want an ordered mission-step list containing either propulsion maneuvers or saved subassemblies. |

> **[IMAGE PLACEHOLDER: `images/mode-toggle.png` — the Analyze Existing / Planning selector at the top of the planner]**

---

## 4. Using Analyze Existing

### Step 1 — Pick the stage

You have three ways to choose which stage to analyze:

1. Type a number into the **Stage** field.
2. Click **Pick Stage**, then click any part on your craft. The planner jumps to that part's stage.
3. Click the orange header of any stage in KSP's normal staging list on the right of the screen.

While Pick Stage is armed, the mod temporarily locks the editor so your click selects a part instead of grabbing and detaching it. Normal editing returns as soon as you release the mouse.

In this mode you cannot select a stage number higher than the craft's current maximum stage — the field clamps itself, including when you add or remove stages.

> **[IMAGE PLACEHOLDER: `images/pick-stage.png` — Pick Stage armed, cursor hovering a fuel tank on the craft]**

### Step 2 — Set up the simulation environment

The left column splits into two panes. The top pane, **Simulation environment**, is always visible and holds everything that affects the run:

- **Minimum TWR** — candidates below this are rejected.
- **TWR gravity** — filled in automatically from the body you select.
- **Max engines** — the largest cluster size the mod will try.
- **Simulation filters** — engine Filter/Exclude text fields, bulkhead matching, ignore monopropellant. Filter and Exclude both accept comma-separated terms; Filter is an OR match and any Exclude match removes the engine.
- **Simulate all engines** — run the sweep.
- **Simulation status** — progress and result count.

Below it, the **Existing stage** pane shows what the mod found on your craft: wet and dry masses, payload above the stage, propellant currently loaded versus full capacity, current engine mass, burn time, tank volume, and the list of engines already installed. Each installed-engine row includes a thumbnail of the KSP part.

> **[IMAGE PLACEHOLDER: `images/analyze-left-column.png` — the two stacked panes, Simulation environment above Existing stage]**

### Step 3 — Choose a body and altitude

Click the current-body button to open a vertical list of celestial bodies. Pick one, then set an altitude with the slider. The mod uses KSP's own atmospheric pressure model and each engine's real atmosphere curve, so the atmospheric numbers are what you'd actually get there.

The body list draws on top of the rest of the window rather than pushing it down, and it stays anchored to the button as you move the planner around.

> **[IMAGE PLACEHOLDER: `images/body-selector.png` — the body list open beneath the current-body button]**

### Step 4 — Read the results

The candidate list shows every compatible engine, simulated against the propellant **actually loaded** in that stage right now (not maximum tank capacity). Each engine row includes a thumbnail of the KSP part next to its name. The sortable **Mass t/eng** column shows the dry mass of one engine part. See [Section 6](#6-reading-the-candidate-table) for what each column means.

### Filters available in this mode

- **Filter** — live, case-insensitive include search. Multiple comma-separated terms use OR matching.
- **Exclude** — live, case-insensitive exclusion search. A candidate is hidden if any comma-separated exclusion term matches.
- **Match stage bulkhead size** — only shows engines whose `top` attach node size matches the `top` node of the engines already on that stage.
- **Ignore monopropellant** — hides any candidate whose mass-bearing propellant includes MonoPropellant.

---

## 5. Using Planning

Planning sizes a stage that doesn't exist yet.

### Step 1 — Enter your targets

| Input | Meaning |
|---|---|
| Target delta-v | Interpreted as **vacuum** delta-v |
| Minimum TWR | Rejection threshold |
| Payload | Mass sitting on top of this stage |
| Planet / body | Sets TWR gravity automatically |
| Altitude | Only affects the atmospheric columns |
| Max engine count | Largest cluster to try |
| Tank structural ratio | See below |

You may type a **future stage number** here that's higher than your craft's current maximum — that's the point of the mode. Planning's top row is deliberately simpler than Analyze Existing's; it has no Pick Stage, Craft max, or Craft wet controls.

The **Planet / body** selectors on both Planning and Analyze Existing use the same shared ComboBox dropdown as Mission Planner. Selecting a different body immediately updates gravity/atmosphere-dependent calculations and clamps the altitude to that body's valid atmosphere range. ComboBox popup rows use the active button font, size, and style so the open dropdown matches normal VesselPlanner buttons.

> **[IMAGE PLACEHOLDER: `images/planning-inputs.png` — the Planning input panel filled in with a sample target]**

### Step 2 — Set the tank ratio

Planning has to guess tank dry mass before you've picked a tank, so it uses a **tank dry mass ÷ propellant mass** ratio. `0.125` is a reasonable stock-like default. If you select a stage that already has resource-bearing parts, the mod infers a ratio from them for you.

The solver loops on this: propellant mass depends on tank dry mass, and tank dry mass depends on propellant mass.

### Step 3 — Read the results and the tank table

Because Planning sizes the stage from **vacuum** delta-v, the stage geometry is fixed. Moving the altitude slider will **not** change propellant load, wet/dry mass, vacuum delta-v, tank requirements, or burn time. It only changes the atmospheric delta-v, atmospheric thrust, and atmospheric TWR shown for that fixed stage.

Below the engine results, the **Tanks needed** table has **Filter** and **Exclude** text fields. Both accept comma-separated terms. Filter terms use OR matching against any tank display name or internal part name in a suggestion; any matching Exclude term hides the entire suggestion. VesselPlanner only includes storage parts that have both a `top` and a `bottom` attach node; radial or one-ended storage parts are excluded from tank selection. Tank analysis evaluates sets containing one, two, or three different tank types. Every tank type in a set must share the same KSP bulkhead profile used for that suggestion. The table lists each matching tank set with a thumbnail for every tank type in the set, plus:

- each tank type and its count
- total number of tanks
- total tank dry mass
- excess capacity
- supplied resource capacity
- the shared bulkhead profile

> **[IMAGE PLACEHOLDER: `images/tanks-needed.png` — the Tanks needed table showing several tank options]**

---

## 6. Reading the candidate table

The list is capped at 300 pixels tall and scrolls, so growing the window won't let the engine list swallow the UI. Sorting and filter changes recalculate immediately.

| Column | What it is |
|---|---|
| **Engine** | KSP part thumbnail plus localized part title when available |
| **#** | How many engines in this candidate configuration |
| **Stage Wet t** | Mass of this stage only — no payload, no upper stages |
| **Start Mass t** | Full vehicle mass accelerated at ignition; this is what drives delta-v and TWR |
| **Atm Δv** | Delta-v at your selected body and altitude |
| **Vac Δv** | Vacuum delta-v |
| **ASL kN/eng** | Sea-level thrust of **one** engine |
| **Vac kN/eng** | Vacuum thrust of **one** engine |
| **TWR** | Atmospheric TWR, using **total** thrust of all engines |
| **Max TWR** | Vacuum-thrust TWR, sortable |
| **Isp** | Specific impulse |
| **Burn** | Burn duration |
| **Fuel** | Propellant required |
| **Δv/Fund** | Atmospheric delta-v divided by total engine cost |
| **Add** | Puts that engine on the editor cursor |

Note the deliberate split: the per-engine `kN` columns show one engine, while TWR uses all of them. The selected-result detail panel shows both per-engine and total thrust so you can see the multiplication.

> **[IMAGE PLACEHOLDER: `images/candidate-table.png` — the full candidate list with several engines ranked]**

### Sorting

Pick one optimization goal from the vertical toggle group:

- Lowest wet stage mass
- Lowest propellant
- Lowest engine cost
- Shortest burn
- Highest TWR
- Highest Isp

### Propellant display

Requirements are shown in KSP resource units and in mass. Where the resource definition supplies a volume, estimated physical tank volume in litres is shown too.

---

## 7. Adding parts to your craft

Three buttons place parts, all using KSP's normal editor spawning path:

- **Add** on any candidate row — places that engine on the cursor.
- **Add Engine** on the selected result — same thing for the highlighted candidate.
- **Add Tank** in the Tanks needed table — places **one** copy in normal Planning. While building a Stage-By-Stage stage, this tank action is labeled **Add to Stage** and records the complete selected tank set in the stage.

For multimode engines, both listed modes point at the same underlying part.

Optionally, the planner can close itself after a successful placement. Analyze Existing and Planning have **separate** preferences for this, and the window only closes if KSP actually put the part on your cursor.

> **[IMAGE PLACEHOLDER: `images/add-engine.png` — an engine attached to the cursor immediately after clicking Add]**

---

## 8. Mission Planner

Select **Mission Planner** at the top of the editor window to build an ordered mission-step list. It uses the same main-window skin and background as the other VesselPlanner editor modes. Each new step begins with a **Step type** choice: **Engines & Tanks** or **Subassemblies**. The two types are mutually exclusive.

Each row shows either an Engines & Tanks maneuver step or a Subassemblies step. Engines & Tanks rows show maneuver, body or route, required delta-v, and **ASL** or **VAC** basis. Subassemblies rows show their subassembly summary and no delta-v requirement. **Double-click a row** to edit that step. Row actions use **+▲** to insert above, **-▼** to insert below, **▲** and **▼** to reorder, and a red **✖** to delete. The bottom **Add New Step** button appends a new step. The bottom of the page shows the total delta-v of the Engines & Tanks steps.

For an **Engines & Tanks** step, select the maneuver and enter or accept its body/route, delta-v, and ASL/VAC basis. This step type does not contain subassemblies. For a **Subassemblies** step, the maneuver/delta-v controls are replaced by the saved-subassembly editor from `saves/<current save>/Subassemblies`. Add one or more entries, set a whole-number **Count** for each, and independently enable **Decoupler** per entry. Adding the same saved subassembly again increases its count. Each available subassembly shows its calculated wet mass; VesselPlanner calculates that mass from saved part masses, craft `modMass` adjustments, and stored resources. Per-copy decoupler mass comes from `GameData/VesselPlanner/decouplerMasses.cfg` using the saved subassembly root part's bulkhead profile(s). Unresolvable subassemblies remain unavailable for adding.

Mission Planner starts with **Unnamed Mission** as the initial mission name. Enter a different **Mission name** at the top of Mission Planner if desired. **Save** writes the mission to `GameData/VesselPlanner/PluginData/MissionPlans/<Mission name>.cfg`, using the mission name as the filename after replacing invalid filename characters. **Load** restores the mission name and the full ordered step list, including each step type and every Subassemblies-step entry, count, Decoupler selection, stored unit mass, and per-copy decoupler mass. Mission files saved before step types were introduced load as Engines & Tanks steps by default.

The maneuver entry window supports:

- Launch and Sub-Orbital Launch
- Orbit, Reentry, Landing, Splashdown
- Resource Transfer and Impact Asteroid
- Transfer To Another Planet
- Change Apoapsis, Change Both Pe And Ap, Change Inclination, Change Periapsis, Change Semi Major Axis
- Fine Tune Closest Approach
- Intercept Asteroid and Intercept Vessel
- Match Planes With Vessel and Match Velocities With Vessel
- Return From A Moon

For **Launch** and **Sub-Orbital Launch**, the body dropdown defaults from mission history: KSP's home body is used when there is no earlier Landing, otherwise the body from the most recent earlier **Landing** is selected. The launch body can then be changed to another body. Orbit/landing and orbital-change maneuvers select one body. For **Transfer To Another Planet**, select only the destination; the source is read-only and is taken from the immediately preceding maneuver (or from the destination of the preceding transfer). A transfer cannot be saved if the previous maneuver does not provide a body. **Return From A Moon** lists only rows marked `isMoon=true` in the delta-v table and automatically derives Needed Δv from that CSV. Maneuver dropdown entries use readable spaces (for example **Sub Orbital Launch**, **Transfer To Another Planet**, and **Return From A Moon**) while saved enum values remain unchanged. Every maneuver has an editable required delta-v and an **ASL/VAC** selection.

Mission Planner loads body and route information from:

```text
GameData/VesselPlanner/PluginData/DeltaVTables/<packName>.csv
```

The table format is:

```text
Origin,Destination,dV_to_low_orbit,ejection_dV,capture_dV,transfer_to_low_orbit_dV,total_capture_dV,dV_low_orbit_to_surface,ascent_dV,plane_change_dV,parent,isMoon,order
```

For **Return From A Moon**, VesselPlanner reads the selected moon's self row and uses `ascent_dV` (or `dV_to_low_orbit` if ascent is zero), then adds the `capture_dV` from the matching `parent -> moon` row as the reverse moon-escape leg. For example, the stock Mun value is 580 + 310 = 890 m/s and Minmus is 180 + 160 = 340 m/s.


When Mission Planner initializes, VesselPlanner detects the active planet pack with `PlanetPackHeuristics`. Known packs use the `PlanetPackKind` name; a single unknown custom pack uses its detected GameData folder name. Mission Planner then loads `<packName>.csv` from `DeltaVTables`; `Stock.csv`, `JNSQ.csv`, `GPP.csv`, and `RSS.csv` are included starter tables.

When a matching row exists, Launch/Sub-Orbital Launch fills `dV_to_low_orbit` for the selected launch body, Transfer To Another Planet fills `total_capture_dV`, and Landing/Splashdown fills `dV_low_orbit_to_surface`. The suggested value remains editable.

While a **Transfer To Another Planet** entry is open, VesselPlanner checks the system clipboard for transfer-planner text. Use the **Clipboard Δv** dropdown to choose **Ejection**, **Insertion**, or **Total**. The mapping is exact: **Ejection** reads `Ejection Δv:`, **Insertion** reads `Insertion Δv:`, and **Total** reads `Total Δv:`. Changing the selection immediately replaces Needed Δv with that selected value from the current matching clipboard block. Clipboard routes that do not match the current transfer are ignored. The dropdown is disabled when no usable clipboard transfer matches the current destination/route, and is enabled automatically when matching data is available. If it becomes disabled while open, VesselPlanner closes the popup through the ComboBox API.

---

## 9. Settings

Open **Settings** from the editor planner. Everything is saved to:

```
GameData/VesselPlanner/PluginData/VesselPlannerSettings.cfg
```

and restored next time you run KSP. The Settings window drags from any unused background area.

> **[IMAGE PLACEHOLDER: `images/settings-window.png` — the Settings window with its page tabs visible]**

### Engine Columns page

Turn any candidate table column on or off individually — Engine, count, Stage Wet, Start Mass, atmospheric and vacuum delta-v, atmospheric and vacuum thrust, cost efficiency, TWR, Max TWR, Isp, Burn, Fuel, and the row Add button. **Show All** and **Hide All** are one-click presets.

### Analyze Existing page

Controls which informational lines appear in the **Existing stage** pane: craft wet/dry mass, payload above stage, current and full stage propellant, KSP stage wet/dry/fuel masses, current engine mass, vehicle start/end mass, current burn, tank capacity and known tank volume, the Current Engines section, and stage-resource lines.

Simulation controls are never hidden by these settings. This page also holds the "close planner after Add" option for Analyze Existing.

### Planning page

Its own independent "close planner after Add / Add Engine / Add Tank" option.

### Filters page

Controls persistence of the text filters with four independent options: **Save engine include Filter**, **Save engine Exclude filter**, **Save tank include Filter**, and **Save tank Exclude filter**. All four are enabled by default. Each field is saved automatically to `VesselPlannerSettings.cfg` only while its own option is enabled. Turning one option off leaves that field's current-session text in place but clears only its persisted value; the companion include/exclude filter is unaffected. Existing 0.7.32 pair settings migrate automatically to the corresponding two per-field options.

### Appearance page

**Use solid backgrounds for all editor windows** gives the planner and the editor Settings window an opaque dark underlay while keeping normal KSP borders and title styling. Off by default. Flight windows use their own separate Flight Settings option.

Part-image rendering can also be tuned here:

- **ZoomFactor for icons** — static list-thumbnail zoom; default `0.8`
- **ZoomFactor for Rotating Images** — rotating hover-preview zoom; default `1.0`
- **Camera Yaw Degrees** — horizontal camera angle; range `0–180`, default `45`
- **Camera Pitch Degrees** — vertical camera angle; range `0–90`, default `20`
- **RotatingPreviewSize** — pre-rendered rotating-frame resolution in pixels; range `32`–`256`, default `100`
- **Degrees per frame** — rotating-preview playback rate control; range `1`–`180`, default `60`
- **Rotating Image Background** — combo-box choice for the enlarged rotating hover preview: `Transparent` (default), `Black`, `Dark Gray`, `Gray`, or `White`. Static list icons remain transparent.

Every numeric part-image setting has both an editable value and a slider. These values are saved in `VesselPlannerSettings.cfg`. Changes clear the thumbnail/rotating-preview caches immediately so newly rendered images use the new settings without restarting KSP. **Reset image settings** restores the defaults.

---

## 10. Flight telemetry plotter

Click the same toolbar button in flight to open the graph.

> **[IMAGE PLACEHOLDER: `images/flight-graph.png` — the telemetry graph mid-ascent with several series plotted]**

### Starting a recording

- **Start at Launch** can be armed while the vessel is in PRELAUNCH. When the vessel leaves PRELAUNCH, existing data is cleared and recording begins automatically. Once you're already flying, this button is disabled.
- **Reset** clears the current plot samples and stage markers.
- Sampling pauses with the game and resumes only after you unpause and the sample delay has elapsed.

### Sample interval

The **Sample** field sits at the top of the plotter toolbar. Default is **0.25 seconds**. The `−` and `+` buttons step it by 0.25 s, and you can type an exact value from 0.05 to 60 seconds. It sets the minimum real-time delay before the next sample is recorded.

### What can be plotted

- Surface velocity and orbital velocity
- Vertical speed
- Acceleration
- G-force
- Altitude ASL and altitude above surface
- Dynamic pressure (q)
- Vehicle mass
- Angle of attack
- Every resource carried by the active vessel
- Output from any stock `ModuleEnviroSensor` on the vessel

All available sources are recorded while plotting, so switching which series are visible can redraw data you already collected.

#
### Stage-By-Stage bulkhead profiles


When a new Stage-By-Stage plan starts, VesselPlanner initializes its payload/starting mass from the current editor vessel's full wet mass. If the editor is empty, the default is 5.0 t. Existing plans keep their saved mass.

When Stage-By-Stage mode is selected, the plan window opens left-aligned with the main VesselPlanner window and directly below the main mode buttons. Choosing **New Stage** opens its modal immediately to the right of the plan window, top-aligned and adjacent to the plan window's right edge.
With solid editor backgrounds enabled, both the Stage-By-Stage Plan and New/Edit Stage dialog use a **0.36** gray solid background shade, lighter than the main planner, to make the planning windows easier to distinguish.

The Stage-By-Stage window also has a **Select Mission Plan** button. Choose a saved Mission Planner file to show that mission in a left-hand sidebar. The Stage-By-Stage plan name is immediately initialized from the selected mission plan name and remains editable. Mission steps are displayed in **reverse order** so the last step is at the top. Selecting a mission does not change stages already in the current Stage-By-Stage plan. Clicking an **Engines & Tanks** step opens an Engines & Tanks stage with that maneuver's required delta-v copied into **Target Δv** and its ASL/VAC basis selected. Clicking a **Subassemblies** step opens a Subassemblies stage with every saved subassembly, Count, and Decoupler setting copied directly into the stage. A mission step therefore never creates a stage containing both propulsion parts and subassemblies. Clicking the same mission step again opens its existing linked stage for editing instead of creating a duplicate.
When you click **Calculate** while editing an existing stage, VesselPlanner sets the Planning body/environment to the **last body referenced in the selected or linked mission plan** before recalculating. If the last body-bearing maneuver is **Transfer To Another Planet**, the transfer destination is used.

The New/Edit Stage dialog can restrict both candidate engines and tank suggestions by KSP `bulkheadProfiles`. The **Bulkhead profiles** control uses the original inline multi-select list. Click the summary button to expand or collapse the list. A profile automatically inherited from the editor craft or previous stage is only a preset: the first different profile manually selected replaces that preset so a single choice is honored by itself. Further manual selections can then intentionally create a multi-profile selection. Use **Clear selection** to remove all profile restrictions and consider every profile. `srf` is listed first, and stack profiles include `size0`, `size1`, `size1p5`, `size2`, and larger sizes. The Planning Tanks table shows each tank's profiles in a sortable **Bulkhead** column. Older plans containing `BulkheadSize` values are translated automatically.

When **New Stage** is opened for the first stage and a vessel already exists in the editor, VesselPlanner presets the profile from the vessel's open lower bottom node, falling back to the top node if necessary. For later stages it presets from the previous stage engine's bottom node, again falling back to the top node.

New/Edit Stage begins with a **Stage type** choice. **Engines & Tanks** keeps the normal delta-v/TWR/engine/tank workflow and its stage-level **Add decoupler mass** toggle. It also provides **Side Boosters** and **Core burns too** layout toggles. **Core burns too** is shown on its own line below **Side Boosters**. When **Side Boosters** is enabled, **Radial decouplers** appears on the following line. These layout choices are saved with the stage and shown in the stage summary; they do not add inferred radial-decoupler mass because the number of side-booster stacks is not known from the stage solution alone. **Subassemblies** instead shows the saved KSP subassemblies for the current save. Add one or more subassemblies, set a whole-number **Count** for each, and independently enable **Decoupler** on each line. A selected decoupler is counted once per subassembly copy, and its reference mass comes from `GameData/VesselPlanner/decouplerMasses.cfg` using the subassembly root part's bulkhead profile(s). Subassembly mass plus any per-copy decoupler mass becomes the completed stage mass and is automatically included in the payload seen by every lower stage. The Subassemblies dialog is taller so the stage-mass and Add/Save/Cancel controls remain fully visible below the two lists. Plans saved before 0.7.22 load their stages as **Engines & Tanks** stages. Stage 2 and later continue to show **Previous stages mass**.

## Flight settings

The Flight Data window has a fixed height and is horizontally resizable from the grab handle centered on its right edge. Drag the handle left or right to change only the window width. The handle keeps ownership of the mouse for the entire resize so the window itself does not jump or drift during the drag. Vertical resizing of the Flight Data window is disabled; the separate Flight Plot Settings window remains resizable in both directions. The graph vertical grid lines keep fixed horizontal positions during resizing. Expanding the graph adds additional grid lines only on the right; contracting it removes only grid lines that no longer fit. The vertical grid keeps fixed spacing. The **Time labels** setting can place elapsed-time labels on **Every line**, **Every other**, or **Every third** vertical grid line; **Every third** is the default. Labels remain centered on their selected fixed grid positions, including positions near the minimum-width boundary.

Open **Settings** from the graph to choose series. Flight data, resources, and sensors are grouped separately, each with All/None controls. The table shows **Plot**, **Sensor**, **Max**, and **Units**. The main plotted list also shows **Current** and **Max** per series. Available vessel resources and sensor outputs refresh automatically; the manual **Refresh available resources/sensors** button has been removed.

Use **Time labels** to choose **Every line**, **Every other**, or **Every third** vertical grid line. The choice is saved in `VesselPlannerSettings.cfg` as `ElapsedTimeLabelGridInterval`; **Every third** is the default.

Enable **Use solid background for Flight Data window** to give the flight graph an opaque dark background while preserving the active KSP/alternate window skin, title, and border. The opaque background is drawn behind the single Flight Data window so resizing does not produce a flickering duplicate/ghost window. The setting is saved in `VesselPlannerSettings.cfg` as `SolidFlightWindowBackground` and is enabled by default for new configurations; an existing saved value is preserved.

> **[IMAGE PLACEHOLDER: `images/flight-settings.png` — the flight Settings series selection table]**

### How the graph scales

- The graph fills left to right with fixed horizontal sample spacing. Once it hits the right edge, the window scrolls and old data moves left.
- Elapsed time since plotting started is labelled along the bottom as `m:ss`, or `h:mm:ss` on long runs. The time labels and vertical grid lines keep fixed horizontal positions when the graph width changes; extra positions are added only on the right as space becomes available. **Time labels** can be shown on every vertical line, every other line, or every third line; every third is the default.
- Stage activations appear as orange dashed vertical lines labelled `Stage N`. They scroll with the data and are cleared by Reset or Start at Launch.
- **Altitude** series use a configurable **Altitude chart top** with a zero baseline. By default that's the launch body's `atmosphereDepth`, or 10% of body diameter for an airless body. Altitude intentionally stores no Max value because this setting controls its scale.
- **Every other series** scales to a per-vessel historical maximum recorded across all launches and reverts in the current save. The lower bound follows currently visible data, so series with wildly different units can share one graph.

Maxima persist in:

```
GameData/VesselPlanner/PluginData/FlightSensorMaxima.tsv
```

Reset deliberately preserves them. Use **Clear Max Values for Vessel** to wipe the current craft's non-altitude maxima without clearing the current plot.

### Exporting

**Export CSV** writes the selected series as comma-separated text. **Export PNG** writes an image of the currently visible graph. The PNG files land in `Screenshots/VesselPlanner`., CSV files land in `VesselPlanner/PluginData/CSV`.  The location for both are configurable in the settings page

---

## 11. How the numbers are calculated

You don't need this section to use the mod, but it explains why the numbers are what they are.

### Which engines appear as candidates

Candidates come from KSP's loaded part database **after** the editor's `ExcludeFilters` are applied. If another mod hides an engine through KSP's editor filter API, it's hidden here too. The currently selected editor category is *not* applied.

### Thrust and TWR

- TWR gravity comes from the selected body's surface gravity automatically.
- For conventional rockets, vacuum thrust is the configured `ModuleEngines.maxThrust`; atmospheric thrust follows the ratio of atmospheric to vacuum Isp from the engine's atmosphere curve.
- Engines whose fuel flow genuinely depends on atmosphere or velocity use KSP's own flow calculation, with a sanity fallback.
- Analyze Existing honours each installed engine's editor thrust limiter and shows individual ASL and vacuum thrust per engine.
- Analyze Existing also carries the mass of loaded stage resources the candidate engine *cannot* burn, so they drag down TWR and delta-v exactly as they would in game.

### Mass boundaries

Analyze Existing prefers KSP's own `VesselDeltaV` / `DeltaVStageInfo` results, which respect the stock staging and fuel-flow model:

- **Stage Wet t** uses `stageMass`
- **Start Mass t** uses the full vehicle `startMass`
- Retained payload is `startMass − stageMass`

Installed engine mass is removed using `DeltaVPartInfo.dryMass` before candidate engine mass is added, so the existing engine isn't counted twice.

The branch scanner still identifies the stage's resource inventory, tank volume, and engine list, and acts as a fallback when the stock delta-v simulation isn't ready yet. Parts found by the branch scan are excluded from the payload-above-stage sum so they aren't double counted.

### Burn time

Mass flow is derived from vacuum thrust and vacuum Isp rather than `getMaxFuelFlow`:

```
mdot = F / (Isp × g0)
t    = mPropellant × Isp × g0 / F
```

Burn rate is therefore independent of the selected body and altitude. In Analyze Existing, `mPropellant` is the mass of the propellant mixture the candidate engine can *actually* burn, limited by what's loaded in the stage — KSP's whole-stage `fuelMass` is not substituted in. This stops unrelated stage resources from inflating burn duration.

The rocket equation uses KSP's standard gravity constant, 9.80665 m/s².

### Resources

The full propellant list is read from each `ModuleEngines` / `ModuleEnginesFX` module. Nothing about LiquidFuel/Oxidizer or any other mixture is assumed.

- Every resource with a positive engine ratio is kept in the requirement set, including intake and electrical resources.
- Resources that contribute to Isp and have mass drive the rocket equation.
- `ignoreForIsp` resources stay visible as auxiliary requirements but don't define mass flow.
- Physical volume comes from `PartResourceDefinition.volume`. There is no hard-coded volume table, so stock and modded resources both work from whatever definitions your install has loaded.

---

## 12. Known limits

The planner is a good heuristic, not a full flight simulator. These are not currently modelled:

- Dynamic tank type/configuration switching by RealFuels, B9, MFT-style modules after the prefab loads.
- Jet velocity curves and intake-air flight-state modelling beyond static pressure-based Isp and thrust.
- Engine variants controlled by arbitrary third-party modules beyond loaded `ModuleEngines`.
- Full MechJeb-style fuel-flow simulation for asparagus, crossfeed, and drop-tank arrangements.

For unusual modded fuel-flow systems, use **Planning** with a manually entered payload mass — it doesn't depend on the stage scan being right.

---

## 13. Version notes

| Version | Change |
|---|---|
| 0.2 | Tanks needed table; Add / Add Engine / Add Tank editor placement; bulkhead-size filtering; Ignore monopropellant |
| 0.3.6 | Existing-stage simulation uses current resource amounts, not max capacity; Planning fixed to vacuum Δv so altitude no longer resizes the stage |
| 0.3.7 | Candidate list capped at 300 px and scrollable; immediate filter recalculation; Δv/Fund column |
| 0.4.12 | Sample interval moved to the flight toolbar with ± 0.25 s buttons; stage markers driven by `GameEvents.onStageActivate` |
| 0.4.13 | Default flight sample interval returned to 0.25 s |
| 0.5.0 | Generic engine-resource support; density and volume read from `PartResourceDefinition` |
| 0.5.7 | Stock `startMass`/`endMass` boundaries; burn time from thrust and Isp instead of `getMaxFuelFlow`; elapsed-time labels on flight graph; planner starts closed |
| 0.5.13 | Burn duration computed from propellant the candidate engine can actually use |
| 0.5.14–0.5.19 | Stage clamping in Analyze Existing; wider mass/thrust columns; column and info-line visibility settings; persistent draggable Settings; per-mode close-after-Add; Alt+P removed; Simulation environment split into its own pane; Appearance opaque-window option |
| 0.5.25 | Corrected the scene-switch callback signature to `GameEvents.FromToAction<GameScenes, GameScenes>` |
| 0.7.46 | Added larger high-resolution hover previews for engine/tank thumbnails in all part lists |
| 0.7.44 | Added part thumbnails to Planning/Analyze engine lists and Planning tank-set rows |
| 0.7.42 | Fixed invisible Stage-By-Stage part thumbnails by explicitly constructing thumbnail transparency |
| 0.7.41 | Added part thumbnails to engine/tank rows in the Stage-By-Stage contents list |
| 0.7.40 | Added a bundled RSS Mission Planner delta-v table normalized to the current CSV schema |
| 0.7.39 | Added a bundled GPP Mission Planner delta-v table aligned with the current CSV schema and GPP map |
| 0.7.38 | Added a bundled JNSQ Mission Planner delta-v table and aligned it with the current CSV schema |
| 0.7.37 | Fixed the remaining EditorStageScanner CommonRoutines compile reference |
| 0.7.36 | Expanded CommonRoutines and removed duplicated reusable helpers across persistence, scanners, UI, databases, and solvers |
| 0.7.35 | Added shared CommonRoutines.FormatManeuver for Mission Planner and Stage-By-Stage |
| 0.7.34 | Tank Filter/Exclude share one row; Stage-By-Stage tank action renamed Add to Stage |
| 0.7.33 | Split engine/tank include and Exclude persistence into four independent settings |
| 0.7.32 | Comma-separated engine/tank Filter and Exclude fields; independent automatic persistence settings |
| 0.7.31 | Mission transfer Source body box uses 30 px height; Stage-By-Stage plan name inherits the selected mission plan name |
| 0.7.30 | Taller New/Edit Stage dialog; Unnamed Mission default; red delete X; CSV-driven Return From A Moon delta-v; spaced maneuver dropdown labels |
| 0.7.29 | Put Core burns too on its own booster-layout line |
| 0.7.28 | Side-booster layout flags; ComboBox button typography; one-to-three-type same-profile tank sets; localized engine titles |
| 0.7.27 | Mission Planner steps are mutually exclusive Engines & Tanks or Subassemblies; mission-linked stages now match the selected step type |
| 0.7.26 | Unified Mission Planner subassembly selection with the Stage-By-Stage available/selected list workflow |
| 0.7.25 | Renamed Assemblies to Subassemblies in the UI; grouped Mission Planner and Stage-By-Stage at the left of the mode row |
| 0.7.23 | Mission Planner multi-assembly counts/decouplers; taller Subassemblies stage dialog |
| 0.7.22 | Added Engines & Tanks vs Subassemblies stage types with per-subassembly count and decoupler options |
| 0.7.21 | Added spacing between the Mission Planner Assembly Add toggle and selector |
| 0.7.20 | Mission Planner assembly selection and Stage-By-Stage assembly mass integration |
| 0.7.18 | Editing a stage sets Planning to the mission plan's last body before Calculate |
| 0.7.17 | Taller Stage-By-Stage mission buttons; tank text filter; tank candidates require both top and bottom attach nodes |
| 0.7.16 | Landing history defaults; mission-linked Stage-By-Stage editing; left-aligned mission list; single-profile Bulkhead preset fix |
| 0.7.15 | Mission Planner Save/Load with mission names; Stage-By-Stage mission selection, reverse-order sidebar, and click-to-prefill New Stage |
| 0.7.14 | Restored the original inline Bulkhead profiles multi-select list and replaced all main Word-manual images with descriptive image-placeholder tags |
| 0.7.13 | Fixed the Bulkhead ComboBox not appearing by replacing the second-modal-window approach with a late/topmost normal popup while the stage dialog temporarily becomes non-modal |
| 0.7.12 | Attempted New/Edit Stage Bulkhead ComboBox layering/input fix using a modal ComboBox popup |
| 0.7.11 | Analyze Existing Planet selector and New/Edit Stage Bulkhead profiles now use the shared Mission Planner ComboBox |
| 0.7.10 | Planning Planet selector now uses the shared Mission Planner ComboBox dropdown |
| 0.7.9 | Mission Planner detects the active planet pack and loads the matching DeltaVTables CSV |
| 0.7.8 | Fixed Mission Planner ComboBox compile error when disabling the Clipboard Δv selector |
| 0.7.7 | Clipboard Δv selector disabled when no matching transfer data exists for the selected destination/route |
| 0.7.6 | Corrected Clipboard Δv mapping: Ejection reads Ejection Δv and Insertion reads Insertion Δv |
| 0.7.5 | Add New Maneuver text button restored; Clipboard Δv choice now immediately refreshes Needed Δv |
| 0.7.4 | Launch body default is editable; +▲/-▼ insert controls; transfer clipboard Ejection/Insertion/Total selector |
| 0.7.3 | Launch body derives from the home body or most recent Landing; Mission Planner row actions use icon buttons; Reload Delta-v Table removed |
| 0.7.2 | Transfer To Another Planet can import matching `Total Δv` values from transfer-planner text on the clipboard |
| 0.7.1 | Mission Planner rows can be double-clicked for editing; transfer source bodies now come from the preceding maneuver |
| 0.7.0 | Added Mission Planner with ordered maneuver rows, CSV-driven body/route data, and automatic delta-v suggestions |
| 0.6.43 | Added selectable time-label frequency: every vertical grid line, every other line, or every third line |
| 0.6.42 | Removed the manual resource/sensor refresh button and corrected elapsed-time labels so every interval stays exactly three grid columns apart |
| 0.6.41 | Every third fixed vertical Flight Data grid line now aligns with an elapsed-time marker; resizing still only adds or removes positions on the right |
| 0.6.40 | Flight Data vertical grid lines now stay at fixed horizontal positions during horizontal resizing; wider graphs add lines only on the right |
| 0.6.36 | Flight Data window now resizes horizontally only from a centered right-edge grab handle; its height is fixed |
| 0.6.35 | Solid Flight Data background now renders behind the single real window, eliminating resize-time duplicate-window flicker |
| 0.6.34 | Flight Data resize grip no longer hands the drag to the window; bottom-docked windows remain bottom-aligned after resizing; solid Flight Data background now defaults on |
| 0.6.33 | Replaced the Word instruction manual with the illustrated screenshot-enhanced edition and documented the current Flight Data solid-background setting |
| 0.6.32 | Added a persistent solid-background option for the Flight Data window in Flight Plot Settings |
| 0.6.31 | Updated the repository Jenkins/build configuration for VesselPlanner release packaging |
| 0.6.30 | Main editor window maximum horizontal resize width increased to 1850 px (minimum remains 1150 px) |
| 0.6.29 | Main editor window horizontally resizable from 1150–1280 px; engine-list header area increased to 34 px |
| 0.6.17 | Fixed New/Edit Stage Tab navigation by polling Tab/Shift+Tab outside the modal IMGUI event stream |
| 0.6.16 | Added Tab/Shift+Tab keyboard navigation to the New/Edit Stage numeric entry fields |

**Editor window width and engine table scrolling:** The main VesselPlanner window is horizontally resizable from **1150 to 1850 pixels**. Drag the grip on the right edge to change the width; the selected width is remembered. If the candidate engine table is wider than the available space, use the horizontal scrollbar at the bottom of the engine list. The sortable column-heading buttons scroll horizontally with the engine rows and remain aligned with their columns. The synchronized header area is **34 pixels** tall.
