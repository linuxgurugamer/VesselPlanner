# Engine Stage Planner for KSP1

Visual Studio 2022 / .NET Framework 4.7.2 project for Kerbal Space Program 1.x (intended for KSP 1.12.x).

## Features

- Runs in the VAB/SPH.
- Select an editor stage.
- Scans all currently available `ModuleEngines` / `ModuleEnginesFX` parts.
- Reads engine thrust, atmosphere curve, propellant ratios and resource densities from KSP at runtime.
- Planning inputs: target delta-v, minimum TWR, payload, gravity, atmospheric pressure, max engine count and tank structural ratio.
- Automatically tests engine counts and ranks valid configurations.
- Sorting modes: lowest wet stage mass, lowest propellant, lowest engine cost, shortest burn, highest TWR, highest ISP.
- Shows required propellant by KSP resource units and mass; known stock resources also show estimated physical tank volume.
- Analyze Existing mode summarizes the selected stage and simulates all compatible engines against its existing full resource capacities.
- Alt+P toggles the window.

## Building

1. Install Visual Studio 2022 with **.NET desktop development** and the .NET Framework 4.7.2 targeting pack.
2. Open `EngineStagePlanner.sln`.
3. Set the MSBuild property `KSP_ROOT` to your KSP directory if KSP is not at the default Steam location.

Example Developer Command Prompt build:

```bat
msbuild EngineStagePlanner.sln /p:Configuration=Release /p:KSP_ROOT="D:\Games\Kerbal Space Program"
```

To also copy the DLL directly into the game's `GameData/EngineStagePlanner/Plugins` directory:

```bat
msbuild EngineStagePlanner.sln /p:Configuration=Release /p:KSP_ROOT="D:\Games\Kerbal Space Program" /p:CopyToKSP=true
```

Or set `KSP_ROOT` as a Windows environment variable and build normally in Visual Studio.

## Installation

After building, copy:

`EngineStagePlanner/bin/Release/EngineStagePlanner.dll`

to:

`Kerbal Space Program/GameData/EngineStagePlanner/Plugins/EngineStagePlanner.dll`

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
- `EngineStagePlannerAddon.cs` - KSP editor addon entry point.

## Current scope / intentionally not yet included

- Automatic insertion/replacement of engines or tanks on the craft.
- Fuel-tank combination/knapsack suggestions.
- RealFuels/B9/MFT-specific physical-volume APIs.
- Jet-engine velocity/atmosphere curves beyond ordinary static atmosphere ISP/thrust scaling.
- Engine variants/configurations controlled by arbitrary third-party modules beyond loaded `ModuleEngines` modules.

Those can be added without changing the core solver API.
