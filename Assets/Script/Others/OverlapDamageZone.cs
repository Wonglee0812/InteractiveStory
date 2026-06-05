using UnityEngine;

public class OverlapDamageZone : MonoBehaviour
{
    [Header("--- DAMAGE SETTINGS ---")]
    [Tooltip("VI: Bán kính vùng quét để đập nát trần nhà.\nEN: Radius of the scan zone to smash the ceiling.")]
    public float crushRadius = 3f;

    [Tooltip("VI: Chọn Layer của trần nhà để tối ưu, không quét trúng bậy bạ.\nEN: Select the ceiling's Layer for optimization, avoiding random hits.")]
    public LayerMask breakableLayer;

    private void Update()
    {
        // VI: Quét một vùng hình cầu xung quanh vị trí này mỗi khung hình
        // EN: Scan a spherical area around this position every frame
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, crushRadius, breakableLayer);

        foreach (Collider col in hitColliders)
        {
            // VI: Tìm component IDamageable trên vật thể trúng phải (chính là trần nhà)
            // EN: Find the IDamageable component on the hit object (which is the ceiling)
            IDamageable damageable = col.GetComponent<IDamageable>();

            if (damageable != null)
            {
                // VI: Truyền lượng sát thương khổng lồ để kích hoạt vỡ ngay lập tức
                // EN: Pass a massive amount of damage to trigger immediate break
                damageable.TakeDamage(999999f, transform.position, Vector3.down);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // VI: Vẽ quả cầu màu đỏ trong Inspector để sếp dễ căn chỉnh độ to nhỏ
        // EN: Draw a red sphere in the Inspector so you can easily adjust the size
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, crushRadius);
    }
}