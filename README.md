# VesselPlanner for KSP1

VesselPlanner is a planning and telemetry mod for **Kerbal Space Program 1.12.x**. It helps you analyze an existing stage, design a future stage, build a vessel one stage at a time, and record flight telemetry.

A Word version of this instruction manual is included at `Manual/VesselPlanner-Manual.docx`. Screenshot locations are marked with descriptive image-placeholder tags.

A detailed Stage-By-Stage walkthrough is included at `Manual/VesselPlanner-Stage-by-Stage-Tutorial.docx`.

### 0.7.46 part-thumbnail hover preview

Hovering over any engine or tank thumbnail in Planning, Analyze Existing, or the Stage-By-Stage build list now shows a larger 160 px preview. The hover image is rendered into a separate 192 px cache entry so it stays sharper than an enlarged copy of the normal 64 px list thumbnail.

### 0.7.44 Planning / Analyze list thumbnails

Planning and Analyze Existing now show cached KSP part thumbnails in their candidate-engine rows. Planning tank-set rows show a thumbnail for each tank type in the set, and Analyze Existing's Current engines table shows the installed engine part image.

### 0.7.42 Thumbnail visibility fix

Stage-By-Stage engine/tank thumbnails now build their transparency explicitly from an opaque chroma-key render, avoiding invisible textures when KSP's editor-icon shader leaves the render target alpha at zero.

### 0.7.41 Stage-By-Stage part thumbnails

Engine and tank rows in the Stage-By-Stage stage contents list now show a small thumbnail of the KSP part beside the line. The thumbnail is generated from KSP's editor icon prefab and cached for reuse.

### 0.7.40 bundled RSS delta-v table

Added `GameData/VesselPlanner/PluginData/DeltaVTables/RSS.csv`, normalized for VesselPlanner's self-row/route-row model. The supplied RSS data now uses real parent-body origins for moon routes, complete interplanetary transfer totals, integer sort orders, and corrected body names. Geostationary/L1/L2 pseudo-destinations are intentionally omitted so they do not appear in Mission Planner body lists.

### 0.7.39 bundled GPP delta-v table

Added `GameData/VesselPlanner/PluginData/DeltaVTables/GPP.csv`, aligned with VesselPlanner's self-row/route-row CSV model and the bundled GPP Release 1.6.0 delta-v map. GPP installations detected by `PlanetPackHeuristics` can now load the matching table automatically.

### 0.7.38 bundled JNSQ delta-v table

Added `GameData/VesselPlanner/PluginData/DeltaVTables/JNSQ.csv`, structured for VesselPlanner's current self-row/route-row CSV model. JNSQ installations detected by `PlanetPackHeuristics` can now load a bundled table automatically.

### 0.7.37 CommonRoutines compile fix

Fixed the remaining unqualified `GetDryPartMass` call in `EditorStageScanner` after the 0.7.36 CommonRoutines refactor.

### 0.7.36 shared common-routines cleanup

`VesselPlanner.Core.CommonRoutines` now owns the reusable helper logic that had been duplicated across the UI, persistence, scanner, database, and solver code. This is an internal refactor: plan formats and user-facing behavior are unchanged. Unity lifecycle callbacks and cache-specific reload methods remain in their owning classes because they depend on class state.

### 0.7.35 shared maneuver formatter

`FormatManeuver` is shared through `VesselPlanner.Core.CommonRoutines`, so Mission Planner and Stage-By-Stage use the same readable maneuver labels.

### 0.7.34 tank-filter layout and Stage-By-Stage tank action

The tank **Filter** and **Exclude** fields now share one row, matching the engine filter layout. While a Stage-By-Stage stage is open, every tank suggestion action button is labeled **Add to Stage** (including mixed tank sets); outside Stage-By-Stage the existing **Add Tank** / **Add Set** labels remain.

### 0.7.33 individual filter persistence

**Settings → Filters** now provides four independent persistence toggles: engine include Filter, engine Exclude, tank include Filter, and tank Exclude. Each text field saves/restores independently. Turning one persistence option off clears only that field's saved value while leaving the current-session text and the other filter in the same category unchanged. Existing 0.7.32 pair settings migrate automatically.

### 0.7.32 include/exclude filter lists and persistence

Engine and tank text filters now have separate **Filter** and **Exclude** fields. Each accepts multiple comma-separated terms: Filter terms use OR matching, while any matching Exclude term removes the candidate. Filter values are saved automatically while their persistence option is enabled. **Settings → Filters** independently controls whether the engine Filter/Exclude pair and tank Filter/Exclude pair are restored across KSP sessions.

