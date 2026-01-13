using UnityEngine;
using Fusion;

public class RunnerCleanup : MonoBehaviour
{
    private NetworkRunner _runner;

    void Start()
    {
        _runner = GetComponent<NetworkRunner>();
    }

    // Force shutdown when the Unity application quits or this object is destroyed
    void OnDestroy()
    {
        if (_runner != null && _runner.IsRunning)
        {
            Debug.Log("🛑 Cleaning up Zombie Runner...");
            _runner.Shutdown();
        }
    }

    // Optional: Call this button when you click "Exit" in your UI
    public void ManualDisconnect()
    {
        if (_runner != null) _runner.Shutdown();
    }
}