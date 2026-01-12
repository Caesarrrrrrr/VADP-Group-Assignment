using UnityEngine;
using TMPro; // Required for changing Text Mesh Pro text

public class StartTraining : MonoBehaviour
{
    [Header("Systems to Manage")]
    public GameObject gestureObject; 
    public GameObject voiceManager;
    public GameObject VoiceBridge;

    [Header("UI Panels")]
    public GameObject skillPanel;
    public GameObject mainMenuPanel;
    public GameObject sideMenuPanel;

    [Header("Button Settings")]
    [Tooltip("Drag the Text (TMP) object that is INSIDE your button here.")]
    public TMP_Text buttonLabel; 
    public string startText = "Start Training";
    public string stopText = "End Training";

    // Track the current state
    private bool isTrainingActive = false;

    void Start()
    {
        // 1. Force the initial "Off" state when game loads
        SetTrainingState(false);
    }

    // 2. Connect this to your Button's OnClick event
    public void ToggleTrainingSession()
    {
        // Flip the state (True becomes False, False becomes True)
        isTrainingActive = !isTrainingActive;
        
        SetTrainingState(isTrainingActive);
    }

    private void SetTrainingState(bool active)
    {
        // Toggle Systems
        if (gestureObject != null) gestureObject.SetActive(active);
        if (voiceManager != null) voiceManager.SetActive(active);
        if (VoiceBridge != null) VoiceBridge.SetActive(active);

        // Toggle UI Panels
        if (skillPanel != null) skillPanel.SetActive(active);      // Show skill panel if active
        if (mainMenuPanel != null) mainMenuPanel.SetActive(!active); // Hide main menu if active (and vice versa)
        if (sideMenuPanel != null) sideMenuPanel.SetActive(!active); // Hide side menu if active (and vice versa)

        // Update Button Text
        if (buttonLabel != null)
        {
            buttonLabel.text = active ? stopText : startText;
        }

        Debug.Log($"Training State: {(active ? "Active" : "Stopped")}");
    }
}