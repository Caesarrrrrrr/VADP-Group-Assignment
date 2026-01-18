using Fusion;
using UnityEngine;

public class NetworkRig : NetworkBehaviour
{
    public Transform visualHead;
    private Transform _localHead;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            SetVisualsActive(false);
            
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                _localHead = rig.centerEyeAnchor;
            }
        }
        else
        {
            SetVisualsActive(true);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && _localHead != null)
        {
            // Sync the main root to the head (optional)
            transform.position = _localHead.position;
            transform.rotation = _localHead.rotation;


        }
    }

    private void SetVisualsActive(bool isActive)
    {
        if (visualHead) visualHead.gameObject.SetActive(isActive);

    }
}