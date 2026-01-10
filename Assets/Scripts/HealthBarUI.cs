using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider; // Drag the Slider component here

    [Header("Settings")]
    public bool alwaysFaceCamera = true;
    private Camera _mainCamera;

    void Start()
    {
        // Cache the camera. In VR, sometimes Camera.main needs a moment to assign.
        _mainCamera = Camera.main;
    }

    // Call this function from the NetworkHealth script
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            // Protect against divide by zero
            float val = (maxHealth > 0) ? currentHealth / maxHealth : 0;
            healthSlider.value = val;
        }
    }

    void LateUpdate()
    {
        // Billboarding: Make the canvas face the local player's camera
        if (alwaysFaceCamera)
        {
            if (_mainCamera == null) _mainCamera = Camera.main; // Retry finding camera if missing

            if (_mainCamera != null)
            {
                // Smoothly rotate to face the camera
                transform.rotation = _mainCamera.transform.rotation;
            }
        }
    }
}