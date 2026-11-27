using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CaveTrigger : MonoBehaviour
{
    [Header("World Lighting Transition")]
    public Light directionalLight;
    public float transitionDuration = 1.0f;
    [Range(0f, 1f)]
    public float caveReflectionIntensity = 0.05f;
    public ParticleSystem dust;

    [Header("Lantern Objects & Light")]
    public GameObject lanternObject;      // 3D lantern model held by player
    public Light lanternLight;            // Point or Spot light inside the lantern
    public KeyCode toggleLanternKey = KeyCode.F;
    public bool autoTurnOnWhenEnteringCave = true;

    [Header("Lantern Cycle & Cooldown Settings")]
    [Tooltip("How long (in seconds) the lantern stays lit before overheating/draining.")]
    public float lanternMaxDuration = 15.0f;
    [Tooltip("How many seconds before shutoff the lantern begins flickering.")]
    public float flickerWarningDuration = 3.5f;
    [Tooltip("How long (in seconds) the player must wait before turning the lantern back on.")]
    public float cooldownDuration = 6.0f;

    [Header("Lantern Flicker Tuning")]
    public float baseLanternIntensity = 2.5f;
    public float minFlickerIntensity = 0.2f;
    public float flickerSpeed = 25f;

    [Header("Optional Lantern UI")]
    public GameObject lanternHUDGroup;
    public Slider lanternChargeSlider;
    public TextMeshProUGUI lanternStatusText;

    [Header("Optional Audio")]
    public AudioSource sfxSource;
    public AudioClip lanternToggleClip;
    public AudioClip lanternFlickerOutClip;

    // Exterior lighting cache
    private float originalIntensity;
    private Color originalAmbient;
    private AmbientMode originalAmbientMode;
    private float originalReflectionIntensity;
    private Coroutine transitionCoroutine;

    // Cave & lantern runtime state
    private bool isPlayerInCave = false;
    private bool isLanternOn = false;
    private bool isCoolingDown = false;
    private float remainingBurnTime = 0f;
    private float remainingCooldownTime = 0f;

    private void Start()
    {
        if (directionalLight != null)
            originalIntensity = directionalLight.intensity;

        originalAmbient = RenderSettings.ambientLight;
        originalAmbientMode = RenderSettings.ambientMode;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;

        if (lanternLight != null)
        {
            baseLanternIntensity = lanternLight.intensity;
            lanternLight.enabled = false;
        }

        if (lanternObject != null)
            lanternObject.SetActive(false);

        if (lanternHUDGroup != null)
            lanternHUDGroup.SetActive(false);

        remainingBurnTime = lanternMaxDuration;
    }

    private void Update()
    {
        if (!isPlayerInCave) return;

        // Ignore lantern input while dialogue, lore inspection, or pause menu is open
        bool uiBlocked =
            (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) ||
            (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) ||
            (InventoryAndCraftingSystem.Instance != null && InventoryAndCraftingSystem.Instance.IsInspectingUI);

        if (!uiBlocked && Input.GetKeyDown(toggleLanternKey))
        {
            HandleManualLanternToggle();
        }

        UpdateLanternCycle();
        UpdateLanternUI();
    }

    private void HandleManualLanternToggle()
    {
        if (isCoolingDown) return;

        if (isLanternOn)
        {
            // Turn off early and trigger cooldown proportional to time used (or full cooldown)
            TurnOffLantern(startCooldown: true);
        }
        else if (remainingBurnTime > 0f)
        {
            TurnOnLantern();
        }
    }

    private void UpdateLanternCycle()
    {
        if (isLanternOn)
        {
            remainingBurnTime -= Time.deltaTime;

            // Flicker warning during the final seconds before shutoff
            if (remainingBurnTime <= flickerWarningDuration && remainingBurnTime > 0f)
            {
                ApplyFlickerEffect();
            }
            else if (lanternLight != null)
            {
                lanternLight.intensity = baseLanternIntensity;
            }

            // Lantern fuel/heat exhausted -> shut off & enter cooldown
            if (remainingBurnTime <= 0f)
            {
                remainingBurnTime = 0f;
                if (sfxSource != null && lanternFlickerOutClip != null)
                    sfxSource.PlayOneShot(lanternFlickerOutClip);

                TurnOffLantern(startCooldown: true);
            }
        }
        else if (isCoolingDown)
        {
            remainingCooldownTime -= Time.deltaTime;

            // Smoothly recharge the bar while cooling down
            float cooldownProgress = 1f - Mathf.Clamp01(remainingCooldownTime / cooldownDuration);
            remainingBurnTime = cooldownProgress * lanternMaxDuration;

            if (remainingCooldownTime <= 0f)
            {
                remainingCooldownTime = 0f;
                isCoolingDown = false;
                remainingBurnTime = lanternMaxDuration;
            }
        }
    }

    private void ApplyFlickerEffect()
    {
        if (lanternLight == null) return;

        // Combine Perlin noise with sudden dips so the flame sputters naturally
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float sputter = Random.value > 0.85f ? 0.15f : 1f;
        lanternLight.intensity = Mathf.Lerp(minFlickerIntensity, baseLanternIntensity, noise) * sputter;
    }

    private void TurnOnLantern()
    {
        isLanternOn = true;
        if (lanternObject != null) lanternObject.SetActive(true);
        if (lanternLight != null)
        {
            lanternLight.intensity = baseLanternIntensity;
            lanternLight.enabled = true;
        }

        if (sfxSource != null && lanternToggleClip != null)
            sfxSource.PlayOneShot(lanternToggleClip);
    }

    private void TurnOffLantern(bool startCooldown)
    {
        isLanternOn = false;
        if (lanternLight != null)
        {
            lanternLight.enabled = false;
            lanternLight.intensity = baseLanternIntensity;
        }

        if (startCooldown)
        {
            isCoolingDown = true;
            // Scale cooldown based on how much of the lantern was used (minimum 1.5s)
            float usedFraction = 1f - Mathf.Clamp01(remainingBurnTime / lanternMaxDuration);
            remainingCooldownTime = Mathf.Max(1.5f, cooldownDuration * usedFraction);
        }
    }

    private void UpdateLanternUI()
    {
        if (lanternChargeSlider != null)
        {
            lanternChargeSlider.value = Mathf.Clamp01(remainingBurnTime / lanternMaxDuration);
        }

        if (lanternStatusText != null)
        {
            if (isCoolingDown)
                lanternStatusText.text = $"Lantern Cooling ({remainingCooldownTime:0.0}s)";
            else if (isLanternOn)
                lanternStatusText.text = remainingBurnTime <= flickerWarningDuration
                    ? "Lantern Low! [F] Off"
                    : "Lantern On [F]";
            else
                lanternStatusText.text = "Press [F] for Lantern";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (dust != null) dust.Play();
        if (InventoryAndCraftingSystem.Instance != null)
            InventoryAndCraftingSystem.Instance.SetPlayerInCave(true);

        StartTransition(0f, Color.black, AmbientMode.Flat, 0.05f, transitionDuration);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (dust != null) dust.Stop();
        if (InventoryAndCraftingSystem.Instance != null)
            InventoryAndCraftingSystem.Instance.SetPlayerInCave(false);

        StartTransition(originalIntensity, originalAmbient, originalAmbientMode, originalReflectionIntensity, transitionDuration);
    }

    private void StartTransition(float targetInt, Color targetCol, AmbientMode targetMode, float targetReflection, float duration)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(TransitionRoutine(targetInt, targetCol, targetMode, targetReflection, duration));
    }

    private IEnumerator TransitionRoutine(float targetInt, Color targetCol, AmbientMode targetMode, float targetReflection, float duration)
    {
        float startInt = directionalLight != null ? directionalLight.intensity : 0f;
        Color startCol = RenderSettings.ambientLight;
        AmbientMode startMode = RenderSettings.ambientMode;
        float startReflection = RenderSettings.reflectionIntensity;

        if (targetMode != startMode)
        {
            RenderSettings.ambientMode = targetMode;
            if (startMode == AmbientMode.Skybox)
            {
                startCol = RenderSettings.ambientSkyColor;
            }
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            if (directionalLight != null)
                directionalLight.intensity = Mathf.Lerp(startInt, targetInt, t);

            RenderSettings.ambientLight = Color.Lerp(startCol, targetCol, t);
            RenderSettings.reflectionIntensity = Mathf.Lerp(startReflection, targetReflection, t);
            yield return null;
        }

        if (directionalLight != null)
            directionalLight.intensity = targetInt;

        RenderSettings.ambientLight = targetCol;
        RenderSettings.ambientMode = targetMode;
        RenderSettings.reflectionIntensity = targetReflection;
    }
}