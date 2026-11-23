using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CraftingRecipe
{
    public string toolA_ID;          // e.g., "FishingRod"
    public string toolB_ID;          // e.g., "FishingHook"
    public string resultToolID;      // e.g., "FishingPole"
    public string resultDisplayName; // e.g., "Assembled Fishing Pole"
    public Sprite resultSprite;      // Combined tool sprite
    [TextArea(1, 3)]
    public string resultLoreDescription;
    public string flagToSetOnCraft;  // e.g., "craftedFishingPole" or "lanternLit"
}

[System.Serializable]
public class InventoryToolEntry
{
    public string toolID;
    public string displayName;
    public Sprite icon;
}

/// <summary>
/// Manages bottom-bar tab switching ("Inventory" vs "List"), visual tool sprites,
/// drag-and-drop crafting combinations, keyboard usage instructions in the inventory tab,
/// Telescope zoom, and Level 3's 1-minute Lantern burn/cooldown cycle.
/// </summary>
public class InventoryAndCraftingSystem : MonoBehaviour
{
    public static InventoryAndCraftingSystem Instance { get; private set; }

    [Header("Reticle UI")]
    public GameObject reticle;

    [Header("Bottom Bar Tab Navigation")]
    public Button inventoryTabButton;        // "Inventory" button in bottom HUD bar
    public Button listTabButton;             // "List" button in bottom HUD bar
    public GameObject hiddenObjectListPanel; // Regular Hidden Objects list panel
    public GameObject inventoryTabPanel;     // Inventory & Crafting panel

    [Header("Inventory Grid & Drag-Drop Crafting")]
    public Transform inventoryGridContainer; // Horizontal/Grid Layout Group holding tool sprites
    public GameObject inventorySlotPrefab;   // Prefab with Image + InventorySlotUI
    public TextMeshProUGUI keybindHintText;  // Displays keyboard button prompt in the Inventory tab
    public List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>();

    [Header("Optional Lore Inspection Popup")]
    public GameObject loreCardPanel;
    public TextMeshProUGUI loreTitleText;
    public TextMeshProUGUI loreBodyText;
    public Image loreIconImage;
    public Button closeLoreButton;

    [Header("Telescope Settings (Level 2)")]
    public Camera playerCamera;
    public GameObject telescopeOverlayUI;
    public float normalFOV = 60f;
    public float telescopeFOV = 22f;
    public bool IsTelescopeActive { get; private set; }

    [Header("Lantern Cycle Settings (Level 3)")]
    public Light playerLanternLight;
    public Slider lanternFuelSlider;
    public TextMeshProUGUI lanternStatusText;
    public float burnDuration = 60f;      // 1-minute illumination
    public float cooldownDuration = 20f;  // 20-second cooldown

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip craftSuccessClip;

    private List<InventoryToolEntry> collectedTools = new List<InventoryToolEntry>();
    private bool isLanternBurning = false;
    private bool isLanternOnCooldown = false;
    private float lanternTimer = 0f;

    public bool IsInspectingUI => loreCardPanel != null && loreCardPanel.activeSelf;

    private void Awake()
    {
        Instance = this;

        if (loreCardPanel != null) loreCardPanel.SetActive(false);
        if (telescopeOverlayUI != null) telescopeOverlayUI.SetActive(false);
        if (closeLoreButton != null) closeLoreButton.onClick.AddListener(CloseLoreCard);

        // Bind bottom-bar tab buttons
        if (inventoryTabButton != null)
            inventoryTabButton.onClick.AddListener(OpenInventoryTab);

        if (listTabButton != null)
            listTabButton.onClick.AddListener(OpenListTab);
    }

    private void Start()
    {
        // Default to showing the regular Hidden Objects List at level start
        OpenListTab();
        RefreshInventoryUI();
    }

    private void Update()
    {
        // Toggle Telescope zoom with [T] once crafted
        if (HasTool("Telescope") && Input.GetKeyDown(KeyCode.T))
        {
            ToggleTelescope();
        }

        // Ignite or relight Lantern with [F] once crafted
        if (HasTool("LitLantern") && Input.GetKeyDown(KeyCode.F))
        {
            TryIgniteLantern();
        }

        UpdateLanternCycle();
    }

    /// <summary>
    /// Switches the bottom HUD area to the Inventory tab showing collected tool sprites.
    /// </summary>
    public void OpenInventoryTab()
    {
        if (hiddenObjectListPanel != null) hiddenObjectListPanel.SetActive(false);
        if (inventoryTabPanel != null) inventoryTabPanel.SetActive(true);
        RefreshInventoryUI();
    }

    /// <summary>
    /// Switches back to the regular Hidden Objects list ("List" button).
    /// </summary>
    public void OpenListTab()
    {
        if (inventoryTabPanel != null) inventoryTabPanel.SetActive(false);
        if (hiddenObjectListPanel != null) hiddenObjectListPanel.SetActive(true);
    }

    /// <summary>
    /// Adds a collected tool from the 3D scene and updates the Inventory sprite grid.
    /// </summary>
    public void AddTool(HiddenObjectItem item)
    {
        if (HasTool(item.itemID)) return;

        collectedTools.Add(new InventoryToolEntry
        {
            toolID = item.itemID,
            displayName = item.displayPrompt,
            icon = item.itemIcon
        });

        ShowLoreCard(item.displayPrompt, item.loreDescription, item.itemIcon);
        RefreshInventoryUI();
    }

    public bool HasTool(string toolID)
    {
        return collectedTools.Exists(t => t.toolID == toolID);
    }

