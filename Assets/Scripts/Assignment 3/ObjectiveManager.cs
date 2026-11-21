using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class StoryObjectiveMapping
{
    public string triggerFlag;
    [TextArea(1, 3)]
    public string objectiveDescription;
}


// Listens to StoryStateManager flags and updates the HUD objective banner.

public class ObjectiveManager : MonoBehaviour
{
    public TextMeshProUGUI objectiveBannerText;
    public string initialObjective = "Speak with the Vacationer on the beach.";
    public List<StoryObjectiveMapping> objectiveMappings = new List<StoryObjectiveMapping>();

    private void Start()
    {
        if (objectiveBannerText != null)
            objectiveBannerText.text = initialObjective;

        if (StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.OnFlagSet += HandleFlagSet;
            // Sync if a flag was already set prior to scene load
            foreach (var mapping in objectiveMappings)
            {
                if (StoryStateManager.Instance.HasFlag(mapping.triggerFlag))
                {
                    objectiveBannerText.text = mapping.objectiveDescription;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (StoryStateManager.Instance != null)
            StoryStateManager.Instance.OnFlagSet -= HandleFlagSet;
    }

    private void HandleFlagSet(string flag)
    {
        foreach (var mapping in objectiveMappings)
        {
            if (mapping.triggerFlag == flag && objectiveBannerText != null)
            {
                objectiveBannerText.text = mapping.objectiveDescription;
                break;
            }
        }
    }
}