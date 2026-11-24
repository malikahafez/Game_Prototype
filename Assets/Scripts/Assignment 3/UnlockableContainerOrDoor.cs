using TMPro;
using UnityEngine;

/// <summary>
/// Opens doors or wooden crates when the player either holds a required tool/flag
/// (e.g., "Crowbar", "RustyKey", or "discoveredLysKidnapping") and presses [E].
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
    public string lockedMessage = "Jammed shut. Need a Crowbar to pry open.";
    public string unlockedPrompt = "Press [E] to Open";
    public DialogueData lockedDialogue;
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
            else if (lockedDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(lockedDialogue);
            }
        }
    }

    private bool CanOpen()
    {
        bool toolOK = string.IsNullOrEmpty(requiredToolID) ||
                      (InventoryAndCraftingSystem.Instance != null &&
                       InventoryAndCraftingSystem.Instance.HasTool(requiredToolID));

        bool flagOK = string.IsNullOrEmpty(requiredStoryFlag) ||
                      (StoryStateManager.Instance != null &&
                       StoryStateManager.Instance.HasFlag(requiredStoryFlag));

        return toolOK && flagOK;
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
                promptText.text = CanOpen() ? unlockedPrompt : lockedMessage;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }
}