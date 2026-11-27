using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SpeakerCameraMapping
{
    [Tooltip("Exact speakerName from DialogueLine (e.g. 'Bowen', 'Lys La Rue', 'Nobs').")]
    public string speakerName;
    [Tooltip("CinemachineCamera or Camera GameObject framing this speaker.")]
    public GameObject speakerCamera;
}

/// <summary>
/// Controls the dialogue UI panel, typewriter text reveal, character portraits,
/// per-speaker cutscene camera switching, and flag updates upon dialogue completion.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Reticle UI")]
    public GameObject reticle;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI bodyText;
    public Image portraitImage;
    public Button nextButton;

    [Header("Settings")]
    public float typewriterSpeed = 0.025f;
    public AudioSource audioSource;

    private DialogueData currentDialogue;
    private DialogueLine[] activeLines;
    private int currentLineIndex;
    private bool isTyping;
    private Coroutine typingCoroutine;

    [Header("Cutscene Camera Switching")]
    [Tooltip("Default fallback camera framing the NPC if no speaker-specific camera is assigned.")]
    public GameObject dialogueCutsceneCamera;
    [Tooltip("Camera framing Bowen (the player) during his dialogue lines. Leave empty to use default view.")]
    public GameObject bowenDialogueCamera;
    [Tooltip("Optional mapping of speaker names (e.g. 'Lys La Rue', 'Nobs') to dedicated cameras.")]
    public List<SpeakerCameraMapping> speakerCameras = new List<SpeakerCameraMapping>();

    [HideInInspector]
    public GameObject activeNpcCamera; // Can be assigned dynamically per NPC by NarrativeTriggerAndTransition

    [Header("NPC Animation")]
    [Tooltip("Drag the NPC's Animator here (or assign dynamically per NPC).")]
    public Animator activeNpcAnimator;
    public string talkingBoolParameter = "isTalking";

    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);

        DisableAllDialogueCameras();
    }

    public void StartDialogue(DialogueData data)
    {
        if (data == null) return;

        if (!string.IsNullOrEmpty(data.requiredFlag) &&
            StoryStateManager.Instance != null &&
            !StoryStateManager.Instance.HasFlag(data.requiredFlag))
        {
            return;
        }

        if (reticle != null) reticle.SetActive(false);

        currentDialogue = data;

        // Meaningful player action reactivity: switch lines if player rushed & triggered 4+ penalties
        if (StoryStateManager.Instance != null &&
            StoryStateManager.Instance.penaltyCount >= 4 &&
            data.highPenaltyLines != null &&
            data.highPenaltyLines.Length > 0)
        {
            activeLines = data.highPenaltyLines;
        }
        else
        {
            activeLines = data.lines;
        }

        currentLineIndex = 0;
        dialoguePanel.SetActive(true);

        if (HiddenObjectManager.Instance != null)
            HiddenObjectManager.Instance.RefreshHUDVisibility();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        if (currentLineIndex >= activeLines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = activeLines[currentLineIndex];
        speakerNameText.text = line.speakerName;
        portraitImage.sprite = line.speakerPortrait;
        portraitImage.enabled = (line.speakerPortrait != null);

        // Switch cutscene camera and NPC talking animation based on who is currently speaking
        SwitchCameraForSpeaker(line.speakerName);

        if (audioSource != null && line.voiceOrFoleyClip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(line.voiceOrFoleyClip);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line.text));
    }

    private void SwitchCameraForSpeaker(string speakerName)
    {
        DisableAllDialogueCameras();

        bool isPlayerSpeaking = !string.IsNullOrEmpty(speakerName) &&
                                speakerName.IndexOf("Bowen", System.StringComparison.OrdinalIgnoreCase) >= 0;

        // Toggle NPC talking animation only when the NPC is speaking
        if (activeNpcAnimator != null && !string.IsNullOrEmpty(talkingBoolParameter))
        {
            activeNpcAnimator.SetBool(talkingBoolParameter, !isPlayerSpeaking);
        }

        // 1. Check if a specific camera is mapped to this speaker's name in Speaker Cameras
        if (!string.IsNullOrEmpty(speakerName))
        {
            foreach (SpeakerCameraMapping mapping in speakerCameras)
            {
                if (mapping.speakerCamera != null &&
                    string.Equals(mapping.speakerName.Trim(), speakerName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    mapping.speakerCamera.SetActive(true);
                    return;
                }
            }
        }

        // 2. If Bowen is speaking, enable Bowen's camera (if assigned)
        if (isPlayerSpeaking)
        {
            if (bowenDialogueCamera != null)
                bowenDialogueCamera.SetActive(true);
            return;
        }

        // 3. Otherwise enable the active NPC's camera, or fall back to dialogueCutsceneCamera
        if (activeNpcCamera != null)
        {
            activeNpcCamera.SetActive(true);
        }
        else if (dialogueCutsceneCamera != null)
        {
            dialogueCutsceneCamera.SetActive(true);
        }
    }

    private void DisableAllDialogueCameras()
    {
        if (dialogueCutsceneCamera != null) dialogueCutsceneCamera.SetActive(false);
        if (bowenDialogueCamera != null) bowenDialogueCamera.SetActive(false);
        if (activeNpcCamera != null) activeNpcCamera.SetActive(false);

        foreach (SpeakerCameraMapping mapping in speakerCameras)
        {
            if (mapping.speakerCamera != null)
                mapping.speakerCamera.SetActive(false);
        }
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        bodyText.text = "";
        foreach (char c in fullText)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        isTyping = false;
    }

    public void OnNextClicked()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            bodyText.text = activeLines[currentLineIndex].text;
            isTyping = false;
        }
        else
        {
            currentLineIndex++;
            DisplayCurrentLine();
        }
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);

        if (activeNpcAnimator != null && !string.IsNullOrEmpty(talkingBoolParameter))
            activeNpcAnimator.SetBool(talkingBoolParameter, false);

        DisableAllDialogueCameras();
        activeNpcCamera = null;
        activeNpcAnimator = null;

        if (HiddenObjectManager.Instance != null)
            HiddenObjectManager.Instance.RefreshHUDVisibility();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (reticle != null) reticle.SetActive(true);

        if (currentDialogue != null && !string.IsNullOrEmpty(currentDialogue.flagToSetWhenFinished) && StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.SetFlag(currentDialogue.flagToSetWhenFinished);
        }
    }
}