### 0.7.31 Mission/Stage-By-Stage naming and Source body sizing

The Add/Edit Mission Step transfer Source body display now uses `GUILayout.Height(30)`. When a saved Mission Planner plan is selected in Stage-By-Stage, the Stage-By-Stage plan name is initialized from the mission plan name and remains editable.

### 0.7.30 Mission Planner polish and moon-return delta-v

The New/Edit Stage dialog is slightly taller. Mission Planner now starts as **Unnamed Mission**, its delete X is red, maneuver dropdown labels are human-readable with spaces, and **Return From A Moon** automatically loads its Needed Δv from the active delta-v CSV using the moon ascent value plus the reverse moon-escape leg derived from the parent-to-moon capture value.

### 0.7.29 booster-layout spacing

The Stage-By-Stage booster-layout controls are now stacked vertically: **Side Boosters** first, **Core burns too** on the next line, and conditional **Radial decouplers** below that.

### 0.7.28 booster layout, tank sets, and localized engines

Stage-By-Stage Engines & Tanks stages now include **Side Boosters** and **Core burns too** layout toggles. When Side Boosters is enabled, **Radial decouplers** is also available. These choices are saved with the stage as layout annotations. Tank analysis now evaluates sets of up to three different tank types as long as every type shares one KSP bulkhead profile. ComboBox dropdown text matches the normal button font, and engine names use KSP localization when available.

### 0.7.27 Mission Planner step types

Mission Planner steps now use the same mutually exclusive type model as Stage-By-Stage: choose **Engines & Tanks** for a maneuver/Delta-v step or **Subassemblies** for a saved-subassembly step. A step cannot contain both. Subassemblies steps use the same available/selected list, Count, and per-copy Decoupler controls, and they open directly as Subassemblies stages when used from Stage-By-Stage.

### 0.7.26 Mission Planner subassembly workflow

The Mission Planner Subassemblies step editor uses the same available/selected two-list workflow as Stage-By-Stage Subassemblies stages, including the same list sizing, per-line **Count**, per-line **Decoupler**, duplicate-add count incrementing, Remove behavior, mass details, and validation.

### 0.7.25 terminology and mode-button layout

Mission Planner and Stage-By-Stage now use **Subassembly/Subassemblies** consistently in the UI to match KSP terminology. The **Mission Planner** and **Stage-By-Stage** mode buttons are grouped at the left edge of the main window, with a clear gap before **Analyze Existing** and **Planning**.


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

The editor window has four operating modes:

- **Mission Planner** — build an ordered maneuver list and estimate required delta-v from a planet-pack CSV table.
- **Stage-By-Stage** — build and save a complete vessel plan one stage at a time.
- **Analyze Existing** — inspect a stage already on the vessel and compare replacement engines.
- **Planning** — design a stage that does not yet exist.

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
  Planning and Analyze Existing use the same shared ComboBox-style dropdown as Mission Planner for this selector.
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

The **Tanks** pane shows tank sets that can hold the required propellant. VesselPlanner evaluates sets containing one, two, or three different tank types; every tank type in a set must share the same KSP `bulkheadProfiles` value used for that suggestion. Use **Filter** and **Exclude** above the table to narrow the list by tank display or internal part name. Each field accepts comma-separated terms. Filter terms are OR matches; if any Exclude term matches any tank in the set, that suggestion is hidden. Only storage parts that have both a `top` and a `bottom` attach node are offered as tank candidates.

Each row shows information such as:

- tank set (including the count of each tank type)
- total number of tanks
- total dry mass
- excess capacity
- total supplied capacity
- shared KSP bulkhead profile

Click a tank-set row to select it. The selected set is highlighted and is also shown beneath the selected engine name.

> **[IMAGE PLACEHOLDER: Tanks pane with one tank row selected]**

### Step 6 — Add parts

Depending on what you are doing, use:

- **Add** — selects the engine part for placement in the editor.
- **Add Engine** — selects the currently highlighted engine for placement.
- **Add Tank** — for a one-type suggestion, selects one copy of that tank for placement.
- **Add Set** — for a mixed suggestion, starts placement with the first tank type and reports the complete set/counts to place.

When a Stage-By-Stage stage is open, the tank-row action is labeled **Add to Stage** for both one-type and mixed tank sets and records that complete tank set in the current stage. **Add Engine & Tanks** captures the selected engine and complete selected tank set together.

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

