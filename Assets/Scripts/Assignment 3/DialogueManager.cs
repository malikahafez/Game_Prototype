using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// Controls the dialogue UI panel, typewriter text reveal, character portraits,
// and flag updates upon dialogue completion.

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

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

    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
    }

    public void StartDialogue(DialogueData data)
    {
        if (data == null) return;

        if (!string.IsNullOrEmpty(data.requiredFlag) &&
            !StoryStateManager.Instance.HasFlag(data.requiredFlag))
        {
            return;
        }

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

        if (audioSource != null && line.voiceOrFoleyClip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(line.voiceOrFoleyClip);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line.text));
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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (currentDialogue != null && !string.IsNullOrEmpty(currentDialogue.flagToSetWhenFinished))
        {
            StoryStateManager.Instance.SetFlag(currentDialogue.flagToSetWhenFinished);
        }
    }
}