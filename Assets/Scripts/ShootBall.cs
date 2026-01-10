using UnityEngine;
using Fusion;

public class ShootBall : MonoBehaviour
{
    private NetworkRunner _runner;

    // Notice: No 'shootForce' or 'lifeTime' here. 
    // Those belong inside the Fireball Prefab (MagicProjectile.cs) now.

    public void ShootingBall(NetworkObject prefabToSpawn, Vector3 spawnPosition, Quaternion aimDirection)
    {
        // 1. SAFETY CHECKS (Keep these, they are perfect)
        if (prefabToSpawn == null)
        {
            Debug.LogError("🔴 CRASH PREVENTED: 'projectilePrefab' is NULL!");
            return;
        }

        if (_runner == null) _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null || !_runner.IsRunning)
        {
            Debug.LogError("🔴 CRASH PREVENTED: Not connected to Fusion Network!");
            return;
        }

        // 2. SPAWN ONLY
        try 
        {
            // We just spawn it. The fireball script (MagicProjectile.cs) wakes up and starts moving itself.
            _runner.Spawn(prefabToSpawn, spawnPosition, aimDirection, _runner.LocalPlayer);
            
            // NO AddForce. 
            // NO Despawn Coroutine.
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔴 SPAWN ERROR: {e.Message}");
        }
    }
}