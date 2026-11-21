using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles zone-entry triggers, NPC dialogue triggers, flag-gated interactions
/// (e.g., fishing out the thermos, unlocking Lys's dwelling), and async scene transitions.
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

    [Header("Scene Transition Settings")]
    public string nextSceneName;
    public GameObject loadingRecapPanel;
    public TextMeshProUGUI recapStoryText;
    [TextArea(3, 6)]
    public string transitionRecapMessage;

    private bool hasFired = false;

    private void OnTriggerEnter(Collider other)
    {
        if (mode != TriggerMode.ZoneEnter) return;
        if (!other.CompareTag("Player")) return;

        TriggerInteraction();
    }

    public void TriggerInteraction()
    {
        if (fireOnce && hasFired) return;

        // Verify required story state before allowing progression
        if (!string.IsNullOrEmpty(requiredFlag) &&
            StoryStateManager.Instance != null &&
            !StoryStateManager.Instance.HasFlag(requiredFlag))
        {
            Debug.Log($"[Blocked] Missing required flag: {requiredFlag}");
            return;
        }

        hasFired = true;

        if (!string.IsNullOrEmpty(flagToSet) && StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.SetFlag(flagToSet);
        }

        if (dialogueToPlay != null && DialogueManager.Instance != null)
        {
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
            if (recapStoryText != null)
                recapStoryText.text = transitionRecapMessage;
        }

        // Allow player time to read the narrative recap excerpt
        yield return new WaitForSeconds(4.0f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}