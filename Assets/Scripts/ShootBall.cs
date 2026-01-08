using UnityEngine;

public class ShootBall : MonoBehaviour
{
    [Header("Physics Controls")]
    [Tooltip("球的飞行速度 (Force)")]
    public float shootForce = 20f;

    [Tooltip("球存活时间")]
    public float ballLifeTime = 3f;

    [Header("Gravity Controls")]
    [Tooltip("下坠力度：0 = 直线飞, 9.8 = 正常重力, 2.0 = 轻微下坠")]
    public float customGravity = 1.5f; // 默认给一个小数值，实现“轻微”下坠

    public void ShootingBall(GameObject prefabToSpawn, Vector3 spawnPosition, Quaternion aimDirection)
    {
        // 1. 生成球体
        GameObject ball = Instantiate(prefabToSpawn, spawnPosition, aimDirection);

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Unity 6 新语法 (旧版本请用 rb.velocity)
            rb.linearVelocity = Vector3.zero;

            // A. 禁用 Unity 自带的重力（防止掉得太快）
            rb.useGravity = false;

            // B. 给球施加向前的冲击力
            rb.AddForce(ball.transform.forward * shootForce, ForceMode.Impulse);

            // C. 添加自定义的“轻微重力”
            // ConstantForce 组件会每帧自动给 Rigidbody 施加力，非常适合模拟自定义重力
            ConstantForce antiGrav = ball.AddComponent<ConstantForce>();
            antiGrav.force = new Vector3(0, -customGravity, 0);
        }

        Destroy(ball, ballLifeTime);
    }
}