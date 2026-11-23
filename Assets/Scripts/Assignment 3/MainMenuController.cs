using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the Starting Menu scene and the Expedition Hub (Level Select).
/// Allows players to start the expedition, navigate through levels in narrative order
/// based on StoryStateManager progression flags, and quit the application.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;

    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button levelSelectButton;
    public Button quitButton;

    [Header("Level Select (Expedition Hub) Buttons")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;
    public Button backButton;

    [Header("Level Lock Overlays & Narrative Preview")]
    public GameObject level2LockedOverlay; // e.g., Wax seal / locked icon
    public GameObject level3LockedOverlay;
    public TextMeshProUGUI chapterTitleText;
    public TextMeshProUGUI chapterDescriptionText;

    [Header("Scene Names (Must Match Build Settings)")]
    public string level1SceneName = "01_Level1_CapeFarewell";
    public string level2SceneName = "02_Level2_FortDinosaur";
    public string level3SceneName = "03_Level3_StoLuCamp";

    [Header("Optional Audio")]
    public AudioSource uiAudioSource;
    public AudioClip buttonClickClip;

    private void Start()
    {
        // Ensure cursor is visible and unlocked on the Main Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        // Show Main Menu panel and hide Level Select panel initially
        ShowMainMenu();

        // Bind Main Menu button listeners
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (levelSelectButton != null)
            levelSelectButton.onClick.AddListener(OnLevelSelectClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // Bind Level Select button listeners
        if (level1Button != null)
            level1Button.onClick.AddListener(() => LoadLevel(level1SceneName));

        if (level2Button != null)
            level2Button.onClick.AddListener(() => LoadLevel(level2SceneName));

        if (level3Button != null)
            level3Button.onClick.AddListener(() => LoadLevel(level3SceneName));

        if (backButton != null)
            backButton.onClick.AddListener(ShowMainMenu);

        RefreshLevelUnlockStates();
    }

    /// <summary>
    /// Starts the game from Act I (Level 1: Cape Farewell Beach).
    /// </summary>
    public void OnStartClicked()
    {
        PlayClickSound();
        LoadLevel(level1SceneName);
    }

    /// <summary>
    /// Opens the Expedition Hub / Nautical Chart level selector and updates locked states.
    /// </summary>
    public void OnLevelSelectClicked()
    {
        PlayClickSound();
        RefreshLevelUnlockStates();

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
    }

    /// <summary>
    /// Returns from the Level Select screen back to the Main Menu.
    /// </summary>
    public void ShowMainMenu()
    {
        PlayClickSound();
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Conditions level access on StoryStateManager flags so players progress in narrative order.
    /// </summary>
    private void RefreshLevelUnlockStates()
    {
        // Level 1 is always unlocked
        if (level1Button != null)
            level1Button.interactable = true;

        // Level 2 unlocks when Act 1 (Thermos recovery) is complete
        bool isAct1Complete = StoryStateManager.Instance != null &&
                              StoryStateManager.Instance.HasFlag("act1Complete");

        if (level2Button != null)
            level2Button.interactable = isAct1Complete;
        if (level2LockedOverlay != null)
            level2LockedOverlay.SetActive(!isAct1Complete);

        // Level 3 unlocks when Act 2 (Tracking Lys's muslin trail) is complete
        bool isAct2Complete = StoryStateManager.Instance != null &&
                              StoryStateManager.Instance.HasFlag("act2Complete");

        if (level3Button != null)
            level3Button.interactable = isAct2Complete;
        if (level3LockedOverlay != null)
            level3LockedOverlay.SetActive(!isAct2Complete);

        // Set default placeholder title and description
        if (chapterTitleText != null)
            chapterTitleText.text = "Expedition Logbook";

        if (chapterDescriptionText != null)
            chapterDescriptionText.text = "Select an unlocked chapter from the Expedition Chart to investigate the fate of the U-33.";
    }

    /// <summary>
    /// Hook onto Button Hover (EventTrigger -> PointerEnter) to update both
    /// the chapter title and story summary on the Level Select screen.
    /// </summary>
    public void PreviewChapterSummary(int chapterIndex)
    {
        bool isAct1Complete = StoryStateManager.Instance != null &&
                              StoryStateManager.Instance.HasFlag("act1Complete");
        bool isAct2Complete = StoryStateManager.Instance != null &&
        StoryStateManager.Instance.HasFlag("act2Complete");
        switch (chapterIndex)
        {
            case 1:
                if (chapterTitleText != null)
                    chapterTitleText.text = "Act I: Cape Farewell Beach";
                if (chapterDescriptionText != null)
                    chapterDescriptionText.text = "You are on a fishing trip with a fellow vacationer, until you find a thermos with a mysterious story set way back in 1916.";
                break;

            case 2:
                if (isAct1Complete)
                {
                    if (chapterTitleText != null)
                        chapterTitleText.text = "Act II: Fort Dinosaur & Jungle";
                    if (chapterDescriptionText != null)
                        chapterDescriptionText.text = "What you thought was just another day gathering camp supplies escalates into something more perilous.";
                }
                else
                {
                    if (chapterTitleText != null)
                        chapterTitleText.text = "Act II: Fort Dinosaur & Jungle";
                    if (chapterDescriptionText != null)
                        chapterDescriptionText.text = "Unlock level to find out!";
                }
                break;

            case 3:
                if (isAct2Complete)
                {
                    if (chapterTitleText != null)
                        chapterTitleText.text = "Act III: Sto-Lu Cliff Camp";
                    if (chapterDescriptionText != null)
                        chapterDescriptionText.text = "Having pursued Lys and her captor, Kho, through the jungle, you finally arrive at the Sto-Lu camp. Do you have what it takes to rescue Lys?";
                }
                else
                {
                    if (chapterTitleText != null)
                        chapterTitleText.text = "Act III: Sto-Lu Cliff Camp";
                    if (chapterDescriptionText != null)
                        chapterDescriptionText.text = "Unlock level to find out!";
                }
                break;
            default:
                if (chapterTitleText != null)
                    chapterTitleText.text = "Expedition Logbook";

                if (chapterDescriptionText != null)
                    chapterDescriptionText.text = "Select an unlocked chapter from the Expedition Chart to investigate the fate of the U-33.";
                break;

        }
    }

    private void LoadLevel(string sceneName)
    {
        PlayClickSound();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Exits the application (also stops Play Mode when testing inside the Unity Editor).
    /// </summary>
    public void OnQuitClicked()
    {
        PlayClickSound();
        Debug.Log("[MainMenu] Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayClickSound()
    {
        if (uiAudioSource != null && buttonClickClip != null)
            uiAudioSource.PlayOneShot(buttonClickClip);
    }
}