using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AIMeleeHitbox : MonoBehaviour
{
    public LayerMask targetMask;
    private float _damage = 0f;
    private float _knockback = 0f; // Thêm biến lưu lực hất tạm thời
    private Collider _hitboxCollider;

    void Awake()
    {
        _hitboxCollider = GetComponent<Collider>();
        if (_hitboxCollider != null)
        {
            _hitboxCollider.isTrigger = true;
            _hitboxCollider.enabled = false; // Mặc định luôn tắt
        }
    }

    public void DeactivateHitbox()
    {
        if (_hitboxCollider != null) _hitboxCollider.enabled = false;
    }

    // [SAFE ABORT]: Tự động ngắt Hitbox nếu script bị tắt ngang
    void OnDisable()
    {
        DeactivateHitbox();
    }

    // Cập nhật hàm này để nhận thêm lực hất từ Skill Controller
    public void ActivateHitbox(float skillDamage, float knockback)
    {
        _damage = skillDamage;
        _knockback = knockback;
        if (_hitboxCollider != null) _hitboxCollider.enabled = true;
    }

    // ... (Giữ nguyên DeactivateHitbox và OnDisable)

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetMask) != 0)
        {
            IDamageable target = other.GetComponent<IDamageable>();
            if (target == null) target = other.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                // [VI] Đã xóa tham số _knockback / [EN] Removed _knockback parameter
                target.TakeDamage(_damage, transform.position);
                DeactivateHitbox();
            }
        }
    }
}