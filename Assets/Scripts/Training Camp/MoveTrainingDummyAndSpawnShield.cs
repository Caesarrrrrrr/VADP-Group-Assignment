using System.Collections;
using UnityEngine;

public class MoveTrainingDummyAndSpawnShield : MonoBehaviour
{
    [Header("Settings")]
    public GameObject shieldPrefab;
    public float moveSpeed = 2.0f;
    public float moveDistance = 1.5f;

    // We store a specific reference to the "Special" dummy here
    private GameObject targetDummy; 
    
    private bool isMoving = false;
    private Vector3 startPosition;
    private GameObject activeShield;

    // --- SETUP FUNCTION ---
    // We call this ONCE from the Spawner when the game starts
    public void RegisterDefaultDummy(GameObject dummy)
    {
        targetDummy = dummy;
        Debug.Log("Default Dummy Registered! Buttons will now control this specific dummy.");
    }

    // --- BUTTON 1: TOGGLE MOVEMENT ---
    public void ToggleMoveDummy()
    {
        // Only works if the registered dummy still exists
        if (targetDummy == null) return;

        isMoving = !isMoving;
        if (isMoving)
        {
            startPosition = targetDummy.transform.position;
        }
    }

    // --- BUTTON 2: SPAWN SHIELD ---
    public void GiveShield()
    {
        if (targetDummy == null) return;

        if (shieldPrefab != null)
        {
            activeShield = Instantiate(shieldPrefab, targetDummy.transform);
            activeShield.transform.localPosition = new Vector3(0, 0f, 0f);
            activeShield.transform.localRotation = Quaternion.identity;
            StartCoroutine(Destroy());
        }
    }

    private IEnumerator Destroy()
    {
        yield return new WaitForSeconds(9f);
        // Clean up the shield if the dummy is destroyed
        if (activeShield != null)
        {
            Destroy(activeShield);
        }
    }

    void Update()
    {
        // Only animate the TARGET dummy
        if (isMoving && targetDummy != null)
        {
            float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
            targetDummy.transform.position = startPosition + (targetDummy.transform.right * offset);
        }
    }
}