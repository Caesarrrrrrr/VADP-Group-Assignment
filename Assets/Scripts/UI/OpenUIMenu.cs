using UnityEngine;
using UnityEngine.XR.Hands;
using TMPro;
using System.Collections;

public class OpenUIMenu : MonoBehaviour
{
    [Header("1. Drag Components Here")]
    [Tooltip("Drag the component that has 'DetectGestures' attached to it (XRHandTrackingEvents)")]
    [SerializeField] private XRHandTrackingEvents handEvents;

    [Tooltip("Drag your 'CenterEyeAnchor' from the OVR Camera Rig here")]
    [SerializeField] private Transform headCamera;

    [Header("2. Menu Settings")]
    [SerializeField] private GameObject menuCanvas;

    [SerializeField] private float pinchDistance = 0.04f;
    [SerializeField] private float unpinchDistance = 0.06f;

    [Header("3. Positioning")]
    [SerializeField] private Vector3 offsetFromHead = new Vector3(0, 0.1f, 0.6f);

    private bool isPinching = false;
    private float cooldown = 0.0f;

    void Start()
    {
        // 1. Force the menu to be Active (Visible) at start
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);

            // 2. Position it immediately in front of the camera
            StartCoroutine(DelayedPositionMenu());
        }
    }

    void OnEnable()
    {
        if (handEvents != null) handEvents.jointsUpdated.AddListener(OnHandUpdate);
    }

    void OnDisable()
    {
        if (handEvents != null) handEvents.jointsUpdated.RemoveListener(OnHandUpdate);
    }

    private void OnHandUpdate(XRHandJointsUpdatedEventArgs args)
    {
        if (args.hand.handedness != Handedness.Left) return;

        var thumb = args.hand.GetJoint(XRHandJointID.ThumbTip);
        var index = args.hand.GetJoint(XRHandJointID.IndexTip);

        if (thumb.TryGetPose(out Pose thumbPose) && index.TryGetPose(out Pose indexPose))
        {
            float dist = Vector3.Distance(thumbPose.position, indexPose.position);

            // RISING EDGE: Pinch just started
            if (dist < pinchDistance && !isPinching)
            {
                if (Time.time > cooldown)
                {
                    isPinching = true;
                    ToggleMenu();
                    cooldown = Time.time + 0.5f;
                }
            }
            // FALLING EDGE: Pinch released
            else if (dist > unpinchDistance)
            {
                isPinching = false;
            }
        }
    }

    void ToggleMenu()
    {
        if (menuCanvas == null) return;

        bool newState = !menuCanvas.activeSelf;
        menuCanvas.SetActive(newState);

        // If we just turned it ON, move it to the correct position
        if (newState)
        {
            PositionMenu();
        }
    }

    private IEnumerator DelayedPositionMenu()
    {
        // Wait for end of frame to ensure all transforms are updated
        yield return new WaitForSeconds(1f);
        PositionMenu();
    }

    // New helper function to handle positioning
    void PositionMenu()
    {
        if (headCamera != null && menuCanvas != null)
        {
            // Calculate position
            Vector3 targetPos = headCamera.position;
            targetPos += headCamera.forward * offsetFromHead.z; // Move forward
            targetPos.y = headCamera.position.y + offsetFromHead.y; // Keep height relative to head

            menuCanvas.transform.position = targetPos;

            // Make it look at the player
            menuCanvas.transform.LookAt(headCamera);
            menuCanvas.transform.Rotate(0, 180, 0); // Corrects the "Backwards" text issue
        }
    }
}