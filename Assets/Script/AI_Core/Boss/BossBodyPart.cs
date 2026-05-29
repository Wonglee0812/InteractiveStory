using UnityEngine;

// VI: Vẫn dùng IDamageable để đạn của Player hay mảnh vỡ nhận diện được
// EN: Still uses IDamageable so Player bullets or debris can recognize it
public class BossBodyPart : MonoBehaviour, IDamageable
{
    [Tooltip("VI: Kéo script BossHealth ở object cha (Root) vào đây.\nEN: Drag the BossHealth script from the parent (Root) object here.")]
    public BossHealth mainBossHealth;

    [Tooltip("VI: Hệ số sát thương (Giáp thân = 0.2, Đuôi/Mắt = 1.5).\nEN: Damage multiplier (Body armor = 0.2, Tail/Eyes = 1.5).")]
    public float damageMultiplier = 1.0f;

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        if (mainBossHealth == null) return;

        // VI: Nhân sát thương và truyền về cục gốc
        // EN: Multiply damage and pass it to the root
        float finalDamage = damage * damageMultiplier;
        mainBossHealth.TakeDamage(finalDamage, hitPoint, hitNormal);
    }
}