using System.Collections;
using UnityEngine;

public class AIArmor : MonoBehaviour, IDamageable
{
    [Header("--- ARMOR STATS ---")]
    public float maxHealth = 150f;
    private float _currentHealth;
    private bool _isBroken = false;

    [Header("--- AI INTEGRATION ---")]
    public AIHealth mainHealth;
    [Tooltip("Bắn vào giáp, Boss mất bao nhiêu % máu? (0.1 = 10%)")]
    public float damageTransfer = 0.1f;

    [Header("--- VISUALS & EFFECTS ---")]
    public float tearOffForce = 15f;
    public GameObject breakVFX;
    public GameObject armorHitImpactPrefab;

    private void Start()
    {
        _currentHealth = maxHealth;
        if (mainHealth != null) mainHealth.RegisterArmor(); // Báo cho Boss biết có thêm 1 giáp
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        if (armorHitImpactPrefab != null)
        {
            Vector3 spawnNormal = hitNormal != Vector3.zero ? hitNormal : Vector3.up;
            Instantiate(armorHitImpactPrefab, hitPoint, Quaternion.LookRotation(spawnNormal));
        }
        // Chuyển tiếp sát thương vào hàm xử lý cục bộ
        ApplyLocalDamage(damage, hitPoint, hitNormal);
    }

    private void ApplyLocalDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_isBroken) return;

        _currentHealth -= damage;

        // Truyền True Damage thẳng vào Boss (bỏ qua giảm trừ)
        if (mainHealth != null && damageTransfer > 0)
        {
            mainHealth.ApplyTrueDamage(damage * damageTransfer, hitPoint);
        }

        if (_currentHealth <= 0)
        {
            BreakArmor(hitNormal);
        }
    }

    private void BreakArmor(Vector3 hitNormal)
    {
        _isBroken = true;

        if (breakVFX != null) Destroy(Instantiate(breakVFX, transform.position, Quaternion.identity), 0.1f);

        if (mainHealth != null)
        {
            mainHealth.UnregisterArmor(); // Trừ số lượng giáp đi
        }

        transform.SetParent(null);
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.mass = 150f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Vector3 forceDir = (hitNormal + Vector3.up * 0.5f).normalized;
        rb.AddForce(forceDir * tearOffForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * tearOffForce, ForceMode.Impulse);

        Destroy(rb.gameObject, 10f);
        gameObject.layer = LayerMask.NameToLayer("Default");
        this.enabled = false;
    }
}