using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab; // Drag your NetworkPlayer prefab here

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            // Spawn the Avatar for the local player
            // We use Vector3.zero because the HardwareFollower will snap it to the headset immediately
            Runner.Spawn(PlayerPrefab, Vector3.zero, Quaternion.identity, player);
        }
    }
}