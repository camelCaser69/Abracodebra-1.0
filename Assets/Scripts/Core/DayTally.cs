// FILE: Assets/Scripts/Core/DayTally.cs
// ============================================================================
//  DAY TALLY (Block 1 B5). What happened to the garden during the Day: bites per
//  pest species, kills (with the gene that did it, when known), repels, plants
//  lost, Doris's plant-eats and fruit set. Reset at dawn (RunManager calls
//  BeginDay when the Day starts); read by the dev readout now and by the dusk
//  ledger (B6) later. Static and passive: callers report, nothing is polled.
//
//  It also owns the D6 auto-pauses: first wave of the run, every plant death,
//  Doris's first plant-eat (flags in DuskRules). A pause is
//  ExecutionPhaseDriver.SetPaused(true) plus a reason the banner shows.
//
//  Damage sources: only reported where the code knows them (thorns, Reactive
//  Burst, explosive and poison genes, projectiles, a status effect's name).
//  An unknown source stays unnamed on purpose: this is not a damage-source system.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Abracodabra.Genes;

namespace WegoSystem {
    public static class DayTally {
        // Per Day
        static readonly Dictionary<string, int> bitesBySpecies = new Dictionary<string, int>();
        static readonly Dictionary<string, int> killsBySpecies = new Dictionary<string, int>();
        static readonly Dictionary<string, int> killsBySource = new Dictionary<string, int>();
        static readonly Dictionary<string, int> repelsBySpecies = new Dictionary<string, int>();
        static readonly Dictionary<int, string> lastBiterByPlant = new Dictionary<int, string>();
        static int plantsLost;
        static int dorisAte;
        static int fruitSet;
        static int dayStartTick;
        static int dayRound;

        // Per run (reset when round 1 begins)
        static bool firstWavePauseDone;
        static bool dorisEatPauseDone;

        static ExecutionPhaseDriver driver;

        /// <summary>Reason of the latest auto-pause (empty until one fires).</summary>
        public static string LastPauseReason { get; private set; } = "";
        /// <summary>Auto-pauses fired this run (for checks).</summary>
        public static int AutoPauseCount { get; private set; }
        /// <summary>Raised with the reason after the Day was paused automatically.</summary>
        public static event Action<string> OnAutoPause;

        public static int Round => dayRound;
        public static int Bites { get; private set; }
        public static int Kills { get; private set; }
        public static int Repels { get; private set; }
        public static int PlantsLost => plantsLost;
        public static int DorisAtePlants => dorisAte;
        public static int FruitSet => fruitSet;

        /// <summary>The density test's number: bites + kills + repels + plant deaths + Doris eats + fruit set.</summary>
        public static int EventCount => Bites + Kills + Repels + plantsLost + dorisAte + fruitSet;

        /// <summary>Day time elapsed, in seconds at x1 (ticks / ticks per second): the unit the wave markers use.</summary>
        public static float DaySeconds {
            get {
                var tm = TickManager.Instance;
                if (tm == null || tm.Config == null || tm.Config.ticksPerRealSecond <= 0f) return 0f;
                return Mathf.Max(0, tm.CurrentTick - dayStartTick) / tm.Config.ticksPerRealSecond;
            }
        }

        public static bool DayRunning =>
            RunManager.HasInstance && RunManager.Instance.CurrentState == RunState.GrowthAndThreat;

        public static int BitesBy(string species) {
            return species != null && bitesBySpecies.TryGetValue(species, out var n) ? n : 0;
        }

        public static int KillsOf(string species) {
            return species != null && killsBySpecies.TryGetValue(species, out var n) ? n : 0;
        }

        public static int KillsBy(string source) {
            return source != null && killsBySource.TryGetValue(source, out var n) ? n : 0;
        }

        /// <summary>"Events this Day: 7 in 31 s" (Day seconds at x1).</summary>
        public static string EventsLine() {
            return string.Format(CultureInfo.InvariantCulture, "Events this Day: {0} in {1:0} s", EventCount, DaySeconds);
        }

        /// <summary>One line with every counter, for checks and the ledger: "bites Bunny 3 · kills Bunny 1 (Thorns 1) · ...".</summary>
        public static string Summary() {
            var sb = new StringBuilder();
            sb.Append("bites ").Append(Join(bitesBySpecies));
            sb.Append(" · kills ").Append(Join(killsBySpecies));
            if (killsBySource.Count > 0) sb.Append(" (by ").Append(Join(killsBySource)).Append(')');
            sb.Append(" · repels ").Append(Join(repelsBySpecies));
            sb.Append(" · plants lost ").Append(plantsLost);
            sb.Append(" · Doris ate ").Append(dorisAte);
            sb.Append(" · fruit set ").Append(fruitSet);
            return sb.ToString();
        }

