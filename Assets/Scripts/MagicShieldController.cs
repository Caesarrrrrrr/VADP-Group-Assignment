using UnityEngine;
using Fusion; // Required for Networking
using UnityEngine.InputSystem; // Required for PC Input fix

// 1. Change to MonoBehaviour (Attach this to your OVRCameraRig or Manager)
public class MagicShieldController : MonoBehaviour
{
    [Header("Core Settings")]
    [Tooltip("Drag the Magic Shield Prefab here (Must have NetworkObject component)")]
    public NetworkObject shieldPrefab; 

    [Tooltip("Target to follow, usually Main Camera (Player Head)")]
    public Transform playerHead;

    [Tooltip("Hold to activate? (Checked = Hold to show / Release to hide; Unchecked = Click to toggle)")]
    public bool holdToActivate = true;

    [Header("Ground Detection")]
    [Tooltip("Default height below head if raycast fails (usually 1.6 or 1.7 meters)")]
    public float defaultHeight = 1.7f;

    [Tooltip("Layer for ground detection: Uncheck 'Player' and 'TransparentFX', select 'Default' or 'Ground'")]
    public LayerMask groundLayer = -1; 

    // Reference to the spawned network object
    private NetworkObject currentShield;
    private NetworkRunner _runner; // Cache the runner

    void Start()
    {
        // Auto-find main camera if not assigned
        if (playerHead == null && Camera.main != null)
        {
            playerHead = Camera.main.transform;
        }
    }

    // 2. Use Update for smooth local input and movement
    void Update()
    {
        // Safety check: Needs a player head to calculate position
        if (playerHead == null) return;

        // --- 1. Input Control ---
        HandleInput();

        // --- 2. Position Following ---
        if (currentShield != null)
        {
            UpdateShieldPosition();
        }
    }

    void HandleInput()
    {
        bool isPressed = false;

        // A. Check Quest Controller Input (Right Index Trigger)
        // Note: You can change 'RTouch' to 'LTouch' if you want left hand
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            isPressed = true;
        }
        // B. Check PC Mouse Input (For safe testing)
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPressed = true;
        }

        // Logic
        if (holdToActivate)
        {
            // Create if holding and doesn't exist
            if (isPressed && currentShield == null) ActivateShield();
            // Destroy if released and exists
            else if (!isPressed && currentShield != null) DeactivateShield();
        }
        else
        {
            // Simple toggle logic for click (optional)
            // Ideally, you'd use 'GetDown' for toggle, but keeping simple here
            if (isPressed && currentShield == null) ActivateShield();
        }
    }

    void ActivateShield()
    {
        // 3. Find the Runner if we haven't already
        if (_runner == null) _runner = FindFirstObjectByType<NetworkRunner>();
        
        // Safety: If not connected, we can't spawn network objects
        if (_runner == null || !_runner.IsRunning) return;

        if (currentShield != null) return;

        if (shieldPrefab != null)
        {
            // 4. Spawn the shield on the network
            // We pass '_runner.LocalPlayer' so we own it
            currentShield = _runner.Spawn(shieldPrefab, GetGroundPosition(), Quaternion.identity, _runner.LocalPlayer);
        }
    }

    void DeactivateShield()
    {
        if (currentShield != null)
        {
            // Safety: Check if runner is still valid before despawning
            if (_runner != null && _runner.IsRunning)
            {
                _runner.Despawn(currentShield);
            }
            currentShield = null;
        }
    }

    void UpdateShieldPosition()
    {
        // Update position every frame
        // Since we spawned it, we are the State Authority. 
        // The NetworkTransform on the prefab will sync this position to everyone else.
        if (currentShield != null && currentShield.HasStateAuthority)
        {
            currentShield.transform.position = GetGroundPosition();
            
            // Optional: Face the same way the player is looking
            // currentShield.transform.rotation = Quaternion.Euler(0, playerHead.eulerAngles.y, 0);
        }
    }

    Vector3 GetGroundPosition()
    {
        RaycastHit hit;

        // Raycast down from head
        if (Physics.Raycast(playerHead.position, Vector3.down, out hit, 3.0f, groundLayer))
        {
            // Found ground -> Place slightly above
            return hit.point + Vector3.up * 0.01f;
        }
        else
        {
            // No ground -> Float at fixed height
            return playerHead.position - Vector3.up * defaultHeight;
        }
    }
}