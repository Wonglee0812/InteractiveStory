using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
public class AIAnimatorBridge : MonoBehaviour
{
    private Animator _animator;
    private NavMeshAgent _navAgent;

    [Header("--- ANIMATION SETTINGS ---")]
    public float animSmoothTime = 0.1f;
    public float maxSpeed = 8f;

    private float _animVelocityX;
    private float _animVelocityZ;
    private float _currentVelocityXRef;
    private float _currentVelocityZRef;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (_animator != null)
        {
            // [VI] 1. Cập nhật tên biến rút súng (Hãy sửa "isWeaponDrawn" cho khớp với chữ bị khuất trong máy bạn)
            // [EN] 1. Update weapon drawn variable (Modify "isWeaponDrawn" to match the cut-off text on your machine)
            _animator.SetBool("isWeaponDrawn", true);

            if (_animator.layerCount > 1)
            {
                _animator.SetLayerWeight(1, 1f);
            }
        }
    }

    void Update()
    {
        UpdateMovementAnimations();
    }

    private void UpdateMovementAnimations()
    {
        if (_animator == null || _navAgent == null) return;

        Vector3 localVelocity = transform.InverseTransformDirection(_navAgent.velocity);
        float targetX = localVelocity.x / maxSpeed;
        float targetZ = localVelocity.z / maxSpeed;

        _animVelocityX = Mathf.SmoothDamp(_animVelocityX, targetX, ref _currentVelocityXRef, animSmoothTime);
        _animVelocityZ = Mathf.SmoothDamp(_animVelocityZ, targetZ, ref _currentVelocityZRef, animSmoothTime);

        _animator.SetFloat("VelocityX", _animVelocityX);
        _animator.SetFloat("VelocityZ", _animVelocityZ);
        _animator.SetBool("IsMoving", _navAgent.velocity.sqrMagnitude > 0.1f);
    }

    // ==========================================
    // CỔNG GIAO TIẾP VỚI SÚNG ĐẠN / COMBAT COMMUNICATION
    // ==========================================
    public void PlayShootAnimation()
    {
        if (_animator != null)
        {
            // [VI] 2. Trong ảnh của bạn, isShooting có ô tick (kiểu Bool). Ta cần bật nó lên và tự tắt đi cực nhanh.
            // [EN] 2. In your image, isShooting has a checkbox (Bool type). We need to turn it on and auto-turn it off very fast.
            _animator.SetBool("isShooting", true);
            Invoke(nameof(ResetShootAnimation), 0.1f);
        }
    }

    private void ResetShootAnimation()
    {
        if (_animator != null) _animator.SetBool("isShooting", false);
    }

    public void SetAiming(bool isAiming)
    {
        // [VI] 3. Đã đổi tên biến từ "AimHeld" thành "isAiming" chuẩn xác
        // [EN] 3. Accurately renamed variable from "AimHeld" to "isAiming"
        if (_animator != null) _animator.SetBool("isAiming", isAiming);
    }
}