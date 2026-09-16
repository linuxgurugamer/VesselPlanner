using System;
using System.Collections.Generic;
using System.Linq;

namespace VesselPlanner.Core
{
    public static class TankPlanner
    {
        // Very large mod installs can expose hundreds of stack tanks with one profile.
        // Searching every C(n,3) set would freeze IMGUI for little benefit, so retain the
        // best useful candidates for each profile before testing combinations. Stock and
        // normal modded installs are normally well below this ceiling and are exhaustive.
        private const int MaxTankTypesPerProfile = 80;
        private const int MaxPairTankTypesPerProfile = 48;
        private const int MaxTripleTankTypesPerProfile = 16;
        private const int MaxLoopCount = 96;

        public static List<TankSuggestion> Suggest(IEnumerable<TankCandidate> tanks, StageSolution solution)
        {
            var results = new List<TankSuggestion>();
            if (solution == null || solution.Propellants.Count == 0 || tanks == null) return results;

            List<TankCandidate> usable = tanks
                .Where(t => t != null && t.Resources != null && t.Resources.Count > 0 && t.BulkheadProfiles != null && t.BulkheadProfiles.Count > 0)
                .Where(t => SupportsAnyRequiredResource(t, solution.Propellants))
                .ToList();
            if (usable.Count == 0) return results;

            var seenSuggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> profiles = usable
                .SelectMany(t => t.BulkheadProfiles)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            foreach (string profile in profiles)
            {
                List<TankCandidate> profileTanks = usable
                    .Where(t => t.BulkheadProfiles.Any(p => string.Equals(p, profile, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(t => UsefulDryMassScore(t, solution.Propellants))
                    .ThenBy(t => t.DryMassTons)
                    .ThenBy(t => t.DisplayName)
                    .Take(MaxTankTypesPerProfile)
                    .ToList();

                for (int a = 0; a < profileTanks.Count; a++)
                    AddBestForSet(results, seenSuggestions, profile, solution.Propellants, profileTanks[a]);

                List<TankCandidate> pairTanks = profileTanks.Take(MaxPairTankTypesPerProfile).ToList();
                for (int a = 0; a < pairTanks.Count; a++)
                {
                    for (int b = a + 1; b < pairTanks.Count; b++)
                        AddBestForSet(results, seenSuggestions, profile, solution.Propellants, pairTanks[a], pairTanks[b]);
                }

                List<TankCandidate> tripleTanks = profileTanks.Take(MaxTripleTankTypesPerProfile).ToList();
                for (int a = 0; a < tripleTanks.Count; a++)
                {
                    for (int b = a + 1; b < tripleTanks.Count; b++)
                    {
                        for (int c = b + 1; c < tripleTanks.Count; c++)
                            AddBestForSet(results, seenSuggestions, profile, solution.Propellants, tripleTanks[a], tripleTanks[b], tripleTanks[c]);
                    }
                }
            }

            return results
                .OrderBy(s => s.TotalDryMassTons)
                .ThenBy(s => s.ExcessFraction)
                .ThenBy(s => s.DifferentTankTypes)
                .ThenBy(s => s.Count)
                .ThenBy(s => s.TankSummary)
                .ToList();
        }

        private static void AddBestForSet(List<TankSuggestion> results, HashSet<string> seen, string profile,
            IList<PropellantRequirement> requirements, params TankCandidate[] tankSet)
        {
            if (tankSet == null || tankSet.Length == 0 || tankSet.Length > 3) return;
            if (!SetCanCoverAllRequirements(tankSet, requirements)) return;

            TankSuggestion best = FindBestCounts(profile, tankSet, requirements);
            if (best == null) return;

            string key = string.Join("|", best.Tanks
                .Where(p => p != null && p.Tank != null && p.Count > 0)
                .OrderBy(p => p.Tank.PartName, StringComparer.OrdinalIgnoreCase)
                .Select(p => (p.Tank.PartName ?? string.Empty) + "=" + p.Count.ToString())
                .ToArray());
            if (key.Length == 0 || !seen.Add(key)) return;
            results.Add(best);
        }

        private static TankSuggestion FindBestCounts(string profile, TankCandidate[] tankSet, IList<PropellantRequirement> requirements)
        {
            if (tankSet.Length == 1)
            {
                int count = MinimumCountToComplete(tankSet[0], requirements, null);
                if (count <= 0 || count == int.MaxValue) return null;
                return BuildSuggestion(profile, requirements, new[] { MakePart(tankSet[0], count) });
            }

            // Put the smallest search ranges in the loop positions. The final tank's count
            // is solved directly from the residual requirement, so every retained solution
            // is the least-overfilled solution for those loop counts.
            TankCandidate[] ordered = tankSet
                .OrderBy(t => MaxUsefulCount(t, requirements))
                .ThenBy(t => t.DryMassTons)
                .ToArray();

            TankSuggestion best = null;
            int max0 = Math.Min(MaxLoopCount, MaxUsefulCount(ordered[0], requirements));
            if (max0 <= 0) return null;

            if (ordered.Length == 2)
            {
                for (int c0 = 1; c0 <= max0; c0++)
                {
                    Dictionary<string, double> provided = NewProvidedMap(requirements);
                    AddTankContribution(provided, ordered[0], c0);
                    if (RequirementsMet(provided, requirements)) continue; // second type would be unnecessary

                    int c1 = MinimumCountToComplete(ordered[1], requirements, provided);
                    if (c1 <= 0 || c1 == int.MaxValue) continue;
                    TankSuggestion candidate = BuildSuggestion(profile, requirements,
                        new[] { MakePart(ordered[0], c0), MakePart(ordered[1], c1) });
                    best = Better(best, candidate);
                }
                return best;
            }

            int max1 = Math.Min(MaxLoopCount, MaxUsefulCount(ordered[1], requirements));
            if (max1 <= 0) return null;
            for (int c0 = 1; c0 <= max0; c0++)
            {
                Dictionary<string, double> firstProvided = NewProvidedMap(requirements);
                AddTankContribution(firstProvided, ordered[0], c0);
                for (int c1 = 1; c1 <= max1; c1++)
                {
                    Dictionary<string, double> provided = new Dictionary<string, double>(firstProvided, StringComparer.OrdinalIgnoreCase);
                    AddTankContribution(provided, ordered[1], c1);
                    if (RequirementsMet(provided, requirements)) continue; // third type would be unnecessary

                    int c2 = MinimumCountToComplete(ordered[2], requirements, provided);
                    if (c2 <= 0 || c2 == int.MaxValue) continue;
                    TankSuggestion candidate = BuildSuggestion(profile, requirements,
                        new[] { MakePart(ordered[0], c0), MakePart(ordered[1], c1), MakePart(ordered[2], c2) });
                    best = Better(best, candidate);
                }
            }
            return best;
        }

        private static TankSuggestion Better(TankSuggestion current, TankSuggestion candidate)
        {
            if (candidate == null) return current;
            if (current == null) return candidate;
            const double eps = 1e-9;
            if (candidate.TotalDryMassTons < current.TotalDryMassTons - eps) return candidate;
            if (candidate.TotalDryMassTons > current.TotalDryMassTons + eps) return current;
            if (candidate.ExcessFraction < current.ExcessFraction - eps) return candidate;
            if (candidate.ExcessFraction > current.ExcessFraction + eps) return current;
            if (candidate.TotalCost < current.TotalCost - eps) return candidate;
            if (candidate.TotalCost > current.TotalCost + eps) return current;
            return candidate.Count < current.Count ? candidate : current;
        }

        private static TankSuggestionPart MakePart(TankCandidate tank, int count)
        {
            return new TankSuggestionPart { Tank = tank, Count = Math.Max(1, count) };
        }

        private static TankSuggestion BuildSuggestion(string profile, IList<PropellantRequirement> requirements, IEnumerable<TankSuggestionPart> parts)
        {
            var suggestion = new TankSuggestion { BulkheadProfile = profile ?? string.Empty };
            foreach (TankSuggestionPart part in parts)
            {
                if (part == null || part.Tank == null || part.Count <= 0) continue;
                suggestion.Tanks.Add(part);
                suggestion.TotalDryMassTons += part.TotalDryMassTons;
                suggestion.TotalCost += part.TotalCost;
            }
            if (suggestion.Tanks.Count == 0) return null;

            double requestedTotal = 0.0;
            double excessTotal = 0.0;
            bool useVolume = requirements.All(r => r.VolumeLiters > 0.0);

            foreach (PropellantRequirement req in requirements)
            {
                double providedUnits = 0.0;
                double providedMass = 0.0;
                double providedVolume = 0.0;
                foreach (TankSuggestionPart part in suggestion.Tanks)
                {
                    TankResourceCapacity cap = FindCapacity(part.Tank, req.ResourceName);
                    if (cap == null) continue;
                    double units = cap.Units * part.Count;
                    providedUnits += units;
                    providedMass += units * cap.DensityTonsPerUnit;
                    if (cap.LitersPerUnit > 0.0) providedVolume += units * cap.LitersPerUnit;
                }

                if (providedUnits + 1e-9 < req.Units) return null;
                if (useVolume)
                {
                    requestedTotal += req.VolumeLiters;
                    excessTotal += Math.Max(0.0, providedVolume - req.VolumeLiters);
                }
                else
                {
                    requestedTotal += req.Units;
                    excessTotal += Math.Max(0.0, providedUnits - req.Units);
                }

                suggestion.Provided.Add(new PropellantRequirement
                {
                    ResourceName = req.ResourceName,
                    IgnoreForIsp = req.IgnoreForIsp,
                    Units = providedUnits,
                    MassTons = providedMass,
                    VolumeLiters = providedVolume
                });
            }

            suggestion.ExcessFraction = requestedTotal > 0.0 ? excessTotal / requestedTotal : 0.0;
            return suggestion;
        }

        private static int MinimumCountToComplete(TankCandidate tank, IList<PropellantRequirement> requirements, IDictionary<string, double> alreadyProvided)
        {
            int count = 0;
            foreach (PropellantRequirement req in requirements)
            {
                double existing = 0.0;
                if (alreadyProvided != null) alreadyProvided.TryGetValue(req.ResourceName, out existing);
                double remaining = req.Units - existing;
                if (remaining <= 1e-9) continue;

                TankResourceCapacity cap = FindCapacity(tank, req.ResourceName);
                if (cap == null || cap.Units <= 0.0) return int.MaxValue;
                count = Math.Max(count, (int)Math.Ceiling(remaining / cap.Units - 1e-12));
            }
            return Math.Max(1, count);
        }

        private static int MaxUsefulCount(TankCandidate tank, IList<PropellantRequirement> requirements)
        {
            int max = 0;
            foreach (PropellantRequirement req in requirements)
            {
                TankResourceCapacity cap = FindCapacity(tank, req.ResourceName);
                if (cap == null || cap.Units <= 0.0) continue;
                max = Math.Max(max, (int)Math.Ceiling(req.Units / cap.Units - 1e-12));
            }
            return Math.Max(0, Math.Min(512, max));
        }

        private static bool SetCanCoverAllRequirements(IEnumerable<TankCandidate> tankSet, IEnumerable<PropellantRequirement> requirements)
        {
            foreach (PropellantRequirement req in requirements)
            {
                if (!tankSet.Any(t =>
                {
                    TankResourceCapacity cap = FindCapacity(t, req.ResourceName);
                    return cap != null && cap.Units > 0.0;
                })) return false;
            }
            return true;
        }

        private static bool SupportsAnyRequiredResource(TankCandidate tank, IEnumerable<PropellantRequirement> requirements)
        {
            return requirements.Any(req =>
            {
                TankResourceCapacity cap = FindCapacity(tank, req.ResourceName);
                return cap != null && cap.Units > 0.0;
            });
        }

        private static TankResourceCapacity FindCapacity(TankCandidate tank, string resourceName)
        {
            return tank == null ? null : tank.Resources.FirstOrDefault(r =>
                string.Equals(r.ResourceName, resourceName, StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, double> NewProvidedMap(IEnumerable<PropellantRequirement> requirements)
        {
            var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (PropellantRequirement req in requirements)
                if (!map.ContainsKey(req.ResourceName)) map.Add(req.ResourceName, 0.0);
            return map;
        }

        private static void AddTankContribution(IDictionary<string, double> provided, TankCandidate tank, int count)
        {
            if (provided == null || tank == null || count <= 0) return;
            foreach (TankResourceCapacity cap in tank.Resources)
            {
                if (cap == null || cap.Units <= 0.0 || !provided.ContainsKey(cap.ResourceName)) continue;
                provided[cap.ResourceName] += cap.Units * count;
            }
        }

        private static bool RequirementsMet(IDictionary<string, double> provided, IEnumerable<PropellantRequirement> requirements)
        {
            foreach (PropellantRequirement req in requirements)
            {
                double value;
                if (provided == null || !provided.TryGetValue(req.ResourceName, out value) || value + 1e-9 < req.Units)
                    return false;
            }
            return true;
        }

        private static double UsefulDryMassScore(TankCandidate tank, IEnumerable<PropellantRequirement> requirements)
        {
            if (tank == null) return double.MaxValue;
            double usefulFraction = 0.0;
            foreach (PropellantRequirement req in requirements)
            {
                if (req.Units <= 0.0) continue;
                TankResourceCapacity cap = FindCapacity(tank, req.ResourceName);
                if (cap == null || cap.Units <= 0.0) continue;
                usefulFraction += Math.Min(1.0, cap.Units / req.Units);
            }
            if (usefulFraction <= 0.0) return double.MaxValue;
            return Math.Max(0.0, tank.DryMassTons) / usefulFraction;
        }
    }
}
