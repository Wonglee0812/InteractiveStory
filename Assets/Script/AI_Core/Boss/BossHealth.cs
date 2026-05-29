using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("--- BOSS STATS ---")]
    public float maxHealth = 3000f;
    public float currentHealth;
    private bool _isDead = false;

    [Header("--- UI HEALTH BAR ---")]
    public Slider frontHealthBar;
    public Slider backHealthBar;

    [Header("--- GHOST BAR SETTINGS ---")]
    public float ghostDelay = 0.5f;
    public float ghostLerpSpeed = 5f;
    private float _ghostTimer = 0f;

    void Awake()
    {
        currentHealth = maxHealth;
        if (frontHealthBar != null) { frontHealthBar.maxValue = maxHealth; frontHealthBar.value = currentHealth; }
        if (backHealthBar != null) { backHealthBar.maxValue = maxHealth; backHealthBar.value = currentHealth; }
    }

    void Update()
    {
        if (_isDead || frontHealthBar == null || backHealthBar == null) return;
        frontHealthBar.value = currentHealth;

        if (backHealthBar.value > frontHealthBar.value)
        {
            _ghostTimer += Time.deltaTime;
            if (_ghostTimer > ghostDelay)
            {
                backHealthBar.value = Mathf.Lerp(backHealthBar.value, frontHealthBar.value, ghostLerpSpeed * Time.deltaTime);
            }
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        if (_isDead) return;
        currentHealth -= damage;
        _ghostTimer = 0f;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (frontHealthBar != null) frontHealthBar.value = 0;
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log("<color=yellow>[BOSS] ĐÃ BỊ TIÊU DIỆT!</color>");

        BossSplineAttacker splineMovement = GetComponentInParent<BossSplineAttacker>();
        if (splineMovement != null)
        {
            splineMovement.StopAllCoroutines();
            splineMovement.enabled = false;
            if (splineMovement.splineCart != null) splineMovement.splineCart.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (frontHealthBar != null && frontHealthBar.transform.parent != null)
            frontHealthBar.transform.parent.gameObject.SetActive(false);

        GameObject objectToDestroy = splineMovement != null ? splineMovement.gameObject : gameObject;
        Destroy(objectToDestroy, 5f);
    }
}