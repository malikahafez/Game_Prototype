using UnityEngine;

public class CaveLightTrigger : MonoBehaviour
{
    public Light directionalLight;
    public float targetIntensity = 0.05f; // Dim but not pitch black
    public Color targetAmbient = Color.black;

    private float originalIntensity;
    private Color originalAmbient;

    void Start() {
        if(directionalLight) originalIntensity = directionalLight.intensity;
        originalAmbient = RenderSettings.ambientLight;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            // Use a Coroutine or Tweener here to smoothly interpolate values
            directionalLight.intensity = targetIntensity;
            RenderSettings.ambientLight = targetAmbient;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            directionalLight.intensity = originalIntensity;
            RenderSettings.ambientLight = originalAmbient;
        }
    }
}