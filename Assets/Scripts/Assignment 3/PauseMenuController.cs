using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using StarterAssets;

/// <summary>
/// Manages the in-game Pause Menu (Resume, Restart Level, Return to Main Menu, Quit,
/// and basic Audio/Sensitivity settings) while safely restoring Time.timeScale and cursor lock states.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("Reticle UI")]
    public GameObject reticle;

    [Header("Pause UI Panels")]
    public GameObject pauseMenuPanel;
    public GameObject settingsSubPanel;

    [Header("Pause Menu Buttons")]
    public Button resumeButton;
    public Button restartLevelButton;
    public Button mainMenuButton;
    public Button settingsButton;
    public Button closeSettingsButton;
    public Button quitButton;

    [Header("Settings Controls")]
    public Slider masterVolumeSlider;
    public Slider mouseSensitivitySlider;

    [Header("Navigation Settings")]
    public string mainMenuSceneName = "00_MainMenu";
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("Optional Audio")]
    public AudioSource uiAudioSource;
    public AudioClip buttonClickClip;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsSubPanel != null) settingsSubPanel.SetActive(false);

        // Bind button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartLevelButton != null)
            restartLevelButton.onClick.AddListener(RestartCurrentLevel);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Bind slider listeners
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = AudioListener.volume;
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (mouseSensitivitySlider != null)
        {
            FirstPersonExplorerController player = FindFirstObjectByType<FirstPersonExplorerController>();
            if (player != null)
                mouseSensitivitySlider.value = player.RotationSpeed;

            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            // If settings sub-panel is open, close it first
            if (settingsSubPanel != null && settingsSubPanel.activeSelf)
            {
                CloseSettings();
                return;
            }

            if (IsPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    /// <summary>
    /// Freezes gameplay time, displays the Pause Menu, and unlocks the cursor.
    /// </summary>
    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        if (HiddenObjectManager.Instance != null)
            HiddenObjectManager.Instance.RefreshHUDVisibility();

        if (reticle != null)
        {
            reticle.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Unfreezes time, hides the Pause Menu, and restores cursor state based on active UI.
    /// </summary>
    public void ResumeGame()
    {
        PlayClickSound();
        IsPaused = false;
        Time.timeScale = 1f;

        if (settingsSubPanel != null) settingsSubPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (HiddenObjectManager.Instance != null)
            HiddenObjectManager.Instance.RefreshHUDVisibility();

        bool isModalOpen =
            (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) ||
            (InventoryAndCraftingSystem.Instance != null && InventoryAndCraftingSystem.Instance.IsInspectingUI);

        if (reticle != null)
            reticle.SetActive(!isModalOpen);

        // Keep cursor unlocked & visible during gameplay so the player can click HUD buttons (Hints, Pause, Tabs)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Reloads the active level scene cleanly and resets per-level counters.
    /// </summary>
    public void RestartCurrentLevel()
    {
        PlayClickSound();
        Time.timeScale = 1f;
        IsPaused = false;

        if (StoryStateManager.Instance != null)
            StoryStateManager.Instance.ResetLevelCounters();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Returns to the Main Menu / Expedition Hub while preserving StoryStateManager progress.
    /// </summary>
    public void ReturnToMainMenu()
    {
        PlayClickSound();
        Time.timeScale = 1f;
        IsPaused = false;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenSettings()
    {
        PlayClickSound();
        if (settingsSubPanel != null)
            settingsSubPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayClickSound();
        if (settingsSubPanel != null)
            settingsSubPanel.SetActive(false);
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        FirstPersonExplorerController player = FindFirstObjectByType<FirstPersonExplorerController>();
        if (player != null)
            player.RotationSpeed = sensitivity;
    }

    public void QuitGame()
    {
        PlayClickSound();
        Time.timeScale = 1f;

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