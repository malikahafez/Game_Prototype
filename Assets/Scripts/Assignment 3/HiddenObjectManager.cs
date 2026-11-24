using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Time Bonus Scoring")]
    [Tooltip("Finishing at or under this time (in seconds) awards the full perfect time bonus.")]
    public float parTimeSeconds = 120f;      // e.g., 2 minutes = perfect score
    [Tooltip("Maximum time (in seconds) after which only the minimum bonus is awarded.")]
    public float maxTimeSeconds = 480f;      // e.g., 8 minutes = floor score
    public int maxTimeBonus = 1000;          // Perfect score bonus
    public int minTimeBonus = 100;           // Minimum guaranteed bonus

    [Header("HUD References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI penaltyCounterText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI hintsText;
    public Transform objectListContainer;
    public GameObject listEntryPrefab; // Prefab with TextMeshProUGUI
    public Button requestHintButton;

    [Header("Level End Screen UI")]
    public GameObject levelEndPanel;
    public TextMeshProUGUI timeBonusText;
    public TextMeshProUGUI hintBonusText;
    public TextMeshProUGUI penaltyDeductionText;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI coinsEarnedText;
    public Button nextLevelButton;
    public Button mainMenuButton;



    [Header("End-of-Level Cutscene & Scene Transitions")]
    [Tooltip("Cutscene panel/recap or Timeline object that plays before loading the target scene.")]
    public GameObject endCutscenePanel;
    public TextMeshProUGUI cutsceneSubtitleText;
    [TextArea(3, 6)]
    public string levelEndCutsceneStory;
    public float cutsceneDuration = 5.0f;
    public Button skipCutsceneButton;

    [Header("End-of-Level Cutscene Routing")]
    public int levelActNumber = 1; // Set to 1 in Level 1, 2 in Level 2, 3 in Level 3
    public string transitionCutsceneSceneName = "04_TransitionCutscene";
    public string nextLevelSceneName = "02_Level2_FortDinosaur";
    public string mainMenuSceneName = "00_MainMenu";


    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip correctPickupClip;
    public AudioClip wrongPickupPenaltyClip;
    public AudioClip hintChimeClip;
    public AudioClip levelCompleteClip;



    [Header("Post-Dialogue Gameplay Start")]
    [Tooltip("Set to 'metVacationer' in Level 1. Leave empty in Levels 2 & 3 to show HUD immediately.")]
    public string startGameplayAfterFlag = "metVacationer";
    public GameObject gameplayHUDPanel; // Assign GameplayHUDPanel here
    public GameObject objectiveUI; // Assign GameplayHUDPanel here

    [Header("Penalty Dialogue")]
    public DialogueData penaltyWarningDialogue;
    public bool triggerOnlyOnFirstDeduction = true; // Fires on the 4th misclick when penalties start deducting points
    private List<HiddenObjectItem> activeQuestItems = new List<HiddenObjectItem>();
    private Dictionary<HiddenObjectItem, TextMeshProUGUI> uiEntries = new Dictionary<HiddenObjectItem, TextMeshProUGUI>();

    [Header("Level 2 Multi-Phase & Count List Settings")]
    public string phase2TriggerFlag = "discoveredLysKidnapping"; // Unlocks Jungle Gate & spawns cloth trail
    public GameObject jungleTrailGroup;                        // Parent GameObject holding all Jungle cloth pieces (disabled at start)
    public string countItemID = "TornCloth";                   // Item ID used by all jungle cloth pieces
    public string countItemDisplayLabel = "Torn Cloth Pieces";

    private int totalCountItems = 0;
    private int collectedCountItems = 0;
    private TextMeshProUGUI countItemUIEntry;


    private float elapsedTime = 0f;
    private bool timerRunning = true;
    private int hintsRemaining;
    private bool hasStartedGameplay = false;
    private string pendingDestinationScene;
    private bool skipCutsceneRequested = false;

    public bool IsLevelCompleteScreenActive => levelEndPanel != null && levelEndPanel.activeSelf;

    private void Awake()
    {
        Instance = this;
        hintsRemaining = maxHintsAllowed;

        if (levelEndPanel != null) levelEndPanel.SetActive(false);
        if (endCutscenePanel != null) endCutscenePanel.SetActive(false);

        if (requestHintButton != null)
            requestHintButton.onClick.AddListener(UseNPCHint);

        // Wire Level End Screen buttons
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(() => TriggerEndCutsceneAndLoad(nextLevelSceneName));

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => TriggerEndCutsceneAndLoad(mainMenuSceneName));

        if (skipCutsceneButton != null)
            skipCutsceneButton.onClick.AddListener(() => skipCutsceneRequested = true);
    }

    private void Start()
    {
        if (StoryStateManager.Instance != null)
            StoryStateManager.Instance.ResetLevelCounters();
        // Call this inside Start() right after ResetLevelCounters():
        if (jungleTrailGroup != null)
            jungleTrailGroup.SetActive(false);

        if (!string.IsNullOrEmpty(phase2TriggerFlag) && StoryStateManager.Instance != null)
            StoryStateManager.Instance.OnFlagSet += OnPhase2FlagTriggered;

        if (!string.IsNullOrEmpty(startGameplayAfterFlag) &&
            (StoryStateManager.Instance == null || !StoryStateManager.Instance.HasFlag(startGameplayAfterFlag)))
        {
            hasStartedGameplay = false;
            timerRunning = false;
            if (StoryStateManager.Instance != null)
                StoryStateManager.Instance.OnFlagSet += OnGameplayStartFlagSet;
        }
        else
        {
            hasStartedGameplay = true;
            timerRunning = true;
        }

        RefreshHUDVisibility();
        UpdateHUD();
    }
    private void OnDestroy()
    {
        if (StoryStateManager.Instance != null)
            StoryStateManager.Instance.OnFlagSet -= OnGameplayStartFlagSet;
    }
    private void OnGameplayStartFlagSet(string flag)
    {
        if (flag == startGameplayAfterFlag)
        {
            hasStartedGameplay = true;
            timerRunning = true;
            RefreshHUDVisibility();

            if (StoryStateManager.Instance != null)
                StoryStateManager.Instance.OnFlagSet -= OnGameplayStartFlagSet;
        }
    }
    /// <summary>
    /// Central check: shows GameplayHUDPanel ONLY when the intro NPC has been spoken to,
    /// no Dialogue is active, and the Pause Menu is closed.
    /// </summary>
    public void RefreshHUDVisibility()
    {
        if (gameplayHUDPanel == null || objectiveUI == null) return;

        bool isDialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        bool isPaused = PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused;

        bool shouldShowHUD = hasStartedGameplay && !isDialogueOpen && !isPaused && !IsLevelCompleteScreenActive;
        gameplayHUDPanel.SetActive(shouldShowHUD);
        objectiveUI.SetActive(!isDialogueOpen && !isPaused && !IsLevelCompleteScreenActive);


    }
    private void Update()
    {
        if (!timerRunning) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) return;
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

        if (!string.IsNullOrEmpty(countItemID) && item.itemID == countItemID)
        {
            totalCountItems++;
            if (countItemUIEntry == null && listEntryPrefab != null && objectListContainer != null)
            {
                GameObject entryObj = Instantiate(listEntryPrefab, objectListContainer);
                countItemUIEntry = entryObj.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (countItemUIEntry != null)
                countItemUIEntry.text = $"{countItemDisplayLabel}: {collectedCountItems}/{totalCountItems}";
            return;
        }

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
        if (!string.IsNullOrEmpty(countItemID) && item.itemID == countItemID)
        {
            collectedCountItems++;
            if (countItemUIEntry != null)
            {
                countItemUIEntry.text = $"{countItemDisplayLabel}: {collectedCountItems}/{totalCountItems}";
                if (collectedCountItems >= totalCountItems)
                {
                    countItemUIEntry.fontStyle = FontStyles.Strikethrough;
                    countItemUIEntry.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
                    if (StoryStateManager.Instance != null)
                        StoryStateManager.Instance.SetFlag("allClothPiecesFound");
                }
            }
        }

        if (!string.IsNullOrEmpty(item.flagToSetOnPickup) && StoryStateManager.Instance != null)
            StoryStateManager.Instance.SetFlag(item.flagToSetOnPickup);

        activeQuestItems.Remove(item);

        // if (activeQuestItems.Count == 0 && !string.IsNullOrEmpty(levelCompletionFlag))
        if (activeQuestItems.Count == 0)
        {
            CompleteLevelSequence();
        }
    }

    public void OnNonQuestItemClicked()
    {
        if (sfxSource != null && wrongPickupPenaltyClip != null)
            sfxSource.PlayOneShot(wrongPickupPenaltyClip);

        if (StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.RegisterWrongPickup();

            // Trigger penalty dialogue either on the 4th misclick (when deductions start) or every misclick
            if (penaltyWarningDialogue != null && DialogueManager.Instance != null)
            {
                if (!triggerOnlyOnFirstDeduction || StoryStateManager.Instance.penaltyCount == 4)
                {
                    DialogueManager.Instance.StartDialogue(penaltyWarningDialogue);
                }
            }
        }

        UpdateHUD();
    }

    /// <summary>
    /// Purchases a hint using coins, plays NPC hint dialogue, and highlights an unfound item.
    /// </summary>
    public void UseNPCHint()
    {
        Debug.Log("using hint");
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
            coinsText.text = $"{StoryStateManager.Instance.coins}";
        if (hintsText != null)
            hintsText.text = $"{hintsRemaining}";

        if (penaltyCounterText != null)
            penaltyCounterText.text = $"{StoryStateManager.Instance.penaltyCount}";
    }

    private void OnPhase2FlagTriggered(string flag)
    {
        if (flag == phase2TriggerFlag && jungleTrailGroup != null)
        {
            jungleTrailGroup.SetActive(true);
            StoryStateManager.Instance.OnFlagSet -= OnPhase2FlagTriggered;
        }
    }

    private void CompleteLevelSequence()
    {
        timerRunning = false;

        if (sfxSource != null && levelCompleteClip != null)
            sfxSource.PlayOneShot(levelCompleteClip);

        // 1. Score & Penalty Calculation
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        // int timeBonus = Mathf.Max(100, 1000 - Mathf.FloorToInt(elapsedTime * 2f));
        // Calculate Time Bonus: 100% of maxTimeBonus if elapsedTime <= parTimeSeconds,
        // scaling smoothly down to minTimeBonus as elapsedTime approaches maxTimeSeconds.
        float overtimeProgress = Mathf.InverseLerp(parTimeSeconds, maxTimeSeconds, elapsedTime);
        int timeBonus = Mathf.RoundToInt(Mathf.Lerp(maxTimeBonus, minTimeBonus, overtimeProgress)); int
                 hintBonus = hintsRemaining * 250;

        int penalties = StoryStateManager.Instance != null ? StoryStateManager.Instance.penaltyCount : 0;
        int penaltyDeduction = 0;
        if (penalties >= 4 && penalties <= 9)
            penaltyDeduction = (penalties - 3) * 50;
        else if (penalties >= 10)
            penaltyDeduction = (6 * 50) + ((penalties - 9) * 100);

        int finalScore = Mathf.Max(0, timeBonus + hintBonus - penaltyDeduction);
        int coinsEarned = Mathf.Max(25, finalScore / 10);

        // 2. Persist coins & completion flag immediately (so Main Menu Level Select unlocks the next level)
        if (StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.coins += coinsEarned;
            if (!string.IsNullOrEmpty(levelCompletionFlag))
                StoryStateManager.Instance.SetFlag(levelCompletionFlag);
        }

        // 3. Populate Level End Screen UI
        if (timeBonusText != null)
            timeBonusText.text = $"+{timeBonus}";

        if (hintBonusText != null)
            hintBonusText.text = $"+{hintBonus}";

        if (penaltyDeductionText != null)
            penaltyDeductionText.text = $"-{penaltyDeduction}";

        if (totalScoreText != null)
            totalScoreText.text = $"{finalScore}";

        if (coinsEarnedText != null)
            coinsEarnedText.text = $"+{coinsEarned}";

        // 4. Show Level End Panel & hide Gameplay HUD
        if (levelEndPanel != null)
            levelEndPanel.SetActive(true);

        RefreshHUDVisibility();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    /// <summary>
    /// Triggered by either "Next Level" or "Main Menu" on the Level End Screen.
    /// Plays the level's end cutscene first, then loads the chosen destination scene.
    /// </summary>
    public void TriggerEndCutsceneAndLoad(string targetSceneName)
    {
        if (StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.cutsceneToPlay = levelActNumber;
            StoryStateManager.Instance.postCutsceneDestinationScene = targetSceneName;
        }

        SceneManager.LoadScene(transitionCutsceneSceneName);
    }
    private IEnumerator PlayCutsceneThenLoadRoutine()
    {
        if (levelEndPanel != null)
            levelEndPanel.SetActive(false);

        if (endCutscenePanel != null)
        {
            endCutscenePanel.SetActive(true);
            if (cutsceneSubtitleText != null)
                cutsceneSubtitleText.text = levelEndCutsceneStory;
        }

        skipCutsceneRequested = false;
        float timer = 0f;

        while (timer < cutsceneDuration && !skipCutsceneRequested)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(pendingDestinationScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}