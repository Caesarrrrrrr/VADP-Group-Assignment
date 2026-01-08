using UnityEngine;

public class WaterMagicGenerator_Ground : MonoBehaviour
{
    [Header("核心设置")]
    [Tooltip("请将制作好的水魔法阵Prefab拖拽到这里")]
    public GameObject waterMagicPrefab;

    [Header("射线检测设置")]
    [Tooltip("射线检测的最大距离（米）")]
    public float maxRayDistance = 10.0f;

    [Tooltip("哪些层级算作地面？非常重要！请确保地面的Layer被选中。")]
    public LayerMask groundLayerMask = -1; // 默认 -1 代表 Everything，建议后续指定为 Ground 层

    [Header("其他")]
    [Tooltip("生成的魔法阵存活时间（秒），0表示不自动销毁")]
    public float lifeTime = 5.0f;

    void Update()
    {
        // MR应用中，Input.GetMouseButtonDown(0) 通常兼容点击屏幕或手柄扳机键
        if (Input.GetMouseButtonDown(0))
        {
            TrySpawnMagicCircleOnGround();
        }
    }

    void TrySpawnMagicCircleOnGround()
    {
        if (waterMagicPrefab == null)
        {
            Debug.LogError("错误：请在 Inspector 面板中赋值 Water Magic Prefab！");
            return;
        }

        // 1. 获取主相机（用户头部）的位置和前方方向
        Transform camTransform = Camera.main.transform;
        Vector3 rayOrigin = camTransform.position;
        Vector3 rayDirection = camTransform.forward;

        RaycastHit hitInfo;

        // 2. 发射射线
        // Physics.Raycast(起点, 方向, 输出碰撞信息, 最大距离, 检测层级)
        if (Physics.Raycast(rayOrigin, rayDirection, out hitInfo, maxRayDistance, groundLayerMask))
        {
            // --- 击中地面了 ---

            // 击中的位置
            Vector3 spawnPosition = hitInfo.point;

            // 击中地面的法线（地面的朝向，比如在斜坡上法线是斜向上的）
            Vector3 surfaceNormal = hitInfo.normal;

            // 3. 计算旋转，让魔法阵平躺在地面上
            // 我们假设你的魔法阵Prefab默认是Y轴向上的。
            // Quaternion.FromToRotation 将 "上(Vector3.up)" 方向旋转对齐到 "地面的法线方向"。
            Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);

            // 4. 生成实例
            GameObject magicInstance = Instantiate(waterMagicPrefab, spawnPosition, spawnRotation);

            // (可选) 稍微向上抬一点点，防止Z-Fighting（即防止魔法阵和地面闪烁重叠）
            magicInstance.transform.position += surfaceNormal * 0.01f;

            Debug.Log($"魔法阵生成于: {spawnPosition}, 击中物体: {hitInfo.collider.name}");

            // 5. 定时销毁
            if (lifeTime > 0)
            {
                Destroy(magicInstance, lifeTime);
            }
        }
        else
        {
            Debug.Log("未查看到地面，无法生成魔法阵。请看向地面。");
        }
    }

    // 在 Scene 视图中绘制射线辅助线，方便调试
    void OnDrawGizmos()
    {
        if (Camera.main == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * maxRayDistance);
    }
}