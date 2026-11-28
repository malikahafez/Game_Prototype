using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CollectedLoreEntry
{
    public string itemID;
    public string itemName;
    [TextArea(2, 4)] public string loreDescription;
    public Sprite itemSprite;
    public int actFound;
}

public class StoryStateManager : MonoBehaviour
{
    public static StoryStateManager Instance { get; private set; }

    [Header("Debug / Inspector Starting Flags")]
    [Tooltip("Add story flags here in the Inspector (e.g. 'act1Complete', 'metVacationer') to test starting directly from any level.")]
    [SerializeField] private List<string> initialDebugFlags = new List<string>();

    [Header("Player Progress & Economy")]
    public int coins = 100;
    public int penaltyCount = 0;

    [Header("Persistent Lore Collection / Codex")]
    public List<CollectedLoreEntry> discoveredLoreItems = new List<CollectedLoreEntry>();

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
    /// <summary>
    /// Clears per-level progression flags so a level can be replayed cleanly,
    /// while preserving act completion flags (act1Complete, act2Complete, gameComplete) and coins.
    /// </summary>
    public void ResetLevelFlags(IEnumerable<string> flagsToClear)
    {
        if (flagsToClear == null) return;

        foreach (string flag in flagsToClear)
        {
            if (!string.IsNullOrWhiteSpace(flag))
            {
                storyFlags.Remove(flag.Trim());
            }
        }
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

    public void RecordDiscoveredLore(string id, string displayName, string description, Sprite icon, int actNumber)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (discoveredLoreItems.Exists(e => e.itemID == id)) return; // Prevent duplicates

        discoveredLoreItems.Add(new CollectedLoreEntry
        {
            itemID = id,
            itemName = displayName,
            loreDescription = description,
            itemSprite = icon,
            actFound = actNumber
        });
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