    /// <summary>
    /// Called by InventorySlotUI when the player drags one tool sprite onto another.
    /// </summary>
    public bool TryCombineTools(string draggedToolID, string targetToolID)
    {
        if (draggedToolID == targetToolID) return false;

        foreach (CraftingRecipe recipe in craftingRecipes)
        {
            bool matchForward = (recipe.toolA_ID == draggedToolID && recipe.toolB_ID == targetToolID);
            bool matchReverse = (recipe.toolA_ID == targetToolID && recipe.toolB_ID == draggedToolID);

            if (matchForward || matchReverse)
            {
                collectedTools.RemoveAll(t => t.toolID == draggedToolID || t.toolID == targetToolID);

                collectedTools.Add(new InventoryToolEntry
                {
                    toolID = recipe.resultToolID,
                    displayName = recipe.resultDisplayName,
                    icon = recipe.resultSprite
                });

                if (sfxSource != null && craftSuccessClip != null)
                    sfxSource.PlayOneShot(craftSuccessClip);

                if (!string.IsNullOrEmpty(recipe.flagToSetOnCraft) && StoryStateManager.Instance != null)
                    StoryStateManager.Instance.SetFlag(recipe.flagToSetOnCraft);

                if (recipe.resultToolID == "LitLantern")
                    TryIgniteLantern();

                ShowLoreCard(recipe.resultDisplayName, recipe.resultLoreDescription, recipe.resultSprite);
                RefreshInventoryUI();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Rebuilds the sprite icons in the Inventory tab and updates the keyboard usage text.
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (inventoryGridContainer != null && inventorySlotPrefab != null)
        {
            foreach (Transform child in inventoryGridContainer)
                Destroy(child.gameObject);

            foreach (InventoryToolEntry tool in collectedTools)
            {
                GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryGridContainer);
                InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(tool.toolID, tool.displayName, tool.icon);
                }
            }
        }

        UpdateKeybindInstructions();
    }

    /// <summary>
    /// Updates the text inside the Inventory tab telling the player which keyboard key uses their tool.
    /// </summary>
    private void UpdateKeybindInstructions()
    {
        if (keybindHintText == null) return;

        List<string> instructions = new List<string>();

        if (HasTool("FishingPole"))
            instructions.Add("• Fishing Pole: Walk to the shoreline hotspot and press [E] to fish.");
        if (HasTool("Telescope"))
            instructions.Add("• Telescope: Press [T] to zoom in and spot distant clues.");
        if (HasTool("LitLantern"))
            instructions.Add("• Brass Lantern: Press [F] to ignite/relight (60s burn, 20s cooldown).");
        if (HasTool("Crowbar"))
            instructions.Add("• Crowbar: Approach a jammed crate or door and press [E] to pry open.");
        if (HasTool("Knife"))
            instructions.Add("• Hunting Knife: Approach Lys's bindings and press [E] to cut free.");

        if (instructions.Count > 0)
        {
            keybindHintText.text = string.Join("\n", instructions);
        }
        else if (collectedTools.Count >= 2)
        {
            keybindHintText.text = "Tip: Drag matching tool sprites onto each other to combine them.";
        }
        else
        {
            keybindHintText.text = "Collected tools appear here. Drag compatible tools together to combine.";
        }
    }

    private void ToggleTelescope()
    {
        IsTelescopeActive = !IsTelescopeActive;
        if (playerCamera != null)
            playerCamera.fieldOfView = IsTelescopeActive ? telescopeFOV : normalFOV;
        if (telescopeOverlayUI != null)
            telescopeOverlayUI.SetActive(IsTelescopeActive);
    }

    public void TryIgniteLantern()
    {
        if (isLanternBurning || isLanternOnCooldown) return;

        isLanternBurning = true;
        lanternTimer = burnDuration;
        if (playerLanternLight != null) playerLanternLight.enabled = true;
        if (lanternFuelSlider != null) lanternFuelSlider.gameObject.SetActive(true);
    }

    private void UpdateLanternCycle()
    {
        if (isLanternBurning)
        {
            lanternTimer -= Time.deltaTime;
            if (lanternFuelSlider != null) lanternFuelSlider.value = lanternTimer / burnDuration;
            if (lanternStatusText != null) lanternStatusText.text = $"Lantern Oil: {Mathf.CeilToInt(lanternTimer)}s";

            if (lanternTimer <= 0f)
            {
                isLanternBurning = false;
                isLanternOnCooldown = true;
                lanternTimer = cooldownDuration;
                if (playerLanternLight != null) playerLanternLight.enabled = false;
            }
        }
        else if (isLanternOnCooldown)
        {
            lanternTimer -= Time.deltaTime;
            if (lanternFuelSlider != null) lanternFuelSlider.value = 1f - (lanternTimer / cooldownDuration);
            if (lanternStatusText != null) lanternStatusText.text = $"Cooling Down: {Mathf.CeilToInt(lanternTimer)}s";

            if (lanternTimer <= 0f)
            {
                isLanternOnCooldown = false;
                if (lanternStatusText != null) lanternStatusText.text = "Press [F] to Relight Lantern";
            }
        }
    }

    public void ShowLoreCard(string title, string description, Sprite icon)
    {
        if (loreCardPanel == null) return;
        if (reticle != null) reticle.SetActive(false);
        loreCardPanel.SetActive(true);
        loreTitleText.text = title;
        loreBodyText.text = description;
        if (loreIconImage != null)
        {
            loreIconImage.sprite = icon;
            loreIconImage.enabled = (icon != null);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseLoreCard()
    {
        if (loreCardPanel != null) loreCardPanel.SetActive(false);
        if (reticle != null) reticle.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
