using UnityEngine;
using UnityEngine.UI;
using FluidHTN;

[RequireComponent(typeof(AIAgent))]
public class AIHealth : MonoBehaviour, IDamageable
{
    [Header("--- HEALTH STATS ---")]
    public float maxHealth = 1000f;
    public float currentHealth;
    private bool _isDead = false;

    [Header("--- ARMOR DEFENSE ---")]
    public float damageReductionPerArmor = 0.15f;
    private int _activeArmorCount = 0;

    [Header("--- UI HEALTH BAR ---")]
    public Transform healthBarCanvas;
    public Slider frontHealthBar;
    public Slider backHealthBar;

    [Header("--- GHOST BAR SETTINGS ---")]
    public float ghostDelay = 0.5f;
    public float ghostLerpSpeed = 5f;
    private float _ghostTimer = 0f;

    private AIAgent _agent;
    private Camera _mainCamera;

    void Awake()
    {
        _agent = GetComponent<AIAgent>();
        currentHealth = maxHealth;
        _mainCamera = Camera.main;
    }

    void Start()
    {
        if (frontHealthBar != null) { frontHealthBar.maxValue = maxHealth; frontHealthBar.value = currentHealth; }
        if (backHealthBar != null) { backHealthBar.maxValue = maxHealth; backHealthBar.value = currentHealth; }
    }

    void Update()
    {
        if (currentHealth <= 0 && !_isDead)
        {
            Die();
            return;
        }

        if (_isDead) return;

        if (frontHealthBar == null || backHealthBar == null) return;
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

    void LateUpdate()
    {
        if (healthBarCanvas != null)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera != null) healthBarCanvas.forward = _mainCamera.transform.forward;
        }
    }

    public void RegisterArmor() { _activeArmorCount++; }
    public void UnregisterArmor() { _activeArmorCount--; if (_activeArmorCount < 0) _activeArmorCount = 0; }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        if (_isDead) return;

        float finalDamage = damage;
        if (_activeArmorCount > 0)
        {
            float reduction = Mathf.Clamp(_activeArmorCount * damageReductionPerArmor, 0f, 0.8f);
            finalDamage *= (1f - reduction);
        }

        currentHealth -= finalDamage;
        _ghostTimer = 0f;

        if (frontHealthBar != null) frontHealthBar.value = currentHealth;

        // [VI] GỌI PHẢN XẠ QUAY ĐẦU / [EN] CALL TURN-AROUND REFLEX
        HandleHitReaction(hitPoint, hitNormal);

        if (currentHealth <= 0) Die();
    }

    public void ApplyTrueDamage(float trueDamage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        if (_isDead) return;

        currentHealth -= trueDamage;
        _ghostTimer = 0f;

        // [VI] GỌI PHẢN XẠ QUAY ĐẦU / [EN] CALL TURN-AROUND REFLEX
        HandleHitReaction(hitPoint, hitNormal);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    // ==========================================
    // [VI] PHẢN XẠ NHẬN DIỆN KẺ ĐỊCH KHI BỊ BẮN LÉN
    // [EN] ENEMY DETECTION REFLEX WHEN SHOT IN BACK
    // ==========================================
    private void HandleHitReaction(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_agent != null && _agent.Context != null && !_isDead)
        {
            // Nếu chưa thấy Player, nó sẽ tính toán vị trí dựa trên hướng đạn dội lại
            if (!_agent.Context.HasState(AIWorldState.HasTarget))
            {
                // Vector hướng đạn tới (hitNormal là hướng nảy ra, nên ta lấy chính hitNormal)
                Vector3 suspectDir = hitNormal != Vector3.zero ? hitNormal : Random.onUnitSphere;
                suspectDir.y = 0; // Đừng ngó lên trời

                // Suy đoán vị trí kẻ địch cách đó 15m
                Vector3 suspectPos = hitPoint + (suspectDir.normalized * 15f);

                _agent.Context.SuspectedLocation = suspectPos;
                _agent.Context.SetState(AIWorldState.HasSuspectedLocation, true, EffectType.PlanAndExecute);

                if (_agent.Context.Group != null)
                    _agent.Context.Group.BroadcastPlayerLocation(suspectPos);
            }
        }
    }

    // [VI] ĐÃ XÓA TriggerLightStagger VÀ TriggerHardStagger
    // [EN] DELETED TriggerLightStagger AND TriggerHardStagger

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        AISkillController skillCtrl = GetComponent<AISkillController>();
        if (skillCtrl != null) skillCtrl.AbortExecution(FluidHTN.TaskStatus.Failure);
        if (healthBarCanvas != null) healthBarCanvas.gameObject.SetActive(false);
        if (_agent.NavAgent != null) _agent.NavAgent.enabled = false;

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;

        AIRagdollController ragdoll = GetComponent<AIRagdollController>();
        if (ragdoll != null) ragdoll.EnableRagdoll();
        else
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.enabled = false;
        }

        MonoBehaviour[] allScripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in allScripts)
        {
            if (script != this && script.GetType() != typeof(AIRagdollController))
            {
                script.enabled = false;
            }
        }

        this.enabled = false;
        Destroy(gameObject, 10f);
    }
}