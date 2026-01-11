using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands.Samples.GestureSample;
using Meta.WitAi;

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
        [Header("Basic Settings")]
        public string name;
        public XRHandShape handShape;
        public GameObject projectilePrefab;
        public SkillType skillType;

        [Header("Voice Settings")]
        public bool requiresVoice;
        [Tooltip("Must match the 'Resolved Value' from Wit.ai exactly (e.g., fireball)")]
        public string voiceKeyword;

        [Header("Sound Settings")]
        [Tooltip("Sound effect to play when skill is cast")]
        public AudioClip skillSound;

        [Range(0, 100)]
        [Tooltip("Volume level (0-100)")]
        public float soundVolume; // No default value to avoid struct error
    }

    [SerializeField] private GestureMapping[] gestureMappings;
    [SerializeField] private float minimumGestureThreshold = 0.6f;
    [SerializeField] private HandShapeCompletenessCalculator handShapeCompletenessCalculator;
    [SerializeField] private MagicSpawner magicSpawner;

    [Header("Voice Service")]
    [SerializeField] private VoiceService voiceService;

    [Header("Cooldown Settings")]
    [SerializeField] private float fireballCooldown = 0.5f;
    [SerializeField] private float shieldCooldown = 5.0f;
    [SerializeField] private float circleCooldown = 3.0f;

    private float lastFireballTime = -100f;
    private float lastShieldTime = -100f;
    private float lastCircleTime = -100f;

    private bool isAimingCircle = false;
    private int aimingGestureIndex = -1;
    private Camera mainCamera;
    private XRHand lastTrackedHand;
    private bool isHandTracked = false;

    // To detect the rising edge (moment gesture starts)
    private bool[] wasGestureActivePreviously;
    // To track grace period
    private float[] lastGestureActiveTime;
    private float gestureGracePeriod = 2.0f;

    void Start()
    {
        mainCamera = Camera.main;
        wasGestureActivePreviously = new bool[gestureMappings.Length];
        lastGestureActiveTime = new float[gestureMappings.Length];

        // Initialize with a past time
        for (int i = 0; i < lastGestureActiveTime.Length; i++) lastGestureActiveTime[i] = -100f;
    }

    void OnEnable() => handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
    void OnDisable() => handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);

    void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
    {
        lastTrackedHand = eventArgs.hand;
        isHandTracked = handTrackingEvents.handIsTracked;

        for (int i = 0; i < gestureMappings.Length; i++)
        {
            var mapping = gestureMappings[i];

            handShapeCompletenessCalculator.TryCalculateHandShapeCompletenessScore(eventArgs.hand, mapping.handShape, out float completenessScore);
            bool isDetected = handTrackingEvents.handIsTracked && completenessScore >= minimumGestureThreshold;

            if (isDetected) lastGestureActiveTime[i] = Time.time;

            // Auto-activate Microphone logic (Rising Edge)
            if (mapping.requiresVoice && isDetected && !wasGestureActivePreviously[i])
            {
                if (voiceService != null && !voiceService.Active)
                {
                    Debug.Log($"[DetectGestures] Gesture {mapping.name} detected -> Activating Microphone");
                    voiceService.Activate();
                }
            }

            wasGestureActivePreviously[i] = isDetected;

            // Instant skills (No Voice)
            if (!mapping.requiresVoice && mapping.skillType != SkillType.SpawnCircle && isDetected)
            {
                ExecuteInstantSkill(mapping, eventArgs.hand);
            }
            // Circle skills
            else if (mapping.skillType == SkillType.SpawnCircle)
            {
                HandleCircleLogic(i, mapping, isDetected);
            }
        }
    }

    // Called by VoiceBridge
    public void OnVoiceCommandReceived(string spokenWord)
    {
        if (string.IsNullOrEmpty(spokenWord)) return;
        spokenWord = spokenWord.ToLower().Trim();

        for (int i = 0; i < gestureMappings.Length; i++)
        {
            var mapping = gestureMappings[i];

            if (mapping.requiresVoice && mapping.voiceKeyword.ToLower() == spokenWord)
            {
                // Check Grace Period
                if (Time.time - lastGestureActiveTime[i] < gestureGracePeriod)
                {
                    Debug.Log($"[Success] Casting {mapping.name}!");
                    ExecuteInstantSkill(mapping, lastTrackedHand);
                }
                else
                {
                    Debug.Log($"[Failed] Keyword matched, but gesture timed out.");
                }
            }
        }
    }

    void ExecuteInstantSkill(GestureMapping mapping, XRHand hand)
    {
        if (mainCamera == null || magicSpawner == null) return;

        bool skillTriggered = false;
        Vector3 soundPosition = mainCamera.transform.position; // Default position

        // Try to get hand position for 3D sound
        if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose p))
        {
            soundPosition = p.position;
        }

        switch (mapping.skillType)
        {
            case SkillType.ShootProjectile:
                if (Time.time >= lastFireballTime + fireballCooldown)
                {
                    Vector3 spawnPos = mainCamera.transform.position + mainCamera.transform.forward * 0.5f;
                    if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
                    {
                        spawnPos = indexPose.position;
                    }

                    magicSpawner.ShootingBall(mapping.projectilePrefab, spawnPos, mainCamera.transform.rotation);
                    lastFireballTime = Time.time;
                    skillTriggered = true;
                }
                break;

            case SkillType.SpawnShield:
                if (Time.time >= lastShieldTime + shieldCooldown)
                {
                    magicSpawner.SpawnShield(mapping.projectilePrefab, mainCamera.transform);
                    lastShieldTime = Time.time;
                    skillTriggered = true;
                }
                break;
        }

        // Play Sound Effect
        if (skillTriggered && mapping.skillSound != null)
        {
            // Convert 0-100 input to 0.0-1.0 float
            AudioSource.PlayClipAtPoint(mapping.skillSound, soundPosition, mapping.soundVolume / 100f);
        }
    }

    void HandleCircleLogic(int index, GestureMapping mapping, bool isDetected)
    {
        if (isDetected)
        {
            isAimingCircle = true;
            aimingGestureIndex = index;
            if (magicSpawner != null) magicSpawner.UpdateAimingPreview(mainCamera.transform);
        }
        else
        {
            if (isAimingCircle && aimingGestureIndex == index)
            {
                if (Time.time >= lastCircleTime + circleCooldown)
                {
                    if (magicSpawner != null) magicSpawner.SpawnGroundCircle(mapping.projectilePrefab, mainCamera.transform);

                    // Play Sound for Circle (at player's feet/camera position)
                    if (mapping.skillSound != null)
                    {
                        AudioSource.PlayClipAtPoint(mapping.skillSound, mainCamera.transform.position, mapping.soundVolume / 100f);
                    }

                    lastCircleTime = Time.time;
                }
                else
                {
                    if (magicSpawner != null) magicSpawner.HidePreview();
                }
                isAimingCircle = false;
                aimingGestureIndex = -1;
            }
        }
    }
}