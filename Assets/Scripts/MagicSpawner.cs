using UnityEngine;

public class MagicSpawner : MonoBehaviour
{
    [Header("Physics Controls")]
    public float shootForce = 20f;
    public float ballLifeTime = 3f;

    [Header("Magic Circle Settings")]
    [Tooltip("Maximum distance the magic circle can be cast.")]
    public float maxCastDistance = 10f;

    [Tooltip("Height of the parabolic arc for the aiming line.")]
    public float arcHeight = 2.0f;

    [Tooltip("Material for the aiming line. If empty, it will default to white.")]
    public Material arcMaterial;

    private LineRenderer currentLineRenderer;
    private GameObject previewLineObj;

    void Awake()
    {
        // Initialize the line renderer object
        previewLineObj = new GameObject("AimingLine");
        currentLineRenderer = previewLineObj.AddComponent<LineRenderer>();

        // Line settings (thinner and clearer)
        currentLineRenderer.startWidth = 0.015f;
        currentLineRenderer.endWidth = 0.005f;

        currentLineRenderer.positionCount = 30;
        currentLineRenderer.enabled = false; // Hidden by default

        // Set Material (Supports transparency)
        if (arcMaterial != null)
        {
            currentLineRenderer.material = arcMaterial;
        }
        else
        {
            currentLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Set Color: White with 50% opacity
        Color semiTransparentColor = new Color(1f, 1f, 1f, 0.5f);
        currentLineRenderer.startColor = semiTransparentColor;
        currentLineRenderer.endColor = semiTransparentColor;
    }

    // 1. Shoot Projectile (Standard physics)
    public void ShootingBall(GameObject prefabToSpawn, Vector3 spawnPosition, Quaternion aimDirection)
    {
        if (prefabToSpawn == null) return;
        GameObject ball = Instantiate(prefabToSpawn, spawnPosition, aimDirection);
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Note: Use rb.velocity in Unity 2022, rb.linearVelocity in Unity 6
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(ball.transform.forward * shootForce, ForceMode.Impulse);
        }
        Destroy(ball, ballLifeTime);
    }

    // 2. Spawn Shield (Forces vertical rotation and ground placement)
    public void SpawnShield(GameObject prefabToSpawn, Transform cameraTransform)
    {
        if (prefabToSpawn == null) return;

        Vector3 spawnPos;
        // Raycast down to find the ground
        if (Physics.Raycast(cameraTransform.position, Vector3.down, out RaycastHit hitInfo, 50f))
            spawnPos = hitInfo.point;
        else
            spawnPos = new Vector3(cameraTransform.position.x, 0f, cameraTransform.position.z);

        // Force Vertical Rotation: Locks X and Z axes to 0
        Quaternion uprightRotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);

        GameObject shield = Instantiate(prefabToSpawn, spawnPos, uprightRotation);
        Destroy(shield, 10f);
    }

    // 3. Update Aiming Preview (Called every frame while aiming)
    public void UpdateAimingPreview(Transform cameraTransform)
    {
        if (currentLineRenderer == null) return;

        currentLineRenderer.enabled = true;

        // Start point: Slightly below camera/chest height
        Vector3 startPoint = cameraTransform.position + Vector3.down * 0.3f + cameraTransform.forward * 0.2f;
        // End point: Calculated ground position
        Vector3 endPoint = CalculateGroundPoint(cameraTransform);

        DrawParabola(startPoint, endPoint);
    }

    // 4. Hide Preview (Called when releasing)
    public void HidePreview()
    {
        if (currentLineRenderer != null)
        {
            currentLineRenderer.enabled = false;
        }
    }

    // 5. Spawn Ground Circle (Logic only, no visual line)
    public void SpawnGroundCircle(GameObject prefabToSpawn, Transform cameraTransform)
    {
        if (prefabToSpawn == null) return;

        Vector3 targetPoint = CalculateGroundPoint(cameraTransform);

        // Spawn flat on the ground
        GameObject circle = Instantiate(prefabToSpawn, targetPoint + Vector3.up * 0.02f, Quaternion.identity);
        Destroy(circle, ballLifeTime);

        HidePreview();
    }

    // Helper: Calculate where the player is looking on the ground
    private Vector3 CalculateGroundPoint(Transform cam)
    {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = new Ray(cam.position, cam.forward);
        float enter;

        if (groundPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            float dist = Vector3.Distance(cam.position, hitPoint);

            // Limit distance
            if (dist > maxCastDistance)
            {
                Vector3 flatDir = cam.forward;
                flatDir.y = 0;
                flatDir.Normalize();
                return new Vector3(cam.position.x, 0, cam.position.z) + flatDir * maxCastDistance;
            }
            return hitPoint;
        }

        // Fallback if looking at the sky
        Vector3 flatForward = cam.forward;
        flatForward.y = 0;
        flatForward.Normalize();
        return new Vector3(cam.position.x, 0, cam.position.z) + flatForward * maxCastDistance;
    }

    // Helper: Draw the Bezier curve
    private void DrawParabola(Vector3 start, Vector3 end)
    {
        Vector3 midPoint = (start + end) / 2;
        midPoint.y += arcHeight;

        for (int i = 0; i < currentLineRenderer.positionCount; i++)
        {
            float t = (float)i / (currentLineRenderer.positionCount - 1);
            Vector3 p = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * midPoint + t * t * end;
            currentLineRenderer.SetPosition(i, p);
        }
    }
}