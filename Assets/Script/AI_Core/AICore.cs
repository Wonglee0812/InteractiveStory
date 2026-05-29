using UnityEngine;

public class AICore : MonoBehaviour, IDamageable
{
    [Header("--- CORE STATS ---")]
    public float maxHealth = 100f;
    private float _currentHealth;
    private bool _isDestroyed = false;

    [Header("--- EXPLOSION MECHANICS ---")]
    public float explosionRadius = 15f;
    public float bossExplosionDamage = 300f; // Boss ăn trọn 300 True Damage
    public float armorExplosionDamage = 500f; // Bóc sạch giáp lân cận
    public GameObject explosionVfx;
    public LayerMask targetMask;

    private void Start()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        // Gọi hàm xử lý vụ nổ nội bộ của linh kiện
        ApplyDamage(damage, hitPoint);
    }

    private void ApplyDamage(float amount, Vector3 hitPoint)
    {
        if (_isDestroyed) return;

        // TUYỆT ĐỐI KHÔNG TRUYỀN SÁT THƯƠNG QUA BOSS KHI CHƯA NỔ
        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            Explode(hitPoint);
        }
    }

    private void Explode(Vector3 hitPoint)
    {
        _isDestroyed = true;

        if (explosionVfx != null)
        {
            GameObject vfx = Instantiate(explosionVfx, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetMask);
        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null) target = hit.GetComponentInParent<IDamageable>();

            if (target != null && (target as MonoBehaviour != this))
            {
                if (target is AIHealth bossHealth)
                {
                    // LÕI NỔ -> TRỪ MÁU KHỦNG & ÉP GỤC NGÃ (HARD STAGGER)
                    bossHealth.ApplyTrueDamage(bossExplosionDamage, transform.position);
                }
                else if (target is AIArmor)
                {
                    // [VI] Đã xóa số 10f / [EN] Removed 10f
                    target.TakeDamage(armorExplosionDamage, hit.transform.position);
                }
            }
        }
        gameObject.SetActive(false);
    }
}