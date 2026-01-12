using UnityEngine;
using TMPro;
using Fusion;

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

        // 1. Try to find the NetworkRunner if we don't have it
        if (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
        }

        // 2. Logic: Determine Status
        if (_runner == null)
        {
            UpdateDisplay("Status: Offline (No Runner)", offlineColor);
        }
        else if (!_runner.IsRunning)
        {
            UpdateDisplay("Status: Starting Network...", loadingColor);
        }
        else if (_runner.IsRunning)
        {
            // We are connected! Gather details.
            string mode = _runner.GameMode == GameMode.Host ? "HOST" : "CLIENT";
            string roomName = _runner.SessionInfo.IsValid ? _runner.SessionInfo.Name : "Joining...";
            int playerCount = _runner.SessionInfo.IsValid ? _runner.SessionInfo.PlayerCount : 0;

            string message = $"Status: ONLINE ({mode})\n" +
                             $"Room: {roomName}\n" +
                             $"Players: {playerCount}";

            UpdateDisplay(message, connectedColor);
        }
    }

    // Helper function to change text and color
    void UpdateDisplay(string text, Color color)
    {
        statusText.text = text;
        statusText.color = color;
    }
}
