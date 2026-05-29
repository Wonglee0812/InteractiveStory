using UnityEngine;

public class BossSharkMovement : MonoBehaviour
{
    [Header("--- TARGETING (MỤC TIÊU) ---")]
    [Tooltip("VI: Kéo Player vào đây để Boss rượt theo.\nEN: Drag the Player here for the Boss to chase.")]
    public Transform playerTarget;

    [Tooltip("VI: Tốc độ bẻ lái. Càng thấp bẻ lái càng nặng nề.\nEN: Steering speed. Lower means heavier steering.")]
    public float turnSpeed = 2.5f;

    [Header("--- MOVEMENT SETTINGS ---")]
    public float baseSpeed = 15f;
    public float enragedSpeedMultiplier = 1.8f;

    [Header("--- HEALTH REFS ---")]
    public BossHealth bossHealth;
    public float enrageThreshold = 0.3f; // 30% máu / 30% health

    private bool _isEnraged = false;

    void Update()
    {
        CheckEnrageStatus();
        MoveAndChase();
    }

    private void CheckEnrageStatus()
    {
        if (_isEnraged || bossHealth == null) return;

        float healthPercent = bossHealth.currentHealth / bossHealth.maxHealth;
        if (healthPercent <= enrageThreshold)
        {
            _isEnraged = true;
            turnSpeed *= 1.5f; // VI: Cuồng nộ thì bẻ lái gắt hơn / EN: Enraged means sharper steering
            Debug.Log("<color=red>[BOSS] BƯỚC VÀO TRẠNG THÁI CUỒNG NỘ! TĂNG TỐC & BẺ LÁI GẮT!</color>");
        }
    }

    private void MoveAndChase()
    {
        if (playerTarget == null) return;

        // ==========================================
        // 1. TÍNH TOÁN HƯỚNG BẺ LÁI (STEERING CALCULATION)
        // ==========================================
        // VI: Tìm hướng về phía Player nhưng bỏ qua trục Y để Boss không ngóc đầu lên trời
        // EN: Find direction to Player but ignore Y axis so Boss doesn't pitch up to the sky
        Vector3 dirToPlayer = (playerTarget.position - transform.position).normalized;
        dirToPlayer.y = 0;

        if (dirToPlayer != Vector3.zero)
        {
            // VI: Xoay thân từ từ về hướng Player (Tạo cảm giác thân hình nặng nề)
            // EN: Slowly rotate body towards Player (Creates a heavy body feel)
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // ==========================================
        // 2. LAO LÊN PHÍA TRƯỚC (FORWARD PROPULSION)
        // ==========================================
        float currentSpeed = _isEnraged ? (baseSpeed * enragedSpeedMultiplier) : baseSpeed;
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }
}