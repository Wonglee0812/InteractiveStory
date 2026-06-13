using UnityEngine;

public class AIWeaponPart : MonoBehaviour, IDamageable
{
    [Header("--- WEAPON STATS ---")]
    public float maxHealth = 100f;
    private float _currentHealth;
    private bool _isBroken = false;

    [Header("--- AI INTEGRATION ---")]
    public AIHealth mainHealth;
    // [VI] ĐÃ XÓA 2 biến skillController và assignedSkill
    // [EN] DELETED the 2 variables skillController and assignedSkill

    [Header("--- EFFECTS ---")]
    public float tearOffForce = 15f;
    public GameObject breakVFX;

    private void Start()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        ApplyDamage(damage, hitPoint, hitNormal);
    }

    private void ApplyDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_isBroken) return;
        _currentHealth -= damage;
        if (_currentHealth <= 0) BreakWeapon(hitNormal);
    }

    private void BreakWeapon(Vector3 hitNormal)
    {
        _isBroken = true;
        if (breakVFX != null) Instantiate(breakVFX, transform.position, Quaternion.identity);

        // [VI] Báo cho bộ não AI biết là súng đã gãy, ngừng bắn ngay lập tức
        // [EN] Tell the AI brain that the gun is broken, stop shooting immediately
        AIGunController gunCtrl = GetComponentInParent<AIGunController>();
        if (gunCtrl != null) gunCtrl.AbortCombat(FluidHTN.TaskStatus.Failure);

        transform.SetParent(null);
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.mass = 100f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Vector3 forceDir = (hitNormal + Vector3.up * 0.5f).normalized;
        rb.AddForce(forceDir * tearOffForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * tearOffForce, ForceMode.Impulse);

        Destroy(rb.gameObject, 10f);
        gameObject.layer = LayerMask.NameToLayer("Default");
        this.enabled = false;
    }
}