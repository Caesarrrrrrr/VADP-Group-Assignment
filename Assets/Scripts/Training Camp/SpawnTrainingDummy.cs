using UnityEngine;
using System.Collections; 

public class SpawnTrainingDummy : MonoBehaviour
{
    [Header("References")]
    public GameObject dummyPrefab;
    public Transform playerHead;
    
    [Tooltip("Drag the Controller script here")]
    public MoveTrainingDummyAndSpawnShield controllerScript; 

    [Header("Settings")]
    public float defaultSpawnDistance = 3.0f; 
    public float buttonSpawnDistance = 1.0f;
    public float defaultFloorY = 0.0f; 

    // We keep track of the latest one just for cleanup (optional)
    private GameObject currentDummy; 

    private void Start()
    {
        StartCoroutine(DelayedSpawn());
    }

    private IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(2.0f);
        
        // 1. Spawn the Default Dummy
        GameObject defaultDummy = SpawnInternal(defaultSpawnDistance);

        // 2. REGISTER it with the controller
        // This makes the Move/Shield buttons lock onto THIS specific dummy
        if (controllerScript != null && defaultDummy != null)
        {
            controllerScript.RegisterDefaultDummy(defaultDummy);
        }
    }

    public void SpawnDummy()
    {
        SpawnInternal(buttonSpawnDistance);
    }

    // Changed return type to GameObject so we can use the reference
    private GameObject SpawnInternal(float distance)
    {
        if (dummyPrefab == null || playerHead == null) return null;

        // Position Math
        Vector3 forwardFlat = playerHead.forward;
        forwardFlat.y = 0; 
        forwardFlat.Normalize();
        Vector3 targetPos = playerHead.position + (forwardFlat * distance);
        
        // Floor Raycast
        Vector3 rayStart = new Vector3(targetPos.x, playerHead.position.y + 0.5f, targetPos.z);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f))
            targetPos.y = hit.point.y; 
        else
            targetPos.y = defaultFloorY; 

        // Instantiate
        GameObject newDummy = Instantiate(dummyPrefab, targetPos, Quaternion.identity);

        // Rotation
        Vector3 directionToPlayer = playerHead.position - newDummy.transform.position;
        directionToPlayer.y = 0; 
        if (directionToPlayer != Vector3.zero)
            newDummy.transform.rotation = Quaternion.LookRotation(directionToPlayer);

        currentDummy = newDummy;
        return newDummy;
    }
}