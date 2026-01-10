using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands.Samples.GestureSample;
using Meta.WitAi; // 引用 Voice SDK

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
        public bool requiresVoice;
        [Tooltip("必须和 Wit.ai 识别到的词完全一致 (比如 fireball)")]
        public string voiceKeyword;
    }

    [SerializeField] private GestureMapping[] gestureMappings;
    [SerializeField] private float minimumGestureThreshold = 0.6f;
    [SerializeField] private HandShapeCompletenessCalculator handShapeCompletenessCalculator;
    [SerializeField] private MagicSpawner magicSpawner;

    // ✅ 用来开启麦克风
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

    // 记录上一帧状态，用于检测“刚刚做出手势”的瞬间
    private bool[] wasGestureActivePreviously;
    // 记录手势最后活跃时间 (宽限期用)
    private float[] lastGestureActiveTime;
    private float gestureGracePeriod = 2.0f; // 宽限期 2秒 (给 Wit.ai 反应时间)

    void Start()
    {
        mainCamera = Camera.main;
        wasGestureActivePreviously = new bool[gestureMappings.Length];
        lastGestureActiveTime = new float[gestureMappings.Length];

        // 初始化时间
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

            if (isDetected)
            {
                lastGestureActiveTime[i] = Time.time; // 更新活跃时间
            }

            // === 自动激活麦克风逻辑 ===
            // 只有当：需要语音 + 刚刚没做手势 + 现在做了手势
            if (mapping.requiresVoice && isDetected && !wasGestureActivePreviously[i])
            {
                if (voiceService != null && !voiceService.Active)
                {
                    Debug.Log($"[DetectGestures] 检测到 {mapping.name} 手势 -> 开启麦克风!");
                    voiceService.Activate(); // 🎤 激活 Wit.ai
                }
            }

            wasGestureActivePreviously[i] = isDetected;

            // 不需要语音的技能直接触发
            if (!mapping.requiresVoice && mapping.skillType != SkillType.SpawnCircle && isDetected)
            {
                ExecuteInstantSkill(mapping, eventArgs.hand);
            }
            else if (mapping.skillType == SkillType.SpawnCircle)
            {
                HandleCircleLogic(i, mapping, isDetected);
            }
        }
    }

    // === 📡 这个函数由 VoiceBridge 调用 ===
    public void OnVoiceCommandReceived(string spokenWord)
    {
        if (string.IsNullOrEmpty(spokenWord)) return;

        spokenWord = spokenWord.ToLower().Trim();
        Debug.Log($"[DetectGestures] 收到指令: {spokenWord}");

        for (int i = 0; i < gestureMappings.Length; i++)
        {
            var mapping = gestureMappings[i];

            if (mapping.requiresVoice && mapping.voiceKeyword.ToLower() == spokenWord)
            {
                // 检查：手势是否在宽限期内？(即使手松开了，只要是2秒内做的都算数)
                if (Time.time - lastGestureActiveTime[i] < gestureGracePeriod)
                {
                    Debug.Log($"[✨施法成功] 匹配关键词: {spokenWord}");
                    ExecuteInstantSkill(mapping, lastTrackedHand);
                }
                else
                {
                    Debug.Log($"[施法失败] 词对上了，但手势断开太久了。");
                }
            }
        }
    }

    // ... (剩下的 HandleCircleLogic 和 ExecuteInstantSkill 保持不变，可以直接用之前的)
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

    void ExecuteInstantSkill(GestureMapping mapping, XRHand hand)
    {
        if (mainCamera == null || magicSpawner == null) return;

        switch (mapping.skillType)
        {
            case SkillType.ShootProjectile:
                if (Time.time >= lastFireballTime + fireballCooldown)
                {
                    // 尝试获取食指指尖位置，如果获取不到就用手腕或者相机前方
                    Vector3 spawnPos = mainCamera.transform.position + mainCamera.transform.forward * 0.5f;

                    if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
                    {
                        spawnPos = indexPose.position;
                    }

                    magicSpawner.ShootingBall(mapping.projectilePrefab, spawnPos, mainCamera.transform.rotation);
                    lastFireballTime = Time.time;
                }
                break;

            case SkillType.SpawnShield:
                if (Time.time >= lastShieldTime + shieldCooldown)
                {
                    magicSpawner.SpawnShield(mapping.projectilePrefab, mainCamera.transform);
                    lastShieldTime = Time.time;
                }
                break;
        }
    }
}