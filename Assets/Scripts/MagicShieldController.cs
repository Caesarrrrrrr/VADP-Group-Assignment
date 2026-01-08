using UnityEngine;

public class MagicShieldController : MonoBehaviour
{
    [Header("核心设置 (Core Settings)")]
    [Tooltip("请将魔法护盾 Prefab 拖拽到这里")]
    public GameObject shieldPrefab;

    [Tooltip("跟随的目标，通常是 Main Camera (玩家头部)")]
    public Transform playerHead;

    [Tooltip("是否按住才显示？(勾选=按住显示/松开消失; 不勾选=点击切换开关)")]
    public bool holdToActivate = true;

    [Header("地面检测设置 (Ground Detection)")]
    [Tooltip("如果不使用射线检测，默认认为地面在头顶下方多少米？(通常设为 1.6 或 1.7)")]
    public float defaultHeight = 1.7f;

    [Tooltip("射线检测的层级：请取消勾选 'Player' 和 'TransparentFX'，只勾选 'Default' 或 'Ground'")]
    public LayerMask groundLayer = -1; // 默认检测所有层

    private GameObject currentShield;

    void Start()
    {
        // 自动获取主相机
        if (playerHead == null)
        {
            playerHead = Camera.main.transform;
        }
    }

    void Update()
    {
        if (playerHead == null) return;

        // --- 1. 输入控制 ---
        HandleInput();

        // --- 2. 位置跟随 ---
        if (currentShield != null)
        {
            UpdateShieldPosition();
        }
    }

    void HandleInput()
    {
        // 这里的 (0) 对应鼠标左键，也兼容 VR 手柄的 Trigger 键（视具体的 Input System 而定）
        if (holdToActivate)
        {
            // 按住模式
            if (Input.GetMouseButtonDown(0)) ActivateShield();
            else if (Input.GetMouseButtonUp(0)) DeactivateShield();
        }
        else
        {
            // 开关模式
            if (Input.GetMouseButtonDown(0))
            {
                if (currentShield == null) ActivateShield();
                else DeactivateShield();
            }
        }
    }

    void ActivateShield()
    {
        if (currentShield != null) return;

        if (shieldPrefab != null)
        {
            // 生成护盾
            currentShield = Instantiate(shieldPrefab, GetGroundPosition(), Quaternion.identity);
        }
    }

    void DeactivateShield()
    {
        if (currentShield != null)
        {
            Destroy(currentShield);
            currentShield = null;
        }
    }

    void UpdateShieldPosition()
    {
        // 每一帧都更新位置，让护盾像影子一样跟随玩家
        currentShield.transform.position = GetGroundPosition();

        // (可选) 如果你希望护盾始终正面朝向玩家前方，取消下面这行的注释：
        // currentShield.transform.rotation = Quaternion.Euler(0, playerHead.eulerAngles.y, 0);
    }

    // --- 核心修改逻辑 ---
    Vector3 GetGroundPosition()
    {
        RaycastHit hit;

        // 从头顶位置 (playerHead) 向下 (Vector3.down) 发射射线
        // maxDistance 设为 3.0米 (足够覆盖人身高)
        if (Physics.Raycast(playerHead.position, Vector3.down, out hit, 3.0f, groundLayer))
        {
            // 情况 A: 找到了真实的地面 (Mesh / 地板)
            // 返回击中点，并稍微向上抬 1cm (0.01f) 防止和地板重叠闪烁
            return hit.point + Vector3.up * 0.01f;
        }
        else
        {
            // 情况 B: 脚下是空的 (比如没有扫描环境，或者站在高处)
            // 强制假设地面在头顶下方 defaultHeight (1.7米) 处
            return new Vector3(playerHead.position.x, playerHead.position.y - defaultHeight, playerHead.position.z);
        }
    }
}