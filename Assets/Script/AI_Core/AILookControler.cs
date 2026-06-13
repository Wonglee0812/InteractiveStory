using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(AIAgent))]
[DefaultExecutionOrder(100)]
public class AILookController : MonoBehaviour
{
    [Header("--- TURRET RIGGING (CƠ KHÍ) ---")]
    public Transform turretAimTarget;
    public Transform turretBase;

    public float lookSmoothTime = 0.15f;
    public float virtualLookDistance = 10f;

    [Tooltip("VI: Góc ngắm ngóc lên tối đa (Độ).\nEN: Maximum elevation angle (Degrees).")]
    public float maxLookUpAngle = 45f;

    [Tooltip("VI: Góc ngắm hạ xuống tối đa (Độ). NHỚ ĐỂ SỐ ÂM!\nEN: Maximum depression angle (Degrees). KEEP AS NEGATIVE!")]
    public float maxLookDownAngle = -25f;

    [Header("--- RADAR SCANNING (QUÉT TÌM KIẾM) ---")]
    public float lookHoldTime = 2.0f;
    public float lookTransitionSpeed = 4f;
    public float scanHorizontalRange = 5.0f;
    public float scanVerticalRange = 0.5f;

    private AIAgent _agent;
    private Vector3 _smoothedTargetPos;
    private Vector3 _targetPosVel;

    private float _lookTimer;
    private Vector3 _targetVisualOffset;
    private Vector3 _currentVisualOffset;

    private void Awake()
    {
        _agent = GetComponent<AIAgent>();

        if (turretBase != null)
            _smoothedTargetPos = turretBase.position + transform.forward * virtualLookDistance;
    }

    private void LateUpdate()
    {
        if (turretBase == null || _agent == null || turretAimTarget == null) return;

        UpdateStaggeredLookAround();
        Vector3 rawTargetPos = GetPointOfInterest();

        bool isEngaged = _agent.Context.HasState(AIWorldState.HasTarget) || _agent.Context.LookFocusOverride.HasValue;
        if (!isEngaged)
        {
            rawTargetPos += transform.right * _currentVisualOffset.x + transform.up * _currentVisualOffset.y;
        }

        // ==========================================
        // [VI] ĐÃ FIX: KHÓA CHẾT TRỤC Y (CHỈ CHO XOAY YAW)
        // [EN] FIXED: HARD LOCK Y AXIS (ONLY ALLOW YAW)
        // ==========================================
        Vector3 dirToTarget = rawTargetPos - turretBase.position;

        // San phẳng hướng nhìn (Loại bỏ hoàn toàn góc ngóc lên / chúi xuống)
        // Flatten the look direction (Completely remove pitch up / pitch down)
        dirToTarget.y = 0f;

        // Tái tạo lại vị trí mục tiêu mới với độ cao bằng đúng gốc cổ
        // Reconstruct the new target position with the exact height of the neck base
        rawTargetPos = turretBase.position + dirToTarget;
        // ==========================================

        _smoothedTargetPos = Vector3.SmoothDamp(_smoothedTargetPos, rawTargetPos, ref _targetPosVel, lookSmoothTime);
        turretAimTarget.position = _smoothedTargetPos;
    }

    private void UpdateStaggeredLookAround()
    {
        bool hasTarget = _agent.Context.HasState(AIWorldState.HasTarget) && _agent.Context.Target != null;
        if (hasTarget || _agent.Context.LookFocusOverride.HasValue)
        {
            _targetVisualOffset = Vector3.zero;
            _currentVisualOffset = Vector3.MoveTowards(_currentVisualOffset, Vector3.zero, Time.deltaTime * lookTransitionSpeed * 2f);
            return;
        }

        if (Time.time >= _lookTimer)
        {
            _lookTimer = Time.time + lookHoldTime + Random.Range(-0.5f, 0.5f);

            bool hasSuspicion = _agent.Context.HasState(AIWorldState.HasSuspectedLocation);
            bool isScanning = _agent.Context.IsScanning;

            float currentMaxX = (isScanning || hasSuspicion) ? scanHorizontalRange : (scanHorizontalRange * 0.3f);
            float currentMaxY = (isScanning || hasSuspicion) ? scanVerticalRange : 0.1f;

            _targetVisualOffset = new Vector3(Random.Range(-currentMaxX, currentMaxX), Random.Range(-currentMaxY, currentMaxY), 0f);
        }
        _currentVisualOffset = Vector3.Lerp(_currentVisualOffset, _targetVisualOffset, Time.deltaTime * lookTransitionSpeed);
    }

    private Vector3 GetPointOfInterest()
    {
        if (_agent.Context.HasState(AIWorldState.HasTarget) && _agent.Context.Target != null)
            return _agent.Context.Target.position + Vector3.up * 1.2f;

        if (_agent.Context.LookFocusOverride.HasValue)
            return _agent.Context.LookFocusOverride.Value;

        if (_agent.Context.HasState(AIWorldState.HasSuspectedLocation))
            return _agent.Context.SuspectedLocation + Vector3.up * 1.2f;

        if (_agent.NavAgent.hasPath && _agent.NavAgent.remainingDistance > 0.5f)
        {
            Vector3 dirToSteering = (_agent.NavAgent.steeringTarget - turretBase.position);
            dirToSteering.y = 0;
            if (dirToSteering != Vector3.zero)
            {
                return turretBase.position + dirToSteering.normalized * virtualLookDistance;
            }
        }

        return turretBase.position + transform.forward * virtualLookDistance;
    }
}