using System;
using System.Collections.Generic;
using UnityEngine;


// Central persistent singleton that tracks story flags, player coins,
// misclick penalties, and hint usage across all scenes.

public class StoryStateManager : MonoBehaviour
{
    public static StoryStateManager Instance { get; private set; }

    private HashSet<string> storyFlags = new HashSet<string>();
    public event Action<string> OnFlagSet;

    [Header("Economy & Penalty Tracking")]
    public int coins = 100;
    public int penaltyCount = 0;
    public int hintsUsedCount = 0;

    [HideInInspector] public int cutsceneToPlay = 1; // 1 = After Level 1, 2 = After Level 2, 3 = After Level 3
    [HideInInspector] public string postCutsceneDestinationScene = "02_Level2_FortDinosaur";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    // Sets a narrative flag and notifies listeners (ObjectiveManager, doors, NPCs).

    public void SetFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return;

        if (storyFlags.Add(flag))
        {
            Debug.Log($"[StoryState] Flag set: {flag}");
            OnFlagSet?.Invoke(flag);
        }
    }

    public bool HasFlag(string flag) => storyFlags.Contains(flag);


    // Resets per-level counters when entering a new scene.

    public void ResetLevelCounters()
    {
        penaltyCount = 0;
        hintsUsedCount = 0;
    }

    public void RegisterWrongPickup()
    {
        penaltyCount++;
        if (penaltyCount == 4)
        {
            SetFlag("highPenaltyWarning");
        }
    }

    public bool TrySpendCoinsForHint(int cost)
    {
        if (coins >= cost)
        {
            coins -= cost;
            hintsUsedCount++;
            return true;
        }
        return false;
    }
}