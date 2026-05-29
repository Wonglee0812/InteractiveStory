using UnityEngine;

// Kế thừa Interface IDamageable của hệ thống
public class AIBodyPart : MonoBehaviour, IDamageable
{
    [Header("--- BODY PART STATS ---")]
    [Tooltip("Kéo script AIHealth ở object cha (Root) vào đây")]
    public AIHealth mainHealth;

    [Tooltip("Hệ số sát thương (VD: Bắn vào Đầu x2, bắn vào Đuôi x0.5)")]
    public float damageMultiplier = 1.0f;

    [Tooltip("Bắn trúng chỗ này có làm quái bị Khựng (Light Stagger) không? (VD: Dành cho Headshot)")]
    public bool canCauseStagger = false;

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        if (mainHealth == null) return;

        // Áp dụng hệ số nhân sát thương theo từng bộ phận (Ví dụ: Đầu x2)
        float finalDamage = damage * damageMultiplier;

        // Truyền sát thương thẳng xuống bộ xử lý trung tâm AIHealth
        mainHealth.TakeDamage(finalDamage, hitPoint, hitNormal);
    }
}