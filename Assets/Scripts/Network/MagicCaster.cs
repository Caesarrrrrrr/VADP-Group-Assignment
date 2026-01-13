using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands.Samples.GestureSample;
using Fusion;
using System.Collections.Generic;

public class MagicCaster : MonoBehaviour
{
    public enum SkillType { ShootProjectile, SpawnCircle, SpawnShield }

    [System.Serializable]
    public struct GestureConfig
    {
        [Header("Settings")]
        public string spellName;
        public XRHandShape handShape;
        public NetworkObject projectilePrefab; 
        public SkillType skillType;

        [Header("Balance")]
        public float cooldown; 

        [Header("Audio")]
        public AudioClip skillSound;
        [Range(0, 100)] public float soundVolume; 
    }

    [Header("References")]
    public XRHandTrackingEvents handEvents;
    public HandShapeCompletenessCalculator shapeCalculator;
    public Camera playerHead;

    [Header("Aiming Visuals")]
    public Material arcMaterial;
    public float arcHeight = 2.0f;
    public float maxCastDistance = 10f;

    [Header("Magic Config")]
    public GestureConfig[] spells;
    public float detectionThreshold = 0.8f;
    public bool debugMode = true;

    // --- PRIVATE STATE ---
    private Dictionary<int, float> _lastCastTimes = new Dictionary<int, float>();
    private NetworkRunner _runner;
    private bool _isAiming;
    private int _aimingSpellIndex = -1;
    private LineRenderer _lineRenderer;
    private GameObject _previewLineObj;
    private Vector3 _currentAimTarget; 

    // NEW: Prevents machine-gun firing. Tracks the last gesture we detected.
    private int _heldGestureIndex = -1; 

    void Start()
    {
        if (playerHead == null) playerHead = Camera.main;
        SetupLineRenderer();
    }

    void OnEnable() => handEvents.jointsUpdated.AddListener(CheckGestures);
    void OnDisable() 
    {
        handEvents.jointsUpdated.RemoveListener(CheckGestures);
        HidePreview();
    }

    void CheckGestures(XRHandJointsUpdatedEventArgs args)
    {
        if (_runner == null) _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null || !_runner.IsRunning) return;

        // 1. AIMING PHASE
        if (_isAiming && _aimingSpellIndex != -1)
        {
            HandleAimingLogic(args.hand);
            return; 
        }

        // 2. DETECTION PHASE
        bool gestureFoundThisFrame = false;

        for (int i = 0; i < spells.Length; i++)
        {
            var spell = spells[i];
            if (spell.handShape == null || spell.projectilePrefab == null) continue;

            shapeCalculator.TryCalculateHandShapeCompletenessScore(args.hand, spell.handShape, out float score);
            
            if (score > detectionThreshold)
            {
                gestureFoundThisFrame = true;

                // FIX: Require Release! 
                // If we are still holding the SAME gesture as last frame, DO NOTHING.
                if (_heldGestureIndex == i) return;

                // If this is a new gesture, mark it as held
                _heldGestureIndex = i;

                if (IsOnCooldown(i)) return;

                if (spell.skillType == SkillType.SpawnCircle)
                {
                    StartAiming(i);
                }
                else
                {
                    CastSpell(i, args.hand, null);
                }
                return; // Only process one spell at a time
            }
        }

