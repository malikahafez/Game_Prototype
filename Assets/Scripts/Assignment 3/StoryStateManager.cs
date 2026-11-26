using System;
using System.Collections.Generic;
using UnityEngine;

public class StoryStateManager : MonoBehaviour
{
    public static StoryStateManager Instance { get; private set; }

    [Header("Debug / Inspector Starting Flags")]
    [Tooltip("Add story flags here in the Inspector (e.g. 'act1Complete', 'metVacationer') to test starting directly from any level.")]
    [SerializeField] private List<string> initialDebugFlags = new List<string>();

    [Header("Player Progress & Economy")]
    public int coins = 100;
    public int penaltyCount = 0;

    [HideInInspector] public int cutsceneToPlay = 1;
    [HideInInspector] public string postCutsceneDestinationScene = "02_Level2_FortDinosaur";

    private HashSet<string> storyFlags = new HashSet<string>();

    public event Action<string> OnFlagSet;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Populate the runtime HashSet from the Inspector list
        foreach (string flag in initialDebugFlags)
        {
            if (!string.IsNullOrWhiteSpace(flag))
            {
                storyFlags.Add(flag.Trim());
            }
        }
    }

    public void SetFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return;

        if (storyFlags.Add(flag))
        {
            OnFlagSet?.Invoke(flag);
        }
    }

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && storyFlags.Contains(flag);
    }

    public void RemoveFlag(string flag)
    {
        if (!string.IsNullOrWhiteSpace(flag))
        {
            storyFlags.Remove(flag);
        }
    }

    public void ResetLevelCounters()
    {
        penaltyCount = 0;
    }

    public void RegisterWrongPickup()
    {
        penaltyCount++;
    }

    public bool TrySpendCoinsForHint(int cost)
    {
        if (coins < cost) return false;
        coins -= cost;
        return true;
    }

#if UNITY_EDITOR
    // Allows adding flags live in the Inspector while in Play Mode
    [ContextMenu("Sync Inspector Debug Flags to HashSet")]
    private void SyncDebugFlagsInPlayMode()
    {
        foreach (string flag in initialDebugFlags)
        {
            if (!string.IsNullOrWhiteSpace(flag))
            {
                SetFlag(flag.Trim());
            }
        }
    }
#endif
}