The **Planet / body** selector in Analyze Existing now uses the same shared ComboBox dropdown as Planning and Mission Planner. Changing the body still clamps altitude to the valid atmosphere range and immediately recalculates the simulation environment.

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


---

## 7. Mission Planner

Select **Mission Planner** at the top of the editor window to build an ordered mission maneuver list. The page uses the same VesselPlanner window background and skin as the other editor modes.

Each mission row shows the maneuver, selected body or route, optional subassemblies, required delta-v, and ASL/VAC basis. **Double-click a row** to edit that maneuver in the entry window. Row actions use compact controls: **+▲** inserts above, **-▼** inserts below, **▲** and **▼** reorder the row, and **✖** deletes it. The bottom **Add New Maneuver** button appends a new maneuver.

Each maneuver can optionally add saved KSP **Subassemblies**. Enable **Add** in the maneuver-entry window to reveal the same available/selected subassembly workflow used by Stage-By-Stage. Add one or more subassemblies, set a whole-number **Count** for each selected line, and independently enable **Decoupler** on each line. Adding the same subassembly again increases its count. VesselPlanner reads `.craft` files from `saves/<current save>/Subassemblies`, shows the calculated wet mass beside each subassembly, and uses the root part's bulkhead profile(s) to obtain a per-copy decoupler reference mass from `GameData/VesselPlanner/decouplerMasses.cfg`. If Count is 3 and Decoupler is enabled, that mission step carries three subassembly copies and three decouplers. The stored mission-step subassembly mass is the sum of every selected copy plus every selected decoupler. The subassembly mass calculation includes saved part masses, craft `modMass` adjustments, and stored resources. Subassemblies whose mass cannot be resolved remain unavailable for adding.

Enter a **Mission name** at the top of the page. **Save** writes the maneuver list to `GameData/VesselPlanner/PluginData/MissionPlans/<Mission name>.cfg` (invalid filename characters are replaced safely), and **Load** opens a list of saved mission plans. Loading restores the mission name and every maneuver's body/route, subassembly entries/counts/decouplers, required delta-v, and ASL/VAC basis. Mission files created by 0.7.20/0.7.21 with one subassembly are converted to a one-entry subassembly list when loaded.

The maneuver dropdown uses readable labels with spaces, including **Sub Orbital Launch**, **Resource Transfer**, **Transfer To Another Planet**, and **Return From A Moon**. Internally, the existing maneuver enum values are unchanged so saved missions remain compatible.

Body selection follows the maneuver. **Launch** and **SubOrbitalLaunch** default their body selector to KSP's home body when there is no earlier Landing, or to the body from the most recent earlier **Landing** maneuver; that default remains editable so another body can be selected. Other body-specific maneuvers select one body, and **Return From A Moon** selects a moon. For **TransferToAnotherPlanet**, the destination is selected but the source is read-only and comes from the immediately preceding maneuver. If that preceding maneuver is also a transfer, its destination is used as the source. A transfer therefore requires a preceding maneuver with a body. Every maneuver has an editable required delta-v and an **ASL** or **VAC** basis.

Mission Planner loads its body/route data from:

```text
GameData/VesselPlanner/PluginData/DeltaVTables/<packName>.csv
```

The CSV columns are:

```text
Origin,Destination,dV_to_low_orbit,ejection_dV,capture_dV,transfer_to_low_orbit_dV,total_capture_dV,dV_low_orbit_to_surface,ascent_dV,plane_change_dV,parent,isMoon,order
```

When Mission Planner initializes, VesselPlanner detects the active planet pack with `PlanetPackHeuristics`. Known packs use the `PlanetPackKind` name (for example `Stock`, `JNSQ`, `RSS`, or `OPM`); a single unrecognized custom pack uses its detected GameData folder name. Mission Planner then loads `<packName>.csv` from `DeltaVTables`. The included starter tables are `Stock.csv`, `JNSQ.csv`, `GPP.csv`, and `RSS.csv`.

When a matching row is available, Launch/Sub-Orbital Launch loads `dV_to_low_orbit` for its selected launch body, Transfer To Another Planet loads `total_capture_dV`, and Landing/Splashdown loads `dV_low_orbit_to_surface`. **Return From A Moon** loads the moon self-row's `ascent_dV` (falling back to `dV_to_low_orbit`) plus the matching parent-to-moon row's `capture_dV`, which represents the reverse moon-escape leg. The loaded value can always be edited manually.