        // If no gesture is detected, RESET the held index so we can fire again
        if (!gestureFoundThisFrame)
        {
            _heldGestureIndex = -1;
        }
    }

    void StartAiming(int index)
    {
        _isAiming = true;
        _aimingSpellIndex = index;
        _lineRenderer.enabled = true;
    }

    void HandleAimingLogic(XRHand hand)
    {
        int index = _aimingSpellIndex;
        var spell = spells[index];
        
        shapeCalculator.TryCalculateHandShapeCompletenessScore(hand, spell.handShape, out float score);
        
        // Lower threshold to keep aiming (hysteresis)
        bool stillHolding = score > (detectionThreshold - 0.15f);

        if (stillHolding)
        {
            Vector3 startPos = GetHandPosition(hand); 
            if (startPos == Vector3.zero) startPos = playerHead.transform.position + Vector3.down * 0.3f;
            _currentAimTarget = CalculateGroundPoint(playerHead.transform);
            DrawParabola(startPos, _currentAimTarget);
        }
        else
        {
            CastSpell(index, hand, _currentAimTarget);
            _isAiming = false;
            _aimingSpellIndex = -1;
            HidePreview();
            
            // Mark gesture as held so it doesn't auto-cast immediately again
            _heldGestureIndex = index; 
        }
    }

    void CastSpell(int index, XRHand hand, Vector3? overridePos)
    {
        var spell = spells[index];
        if (IsOnCooldown(index)) return;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        bool valid = false;

        if (spell.skillType == SkillType.SpawnCircle)
        {
            if (overridePos.HasValue) { spawnPos = overridePos.Value + Vector3.up * 0.02f; spawnRot = Quaternion.identity; valid = true; }
        }
        else if (spell.skillType == SkillType.SpawnShield)
        {
             Vector3 fwd = playerHead.transform.position + (playerHead.transform.forward * 1.5f);
             if (Physics.Raycast(fwd + Vector3.up, Vector3.down, out RaycastHit hit, 5f)) spawnPos = hit.point;
             else spawnPos = new Vector3(fwd.x, playerHead.transform.position.y - 1.5f, fwd.z);
             spawnRot = Quaternion.Euler(0, playerHead.transform.eulerAngles.y, 0);
             valid = true;
        }
        else // Projectile
        {
            spawnPos = GetHandPosition(hand);
            if (spawnPos != Vector3.zero)
            {
                spawnPos += playerHead.transform.forward * 0.5f; // Push further out
                spawnRot = playerHead.transform.rotation;
                valid = true;
            }
        }

        if (valid)
        {
            // FIX: Prevent Crash if Runner is busy
            try 
            {
                if (debugMode) Debug.Log($"[Spawning] {spell.spellName}");
                _runner.Spawn(spell.projectilePrefab, spawnPos, spawnRot, _runner.LocalPlayer);
                
                if (spell.skillSound != null) AudioSource.PlayClipAtPoint(spell.skillSound, spawnPos, spell.soundVolume / 100f);
                _lastCastTimes[index] = Time.time;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MagicCaster] Spawn Failed: {e.Message}");
            }
        }
    }

    bool IsOnCooldown(int index)
    {
        if (!_lastCastTimes.ContainsKey(index)) return false;
        return (Time.time - _lastCastTimes[index]) < spells[index].cooldown;
    }

    Vector3 GetHandPosition(XRHand hand)
    {
        if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose p)) return p.position;
        return Vector3.zero;
    }

    void SetupLineRenderer()
    {
        _previewLineObj = new GameObject("AimingLine_Visual");
        _lineRenderer = _previewLineObj.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.02f; _lineRenderer.endWidth = 0.01f; _lineRenderer.positionCount = 20;
        _lineRenderer.enabled = false;
        if (arcMaterial != null) _lineRenderer.material = arcMaterial;
        else _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }
    void HidePreview() { if (_lineRenderer != null) _lineRenderer.enabled = false; }
    
    Vector3 CalculateGroundPoint(Transform cam)
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, maxCastDistance)) return hit.point;
        Vector3 flatFwd = cam.forward; flatFwd.y = 0; flatFwd.Normalize();
        return cam.position + flatFwd * maxCastDistance - (Vector3.up * 1.5f); 
    }
    void DrawParabola(Vector3 start, Vector3 end)
    {
        Vector3 mid = (start + end) / 2; mid.y += arcHeight; 
        for (int i = 0; i < _lineRenderer.positionCount; i++) {
            float t = (float)i / (_lineRenderer.positionCount - 1);
            _lineRenderer.SetPosition(i, (1 - t) * (1 - t) * start + 2 * (1 - t) * t * mid + t * t * end);
        }
    }
}