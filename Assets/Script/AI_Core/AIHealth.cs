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

    private void HandleHitReaction(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_agent != null && _agent.Context != null && !_isDead)
        {
            if (!_agent.Context.HasState(AIWorldState.HasTarget))
            {
                // [VI] TÌM NGAY LẬP TỨC PLAYER VÀ ĐƯA VÀO TRẠNG THÁI CHIẾN ĐẤU (COMBAT)
                // [EN] INSTANTLY FIND PLAYER AND ENTER COMBAT STATE
                Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

                if (playerTransform != null)
                {
                    VisionSensor vs = GetComponent<VisionSensor>();
                    if (vs != null)
                    {
                        vs.ForceDetectPlayer(playerTransform);
                    }
                }
            }
        }
    }

    // [VI] ĐÃ XÓA TriggerLightStagger VÀ TriggerHardStagger
    // [EN] DELETED TriggerLightStagger AND TriggerHardStagger

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        GetComponent<TargetBracket>()?.OnTargetDeath();
        // 1. NGỪNG BẮN SÚNG / STOP SHOOTING
        AIGunController gunCtrl = GetComponent<AIGunController>();
        if (gunCtrl != null) gunCtrl.AbortCombat(FluidHTN.TaskStatus.Failure);

        // 2. ẨN GIAO DIỆN & VẬT LÝ / HIDE UI & PHYSICS
        if (healthBarCanvas != null) healthBarCanvas.gameObject.SetActive(false);
        if (_agent.NavAgent != null) _agent.NavAgent.enabled = false;

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;

        // 3. XỬ LÝ ANIMATION NGÃ CHẾT / HANDLE DEATH ANIMATION
        AIRagdollController ragdoll = GetComponent<AIRagdollController>();
        if (ragdoll != null)
        {
            ragdoll.EnableRagdoll();
        }
        else
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null)
            {
                // [VI] Kích hoạt hoạt ảnh gục ngã thay vì tắt Animator
                // [EN] Trigger the falling animation instead of turning off the Animator
                anim.SetTrigger("isDie");
            }
        }

        // 4. TẮT CÁC SCRIPT ĐỂ TỐI ƯU / DISABLE SCRIPTS FOR OPTIMIZATION
        MonoBehaviour[] allScripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in allScripts)
        {
            // [VI] Bỏ qua AIAnimatorBridge để nó không làm gián đoạn animation chết
            // [EN] Skip AIAnimatorBridge so it doesn't interrupt the death animation
            if (script != this &&
                script.GetType() != typeof(AIRagdollController) &&
                script.GetType() != typeof(AIAnimatorBridge))
            {
                script.enabled = false;
            }
        }

        this.enabled = false;
        Destroy(gameObject, 10f); // Xóa sạch xác sau 10 giây / Clean up corpse after 10 seconds
    }
}