For **Transfer To Another Planet**, VesselPlanner also watches the system clipboard while the maneuver entry window is open. A **Clipboard Δv** dropdown selects **Ejection**, **Insertion**, or **Total**. The choices map directly to the matching clipboard lines: **Ejection** reads only `Ejection Δv:`, **Insertion** reads only `Insertion Δv:`, and **Total** reads only `Total Δv:`. Changing the selection immediately reloads that value into Needed Δv. A matching clipboard value takes precedence over the CSV suggestion; unrelated clipboard routes are ignored. The **Clipboard Δv** dropdown is disabled when the clipboard does not contain usable transfer data for the currently selected destination/route, and it enables automatically when matching data is present. The disabled state now closes any already-open clipboard popup through the ComboBox API, avoiding direct access to ComboBox internals.

## 8. Creating a Stage-By-Stage Plan

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
- a **Select Mission Plan** button
- list of planned stages

Use **Select Mission Plan** to choose a mission saved from Mission Planner. The selected mission is displayed in a dedicated, **left-justified** list along the **left** side of the Stage-By-Stage window, with the mission's last maneuver at the top and its first maneuver at the bottom. The maneuver buttons are taller to make multi-line mission entries easier to read and click. Selecting a mission does not replace or clear the current Stage-By-Stage plan.

Click a mission line to open **New Stage** for that maneuver. VesselPlanner copies the maneuver's required delta-v and ASL/VAC basis and also copies every subassembly entry assigned to the mission step, including each count and Decoupler selection. In **Engines & Tanks** mode the dialog shows a read-only Mission subassemblies summary and includes the complete subassembly-plus-decoupler mass as fixed stage dry mass during Calculate. The linked stage list shows every counted subassembly and its matching decoupler rows before the propulsion parts. The stage is linked to the mission-plan name, mission step number, and maneuver; each stage row shows a second line such as **Mission step 4: Landing**. If that mission step already has a linked stage, clicking the mission line opens the existing stage in **Edit Stage** instead of creating a duplicate. Because the mission subassemblies and their decouplers are part of the completed stage wet mass, they are automatically carried into the payload of every lower stage.
When **Calculate** is clicked from **Edit Stage**, VesselPlanner sets the Planning body to the **last body referenced in the selected or linked mission plan** before solving the stage. If the final body-bearing maneuver is an interplanetary transfer, its destination body is used.

When you start a new Stage-By-Stage plan, **Payload (t)** is initialized automatically:

- if a vessel is already in the editor, VesselPlanner uses that vessel's current full wet mass;
- if the editor is empty, VesselPlanner uses **5.0 t**.

A plan that already contains stages keeps its existing payload mass when you return to Stage-By-Stage mode.

### Step 2 — Create a stage

Click **New Stage**.

The **New Stage** dialog opens immediately to the right of the Stage-By-Stage Plan window, with both top edges aligned and the dialog's left edge adjacent to the plan window's right edge.
The New Stage/Edit Stage dialog uses the same **0.36** gray solid background shade as the Stage-By-Stage Plan.

The New Stage dialog is modal. Other VesselPlanner windows cannot be used until you finish or cancel the stage.

First choose the **Stage type**:

- **Engines & Tanks** — the normal propulsion stage workflow. Enter **Target Δv**, **Δv basis**, **Minimum TWR**, **Max engines**, **Additional Cargo Mass**, optional **Add decoupler mass**, and optional **Bulkhead profiles**, then click **Calculate**.
- **Subassemblies** — a non-solver stage made from one or more saved KSP subassemblies. The dialog lists the current save's subassemblies and their calculated wet masses. Click **Add** for each one you want, set the **Count**, and independently enable **Decoupler** for each subassembly line. Click **Add Stage** (or **Save Stage** while editing) when finished. The Subassemblies New/Edit Stage dialog is taller so the stage-mass and action controls remain fully visible below the two lists.

**Additional Cargo Mass** is non-propellant dry mass carried by an Engines & Tanks stage. It is separate from the vessel payload.

