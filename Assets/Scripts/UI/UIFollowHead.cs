using UnityEngine;

public class UIFollowHead : MonoBehaviour
{
    public Transform cameraToFollow; // Drag CenterEyeAnchor here

    [Header("Position Settings")]
    public float distance = 0.5f;    // How far away (Forward)
    public float heightOffset = -0.2f; // How much lower (Negative = Down)

    [Header("Movement Smoothness")]
    public float smoothSpeed = 5f;

    void Update()
    {
        if (cameraToFollow == null) return;

        // 1. Calculate the base target (In front of face)
        Vector3 targetPosition = cameraToFollow.position + (cameraToFollow.forward * distance);

        // 2. Apply the Height Offset (Move it down)
        // We adjust the Y value directly relative to the world
        targetPosition.y += heightOffset;

        // 3. Calculate rotation
        // We still want it to look at the head, but we calculate the rotation
        // from the NEW lower position so it tilts up slightly to face you.
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - cameraToFollow.position);

        // 4. Smoothly move there
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}