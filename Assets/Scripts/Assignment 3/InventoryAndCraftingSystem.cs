using Unity.Cinemachine;
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
/// Telescope zoom, and Level 3's cave-gated 1-minute Lantern burn/flicker/cooldown cycle.
/// </summary>
public class InventoryAndCraftingSystem : MonoBehaviour
{
    public static InventoryAndCraftingSystem Instance { get; private set; }

    [Header("Reticle UI")]
    public GameObject reticle;

    [Header("Bottom Bar Tab Navigation")]
    public Button inventoryTabButton;          // "Inventory" button in bottom HUD bar
    public Button listTabButton;               // "List" button in bottom HUD bar
    public GameObject hiddenObjectListPanel;   // Regular Hidden Objects list panel
    public GameObject inventoryTabPanel;       // Inventory & Crafting panel

    [Header("Inventory Grid & Drag-Drop Crafting")]
    public Transform inventoryGridContainer;   // Horizontal/Grid Layout Group holding tool sprites
    public GameObject inventorySlotPrefab;     // Prefab with Image + InventorySlotUI
    public TextMeshProUGUI keybindHintText;    // Displays keyboard button prompt in the Inventory tab
    public List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>();

    [Header("Optional Lore Inspection Popup")]
    public GameObject loreCardPanel;
    public TextMeshProUGUI loreTitleText;
    public TextMeshProUGUI loreBodyText;
    public Image loreIconImage;
    public Button closeLoreButton;

    [Header("Telescope Settings (Level 2)")]
    public CinemachineCamera playerVirtualCamera; // Drag PlayerFollowCamera here
    public GameObject telescopeOverlayUI;
    public float normalFOV = 60f;
    public float telescopeFOV = 22f;
    public bool IsTelescopeActive { get; private set; }

    [Header("Lantern & Cave Settings (Level 3)")]
    [Tooltip("Parent UI panel holding the lantern slider, status text, and icon. Enabled only while in cave/lantern mode.")]
    public GameObject lanternUIPanel;
    [Tooltip("Parent 3D Lantern object attached near the PlayerCapsule (contains fire VFX and Point Light).")]
    public GameObject playerLanternObject;
    public Light playerLanternLight;
    public ParticleSystem lanternFireVFX;
    public Slider lanternFuelSlider;
    public TextMeshProUGUI lanternStatusText;

    [Header("Lantern Cycle & Flicker Tuning")]
    public float burnDuration = 60f;               // 1-minute full illumination
    public float flickerWarningDuration = 5f;      // Flickers during final 5 seconds
    public float cooldownDuration = 20f;           // 20-second cooldown
    public float activeLightIntensity = 3.0f;      // Full brightness while burning
    public float cooldownLightIntensity = 0.4f;    // Dim ember glow during cooldown (never pitch black)
    public float flickerSpeed = 24f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip craftSuccessClip;
    public AudioClip lanternIgniteClip;
    public AudioClip lanternOutClip;

    private List<InventoryToolEntry> collectedTools = new List<InventoryToolEntry>();
    private bool isPlayerInCave = false;
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

        // Keep held lantern and lantern UI hidden at start (outside cave)
        if (playerLanternLight != null)
        {
            activeLightIntensity = playerLanternLight.intensity;
            playerLanternLight.enabled = false;
        }
        if (lanternFireVFX != null)
            lanternFireVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (playerLanternObject != null)
            playerLanternObject.SetActive(false);
        if (lanternFuelSlider != null)
            lanternFuelSlider.gameObject.SetActive(false);
        if (lanternStatusText != null)
            lanternStatusText.gameObject.SetActive(false);
        if (lanternUIPanel != null)
            lanternUIPanel.SetActive(false);

