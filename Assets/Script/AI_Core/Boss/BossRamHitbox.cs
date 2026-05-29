using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossRamHitbox : MonoBehaviour
{
    [Header("--- RAM DAMAGE SETTINGS ---")]
    public float ramDamage = 60f;
    public float knockbackForce = 25f;

    [Tooltip("VI: Đảm bảo hitbox chỉ va chạm với Player và Môi trường.\nEN: Ensure hitbox only collides with Player and Environment.")]
    public LayerMask targetMask;

    // VI: Hàm kích hoạt khi Đầu Boss tông vào vật thể
    // EN: Function triggered when Boss Head rams an object
    private void OnTriggerEnter(Collider other)
    {
        // VI: Kiểm tra xem vật thể có nằm trong LayerMask cho phép không
        // EN: Check if the object is within the allowed LayerMask
        if (((1 << other.gameObject.layer) & targetMask) == 0) return;

        // ==========================================
        // 1. PHÁ HỦY TỨC THÌ MÔI TRƯỜNG (INSTANT DESTRUCTION)
        // ==========================================
        DestructionBuild destructibleWall = other.GetComponent<DestructionBuild>();
        if (destructibleWall != null)
        {
            // VI: Ép tường nhận một lượng sát thương khổng lồ để vỡ ngay lập tức thay vì trừ từ từ
            // EN: Force the wall to take massive damage to shatter instantly instead of gradually
            IDamageable wallDamageable = other.GetComponent<IDamageable>();
            if (wallDamageable != null)
            {
                wallDamageable.TakeDamage(99999f, transform.position);
            }
            return; // VI: Đã tông vỡ tường thì kết thúc nhịp xử lý này / EN: Wall shattered, end this processing tick
        }

        // ==========================================
        // 2. GÂY SÁT THƯƠNG CHO PLAYER (DAMAGE PLAYER)
        // ==========================================
        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null) target = other.GetComponentInParent<IDamageable>();

        if (target != null)
        {
            // VI: Boss không tự tự sát thương chính mình
            // EN: Boss does not damage itself
            if (other.GetComponent<AIHealth>() != null || other.GetComponent<AIBodyPart>() != null) return;

            // VI: Tính toán hướng hất văng (Đẩy lùi và hất hếch lên trên một chút)
            // EN: Calculate knockback direction (Push back and slightly upwards)
            Vector3 pushDirection = (other.transform.position - transform.position).normalized;
            pushDirection.y = 0.5f;

            // VI: Truyền sát thương (Tùy thuộc vào hàm TakeDamage của Player có nhận thêm Vector3 knockback hay không)
            // EN: Apply damage (Depending on whether Player's TakeDamage accepts a Vector3 knockback)
            target.TakeDamage(ramDamage, transform.position);

            // VI: Gọi hiệu ứng rung màn hình tại đây nếu có
            // EN: Call screen shake effect here if available
        }
    }
}