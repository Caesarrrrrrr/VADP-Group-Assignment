using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands.Samples.GestureSample;
using Fusion;
using System.Collections.Generic;

public class MagicCaster : NetworkBehaviour
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
    [Tooltip("Hand must relax below this score to fire again")]
    public float releaseThreshold = 0.65f; 
    public bool debugMode = true;

    // --- PRIVATE STATE ---
    private Dictionary<int, float> _lastCastTimes = new Dictionary<int, float>();
    private bool _isAiming;
    private int _aimingSpellIndex = -1;
    private LineRenderer _lineRenderer;
    private GameObject _previewLineObj;
    private Vector3 _currentAimTarget; 
    private int _heldGestureIndex = -1; 

    public override void Spawned()
    {
        if (playerHead == null) playerHead = Camera.main;
        SetupLineRenderer();

        // LOGIC: If I have input authority, I am ready to cast.
        if (HasInputAuthority)
        {
            Debug.Log("✅ Magic Ready! Cast immediately (Unlocked).");
        }
    }

    void OnEnable() { if (handEvents != null) handEvents.jointsUpdated.AddListener(CheckGestures); }
    void OnDisable() { if (handEvents != null) handEvents.jointsUpdated.RemoveListener(CheckGestures); HidePreview(); }

    void CheckGestures(XRHandJointsUpdatedEventArgs args)
    {
        // 1. Only run for my own hands
        if (Object != null && !Object.HasInputAuthority) return;
        if (Runner == null || !Runner.IsRunning) return;

        // 2. AIMING PHASE
        if (_isAiming && _aimingSpellIndex != -1)
        {
            HandleAimingLogic(args.hand);
            return; 
        }

        // 3. RELEASE PHASE (Prevents Machine Gun)
        if (_heldGestureIndex != -1)
        {
            var heldSpell = spells[_heldGestureIndex];
            shapeCalculator.TryCalculateHandShapeCompletenessScore(args.hand, heldSpell.handShape, out float releaseScore);
            
            if (releaseScore < releaseThreshold)
            {
                if (debugMode) Debug.Log($"[Magic] 🔓 Released {_heldGestureIndex}. Ready.");
                _heldGestureIndex = -1;
            }
            return; 
        }

        // 4. DETECTION PHASE
        for (int i = 0; i < spells.Length; i++)
        {
            var spell = spells[i];
            if (spell.handShape == null || spell.projectilePrefab == null) continue;

            shapeCalculator.TryCalculateHandShapeCompletenessScore(args.hand, spell.handShape, out float score);
            
            // Debug score to ensure tracking works
            // if (debugMode && score > 0.5f) Debug.Log($"Checking {spell.spellName}: {score:F2}");

            if (score > detectionThreshold)
            {
                if (IsOnCooldown(i)) return;

                _heldGestureIndex = i;
                if (debugMode) Debug.Log($"[Magic] ⚡ DETECTED {spell.spellName}!");

                if (spell.skillType == SkillType.SpawnCircle) StartAiming(i);
                else CastSpell(i, args.hand, null);
                return; 
            }
        }
    }

    // --- LOGIC HELPERS ---
    void StartAiming(int index) { _isAiming = true; _aimingSpellIndex = index; _lineRenderer.enabled = true; }

    void HandleAimingLogic(XRHand hand)
    {
        int index = _aimingSpellIndex;
        shapeCalculator.TryCalculateHandShapeCompletenessScore(hand, spells[index].handShape, out float score);
        
        if (score > releaseThreshold) 
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
            _heldGestureIndex = index; 
        }
    }

    void CastSpell(int index, XRHand hand, Vector3? overridePos)
    {
        var spell = spells[index];
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        bool valid = false;

        if (spell.skillType == SkillType.SpawnCircle && overridePos.HasValue) {
            spawnPos = overridePos.Value + Vector3.up * 0.02f; valid = true;
        }
        else if (spell.skillType == SkillType.SpawnShield) {
             Vector3 fwd = playerHead.transform.position + (playerHead.transform.forward * 1.5f);
             if (Physics.Raycast(fwd + Vector3.up, Vector3.down, out RaycastHit hit, 5f)) spawnPos = hit.point;
             else spawnPos = new Vector3(fwd.x, playerHead.transform.position.y - 1.5f, fwd.z);
             spawnRot = Quaternion.Euler(0, playerHead.transform.eulerAngles.y, 0);
             valid = true;
        }
        else { 
            spawnPos = GetHandPosition(hand);
            if (spawnPos != Vector3.zero) {
                spawnPos += playerHead.transform.forward * 0.5f; 
                spawnRot = playerHead.transform.rotation;
                valid = true;
            }
        }

        if (valid)
        {
            if (debugMode) Debug.Log($"[Spawning] {spell.spellName}");
            Runner.Spawn(spell.projectilePrefab, spawnPos, spawnRot, Runner.LocalPlayer);
            if (spell.skillSound != null) AudioSource.PlayClipAtPoint(spell.skillSound, spawnPos, spell.soundVolume / 100f);
            _lastCastTimes[index] = Time.time;
        }
    }

    bool IsOnCooldown(int index) {
        if (!_lastCastTimes.ContainsKey(index)) return false;
        return (Time.time - _lastCastTimes[index]) < spells[index].cooldown;
    }
    Vector3 GetHandPosition(XRHand hand) {
        if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose p)) return p.position;
        return Vector3.zero;
    }
    void SetupLineRenderer() {
        _previewLineObj = new GameObject("AimingLine_Visual");
        _lineRenderer = _previewLineObj.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.02f; _lineRenderer.endWidth = 0.01f; _lineRenderer.positionCount = 20;
        _lineRenderer.enabled = false;
        if (arcMaterial != null) _lineRenderer.material = arcMaterial;
        else _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }
    void HidePreview() { if (_lineRenderer != null) _lineRenderer.enabled = false; }
    Vector3 CalculateGroundPoint(Transform cam) {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, maxCastDistance)) return hit.point;
        Vector3 flatFwd = cam.forward; flatFwd.y = 0; flatFwd.Normalize();
        return cam.position + flatFwd * maxCastDistance - (Vector3.up * 1.5f); 
    }
    void DrawParabola(Vector3 start, Vector3 end) {
        Vector3 mid = (start + end) / 2; mid.y += arcHeight; 
        for (int i = 0; i < _lineRenderer.positionCount; i++) {
            float t = (float)i / (_lineRenderer.positionCount - 1);
            _lineRenderer.SetPosition(i, (1 - t) * (1 - t) * start + 2 * (1 - t) * t * mid + t * t * end);
        }
    }
}