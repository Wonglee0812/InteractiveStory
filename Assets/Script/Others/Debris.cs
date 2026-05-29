using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Debris : MonoBehaviour
{
    [Header("--- DEBRIS DAMAGE SETTINGS ---")]
    [Tooltip("Sát thương cơ bản khi rơi trúng / Base damage when dropping")]
    public float baseDamage = 150f;

    [Tooltip("Vận tốc rơi tối thiểu để gây sát thương / Minimum velocity to deal damage")]
    public float minVelocityThreshold = 5f;

    [Tooltip("Lực hất văng mục tiêu / Knockback force applied to target")]
    public float knockbackForce = 10f;

    [Tooltip("Cộng thêm sát thương dựa trên tốc độ bay? / Add extra damage based on flying speed?")]
    public bool scaleDamageWithVelocity = true;

    // [VI] Biến khóa để 1 mảnh vỡ không trừ máu quái vật 60 lần/giây khi trượt trên lưng nó
    // [EN] Lock variable so 1 debris piece doesn't drain monster health 60 times/sec while sliding on its back
    private bool _hasDealtDamage = false;

    private void OnCollisionEnter(Collision collision)
    {
        // 1. [VI] CHỐNG DOMINO: Nếu chạm vào mảnh vỡ khác -> Bỏ qua ngay lập tức
        // 1. [EN] ANTI-DOMINO: If it hits another piece of debris -> Ignore immediately
        if (collision.gameObject.GetComponent<Debris>() != null) return;

        // 2. [VI] CHỐNG DOMINO 2: Ngăn không cho mảnh vỡ đập vỡ lây sang các cây cột nguyên vẹn khác
        // 2. [EN] ANTI-DOMINO 2: Prevent debris from shattering other intact pillars
        if (collision.gameObject.GetComponent<DestructionBuild>() != null) return;

        // 3. [VI] Khóa sát thương một lần
        // 3. [EN] One-time damage lock
        if (_hasDealtDamage) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minVelocityThreshold) return;

        IDamageable target = collision.gameObject.GetComponent<IDamageable>();
        if (target == null) target = collision.gameObject.GetComponentInParent<IDamageable>();

        if (target != null && target as MonoBehaviour != this)
        {
            float finalDamage = baseDamage;
            if (scaleDamageWithVelocity) finalDamage += impactSpeed * 0.5f;

            Vector3 hitPoint = collision.contacts[0].point;

            target.TakeDamage(finalDamage, hitPoint);
            Debug.Log($"<color=orange>[BẪY MÔI TRƯỜNG]</color> Đá rơi đập trúng {collision.gameObject.name} gây {finalDamage:F1} sát thương!");

            _hasDealtDamage = true;
        }
    }
}