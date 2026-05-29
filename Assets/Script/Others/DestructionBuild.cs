using UnityEngine;

public class DestructionBuild : MonoBehaviour, IDamageable
{
    [Header("--- STATS ---")]
    public float maxHealth = 50f;
    private float _currentHealth;
    private bool _isDestroyed = false;

    [Header("--- DESTRUCTION PREFAB ---")]
    [Tooltip("Prefab chứa các mảnh vỡ nhỏ có gắn Rigidbody / Prefab containing small fractured pieces with Rigidbodies")]
    public GameObject fracturedPrefab;

    [Tooltip("Thời gian dọn dẹp các mảnh vỡ sau khi vỡ (giây) / Cleanup time for fractured pieces after breaking (seconds)")]
    public float cleanupTime = Mathf.Infinity;

    [Header("--- PHYSICS JUICE ---")]
    [Tooltip("Lực nổ phụ thêm vào để các mảnh vỡ bung ra đẹp hơn / Extra explosion force to make pieces scatter nicely")]
    public float breakForce = 5f;

    private void Start()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        ApplyDamage(damage, hitPoint);
    }

    private void ApplyDamage(float amount, Vector3 hitPoint)
    {
        if (_isDestroyed) return;

        _currentHealth -= amount;

        // [VI] Hết máu thì vỡ / [EN] Break when out of health
        if (_currentHealth <= 0)
        {
            BreakApart(hitPoint);
        }
    }

    private void BreakApart(Vector3 hitPoint)
    {
        _isDestroyed = true;

        // 1. [VI] Sinh ra Prefab vỡ nát tại đúng vị trí và góc xoay hiện tại
        // 1. [EN] Spawn the fractured Prefab at the exact current position and rotation
        if (fracturedPrefab != null)
        {
            GameObject fracturedObj = Instantiate(fracturedPrefab, transform.position, transform.rotation);

            // 2. [VI] Lấy tất cả Rigidbody của các mảnh vỡ con và tạo lực đẩy văng ra
            // 2. [EN] Get all Rigidbodies of child pieces and apply an outward burst force
            Rigidbody[] rigidbodies = fracturedObj.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb != null)
                {
                    // [VI] Ép lực bung ra từ điểm trúng đòn
                    // [EN] Force an outward burst from the hit point
                    rb.AddExplosionForce(breakForce * 50f, hitPoint, 5f, 1f, ForceMode.Impulse);
                }
            }

            // 3. [VI] Hẹn giờ tự hủy đống rác mảnh vỡ để chống lag game
            // 3. [EN] Set a timer to self-destruct the debris trash to prevent game lag
            Destroy(fracturedObj, cleanupTime);
        }

        // 4. [VI] Xóa bỏ vật thể nguyên vẹn gốc
        // 4. [EN] Destroy the original intact object
        Destroy(gameObject);
    }
}