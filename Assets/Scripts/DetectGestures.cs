using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands.Samples.GestureSample;
using Fusion;

public class DetectGestures : MonoBehaviour
{
    [SerializeField] private XRHandTrackingEvents handTrackingEvents;

    // 1. DEFINE A CUSTOM STRUCT TO LINK SHAPE -> PREFAB
    [System.Serializable]
    public struct GestureMapping
    {
        public string name; // Just for organization in Inspector
        public XRHandShape handShape;
        public NetworkObject projectilePrefab; // Drag Sphere or Cube here
    }

    // 2. USE THIS LIST INSTEAD OF JUST 'XRHandShape[]'
    [SerializeField] private GestureMapping[] gestureMappings;

    [SerializeField] private float gestureDetectionInterval = 0.1f;
    [SerializeField] private float minimumGestureThreshold = 0.9f;
    [SerializeField] private HandShapeCompletenessCalculator handShapeCompletenessCalculator;
    [SerializeField] private ShootBall shootBall;

    [Header("Shooting Settings")]
    [SerializeField] private float shootCooldown = 2.0f;

    private float timeOfLastConditionsCheck;
    private float timeOfLastShot;
    private Camera mainCamera;

    void OnEnable() => handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
    void OnDisable() => handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);

    void Start()
    {
        mainCamera = Camera.main;
        timeOfLastShot = -shootCooldown;

        // Safety Check: Warn if references are missing
        if (shootBall == null) Debug.LogError("DetectGestures: ShootBall script is missing!");
        if (handShapeCompletenessCalculator == null) Debug.LogError("DetectGestures: Calculator is missing!");
    }

    void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
    {
        if (Time.time - timeOfLastConditionsCheck < gestureDetectionInterval)
            return;

        timeOfLastConditionsCheck = Time.time;

        // 3. LOOP THROUGH THE MAPPINGS
        foreach (var mapping in gestureMappings)
        {
            // SAFETY CHECK: Skip if the shape or prefab is missing
            if (mapping.handShape == null || mapping.projectilePrefab == null) continue;

            // Calculate score using the shape from the mapping
            if (handShapeCompletenessCalculator != null)
            {
                handShapeCompletenessCalculator.TryCalculateHandShapeCompletenessScore(eventArgs.hand,
                    mapping.handShape, out float completenessScore);

                var detected = handTrackingEvents.handIsTracked && completenessScore >= minimumGestureThreshold;

                if (detected)
                {
                    if (Time.time >= timeOfLastShot + shootCooldown)
                    {
                        Debug.Log($"Detected: {mapping.name} | Shooting: {mapping.projectilePrefab.name}");

                        if (eventArgs.hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
                        {
                            // 4. PASS THE SPECIFIC PREFAB TO THE FUNCTION
                            FireAtLookDirection(mapping.projectilePrefab, indexPose.position);
                            timeOfLastShot = Time.time;
                        }
                    }
                }
            }
        }
    }

    // 5. UPDATE FUNCTION TO ACCEPT THE PREFAB
    void FireAtLookDirection(NetworkObject prefabToShoot, Vector3 handPosition)
    {
        if (mainCamera == null) return;
        if (shootBall == null) return;

        Quaternion aimRotation = mainCamera.transform.rotation;
        // Optional: Aim slightly up so it doesn't hit the floor immediately
        // Quaternion aimRotation = Quaternion.LookRotation(mainCamera.transform.forward + Vector3.up * 0.1f);

        Vector3 lookDirection = mainCamera.transform.forward;
        Vector3 spawnPos = handPosition + (lookDirection * 0.2f); // Push out 20cm to avoid hitting own hand

        // Pass the specific prefab to the ShootBall script
        shootBall.ShootingBall(prefabToShoot, spawnPos, aimRotation);
    }

    void Update() { }
}