        static string Join(Dictionary<string, int> map) {
            if (map.Count == 0) return "0";
            var parts = new List<string>();
            foreach (var kv in map) parts.Add(kv.Key + " " + kv.Value);
            return string.Join(", ", parts);
        }

        // ── Lifecycle ────────────────────────────────────────────────

        /// <summary>Dawn: the Day starts. Clears the Day's counts; round 1 also clears the per-run pause flags.</summary>
        public static void BeginDay(int round) {
            bitesBySpecies.Clear();
            killsBySpecies.Clear();
            killsBySource.Clear();
            repelsBySpecies.Clear();
            lastBiterByPlant.Clear();
            Bites = 0;
            Kills = 0;
            Repels = 0;
            plantsLost = 0;
            dorisAte = 0;
            fruitSet = 0;
            dayRound = round;
            dayStartTick = TickManager.Instance != null ? TickManager.Instance.CurrentTick : 0;

            if (round <= 1) {
                firstWavePauseDone = false;
                dorisEatPauseDone = false;
                AutoPauseCount = 0;
                LastPauseReason = "";
            }
        }

        // ── Reports (all ignored outside the Day) ────────────────────

        public static void NoteBite(string species, PlantGrowth plant) {
            if (!DayRunning) return;
            Bites++;
            Add(bitesBySpecies, species);
            if (plant != null) lastBiterByPlant[plant.GetInstanceID()] = species;
        }

        public static void NoteKill(string species, string source) {
            if (!DayRunning) return;
            Kills++;
            Add(killsBySpecies, species);
            if (!string.IsNullOrEmpty(source)) Add(killsBySource, source);
        }

        public static void NoteRepel(string species) {
            if (!DayRunning) return;
            Repels++;
            Add(repelsBySpecies, species);
        }

        public static void NoteFruitSet() {
            if (!DayRunning) return;
            fruitSet++;
        }

        public static void NotePlantDied(PlantGrowth plant) {
            if (!DayRunning) return;
            plantsLost++;

            string reason = "A plant died.";
            if (plant != null && lastBiterByPlant.TryGetValue(plant.GetInstanceID(), out var species)) {
                reason = $"A plant died: the {Plural(species, 2)} ate its leaves.";
            }
            if (DuskRules.Active.autoPausePlantDeath) AutoPause(reason);
        }

        public static void NoteDorisAtePlant() {
            if (!DayRunning) return;
            dorisAte++;
            if (!dorisEatPauseDone) {
                dorisEatPauseDone = true;
                if (DuskRules.Active.autoPauseDorisFirstEat) {
                    AutoPause("Doris is starving and ate a plant. She does this at most once a Day.");
                }
            }
        }

        /// <summary>A pest group of the Day has just put its first animal on the map.</summary>
        public static void NoteGroupArrived(WaveSpawnEntry entry) {
            if (!DayRunning || entry == null || firstWavePauseDone) return;
            firstWavePauseDone = true;
            if (!DuskRules.Active.autoPauseFirstWave) return;

            string name = entry.animalDefinition != null && !string.IsNullOrEmpty(entry.animalDefinition.animalName)
                ? entry.animalDefinition.animalName : "pest";
            string where = entry.spawnLocationType == WaveSpawnLocationType.FixedEntry
                ? " from the " + WaveEntryGeometry.EdgeName(entry.entryEdge) : "";
            AutoPause($"First wave: {entry.spawnCount} {Plural(name, entry.spawnCount)}{where}.");
        }

        // ── Auto-pause ───────────────────────────────────────────────

        static void AutoPause(string reason) {
            if (!DayRunning) return;

            if (driver == null) driver = UnityEngine.Object.FindAnyObjectByType<ExecutionPhaseDriver>();
            if (driver != null) driver.SetPaused(true);

            AutoPauseCount++;
            LastPauseReason = reason;
            Debug.Log($"[DayTally] Auto-pause: {reason}");
            OnAutoPause?.Invoke(reason);
        }

        // ── Helpers ──────────────────────────────────────────────────

        static void Add(Dictionary<string, int> map, string key) {
            if (string.IsNullOrEmpty(key)) key = "pest";
            map.TryGetValue(key, out var n);
            map[key] = n + 1;
        }

        /// <summary>"Bunny", 2 -> "bunnies"; 1 -> "bunny". Lower case, plain English rules.</summary>
        public static string Plural(string name, int count) {
            string s = string.IsNullOrEmpty(name) ? "pest" : name.ToLowerInvariant();
            if (count == 1) return s;
            if (s.EndsWith("y") && s.Length > 1 && "aeiou".IndexOf(s[s.Length - 2]) < 0) return s.Substring(0, s.Length - 1) + "ies";
            if (s.EndsWith("s") || s.EndsWith("x") || s.EndsWith("ch") || s.EndsWith("sh")) return s + "es";
            return s + "s";
        }
    }
}
