using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles zone-entry triggers, NPC dialogue triggers, flag-gated interactions
/// (with optional fallback dialogue when the prerequisite flag is missing), and async scene transitions.
/// </summary>
public class NarrativeTriggerAndTransition : MonoBehaviour
{
    public enum TriggerMode { ZoneEnter, DirectInteract, SceneTransitionGate }

    [Header("Trigger Mode")]
    public TriggerMode mode = TriggerMode.DirectInteract;
    public bool isNPC = false;
    public bool fireOnce = true;

    [Header("Narrative Conditions & Actions")]
    public string requiredFlag;
    public string flagToSet;
    public DialogueData dialogueToPlay;
    [Tooltip("Optional dialogue played if the player interacts BEFORE 'requiredFlag' is set (does not consume fireOnce or set flagToSet).")]
    public DialogueData missingFlagFallbackDialogue;

    [Header("Scene Transition Settings")]
    public string nextSceneName;
    public GameObject loadingRecapPanel;
    public TextMeshProUGUI recapStoryText;
    [TextArea(3, 6)]
    public string transitionRecapMessage;
    public Animator npcAnimator;

    private bool hasFired = false;

    public GameObject npcDialogueCamera;

    private void OnTriggerEnter(Collider other)
    {
        if (mode != TriggerMode.ZoneEnter) return;
        if (!other.CompareTag("Player")) return;
        TriggerInteraction();
    }

    public void TriggerInteraction()
    {
        if (fireOnce && hasFired) return;

        // Check prerequisite story state before allowing main progression
        if (!string.IsNullOrEmpty(requiredFlag) &&
            StoryStateManager.Instance != null &&
            !StoryStateManager.Instance.HasFlag(requiredFlag))
        {
            Debug.Log($"[Blocked] Missing required flag: {requiredFlag}");

            // Play repeatable fallback dialogue (e.g., Lys asking for a knife) without setting hasFired
            if (missingFlagFallbackDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.activeNpcAnimator = npcAnimator;
                DialogueManager.Instance.StartDialogue(missingFlagFallbackDialogue);
            }
            return;
        }

        hasFired = true;

        if (!string.IsNullOrEmpty(flagToSet) && StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.SetFlag(flagToSet);
        }

        if (dialogueToPlay != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.activeNpcAnimator = npcAnimator;
            DialogueManager.Instance.activeNpcCamera = npcDialogueCamera;
            DialogueManager.Instance.StartDialogue(dialogueToPlay);
        }

        if (mode == TriggerMode.SceneTransitionGate && !string.IsNullOrEmpty(nextSceneName))
        {
            StartCoroutine(LoadNextLevelWithRecap());
        }
    }

    private IEnumerator LoadNextLevelWithRecap()
    {
        if (loadingRecapPanel != null)
        {
            loadingRecapPanel.SetActive(true);
            if (recapStoryText != null) recapStoryText.text = transitionRecapMessage;
        }

        yield return new WaitForSeconds(4.0f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}