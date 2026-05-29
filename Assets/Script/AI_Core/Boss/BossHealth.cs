using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("--- BOSS STATS ---")]
    public float maxHealth = 3000f;
    public float currentHealth;
    private bool _isDead = false;

    [Header("--- UI HEALTH BAR ---")]
    [Tooltip("VI: Thanh máu chính tụt ngay lập tức (Thường màu Đỏ).\nEN: Main health bar that drops instantly (Usually Red).")]
    public Slider frontHealthBar;

    [Tooltip("VI: Thanh máu ảo từ từ tụt theo (Thường màu Vàng/Trắng).\nEN: Ghost health bar that slowly catches up (Usually Yellow/White).")]
    public Slider backHealthBar;

    [Header("--- GHOST BAR SETTINGS ---")]
    [Tooltip("VI: Thời gian trễ trước khi thanh ảo bắt đầu tụt.\nEN: Delay time before the ghost bar starts dropping.")]
    public float ghostDelay = 0.5f;

    [Tooltip("VI: Tốc độ tụt của thanh ảo.\nEN: Drop speed of the ghost bar.")]
    public float ghostLerpSpeed = 5f;

    private float _ghostTimer = 0f;

    void Awake()
    {
        currentHealth = maxHealth;

        // VI: Thiết lập giá trị ban đầu cho cả 2 thanh máu
        // EN: Set initial values for both health bars
        if (frontHealthBar != null)
        {
            frontHealthBar.maxValue = maxHealth;
            frontHealthBar.value = currentHealth;
        }
        if (backHealthBar != null)
        {
            backHealthBar.maxValue = maxHealth;
            backHealthBar.value = currentHealth;
        }
    }

    void Update()
    {
        if (_isDead || frontHealthBar == null || backHealthBar == null) return;

        // VI: Liên tục đồng bộ thanh máu chính với máu hiện tại
        // EN: Continuously sync main health bar with current health
        frontHealthBar.value = currentHealth;

        // VI: Xử lý hiệu ứng thanh máu ảo tụt dần theo thanh chính
        // EN: Handle the ghost bar effect slowly catching up to the main bar
        if (backHealthBar.value > frontHealthBar.value)
        {
            _ghostTimer += Time.deltaTime;
            if (_ghostTimer > ghostDelay)
            {
                backHealthBar.value = Mathf.Lerp(backHealthBar.value, frontHealthBar.value, ghostLerpSpeed * Time.deltaTime);
            }
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        if (_isDead) return;

        currentHealth -= damage;

        // VI: Reset lại thời gian trễ mỗi khi Boss nhận thêm sát thương mới
        // EN: Reset the delay timer every time the Boss takes new damage
        _ghostTimer = 0f;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (frontHealthBar != null) frontHealthBar.value = 0;
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log("<color=yellow>[BOSS] ĐÃ BỊ TIÊU DIỆT!</color>");

        // VI: Tắt component di chuyển để Boss gục tại chỗ
        // EN: Disable movement component so Boss collapses on the spot
        BossSharkMovement movement = GetComponent<BossSharkMovement>();
        if (movement != null) movement.enabled = false;

        // VI: Có thể gọi Instantiate hiệu ứng cháy nổ bùng nổ ở đây
        // EN: Can Instantiate massive explosion effects here

        Destroy(gameObject, 5f); // Hủy xác sau 5 giây / Destroy corpse after 5 seconds
    }
}