using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoreCollectionUI : MonoBehaviour
{
    [Header("Grid & Slot Prefab")]
    public Transform gridContainer;         // ScrollView Content with GridLayoutGroup
    public GameObject collectionSlotPrefab; // Button prefab with Image + TextMeshProUGUI

    [Header("Selected Item Detail View")]
    public Image detailIconImage;
    public TextMeshProUGUI detailTitleText;
    public TextMeshProUGUI detailActText;
    public TextMeshProUGUI detailLoreBodyText;

    private void OnEnable()
    {
        PopulateCollection();
    }

    public void PopulateCollection()
    {
        if (gridContainer == null || collectionSlotPrefab == null) return;

        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        if (StoryStateManager.Instance == null || StoryStateManager.Instance.discoveredLoreItems.Count == 0)
        {
            if (detailTitleText != null) detailTitleText.text = "No Discoveries Yet";
            if (detailActText != null) detailActText.text = "";
            if (detailLoreBodyText != null) detailLoreBodyText.text = "Explore the environments and collect items to record their history here.";
            if (detailIconImage != null) detailIconImage.enabled = false;
            return;
        }

        var items = StoryStateManager.Instance.discoveredLoreItems;
        for (int i = 0; i < items.Count; i++)
        {
            CollectedLoreEntry entry = items[i];
            GameObject slot = Instantiate(collectionSlotPrefab, gridContainer);

            Image iconImg = slot.GetComponentInChildren<Image>();
            if (iconImg != null && entry.itemSprite != null) iconImg.sprite = entry.itemSprite;

            TextMeshProUGUI label = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = entry.itemName;

            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowEntryDetails(entry));
            }
        }

        // Automatically display the first item's lore when opened
        ShowEntryDetails(items[0]);
    }

    private void ShowEntryDetails(CollectedLoreEntry entry)
    {
        if (detailTitleText != null) detailTitleText.text = entry.itemName;
        if (detailActText != null) detailActText.text = $"Discovered in Act {entry.actFound}";
        if (detailLoreBodyText != null) detailLoreBodyText.text = entry.loreDescription;
        if (detailIconImage != null)
        {
            detailIconImage.sprite = entry.itemSprite;
            detailIconImage.enabled = (entry.itemSprite != null);
        }
    }
}