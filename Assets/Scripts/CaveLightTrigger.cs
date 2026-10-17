using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class CaveLightTrigger : MonoBehaviour
{
    [Header("Settings")]
    public Light directionalLight;
    public float transitionDuration = 1.0f; // Time to fade to black

    private float originalIntensity;
    private Color originalAmbient;
    private AmbientMode originalAmbientMode;
    private float originalReflectionIntensity;

    private Coroutine transitionCoroutine;

    void Start()
    {
        if (directionalLight)
            originalIntensity = directionalLight.intensity;

        originalAmbient = RenderSettings.ambientLight;
        originalAmbientMode = RenderSettings.ambientMode;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Enter Cave: Zero light, black ambient, Flat mode, and ZERO reflections
            StartTransition(0f, Color.black, AmbientMode.Flat, 0.3f, transitionDuration);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Exit Cave: Restore original exterior settings
            StartTransition(originalIntensity, originalAmbient, originalAmbientMode, originalReflectionIntensity, transitionDuration);
        }
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
        float startInt = directionalLight ? directionalLight.intensity : 0f;
        Color startCol = RenderSettings.ambientLight;
        AmbientMode startMode = RenderSettings.ambientMode;
        float startReflection = RenderSettings.reflectionIntensity;

        // Update ambient mode immediately so lerping works properly
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

            if (directionalLight)
            {
                directionalLight.intensity = Mathf.Lerp(startInt, targetInt, t);
            }

            RenderSettings.ambientLight = Color.Lerp(startCol, targetCol, t);
            RenderSettings.reflectionIntensity = Mathf.Lerp(startReflection, targetReflection, t);

            yield return null;
        }

        // Snap to absolute final values
        if (directionalLight)
        {
            directionalLight.intensity = targetInt;
        }
        RenderSettings.ambientLight = targetCol;
        RenderSettings.ambientMode = targetMode;
        RenderSettings.reflectionIntensity = targetReflection;
    }
}