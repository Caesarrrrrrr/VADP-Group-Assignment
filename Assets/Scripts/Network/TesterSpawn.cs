using UnityEngine;
using Fusion;

public class TesterSpawn : NetworkBehaviour
{
    public NetworkObject dummyPrefab; // Drag your Network Rig / Enemy Prefab here

    public override void Spawned()
    {
        // Only the Host should spawn enemies/dummies
        if (Object.HasStateAuthority)
        {
            Vector3 dummyPos = new Vector3(0, 0, 2); // 2 meters in front
            Runner.Spawn(dummyPrefab, dummyPos, Quaternion.identity);
            Debug.Log("🎯 Target Dummy Spawned!");
        }
    }
}