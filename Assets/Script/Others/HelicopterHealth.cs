using UnityEngine;
using UnityEngine.Events; // [VI] Bắt buộc cho UnityEvent / [EN] Mandatory for UnityEvent

public class HelicopterHealth : MonoBehaviour, IDamageable
{
    [Header("--- HEALTH & DAMAGE SETTINGS ---")]
    [Tooltip("VI: Tick chọn nếu muốn máy bay có thể bị bắn rớt.\nEN: Check if you want the helicopter to be shootable.")]
    public bool canBeDamaged = false;

    [Tooltip("VI: Lượng máu tối đa của máy bay (Chỉ có tác dụng nếu được tick ở trên).\nEN: Max health of the helicopter (Only active if checked above).")]
    public float maxHealth = 1000f;

    private float _currentHealth;
    private bool _isDestroyed = false;

    [Header("--- DESTRUCTION EFFECTS ---")]
    [Tooltip("VI: Prefab hiệu ứng cháy nổ khi máy bay rớt.\nEN: Explosion effect prefab when helicopter crashes.")]
    public GameObject explosionPrefab;

    [Header("--- ENDING EVENTS ---")]
    [Tooltip("VI: Kéo GameManager vào đây để gọi hàm EndingShootHelicopter() khi máu = 0.\nEN: Drag GameManager here to call EndingShootHelicopter() when health = 0.")]
    public UnityEvent onHelicopterDestroyed;

    private void Start()
    {
        // [VI] Khởi tạo máu đầy lúc bắt đầu
        // [EN] Initialize full health at start
        _currentHealth = maxHealth;
    }

    // [VI] Hàm nhận sát thương bắt buộc từ IDamageable
    // [EN] Mandatory damage receiving function from IDamageable
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        // [VI] Nếu không cho phép nhận sát thương hoặc đã nổ rồi thì bỏ qua hoàn toàn
        // [EN] If taking damage is not allowed or it's already destroyed, ignore completely
        if (!canBeDamaged || _isDestroyed) return;

        _currentHealth -= damage;

        // [VI] Kiểm tra máu để kích hoạt cháy nổ
        // [EN] Check health to trigger explosion
        if (_currentHealth <= 0)
        {
            DestroyHelicopter();
        }
    }

    private void DestroyHelicopter()
    {
        _isDestroyed = true;

        // 1. [VI] Sinh ra hiệu ứng cháy nổ
        // 1. [EN] Spawn the explosion effect
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, transform.rotation);
        }

        // 2. [VI] Gọi sự kiện rẽ nhánh Ending trong GameManager
        // 2. [EN] Call the Ending branching event in GameManager
        if (onHelicopterDestroyed != null)
        {
            onHelicopterDestroyed.Invoke();
        }

        // 3. [VI] Ẩn mô hình máy bay đi (không dùng Destroy để tránh lỗi các script đang chạy ngầm)
        // 3. [EN] Hide the helicopter model (do not use Destroy to avoid breaking background scripts)
        gameObject.SetActive(false);
    }
}