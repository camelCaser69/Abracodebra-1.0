// FILE: Assets/Scripts/Core/DuskRules.cs
// ============================================================================
//  DUSK RULES (Block 1 "Doris by Dusk", B3). Every tunable of the run in one
//  ScriptableObject: the season length and its waves, Doris's belly, the round
//  ramp, the mercy cap, supper pacing and the end texts.
//  Loaded in code from Resources (no scene wiring): Assets/Scriptable Objects/
//  Run/Resources/DuskRules.asset. Missing asset = code defaults + a warning.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

namespace WegoSystem {
    public enum DuskVerdict {
        NextRound,
        LostGardenGone,
        LostDorisHungry,
        Won
    }

    [CreateAssetMenu(fileName = "DuskRules", menuName = "Abracodabra/Run/Dusk Rules")]
    public class DuskRules : ScriptableObject {
        public const string ResourcePath = "DuskRules";

        [Header("Season")]
        [Tooltip("Surviving the dusk of this round wins the run.")]
        [Min(1)] public int roundsToWin = 5;

        [Tooltip("The wave of each round (element 0 = round 1). Empty = WaveManager's scene list. " +
                 "A round without a wave still has a Day: the timer runs, nothing spawns.")]
        public List<WaveDefinition> waves = new List<WaveDefinition>();

        [Header("Doris's belly")]
        [Tooltip("Pips in the belly readout. Belly = round((Max - hunger) / Max * pips).")]
        [Min(1)] public int bellyPips = 8;

        [Tooltip("Doris's hunger per Day tick in round 1. 0 = use DorisDefinition.hungerPerTick. " +
                 "0.5 = 50 hunger over a 100-tick Day = 4 of 8 pips.")]
        [Min(0f)] public float dorisHungerPerTick = 0.5f;

        [Tooltip("The hunger rate grows by this fraction each round: rate x (1 + ramp x (round - 1)).")]
        [Min(0f)] public float hungerRampPerRound = 0.15f;

        [Tooltip("Mercy cap: a starving Doris eats at most this many plants per Day (0 = never).")]
        [Min(0)] public int maxPlantsEatenPerDay = 1;

        [Tooltip("A starving Doris eats a plant only once her belly is down to this many pips " +
                 "(the hover says 'at 0 she eats a plant').")]
        [Min(0)] public int eatsPlantAtBellyPips = 0;

        [Header("Supper at dusk")]
        [Tooltip("Seconds each fruit takes to fly to Doris (one at a time).")]
        [Min(0.05f)] public float secondsPerFruit = 0.3f;

        [Tooltip("Supper never takes longer than this: with many fruit each one flies faster " +
                 "(min(secondsPerFruit, maxSupperSeconds / fruit), at least 0.05 s).")]
        [Min(1f)] public float maxSupperSeconds = 8f;

        [Tooltip("Height of the fruit's arc, in world units.")]
        [Min(0f)] public float fruitArcHeight = 1.2f;

        [Tooltip("A flying fruit is drawn this many times its size on the plant, so it can be followed.")]
        [Min(1f)] public float fruitFlightScale = 5f;

        [Tooltip("Seconds to hold after the last fruit (and after an empty supper) before the dusk verdict.")]
        [Min(0f)] public float holdAfterSupper = 0.8f;

        [Header("Auto-pause (D6, B5)")]
        [Tooltip("Pause the Day (Space resumes) when the first pest group of the run walks in.")]
        public bool autoPauseFirstWave = true;

        [Tooltip("Pause the Day every time a plant dies.")]
        public bool autoPausePlantDeath = true;

        [Tooltip("Pause the Day the first time Doris eats a plant in a run.")]
        public bool autoPauseDorisFirstEat = true;

        [Header("Dusk ledger (B6)")]
        [Tooltip("After supper the dusk ledger waits for Continue before the dusk check is applied. " +
                 "Off = the check is applied at once (scripted runs). With no ledger UI in the scene it is applied at once either way.")]
        public bool ledgerWaitsForContinue = true;

        [Header("Player")]
        [Tooltip("D4: player starvation does not end the run in Block 1. While off, the HUD hides the player HUNGER bar (B6).")]
        public bool playerStarvationEndsRun = false;

        [Header("End texts")]
        public string gardenGoneText = "The garden is gone.";
        public string dorisHungryText = "Doris went to bed hungry.";
        public string winText = "Doris sleeps. You made it through the season.";

        static DuskRules active;

        /// <summary>The run's rules: the Resources asset, or code defaults if it is missing.</summary>
        public static DuskRules Active {
            get {
                if (active == null) {
                    active = Resources.Load<DuskRules>(ResourcePath);
                    if (active == null) {
                        Debug.LogWarning($"[DuskRules] No DuskRules asset at Resources/{ResourcePath}. Using code defaults.");
                        active = CreateInstance<DuskRules>();
                    }
                }
                return active;
            }
        }

        /// <summary>Hunger-rate multiplier for a round: 1 + ramp x (round - 1).</summary>
        public float HungerMultiplierForRound(int round) {
            return 1f + hungerRampPerRound * Mathf.Max(0, round - 1);
        }

        /// <summary>Belly pips for a hunger value, rounded half up (hunger 0 = full belly).</summary>
        public int BellyPipsFor(float hunger, float maxHunger) {
            if (maxHunger <= 0f) return 0;
            float fullness = Mathf.Clamp01((maxHunger - hunger) / maxHunger);
            return Mathf.Clamp(Mathf.FloorToInt(fullness * bellyPips + 0.5f), 0, bellyPips);
        }

        /// <summary>
        /// The dusk check, after supper. No living plant comes first (a garden that is gone
        /// also explains a hungry Doris), then an empty belly, then the win at the last round.
        /// </summary>
        public DuskVerdict Judge(int livingPlants, int bellyPipsAfterSupper, int round) {
            if (livingPlants <= 0) return DuskVerdict.LostGardenGone;
            if (bellyPipsAfterSupper <= 0) return DuskVerdict.LostDorisHungry;
            if (round >= roundsToWin) return DuskVerdict.Won;
            return DuskVerdict.NextRound;
        }

        public string TextFor(DuskVerdict verdict) {
            switch (verdict) {
                case DuskVerdict.LostGardenGone: return gardenGoneText;
                case DuskVerdict.LostDorisHungry: return dorisHungryText;
                case DuskVerdict.Won: return winText;
                default: return string.Empty;
            }
        }
    }
}
