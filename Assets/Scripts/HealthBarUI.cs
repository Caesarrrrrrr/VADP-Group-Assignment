using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider; // Drag the Slider component here

    [Header("Settings")]
    public bool alwaysFaceCamera = true;
    private Camera mainCamera;

    void Start()
    {
        // Find the camera automatically
        mainCamera = Camera.main;
    }

    // Call this function from ANY script to update the bar
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            // Converts values to a 0.0 to 1.0 scale for the slider
            healthSlider.value = currentHealth / maxHealth;
        }
    }

    void LateUpdate()
    {
        // Makes the canvas always face the camera (Billboarding)
        if (alwaysFaceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}