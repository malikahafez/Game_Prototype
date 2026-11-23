using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles the Level 1 shoreline fishing interaction:
/// 1. Detects when the player enters the water trigger zone.
/// 2. Checks if the player has crafted the "FishingPole" ("craftedFishingPole").
/// 3. On pressing [E], displays the fishing rod in hand, plays the casting animation,
///    and spawns/enables the sealed 1916 Thermos on the shore for pickup.
/// </summary>
public class FishingSpotController : MonoBehaviour
{
    [Header("Prerequisite Settings")]
    public string requiredToolID = "FishingPole";
    public string requiredStoryFlag = "craftedFishingPole";
    public KeyCode interactKey = KeyCode.E;

    [Header("Fishing Rod & Animation")]
    [Tooltip("Child object attached to the Player's camera/hand, disabled by default.")]
    public GameObject handFishingRodModel;
    [Tooltip("Optional Animator on the handFishingRodModel with a 'Cast' trigger.")]
    public Animator fishingRodAnimator;
    public string castTriggerName = "Cast";
    public float castDuration = 2.5f;

    [Header("Thermos Reward")]
    [Tooltip("The 3D Thermos prop on the shore (set inactive at scene start).")]
    public GameObject shoreThermosObject;
    [Tooltip("Optional particle splash/sparkle when the thermos lands on the shore.")]
    public ParticleSystem shoreRevealEffect;

    [Header("UI Prompt & Dialogue")]
    public GameObject interactionPromptUI; // e.g., "Press [E] to Cast Fishing Line"
    public TextMeshProUGUI promptText;
    public DialogueData missingPoleDialogue; // Optional internal monologue if player lacks the pole

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip castLineClip;
    public AudioClip waterSplashClip;

    private bool playerInZone = false;
    private bool hasFished = false;
    private bool isCasting = false;

    private void Start()
    {
        // Hide the hand fishing rod, prompt UI, and shore thermos at the start of Level 1
        if (handFishingRodModel != null)
            handFishingRodModel.SetActive(false);

        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(false);

        if (shoreThermosObject != null)
            shoreThermosObject.SetActive(false);
    }

    private void Update()
    {
        if (!playerInZone || hasFished || isCasting) return;

        // Ignore input if Dialogue, Lore Card, or Pause Menu is open
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) ||
            (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) ||
            (InventoryAndCraftingSystem.Instance != null && InventoryAndCraftingSystem.Instance.IsInspectingUI))
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            if (HasAssembledFishingPole())
            {
                StartCoroutine(PerformFishingSequence());
            }
            else
            {
                // Player reached the shore without crafting the Fishing Pole yet
                if (missingPoleDialogue != null && DialogueManager.Instance != null)
                    DialogueManager.Instance.StartDialogue(missingPoleDialogue);
            }
        }
    }

    private bool HasAssembledFishingPole()
    {
        bool hasTool = InventoryAndCraftingSystem.Instance != null &&
                       InventoryAndCraftingSystem.Instance.HasTool(requiredToolID);

        bool hasFlag = StoryStateManager.Instance != null &&
                       StoryStateManager.Instance.HasFlag(requiredStoryFlag);

        return hasTool || hasFlag;
    }

    private IEnumerator PerformFishingSequence()
    {
        isCasting = true;
        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(false);

        // 1. Show the Fishing Rod in the player's hand
        if (handFishingRodModel != null)
            handFishingRodModel.SetActive(true);

        if (sfxSource != null && castLineClip != null)
            sfxSource.PlayOneShot(castLineClip);

        // 2. Play Animator trigger if assigned, otherwise run smooth procedural cast swing
        if (fishingRodAnimator != null && fishingRodAnimator.runtimeAnimatorController != null)
        {
            fishingRodAnimator.SetTrigger(castTriggerName);
            yield return new WaitForSeconds(castDuration);
        }
        else if (handFishingRodModel != null)
        {
            yield return StartCoroutine(ProceduralCastAnimation(handFishingRodModel.transform, castDuration));
        }
        else
        {
            yield return new WaitForSeconds(castDuration);
        }

        // 3. Play splash SFX and hide the hand fishing rod
        if (sfxSource != null && waterSplashClip != null)
            sfxSource.PlayOneShot(waterSplashClip);

        if (handFishingRodModel != null)
            handFishingRodModel.SetActive(false);

        // 4. Reveal the Thermos on the shore for the player to pick up
        if (shoreThermosObject != null)
        {
            shoreThermosObject.SetActive(true);
            if (shoreRevealEffect != null)
                shoreRevealEffect.Play();
        }

        hasFished = true;
        isCasting = false;
    }

    /// <summary>
    /// Fallback procedural casting motion (wind up back, whip forward, reel in)
    /// so the sequence animates smoothly even without an Animator Controller.
    /// </summary>
    private IEnumerator ProceduralCastAnimation(Transform rodTransform, float totalDuration)
    {
        Quaternion startRot = rodTransform.localRotation;
        Quaternion windUpRot = startRot * Quaternion.Euler(-35f, 0f, 0f);
        Quaternion castForwardRot = startRot * Quaternion.Euler(45f, 0f, 0f);

        // Phase 1: Wind up back (25% of duration)
        float t = 0f;
        float windUpTime = totalDuration * 0.25f;
        while (t < windUpTime)
        {
            t += Time.deltaTime;
            rodTransform.localRotation = Quaternion.Slerp(startRot, windUpRot, t / windUpTime);
            yield return null;
        }

        // Phase 2: Whip rod forward to cast line (20% of duration)
        t = 0f;
        float whipTime = totalDuration * 0.20f;
        while (t < whipTime)
        {
            t += Time.deltaTime;
            rodTransform.localRotation = Quaternion.Slerp(windUpRot, castForwardRot, t / whipTime);
            yield return null;
        }

        // Phase 3: Hold line in water & reel back (55% of duration)
        yield return new WaitForSeconds(totalDuration * 0.35f);

        t = 0f;
        float returnTime = totalDuration * 0.20f;
        while (t < returnTime)
        {
            t += Time.deltaTime;
            rodTransform.localRotation = Quaternion.Slerp(castForwardRot, startRot, t / returnTime);
            yield return null;
        }

        rodTransform.localRotation = startRot;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasFished || !other.CompareTag("Player")) return;

        playerInZone = true;
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(true);
            if (promptText != null)
            {
                promptText.text = HasAssembledFishingPole()
                    ? "Press [E] to Cast Fishing Line"
                    : "You need an assembled Fishing Pole to fish here.";
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = false;
        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(false);
    }
}