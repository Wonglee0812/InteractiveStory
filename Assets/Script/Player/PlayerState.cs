using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerState : MonoBehaviour, IDamageable // [VI] Kế thừa interface để quái có thể gọi / [EN] Inherit interface so monsters can call it
{
    [Header("Health Stats")]
    public float maxHealth = 100f;
    private float _currentHealth;
    public Slider healthSlider;
    public Slider backHealthSlider; // [VI] Thanh máu ảo / [EN] Ghost health bar

    [Header("Protection System (Combo Breaker)")]
    [SerializeField] private int maxHitsBeforeInvincible = 3;
    [SerializeField] private float hitTrackingWindow = 3f; // [VI] Thời gian lưu nhớ hit / [EN] Time window to remember hits
    [SerializeField] private float invulnerabilityDuration = 3f; // [VI] Thời gian bất tử / [EN] Invulnerability time

    private List<float> _recentHitTimestamps = new List<float>();
    public bool IsInvulnerable { get; private set; } = false;

    [Header("Debuff System")]
    [SerializeField] private float slowMultiplier = 0.8f; // [VI] Còn 60% tốc độ / [EN] 60% speed remaining
    private float _slowTimer = 0f;

    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 15f; // [VI] Hồi 15 điểm mỗi giây / [EN] Regen 15 points per second
    public float slideStaminaCost = 25f; // [VI] Tốn 25 điểm cho 1 lần lướt / [EN] Costs 25 points per slide
    public Slider staminaSlider;
    public Slider backStaminaSlider; // [VI] Thanh thể lực ảo / [EN] Ghost stamina bar
    public float currentStamina { get; private set; }

    // [VI] Biến để Player.cs đọc tốc độ / [EN] Variable for Player.cs to read speed
    public float CurrentSpeedMultiplier { get; private set; } = 1f;
    [SerializeField] private PlayerCharater playerCharacter;

    [Header("--- GHOST BAR SETTINGS ---")]
    public float ghostDelay = 0.5f;
    public float ghostLerpSpeed = 5f;
    private float _healthGhostTimer = 0f;
    private float _staminaGhostTimer = 0f;

    // ==========================================
    // [VI] THÊM HỆ THỐNG KNOCKBACK TẠI ĐÂY
    // [EN] ADDED KNOCKBACK SYSTEM HERE
    // ==========================================
    [Header("--- KNOCKBACK SYSTEM ---")]
    [SerializeField] private float knockbackForce = 15f;
    [SerializeField] private float knockbackDuration = 0.15f;

    private void Start()
    {
        _currentHealth = maxHealth;
        currentStamina = maxStamina; // [VI] Đầy thể lực khi bắt đầu / [EN] Full stamina at start
        playerCharacter = GetComponent<PlayerCharater>();

        if (healthSlider != null) { healthSlider.maxValue = maxHealth; healthSlider.value = _currentHealth; }
        if (backHealthSlider != null) { backHealthSlider.maxValue = maxHealth; backHealthSlider.value = _currentHealth; }

        if (staminaSlider != null) { staminaSlider.maxValue = maxStamina; staminaSlider.value = currentStamina; }
        if (backStaminaSlider != null) { backStaminaSlider.maxValue = maxStamina; backStaminaSlider.value = currentStamina; }
    }

    private void Update()
    {
        // [VI] Quản lý thời gian làm chậm / [EN] Manage slow down timer
        if (_slowTimer > 0)
        {
            _slowTimer -= Time.deltaTime;
            CurrentSpeedMultiplier = slowMultiplier; // 60%
        }
        else
        {
            CurrentSpeedMultiplier = 1f; // 100%
        }

        bool isSliding = playerCharacter != null && playerCharacter.CurrentStance == Stance.Slide;

        if (!isSliding && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (staminaSlider != null) staminaSlider.value = currentStamina;
        if (healthSlider != null) healthSlider.value = _currentHealth;

        // [VI] Logic thanh ảo cho MÁU / [EN] Ghost bar logic for HEALTH
        if (backHealthSlider != null && healthSlider != null)
        {
            if (backHealthSlider.value > healthSlider.value)
            {
                _healthGhostTimer += Time.deltaTime;
                if (_healthGhostTimer > ghostDelay)
                {
                    backHealthSlider.value = Mathf.Lerp(backHealthSlider.value, healthSlider.value, ghostLerpSpeed * Time.deltaTime);
                }
            }
            else
            {
                backHealthSlider.value = healthSlider.value; // Hồi máu thì thanh ảo tăng theo ngay
            }
        }

        // [VI] Logic thanh ảo cho THỂ LỰC / [EN] Ghost bar logic for STAMINA
        if (backStaminaSlider != null && staminaSlider != null)
        {
            if (backStaminaSlider.value > staminaSlider.value)
            {
                _staminaGhostTimer += Time.deltaTime;
                if (_staminaGhostTimer > ghostDelay)
                {
                    backStaminaSlider.value = Mathf.Lerp(backStaminaSlider.value, staminaSlider.value, ghostLerpSpeed * Time.deltaTime);
                }
            }
            else
            {
                backStaminaSlider.value = staminaSlider.value; // Hồi thể lực thì thanh ảo tăng theo ngay
            }
        }
    }

    public bool TryConsumeStaminaForSlide()
    {
        if (currentStamina >= slideStaminaCost)
        {
            currentStamina -= slideStaminaCost;
            _staminaGhostTimer = 0f; // [VI] Reset thời gian chờ thanh ảo / [EN] Reset ghost timer
            return true;
        }

        Debug.Log("<color=red>[PLAYER]</color> HẾT THỂ LỰC! Không thể lướt!");
        return false;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        if (IsInvulnerable || _currentHealth <= 0) return;

        _currentHealth -= damage;
        _healthGhostTimer = 0f; // [VI] Reset thời gian chờ thanh ảo / [EN] Reset ghost timer

        if (healthSlider != null) healthSlider.value = _currentHealth;

        // ==========================================
        // [VI] GỌI LOGIC ĐẨY LÙI KHI TRÚNG ĐẠN
        // [EN] CALL KNOCKBACK LOGIC ON HIT
        // ==========================================
        Vector3 pushDirection = hitNormal;
        if (pushDirection == Vector3.zero)
        {
            // Nếu không có hitNormal, tính hướng từ điểm chạm đến Player
            pushDirection = (transform.position - hitPoint).normalized;
        }
        pushDirection.y = 0; // Đừng hất Player văng lên trời
        StartCoroutine(ApplyKnockback(pushDirection.normalized, knockbackForce));

        _recentHitTimestamps.Add(Time.time);

        float currentTime = Time.time;
        _recentHitTimestamps.RemoveAll(timestamp => currentTime - timestamp > hitTrackingWindow);
        if (_recentHitTimestamps.Count >= maxHitsBeforeInvincible)
        {
            StartCoroutine(InvulnerabilityRoutine());
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    // ==========================================
    // [VI] COROUTINE XỬ LÝ LỰC ĐẨY LÙI
    // [EN] COROUTINE HANDLING PUSHBACK FORCE
    // ==========================================
    private IEnumerator ApplyKnockback(Vector3 direction, float force)
    {
        float elapsed = 0f;
        CharacterController cc = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            // Lực giảm dần về 0 theo thời gian (Smooth fade out)
            float currentForce = Mathf.Lerp(force, 0, elapsed / knockbackDuration);
            Vector3 moveVector = direction * currentForce * Time.deltaTime;

            if (cc != null)
                cc.Move(moveVector);
            else if (rb != null && !rb.isKinematic)
                rb.MovePosition(rb.position + moveVector);
            else
                transform.position += moveVector;

            yield return null;
        }
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        IsInvulnerable = true;
        _recentHitTimestamps.Clear();
        yield return new WaitForSeconds(invulnerabilityDuration);
        IsInvulnerable = false;
    }

    private void Die()
    {
        Invoke("RestartLevel", 1.5f);
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}