using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands.Samples.GestureSample;
using Fusion;

public class MagicCaster : MonoBehaviour
{
    [Header("References")]
    public XRHandTrackingEvents handEvents;
    public HandShapeCompletenessCalculator shapeCalculator;
    public Camera playerHead;

    [Header("Magic Setup")]
    public GestureConfig[] spells;

    [Header("Settings")]
    public float cooldown = 1.5f;
    public float detectionThreshold = 0.8f;

    [Header("Debug")]
    [Tooltip("Check this to see detection scores in the Console")]
    public bool debugMode = true; 

    private float _lastCastTime;
    private NetworkRunner _runner;

    [System.Serializable]
    public struct GestureConfig
    {
        public string spellName;
        public XRHandShape handShape;
        public NetworkObject projectilePrefab; 
    }

    void Start()
    {
        if (playerHead == null) playerHead = Camera.main;

        // DEBUG 1: Initialization Check
        if (handEvents == null) Debug.LogError("❌ MagicCaster: HandEvents is missing!");
        if (shapeCalculator == null) Debug.LogError("❌ MagicCaster: ShapeCalculator is missing!");
        if (spells.Length == 0) Debug.LogWarning("⚠️ MagicCaster: No spells defined in list!");
    }

    void OnEnable() => handEvents.jointsUpdated.AddListener(CheckGestures);
    void OnDisable() => handEvents.jointsUpdated.RemoveListener(CheckGestures);

    void CheckGestures(XRHandJointsUpdatedEventArgs args)
    {
        // 1. Global Cooldown Check
        if (Time.time < _lastCastTime + cooldown) return;

        // 2. Find Runner (Network Connection Check)
        if (_runner == null) _runner = FindFirstObjectByType<NetworkRunner>();
        
        if (_runner == null || !_runner.IsRunning) 
        {
            // DEBUG 2: Connection Check (Only log once every few seconds to avoid spam)
            if (Time.frameCount % 300 == 0 && debugMode) 
                Debug.LogWarning("⚠️ MagicCaster: NetworkRunner not ready. Cannot Cast.");
            return; 
        }

        // 3. Check every spell
        foreach (var spell in spells)
        {
            if (spell.handShape == null || spell.projectilePrefab == null) 
            {
                if (debugMode) Debug.LogError($"❌ Spell '{spell.spellName}' has missing Shape or Prefab!");
                continue;
            }

            shapeCalculator.TryCalculateHandShapeCompletenessScore(args.hand, spell.handShape, out float score);

            // DEBUG 3: Score Monitoring
            // If we are somewhat close (score > 0.5), log it so we can tune threshold
            if (debugMode && score > 0.5f && score < detectionThreshold)
            {
                Debug.Log($"Hand Shape '{spell.spellName}' detected but score too low: {score:F2} / {detectionThreshold}");
            }

            if (score > detectionThreshold)
            {
                if (debugMode) Debug.Log($"✅ SUCCESS: Gesture '{spell.spellName}' matched! Score: {score:F2}");
                
                CastSpell(spell, args.hand);
                _lastCastTime = Time.time;
                break; 
            }
        }
    }

    void CastSpell(GestureConfig spell, XRHand hand)
    {
        if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose tipPose))
        {
            Vector3 spawnPos = tipPose.position + (playerHead.transform.forward * 0.2f);
            Quaternion aimRot = playerHead.transform.rotation;

            Debug.Log($"✨ Spawning Projectile: {spell.projectilePrefab.name}");

            try
            {
                _runner.Spawn(spell.projectilePrefab, spawnPos, aimRot, _runner.LocalPlayer);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"🔴 SPAWN CRASH: {e.Message}");
            }
        }
    }
}