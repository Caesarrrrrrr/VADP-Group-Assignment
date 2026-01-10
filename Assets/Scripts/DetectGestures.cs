using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands.Samples.GestureSample;

public class DetectGestures : MonoBehaviour
{
    [SerializeField] private XRHandTrackingEvents handTrackingEvents;

    public enum SkillType
    {
        ShootProjectile,
        SpawnShield,
        SpawnCircle
    }

    [System.Serializable]
    public struct GestureMapping
    {
        public string name;
        public XRHandShape handShape;
        public GameObject projectilePrefab;
        public SkillType skillType;
    }

    [SerializeField] private GestureMapping[] gestureMappings;
    [SerializeField] private float minimumGestureThreshold = 0.6f;
    [SerializeField] private HandShapeCompletenessCalculator handShapeCompletenessCalculator;
    [SerializeField] private MagicSpawner magicSpawner;

    [Header("Cooldown Settings (Seconds)")]
    [Tooltip("Cooldown for Fireball")]
    [SerializeField] private float fireballCooldown = 0.5f;

    [Tooltip("Cooldown for Shield")]
    [SerializeField] private float shieldCooldown = 5.0f;

    [Tooltip("Cooldown for Magic Circle")]
    [SerializeField] private float circleCooldown = 3.0f;

    // Track the last execution time for each skill
    private float lastFireballTime = -100f;
    private float lastShieldTime = -100f;
    private float lastCircleTime = -100f;

    // State for Magic Circle Aiming
    private bool isAimingCircle = false;
    private int aimingGestureIndex = -1;
    private Camera mainCamera;

    void OnEnable() => handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
    void OnDisable() => handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);

    void Start()
    {
        mainCamera = Camera.main;
        // Initialize timers so skills are ready immediately
        lastFireballTime = -fireballCooldown;
        lastShieldTime = -shieldCooldown;
        lastCircleTime = -circleCooldown;
    }

    void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
    {
        for (int i = 0; i < gestureMappings.Length; i++)
        {
            var mapping = gestureMappings[i];

            // 1. Calculate Score
            handShapeCompletenessCalculator.TryCalculateHandShapeCompletenessScore(eventArgs.hand, mapping.handShape, out float completenessScore);
            bool isDetected = handTrackingEvents.handIsTracked && completenessScore >= minimumGestureThreshold;

            // 2. Logic Flow based on Skill Type
            if (mapping.skillType == SkillType.SpawnCircle)
            {
                // Special logic for Circle: Hold to Aim, Release to Cast
                HandleCircleLogic(i, mapping, isDetected);
            }
            else
            {
                // Instant cast logic for Fireball and Shield
                if (isDetected)
                {
                    ExecuteInstantSkill(mapping, eventArgs.hand);
                }
            }
        }
    }

    // Logic: Aim while holding, Cast when released
    void HandleCircleLogic(int index, GestureMapping mapping, bool isDetected)
    {
        if (isDetected)
        {
            // === HOLDING GESTURE: AIMING ===
            isAimingCircle = true;
            aimingGestureIndex = index;

            // Show preview line
            if (magicSpawner != null)
                magicSpawner.UpdateAimingPreview(mainCamera.transform);
        }
        else
        {
            // === GESTURE RELEASED: CASTING ===
            if (isAimingCircle && aimingGestureIndex == index)
            {
                // Check Cooldown
                if (Time.time >= lastCircleTime + circleCooldown)
                {
                    Debug.Log("Magic Circle Cast!");
                    if (magicSpawner != null)
                        magicSpawner.SpawnGroundCircle(mapping.projectilePrefab, mainCamera.transform);

                    lastCircleTime = Time.time;
                }
                else
                {
                    float remaining = (lastCircleTime + circleCooldown) - Time.time;
                    Debug.Log($"Circle Cooldown: {remaining:F1}s remaining");

                    // Hide the preview line even if cast failed due to cooldown
                    if (magicSpawner != null) magicSpawner.HidePreview();
                }

                // Reset State
                isAimingCircle = false;
                aimingGestureIndex = -1;
            }
        }
    }

    // Logic: Instant Cast
    void ExecuteInstantSkill(GestureMapping mapping, XRHand hand)
    {
        if (mainCamera == null || magicSpawner == null) return;

        switch (mapping.skillType)
        {
            case SkillType.ShootProjectile:
                // Check Fireball Cooldown
                if (Time.time >= lastFireballTime + fireballCooldown)
                {
                    if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
                    {
                        Vector3 spawnPos = indexPose.position + (mainCamera.transform.forward * 0.2f);
                        magicSpawner.ShootingBall(mapping.projectilePrefab, spawnPos, mainCamera.transform.rotation);

                        lastFireballTime = Time.time;
                    }
                }
                break;

            case SkillType.SpawnShield:
                // Check Shield Cooldown
                if (Time.time >= lastShieldTime + shieldCooldown)
                {
                    magicSpawner.SpawnShield(mapping.projectilePrefab, mainCamera.transform);

                    lastShieldTime = Time.time;
                    Debug.Log("Shield Spawned. Cooldown started.");
                }
                break;
        }
    }
}