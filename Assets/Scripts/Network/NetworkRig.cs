using Fusion;
using UnityEngine;

public class NetworkRig : NetworkBehaviour
{
    public Transform visualHead;
    //public Transform visualLeftHand;
    //public Transform visualRightHand;

    // References to the real OVR hardware
    private Transform _localHead;
    private Transform _localLeftHand;
    private Transform _localRightHand;

    public override void Spawned()
    {
        // 1. If I am the owner, I don't need to see my own head.
        //    Hide the visuals for the local player so they don't block the camera.
        if (HasStateAuthority)
        {
            SetVisualsActive(false);
            
            // Find the OVR Rigs in the scene
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                _localHead = rig.centerEyeAnchor;
                _localLeftHand = rig.leftHandAnchor;
                _localRightHand = rig.rightHandAnchor;
            }
        }
        else
        {
            // If this is someone else, SHOW the visuals
            SetVisualsActive(true);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Only the owner updates the network position based on their real hands
        if (HasStateAuthority && _localHead != null)
        {
            // Sync the main root to the head (optional)
            transform.position = _localHead.position;
            transform.rotation = _localHead.rotation;

            // Update visual offsets if your prefab hierarchy allows, 
            // or just rely on NetworkTransform child components if you have them.
            // For simple syncing, we just move the transforms here if they are not NetworkTransforms themselves.
            // if (visualLeftHand) {
            //     visualLeftHand.position = _localLeftHand.position;
            //     visualLeftHand.rotation = _localLeftHand.rotation;
            // }
            // if (visualRightHand) {
            //     visualRightHand.position = _localRightHand.position;
            //     visualRightHand.rotation = _localRightHand.rotation;
            // }
        }
    }

    private void SetVisualsActive(bool isActive)
    {
        if (visualHead) visualHead.gameObject.SetActive(isActive);
        // if (visualLeftHand) visualLeftHand.gameObject.SetActive(isActive);
        // if (visualRightHand) visualRightHand.gameObject.SetActive(isActive);
    }
}