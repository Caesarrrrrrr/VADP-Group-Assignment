using UnityEngine;
using TMPro;
using Fusion;
using System.Linq; // Added for better search

public class NetworkUIManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI statusText;

    [Header("Settings")]
    public Color offlineColor = Color.red;
    public Color loadingColor = Color.yellow;
    public Color connectedColor = Color.green;

    private NetworkRunner _runner;

    void Update()
    {
        if (statusText == null) return;

        // 1. Refresh Runner Reference if null or dead
        if (_runner == null || !_runner.IsRunning)
        {
            // Find ANY runner that is actually running (Ignore zombies)
            var allRunners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
            _runner = allRunners.FirstOrDefault(r => r.IsRunning);
        }

        // 2. Logic: Determine Status
        if (_runner == null)
        {
            UpdateDisplay("Status: Waiting for Runner...", offlineColor);
        }
        else if (!_runner.IsRunning)
        {
            UpdateDisplay("Status: Starting Network...", loadingColor);
        }
        else if (_runner.IsRunning)
        {
            string mode = _runner.GameMode == GameMode.Host ? "HOST" : "CLIENT";
            
            // Check if SessionInfo is ready (sometimes null briefly)
            string roomName = (_runner.SessionInfo != null && _runner.SessionInfo.IsValid) 
                              ? _runner.SessionInfo.Name 
                              : "Joining...";
                              
            int playerCount = (_runner.SessionInfo != null && _runner.SessionInfo.IsValid) 
                              ? _runner.SessionInfo.PlayerCount 
                              : 0;

            string message = $"Status: ONLINE ({mode})\n" +
                             $"Room: {roomName}\n" +
                             $"Players: {playerCount}";

            UpdateDisplay(message, connectedColor);
        }
    }

    void UpdateDisplay(string text, Color color)
    {
        // Only update if text actually changed to save performance
        if (statusText.text != text)
        {
            statusText.text = text;
            statusText.color = color;
        }
    }
}