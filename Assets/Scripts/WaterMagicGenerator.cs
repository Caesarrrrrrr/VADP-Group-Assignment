using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

public class WaterMagicGenerator : MonoBehaviour // 1. Change to MonoBehaviour
{
    [Header("Core Settings")]
    [Tooltip("Drag your Water Magic Prefab here (Must have NetworkObject component)")]
    public NetworkObject waterMagicPrefab;

    [Header("Raycast Settings")]
    public float maxRayDistance = 10.0f;
    public LayerMask groundLayerMask = -1; 
    public float lifeTime = 5.0f;

    private bool _wasPressedLastFrame = false;
    private NetworkRunner _runner; // 2. Cache the Runner

    // 3. Use Update() for local input
    void Update()
    {
        // Detect Input
        bool isPressedNow = false;

        // A. Quest Trigger (Right Hand)
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            isPressedNow = true;
        }
        // B. PC Mouse Click
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPressedNow = true;
        }

        if (isPressedNow && !_wasPressedLastFrame)
        {
            TrySpawnMagicCircleOnGround();
        }

        _wasPressedLastFrame = isPressedNow;
    }

    void TrySpawnMagicCircleOnGround()
    {
        // 4. Find Runner if missing
        if (_runner == null) _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null || !_runner.IsRunning) return;

        Transform camTransform = Camera.main.transform;
        if (camTransform == null) return;

        RaycastHit hitInfo;
        if (Physics.Raycast(camTransform.position, camTransform.forward, out hitInfo, maxRayDistance, groundLayerMask))
        {
            Vector3 spawnPosition = hitInfo.point;
            Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);

            // 5. Network Spawn (Logic is the same)
            NetworkObject magicInstance = _runner.Spawn(waterMagicPrefab, spawnPosition, spawnRotation, _runner.LocalPlayer);
            
            // Adjust height slightly to prevent flickering (Z-fighting)
            magicInstance.transform.position += hitInfo.normal * 0.02f;

            if (lifeTime > 0)
            {
                StartCoroutine(DespawnRoutine(magicInstance, lifeTime));
            }
        }
    }

    System.Collections.IEnumerator DespawnRoutine(NetworkObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null && _runner != null && obj.HasStateAuthority)
        {
            _runner.Despawn(obj);
        }
    }
}