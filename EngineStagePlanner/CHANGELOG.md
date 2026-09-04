## 0.3.1
- Moved Δv basis selector directly into the Candidate engine filters area.
- Added explicit Vacuum / Sea Level selector buttons.
- Moved bulkhead-size filtering into the Candidate engine filters area and displays detected stage profiles.

# Changelog

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
