using UnityEngine;

public class PrefabTriggerUI : MonoBehaviour
{
    [Tooltip("Type the EXACT name of the UI object in the scene you want to trigger.")]
    public string targetUIName = "Gesture_ThunderClap_UI";

    void Start()
    {
        // 1. Find the UI object in the scene by its name
        GameObject uiObj = GameObject.Find(targetUIName);

        if (uiObj != null)
        {
            // 2. Get the script and trigger the animation
            SimpleCooldownUI uiScript = uiObj.GetComponent<SimpleCooldownUI>();
            if (uiScript != null)
            {
                uiScript.StartCooldown();
            }
        }
        else
        {
            Debug.LogWarning($"Could not find UI object named '{targetUIName}'");
        }
    }
}