        // Bind bottom-bar tab buttons
        if (inventoryTabButton != null) inventoryTabButton.onClick.AddListener(OpenInventoryTab);
        if (listTabButton != null) listTabButton.onClick.AddListener(OpenListTab);
    }

    private void Start()
    {
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

        // Ignite or relight Lantern with [F] only when inside the cave and crafted
        if (isPlayerInCave && HasTool("LitLantern") && Input.GetKeyDown(KeyCode.F))
        {
            TryIgniteLantern();
        }

        if (isPlayerInCave)
        {
            UpdateLanternCycle();
        }
    }

    /// <summary>
    /// Called by CaveLightTrigger when the player enters or exits the cave.
    /// Controls whether the held lantern object and its cycle are active.
    /// </summary>
    public void SetPlayerInCave(bool inCave)
    {
        isPlayerInCave = inCave;

        if (inCave)
        {
            bool ownsLantern = HasTool("LitLantern");
            if (playerLanternObject != null)
                playerLanternObject.SetActive(ownsLantern);

            if (lanternUIPanel != null)
                lanternUIPanel.SetActive(ownsLantern);

            if (ownsLantern)
            {
                if (lanternFuelSlider != null) lanternFuelSlider.gameObject.SetActive(true);
                if (lanternStatusText != null) lanternStatusText.gameObject.SetActive(true);

                // If neither burning nor cooling down, ignite automatically upon stepping into the cave
                if (!isLanternBurning && !isLanternOnCooldown)
                {
                    TryIgniteLantern();
                }
                else
                {
                    ApplyCurrentLanternVisualState();
                }
            }
        }
        else
        {
            // Exiting the cave: hide the lantern object, stop VFX, and turn off UI
            if (lanternUIPanel != null)
                lanternUIPanel.SetActive(false);
            if (lanternFireVFX != null)
                lanternFireVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (playerLanternLight != null)
                playerLanternLight.enabled = false;
            if (playerLanternObject != null)
                playerLanternObject.SetActive(false);
            if (lanternFuelSlider != null)
                lanternFuelSlider.gameObject.SetActive(false);
            if (lanternStatusText != null)
                lanternStatusText.gameObject.SetActive(false);
        }
    }

    public void OpenInventoryTab()
    {
        if (hiddenObjectListPanel != null) hiddenObjectListPanel.SetActive(false);
        if (inventoryTabPanel != null) inventoryTabPanel.SetActive(true);
        RefreshInventoryUI();
    }

    public void OpenListTab()
    {
        if (inventoryTabPanel != null) inventoryTabPanel.SetActive(false);
        if (hiddenObjectListPanel != null) hiddenObjectListPanel.SetActive(true);
    }

    public void AddTool(HiddenObjectItem item)
    {
        if (HasTool(item.itemID)) return;

        collectedTools.Add(new InventoryToolEntry
        {
            toolID = item.itemID,
            displayName = item.displayPrompt,
            icon = item.itemIcon
        });

        // If the player acquired an already-lit lantern while inside the cave, enable it immediately
        if (item.itemID == "LitLantern" && isPlayerInCave)
        {
            SetPlayerInCave(true);
        }

        ShowLoreCard(item.displayPrompt, item.loreDescription, item.itemIcon);
        RefreshInventoryUI();
    }

    public bool HasTool(string toolID)
    {
        return collectedTools.Exists(t => t.toolID == toolID);
    }

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

                // Only show and ignite the physical lantern if currently inside the cave
                if (recipe.resultToolID == "LitLantern" && isPlayerInCave)
                {
                    SetPlayerInCave(true);
                }

                ShowLoreCard(recipe.resultDisplayName, recipe.resultLoreDescription, recipe.resultSprite);
                RefreshInventoryUI();
                return true;
            }
        }
        return false;
    }

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

    private void UpdateKeybindInstructions()
    {
        if (keybindHintText == null) return;

        List<string> instructions = new List<string>();
        if (HasTool("FishingPole")) instructions.Add("• Fishing Pole: Walk to the shoreline hotspot and press [E] to fish.");
        if (HasTool("Telescope")) instructions.Add("• Telescope: Press [T] to zoom in and spot distant clues.");
        if (HasTool("LitLantern")) instructions.Add("• Brass Lantern: Inside the cave, press [F] to relight (60s burn, 20s cooldown).");
        if (HasTool("Crowbar")) instructions.Add("• Crowbar: Approach a jammed crate or door and press [E] to pry open.");
        if (HasTool("Knife")) instructions.Add("• Hunting Knife: Approach Lys's bindings and press [E] to cut free.");

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
        if (playerVirtualCamera != null)
        {
            var lens = playerVirtualCamera.Lens;
            lens.FieldOfView = IsTelescopeActive ? telescopeFOV : normalFOV;
            playerVirtualCamera.Lens = lens;
        }
        if (telescopeOverlayUI != null) telescopeOverlayUI.SetActive(IsTelescopeActive);
    }

    public void TryIgniteLantern()
    {
        if (!isPlayerInCave || !HasTool("LitLantern")) return;
        if (isLanternBurning || isLanternOnCooldown) return;

        isLanternBurning = true;
        lanternTimer = burnDuration;

        if (playerLanternObject != null && !playerLanternObject.activeSelf)
            playerLanternObject.SetActive(true);

        ApplyCurrentLanternVisualState();

        if (sfxSource != null && lanternIgniteClip != null)
            sfxSource.PlayOneShot(lanternIgniteClip);

        if (lanternUIPanel != null) lanternUIPanel.SetActive(true);
        if (lanternFuelSlider != null) lanternFuelSlider.gameObject.SetActive(true);
        if (lanternStatusText != null) lanternStatusText.gameObject.SetActive(true);
    }

    private void ApplyCurrentLanternVisualState()
    {
        if (isLanternBurning)
        {
            if (playerLanternLight != null)
            {
                playerLanternLight.enabled = true;
                playerLanternLight.intensity = activeLightIntensity;
            }
            if (lanternFireVFX != null && !lanternFireVFX.isPlaying)
            {
                lanternFireVFX.Play(true);
            }
        }
        else
        {
            // Cooldown or unlit: stop fire VFX particles, keep point light dimly lit
            if (lanternFireVFX != null)
                lanternFireVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (playerLanternLight != null)
            {
                playerLanternLight.enabled = true;
                playerLanternLight.intensity = cooldownLightIntensity;
            }
        }
    }

    private void UpdateLanternCycle()
    {
        if (!HasTool("LitLantern")) return;

        if (isLanternBurning)
        {
            lanternTimer -= Time.deltaTime;

            // Flicker warning during the final seconds before flame dies
            if (lanternTimer <= flickerWarningDuration && lanternTimer > 0f)
            {
                if (playerLanternLight != null)
                {
                    float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
                    float sputter = Random.value > 0.85f ? 0.25f : 1f;
                    playerLanternLight.intensity = Mathf.Lerp(cooldownLightIntensity + 0.2f, activeLightIntensity, noise) * sputter;
                }
            }
            else if (playerLanternLight != null)
            {
                playerLanternLight.intensity = activeLightIntensity;
            }

            if (lanternFuelSlider != null)
                lanternFuelSlider.value = Mathf.Clamp01(lanternTimer / burnDuration);

            if (lanternStatusText != null)
                lanternStatusText.text = lanternTimer <= flickerWarningDuration
                    ? $"Lantern Flickering: {Mathf.CeilToInt(lanternTimer)}s"
                    : $"Lantern Oil: {Mathf.CeilToInt(lanternTimer)}s";

            // Burn finished -> stop particles, dim point light to cooldown intensity, start cooldown
            if (lanternTimer <= 0f)
            {
                isLanternBurning = false;
                isLanternOnCooldown = true;
                lanternTimer = cooldownDuration;

                if (sfxSource != null && lanternOutClip != null)
                    sfxSource.PlayOneShot(lanternOutClip);

                ApplyCurrentLanternVisualState();
            }
        }
        else if (isLanternOnCooldown)
        {
            lanternTimer -= Time.deltaTime;

            if (lanternFuelSlider != null)
                lanternFuelSlider.value = 1f - Mathf.Clamp01(lanternTimer / cooldownDuration);

            if (lanternStatusText != null)
                lanternStatusText.text = $"Cooling Down: {Mathf.CeilToInt(lanternTimer)}s";

            if (lanternTimer <= 0f)
            {
                isLanternOnCooldown = false;
                if (lanternStatusText != null)
                    lanternStatusText.text = "Press [F] to Relight Lantern";
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