For an Engines & Tanks stage, **Add decoupler mass** adds one stage decoupler using the selected stack profile. Engines & Tanks stages also have **Side Boosters** and **Core burns too** layout toggles. When **Side Boosters** is enabled, **Radial decouplers** appears as an additional toggle. These booster-layout choices are saved with the stage and shown in its summary; they describe the intended arrangement and do not add an inferred radial-decoupler mass. For a Subassemblies stage, each subassembly line has its own **Decoupler** toggle. If the count is 3 and Decoupler is enabled, the stage includes three decouplers. The reference mass comes from `GameData/VesselPlanner/decouplerMasses.cfg` using the saved subassembly root part's bulkhead profile(s); when several profiles match, the largest matching value is used.

For Stage 2 and later, the dialog also shows **Previous stages mass**. This is the original plan payload plus the full wet mass of all previously planned stages, including Subassemblies stages and their selected decouplers, and it is automatically used as the payload for the new stage.

In Engines & Tanks mode, Tab moves through the numeric entry fields; Shift+Tab moves backward.

> **[IMAGE PLACEHOLDER: New Stage dialog with all entry fields visible]**

### Step 3 — Choose bulkhead profiles if needed

VesselPlanner presets the Bulkhead Profiles selection when a new stage is opened:

- For the first stage, if a vessel already exists in the editor, the profile comes from the vessel's open lower `bottom` node; if that part has no bottom node, its `top` node is used.
- For later stages, the profile comes from the selected engine in the previous stage, using that engine's `bottom` node and falling back to its `top` node.
- If no applicable node can be found, no profile is preselected.

The **Bulkhead profiles** control uses the original inline multi-select list. Click the summary button to expand or collapse the list, then toggle profiles directly. An automatically preset profile is only a default: the first different profile you manually select **replaces** that preset, so choosing one profile filters only to that profile. After that first manual choice you may select additional profiles intentionally for a multi-profile stage. The list stays open while selections are changed. Choose **Clear selection** to remove all profile restrictions and consider every profile. New/Edit Stage remains modal while this inline list is open.

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

An **Engines & Tanks** stage lists any mission subassemblies first (including per-copy mission decouplers), then selected **tanks**, the optional stage **decoupler**, and selected **engines**. A **Subassemblies** stage lists each subassembly with its quantity and mass; when that line's Decoupler option is enabled, a matching decoupler line is shown with the same quantity. All of these subassembly/decoupler masses are part of the completed stage wet mass used as payload for all lower stages.

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
- Planned engine/tank parts receive an **Add** button for placing them in the editor. Subassembly stages remain listed with their quantities/decouplers and are placed using KSP's normal saved-subassembly workflow.
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

## 9. Editor Settings

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

### Filters

Engine and tank text filtering each provide a **Filter** field and an **Exclude** field. Both accept comma-separated terms. Filter terms use OR matching; any matching Exclude term hides the candidate. Text changes take effect immediately. The Filters settings page independently controls persistence for engine include, engine Exclude, tank include, and tank Exclude. All four persistence options are enabled by default.

### Appearance

Appearance options include:

- **KSP skin** or **Alternate skin**
- **Use solid backgrounds for all editor windows**

Solid backgrounds are enabled by default for new configurations. The setting applies to the main editor planner, Settings window, and Stage-By-Stage windows. Flight windows are controlled separately from Flight Settings.

---

# Flight Telemetry

## 10. Using the Flight Graph

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

## 11. Flight Settings and Export

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

## 12. Important Calculation Notes

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

## 13. Files Created by VesselPlanner

Typical persistent files are stored under:

```text
GameData/VesselPlanner/PluginData/
```

Important files and folders include:

```text
VesselPlannerSettings.cfg          Editor and flight settings
FlightSensorMaxima.tsv             Per-vessel telemetry maxima
Plans/                             Saved Stage-By-Stage plans
MissionPlans/                      Saved Mission Planner missions
CSV/                               Default CSV export folder
DeltaVTables/*.csv                 Mission Planner planet-pack body/route delta-v tables
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

## 14. Troubleshooting

### VesselPlanner does not appear

Check that:

- ToolbarController is installed.
- ClickThroughBlocker is installed.
- `VesselPlanner.dll` is under `GameData/VesselPlanner/Plugins`.
- the old `GameData/EngineStagePlanner` installation has been removed.

### An engine or tank is missing from the list

Check:

- engine include/exclude text filters
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

## 15. License

See `License.md` for license information.

> **Editor layout:** The main VesselPlanner window is horizontally resizable from **1150 px to 1850 px** using the grip on its right edge; the selected width is remembered. When the candidate-engine table is wider than the window, use its horizontal scrollbar; the sortable column headings scroll horizontally with the engine rows so the headings stay aligned. The synchronized header area is **34 px** tall.
