# VesselPlanner — User Manual

For Kerbal Space Program 1.12.x

VesselPlanner helps you pick engines. In the VAB/SPH it tells you which engines will actually meet a delta-v and TWR target for a given stage. In flight it plots telemetry on a scrolling graph.

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

## 3. The two editor modes

| Mode | Use it when |
|---|---|
| **Analyze Existing** | You already have a craft built and want to know which engine to put on a stage that already has tanks and fuel. |
| **Planning** | You have a delta-v target and a payload mass, and you want the mod to size the stage for you from scratch. |

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
- **Simulation filters** — engine name text filter, bulkhead matching, ignore monopropellant.
- **Simulate all engines** — run the sweep.
- **Simulation status** — progress and result count.

Below it, the **Existing stage** pane shows what the mod found on your craft: wet and dry masses, payload above the stage, propellant currently loaded versus full capacity, current engine mass, burn time, tank volume, and the list of engines already installed.

> **[IMAGE PLACEHOLDER: `images/analyze-left-column.png` — the two stacked panes, Simulation environment above Existing stage]**

### Step 3 — Choose a body and altitude

Click the current-body button to open a vertical list of celestial bodies. Pick one, then set an altitude with the slider. The mod uses KSP's own atmospheric pressure model and each engine's real atmosphere curve, so the atmospheric numbers are what you'd actually get there.

The body list draws on top of the rest of the window rather than pushing it down, and it stays anchored to the button as you move the planner around.

> **[IMAGE PLACEHOLDER: `images/body-selector.png` — the body list open beneath the current-body button]**

### Step 4 — Read the results

The candidate list shows every compatible engine, simulated against the propellant **actually loaded** in that stage right now (not maximum tank capacity). The sortable **Mass t/eng** column shows the dry mass of one engine part. See [Section 6](#6-reading-the-candidate-table) for what each column means.

### Filters available in this mode

- **Engine name** — live, case-insensitive text search.
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

> **[IMAGE PLACEHOLDER: `images/planning-inputs.png` — the Planning input panel filled in with a sample target]**

### Step 2 — Set the tank ratio

Planning has to guess tank dry mass before you've picked a tank, so it uses a **tank dry mass ÷ propellant mass** ratio. `0.125` is a reasonable stock-like default. If you select a stage that already has resource-bearing parts, the mod infers a ratio from them for you.

The solver loops on this: propellant mass depends on tank dry mass, and tank dry mass depends on propellant mass.

### Step 3 — Read the results and the tank table

Because Planning sizes the stage from **vacuum** delta-v, the stage geometry is fixed. Moving the altitude slider will **not** change propellant load, wet/dry mass, vacuum delta-v, tank requirements, or burn time. It only changes the atmospheric delta-v, atmospheric thrust, and atmospheric TWR shown for that fixed stage.

Below the engine results, the **Tanks needed** table lists each available tank type with:

- how many copies you'd need
- total tank dry mass
- excess capacity
- supplied resource capacity

> **[IMAGE PLACEHOLDER: `images/tanks-needed.png` — the Tanks needed table showing several tank options]**

---

## 6. Reading the candidate table

The list is capped at 300 pixels tall and scrolls, so growing the window won't let the engine list swallow the UI. Sorting and filter changes recalculate immediately.

| Column | What it is |
|---|---|
| **Engine** | Part name |
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
- **Add Tank** in the Tanks needed table — places **one** copy. Click it again for each copy the table calls for.

For multimode engines, both listed modes point at the same underlying part.

Optionally, the planner can close itself after a successful placement. Analyze Existing and Planning have **separate** preferences for this, and the window only closes if KSP actually put the part on your cursor.

> **[IMAGE PLACEHOLDER: `images/add-engine.png` — an engine attached to the cursor immediately after clicking Add]**

---

## 8. Settings

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

### Appearance page

**Use solid backgrounds for all editor windows** gives the planner and the editor Settings window an opaque dark underlay while keeping normal KSP borders and title styling. Off by default. Flight windows use their own separate Flight Settings option.

---

## 9. Flight telemetry plotter

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

The New/Edit Stage dialog can restrict both candidate engines and tank suggestions by KSP `bulkheadProfiles`. `srf` is listed first, and stack profiles include `size0`, `size1`, `size1p5`, `size2`, and larger sizes. The Planning Tanks table shows each tank's profiles in a sortable **Bulkhead** column. Leaving the selection empty considers all profiles. Older plans containing `BulkheadSize` values are translated automatically.

When **New Stage** is opened for the first stage and a vessel already exists in the editor, VesselPlanner presets the profile from the vessel's open lower bottom node, falling back to the top node if necessary. For later stages it presets from the previous stage engine's bottom node, again falling back to the top node.

The New/Edit Stage dialog also has an **Add decoupler mass** toggle. When enabled, the matching value from `GameData/VesselPlanner/decouplerMasses.cfg` is added to fixed stage dry mass. Stage 2 and later show **Previous stages mass** and automatically use the cumulative wet mass of the original payload and all prior planned stages as the new stage payload.

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

## 10. How the numbers are calculated

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

## 11. Known limits

The planner is a good heuristic, not a full flight simulator. These are not currently modelled:

- Dynamic tank type/configuration switching by RealFuels, B9, MFT-style modules after the prefab loads.
- Jet velocity curves and intake-air flight-state modelling beyond static pressure-based Isp and thrust.
- Engine variants controlled by arbitrary third-party modules beyond loaded `ModuleEngines`.
- Full MechJeb-style fuel-flow simulation for asparagus, crossfeed, and drop-tank arrangements.

For unusual modded fuel-flow systems, use **Planning** with a manually entered payload mass — it doesn't depend on the stage scan being right.

---

## 12. Version notes

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
