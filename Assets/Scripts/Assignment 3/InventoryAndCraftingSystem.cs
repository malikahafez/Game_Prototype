using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages collected tools, lore inspection cards, item crafting combinations
/// (Fishing Rod + Hook, Lantern + Lighter), Telescope zoom, and Level 3's
/// 1-minute Lantern illumination / 20-second cooldown cycle.
/// </summary>
public class InventoryAndCraftingSystem : MonoBehaviour
{
    public static InventoryAndCraftingSystem Instance { get; private set; }

    [Header("Lore Inspection UI")]
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

    private HashSet<string> ownedTools = new HashSet<string>();
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
    }

    private void Update()
    {
        // Toggle Telescope zoom with Right-Click or T once crafted
        if (ownedTools.Contains("Telescope") && Input.GetKeyDown(KeyCode.T))
        {
            ToggleTelescope();
        }

        // Re-ignite Lantern with F once crafted and off cooldown
        if (ownedTools.Contains("LitLantern") && Input.GetKeyDown(KeyCode.F))
        {
            TryIgniteLantern();
        }

        UpdateLanternCycle();
    }

    public void AddTool(HiddenObjectItem item)
    {
        ownedTools.Add(item.itemID);
        ShowLoreCard(item.displayPrompt, item.loreDescription, item.itemIcon);
        CheckAutoCombinations();
    }

    /// <summary>
    /// Automatically combines matching tool pairs when both parts are collected.
    /// </summary>
    private void CheckAutoCombinations()
    {
        // Level 1 Combination: Fishing Rod + Fishing Hook -> Fishing Pole
        if (ownedTools.Contains("FishingRod") && ownedTools.Contains("FishingHook") && !ownedTools.Contains("FishingPole"))
        {
            ownedTools.Add("FishingPole");
            StoryStateManager.Instance?.SetFlag("craftedFishingPole");
            ShowLoreCard("Assembled Fishing Pole", "You tied the iron hook to the sturdy line. Ready to fish the thermos from the shore.", null);
        }

        // Level 3 Combination: Lantern + Lighter -> Lit Lantern
        if (ownedTools.Contains("Lantern") && ownedTools.Contains("Lighter") && !ownedTools.Contains("LitLantern"))
        {
            ownedTools.Add("LitLantern");
            StoryStateManager.Instance?.SetFlag("lanternLit");
            TryIgniteLantern();
            ShowLoreCard("Ignited Brass Lantern", "Provides 60 seconds of warm light inside the dark Sto-Lu cave before needing a 20-second cooldown.", null);
        }
    }

    public bool HasTool(string toolID) => ownedTools.Contains(toolID);

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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}