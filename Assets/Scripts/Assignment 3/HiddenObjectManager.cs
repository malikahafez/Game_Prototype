using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Hidden Object list, count-up timer, non-quest item penalties,
/// NPC hint system, and end-of-level score/coin calculation.
/// </summary>
public class HiddenObjectManager : MonoBehaviour
{
    public static HiddenObjectManager Instance { get; private set; }

    [Header("Level Settings")]
    public string levelCompletionFlag = "act1Complete";
    public int maxHintsAllowed = 3;
    public int hintCoinCost = 25;

    [Header("HUD References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI penaltyCounterText;
    public TextMeshProUGUI coinsText;
    public Transform objectListContainer;
    public GameObject listEntryPrefab; // Prefab with TextMeshProUGUI
    public Button requestHintButton;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip correctPickupClip;
    public AudioClip wrongPickupPenaltyClip;
    public AudioClip hintChimeClip;

    private List<HiddenObjectItem> activeQuestItems = new List<HiddenObjectItem>();
    private Dictionary<HiddenObjectItem, TextMeshProUGUI> uiEntries = new Dictionary<HiddenObjectItem, TextMeshProUGUI>();

    private float elapsedTime = 0f;
    private bool timerRunning = true;
    private int hintsRemaining;

    private void Awake()
    {
        Instance = this;
        hintsRemaining = maxHintsAllowed;
        if (requestHintButton != null)
            requestHintButton.onClick.AddListener(UseNPCHint);
    }

    private void Start()
    {
        if (StoryStateManager.Instance != null)
            StoryStateManager.Instance.ResetLevelCounters();

        UpdateHUD();
    }

    private void Update()
    {
        if (!timerRunning) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        elapsedTime += Time.deltaTime;
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        if (timerText != null)
            timerText.text = $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Registers a required quest item into the HUD list (or switches lists mid-level, e.g., Level 2 jungle).
    /// </summary>
    public void RegisterQuestItem(HiddenObjectItem item)
    {
        if (activeQuestItems.Contains(item)) return;
        activeQuestItems.Add(item);

        if (listEntryPrefab != null && objectListContainer != null)
        {
            GameObject entryObj = Instantiate(listEntryPrefab, objectListContainer);
            TextMeshProUGUI label = entryObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = item.displayPrompt; // Word, Silhouette label, or Clue text
                uiEntries[item] = label;
            }
        }
    }

    public void OnQuestItemCollected(HiddenObjectItem item)
    {
        if (!activeQuestItems.Contains(item)) return;

        if (sfxSource != null && correctPickupClip != null)
            sfxSource.PlayOneShot(correctPickupClip);

        // Strike through and fade the collected entry in the HUD list
        if (uiEntries.TryGetValue(item, out TextMeshProUGUI label))
        {
            label.fontStyle = FontStyles.Strikethrough;
            label.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        }

        if (!string.IsNullOrEmpty(item.flagToSetOnPickup) && StoryStateManager.Instance != null)
            StoryStateManager.Instance.SetFlag(item.flagToSetOnPickup);

        activeQuestItems.Remove(item);

        if (activeQuestItems.Count == 0 && !string.IsNullOrEmpty(levelCompletionFlag))
        {
            CompleteLevelSequence();
        }
    }

    public void OnNonQuestItemClicked()
    {
        if (sfxSource != null && wrongPickupPenaltyClip != null)
            sfxSource.PlayOneShot(wrongPickupPenaltyClip);

        if (StoryStateManager.Instance != null)
            StoryStateManager.Instance.RegisterWrongPickup();

        UpdateHUD();
    }

    /// <summary>
    /// Purchases a hint using coins, plays NPC hint dialogue, and highlights an unfound item.
    /// </summary>
    public void UseNPCHint()
    {
        if (hintsRemaining <= 0 || activeQuestItems.Count == 0) return;

        if (StoryStateManager.Instance != null &&
            StoryStateManager.Instance.TrySpendCoinsForHint(hintCoinCost))
        {
            hintsRemaining--;
            if (sfxSource != null && hintChimeClip != null)
                sfxSource.PlayOneShot(hintChimeClip);

            HiddenObjectItem target = activeQuestItems[0];
            target.HighlightHintParticle();

            if (target.npcHintDialogue != null && DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(target.npcHintDialogue);

            UpdateHUD();
        }
    }

    private void UpdateHUD()
    {
        if (StoryStateManager.Instance == null) return;

        if (coinsText != null)
            coinsText.text = $"Coins: {StoryStateManager.Instance.coins} | Hints: {hintsRemaining}";

        if (penaltyCounterText != null)
            penaltyCounterText.text = $"Penalties: {StoryStateManager.Instance.penaltyCount}";
    }

    private void CompleteLevelSequence()
    {
        timerRunning = false;
        int timeBonus = Mathf.Max(100, 1000 - Mathf.FloorToInt(elapsedTime * 2f));
        int hintBonus = hintsRemaining * 250;

        int penalties = StoryStateManager.Instance != null ? StoryStateManager.Instance.penaltyCount : 0;
        int penaltyDeduction = 0;
        if (penalties >= 4 && penalties <= 9) penaltyDeduction = (penalties - 3) * 50;
        else if (penalties >= 10) penaltyDeduction = (6 * 50) + ((penalties - 9) * 100);

        int finalScore = Mathf.Max(0, timeBonus + hintBonus - penaltyDeduction);
        int coinsEarned = Mathf.Max(25, finalScore / 10);

        if (StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.coins += coinsEarned;
            StoryStateManager.Instance.SetFlag(levelCompletionFlag);
        }

        Debug.Log($"[Level Complete] Score: {finalScore} | Coins Earned: {coinsEarned}");
    }
}