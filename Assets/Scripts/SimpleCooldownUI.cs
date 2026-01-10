using UnityEngine;
using UnityEngine.UI;

public class SimpleCooldownUI : MonoBehaviour
{
    [Header("Settings")]
    public Image fillImage;
    public Color cooldownColor = Color.cyan; // You can pick the color here!
    public float cooldownDuration = 2.0f;    // Set this to match your DetectGestures script

    private float timer = 0;
    private bool isAnimating = false;

    void Start()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.color = cooldownColor;
        }
    }

    void Update()
    {
        if (isAnimating)
        {
            timer -= Time.deltaTime;

            // Update the fill amount (1.0 is full, 0.0 is empty)
            if (fillImage != null)
                fillImage.fillAmount = timer / cooldownDuration;

            if (timer <= 0)
            {
                isAnimating = false;
                if (fillImage != null) fillImage.fillAmount = 0;
            }
        }
    }

    // This function will be called by the ball when it spawns
    public void StartCooldown()
    {
        timer = cooldownDuration;
        isAnimating = true;

        if (fillImage != null)
        {
            fillImage.fillAmount = 1;
            fillImage.color = cooldownColor; // Apply your chosen color
        }
    }
}