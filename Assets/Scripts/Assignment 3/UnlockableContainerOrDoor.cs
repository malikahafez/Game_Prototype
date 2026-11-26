using TMPro;
using UnityEngine;

/// <summary>
/// Opens doors or wooden crates when the player either holds a required tool/flag
/// (e.g., "Crowbar", "DwellingKey", or "discoveredLysKidnapping") and presses [E].
/// Displays distinct locked messages depending on which requirement is missing.
/// </summary>
public class UnlockableContainerOrDoor : MonoBehaviour
{
    [Header("Unlock Requirements")]
    [Tooltip("Leave empty if unlocked via Story Flag, or enter Tool ID (e.g. 'Crowbar' or 'DwellingKey').")]
    public string requiredToolID = "Crowbar";
    [Tooltip("Optional Story Flag that unlocks this door automatically (e.g. 'discoveredLysKidnapping').")]
    public string requiredStoryFlag = "";
    public KeyCode interactKey = KeyCode.E;

    [Header("Door / Lid Transform")]
    public Transform hingedTransform;     // Door pivot or crate lid
    public Vector3 openLocalEulerAngles = new Vector3(0f, 95f, 0f);
    public bool disableColliderOnOpen = true;

    [Header("Feedback & Narrative")]
    public GameObject promptUI;
    public TextMeshProUGUI promptText;
    public string unlockedPrompt = "Press [E] to Open";

    [Tooltip("Shown when the player is missing the required Tool (e.g. Crowbar or Key).")]
    public string missingToolMessage = "Jammed shut. Need a Crowbar to pry open.";
    [Tooltip("Shown when the player hasn't reached the required story milestone yet.")]
    public string missingFlagMessage = "The gate is barred. Check on Lys before heading into the jungle.";

    public DialogueData missingToolDialogue;
    public DialogueData missingFlagDialogue;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip openClip;

    private bool playerInRange = false;
    private bool isOpen = false;

    private void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange || isOpen) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (CanOpen())
            {
                OpenObject();
            }
            else
            {
                DialogueData dialogueToPlay = GetMissingRequirementDialogue();
                if (dialogueToPlay != null && DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(dialogueToPlay);
                }
            }
        }
    }

    private bool HasRequiredTool()
    {
        return string.IsNullOrEmpty(requiredToolID) ||
               (InventoryAndCraftingSystem.Instance != null &&
                InventoryAndCraftingSystem.Instance.HasTool(requiredToolID));
    }

    private bool HasRequiredFlag()
    {
        return string.IsNullOrEmpty(requiredStoryFlag) ||
               (StoryStateManager.Instance != null &&
                StoryStateManager.Instance.HasFlag(requiredStoryFlag));
    }

    private bool CanOpen()
    {
        return HasRequiredTool() && HasRequiredFlag();
    }

    private string GetPromptMessage()
    {
        if (!HasRequiredFlag())
            return missingFlagMessage;

        if (!HasRequiredTool())
            return missingToolMessage;

        return unlockedPrompt;
    }

    private DialogueData GetMissingRequirementDialogue()
    {
        if (!HasRequiredFlag())
            return missingFlagDialogue;

        if (!HasRequiredTool())
            return missingToolDialogue;

        return null;
    }

    private void OpenObject()
    {
        isOpen = true;
        if (promptUI != null) promptUI.SetActive(false);

        if (hingedTransform != null)
            hingedTransform.localEulerAngles = openLocalEulerAngles;
        else
            gameObject.SetActive(false);

        if (disableColliderOnOpen && TryGetComponent<Collider>(out Collider col))
            col.enabled = false;

        if (sfxSource != null && openClip != null)
            sfxSource.PlayOneShot(openClip);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpen || !other.CompareTag("Player")) return;
        playerInRange = true;

        if (promptUI != null)
        {
            promptUI.SetActive(true);
            if (promptText != null)
                promptText.text = GetPromptMessage();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }
}