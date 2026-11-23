using System.Collections;
using UnityEngine;

/// <summary>
/// Represents either a required Hidden Object List item, a usable inventory tool,
/// or a non-quest decoy prop (which snaps back to its original position and adds a penalty).
/// </summary>
public class HiddenObjectItem : MonoBehaviour
{
    public enum ItemCategory { QuestListItem, InventoryTool, NonQuestDecoy }

    [Header("Item Configuration")]
    public ItemCategory category = ItemCategory.QuestListItem;
    public string itemID;
    public string displayPrompt = "Brass Compass";
    [TextArea(2, 4)]
    public string loreDescription = "An early 20th-century brass compass recovered from the shore.";
    public Sprite itemIcon;

    [Header("Optional Narrative & Hint Links")]
    public string requiredFlagToCollect; // e.g., requires "craftedTelescope" or "lanternLit"
    public string flagToSetOnPickup;
    public DialogueData npcHintDialogue;
    public ParticleSystem hintGlowEffect;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        if ((category == ItemCategory.QuestListItem || category == ItemCategory.InventoryTool) && HiddenObjectManager.Instance != null)
        {
            Debug.Log("registered hidden object quest item");
            HiddenObjectManager.Instance.RegisterQuestItem(this);
        }
    }

    public void OnClicked()
    {
        // Check if a prerequisite tool/flag is needed (e.g., high items needing Telescope)
        if (!string.IsNullOrEmpty(requiredFlagToCollect) &&
            StoryStateManager.Instance != null &&
            !StoryStateManager.Instance.HasFlag(requiredFlagToCollect))
        {
            Debug.Log($"[Locked Item] Requires flag: {requiredFlagToCollect}");
            return;
        }

        if (category == ItemCategory.QuestListItem)
        {
            HiddenObjectManager.Instance.OnQuestItemCollected(this);
            gameObject.SetActive(false);
        }
        else if (category == ItemCategory.InventoryTool)
        {
            HiddenObjectManager.Instance.OnQuestItemCollected(this);

            if (InventoryAndCraftingSystem.Instance != null)
                InventoryAndCraftingSystem.Instance.AddTool(this);

            if (!string.IsNullOrEmpty(flagToSetOnPickup) && StoryStateManager.Instance != null)
                StoryStateManager.Instance.SetFlag(flagToSetOnPickup);

            gameObject.SetActive(false);
        }
        else if (category == ItemCategory.NonQuestDecoy)
        {
            HiddenObjectManager.Instance.OnNonQuestItemClicked();
            StartCoroutine(SnapBackAnimation());
        }
    }

    public void HighlightHintParticle()
    {
        if (hintGlowEffect != null)
        {
            // hintGlowEffect.transform.position = transform.position + Vector3.up * 0.2f;
            hintGlowEffect.gameObject.SetActive(true);
            hintGlowEffect.Clear();
            hintGlowEffect.Play(true);
            Debug.Log($"[Hint] Playing particle effect on: {displayPrompt}");
        }
        else
        {
            Debug.LogWarning($"[Hint] No ParticleSystem assigned to Hint Glow Effect on: {displayPrompt}");
        }
    }

    private IEnumerator SnapBackAnimation()
    {
        // Briefly lift the decoy item so the player sees they picked it up, then snap it back
        transform.position = originalPosition + Vector3.up * 0.5f;
        yield return new WaitForSeconds(0.35f);
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }
}