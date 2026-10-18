using UnityEngine;
using TMPro;

public class ObjectTrackerManager : MonoBehaviour
{
    [System.Serializable]
    public struct CollectibleQuestItem
    {
        public string itemName;              // Name identifier
        public TextMeshProUGUI uiText;       // Reference to the UI text in the list
        public Color completedColor;         // Color when found (e.g., Green or Gray)
    }

    public CollectibleQuestItem[] questItems;

    // Call this method when an item is collected
    public void CollectItem(int itemIndex)
    {
        if (itemIndex >= 0 && itemIndex < questItems.Length)
        {
            CollectibleQuestItem item = questItems[itemIndex];

            if (item.uiText != null)
            {
                // Change the text color to indicate completion
                item.uiText.color = item.completedColor;

                // Optional: Add a strike-through line effect using TMPro rich text
                // item.uiText.text = "<s>" + item.uiText.text + "</s>";
            }
        }
    }
}
