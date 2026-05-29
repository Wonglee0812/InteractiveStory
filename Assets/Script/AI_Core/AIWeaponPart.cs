using UnityEngine;

public class AIWeaponPart : MonoBehaviour, IDamageable
{
    [Header("--- WEAPON STATS ---")]
    public float maxHealth = 100f;
    private float _currentHealth;
    private bool _isBroken = false;

    [Header("--- AI INTEGRATION ---")]
    public AIHealth mainHealth;
    public AISkillController skillController;
    public AISkillData assignedSkill;

    [Header("--- EFFECTS ---")]
    public float tearOffForce = 15f;
    public GameObject breakVFX;

    private void Start()
    {
        _currentHealth = maxHealth;
        if (skillController != null && assignedSkill != null)
            skillController.AddSkill(assignedSkill);
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

        if (skillController != null && assignedSkill != null)
            skillController.RemoveSkill(assignedSkill);

        // [VI] ĐÃ XÓA LOGIC GỌI STAGGER LÊN MAIN HEALTH
        // [EN] REMOVED STAGGER LOGIC CALL TO MAIN HEALTH

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