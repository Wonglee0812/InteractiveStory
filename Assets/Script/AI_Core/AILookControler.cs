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
            // VI: Dùng transform.up (Trục Y cục bộ của Mech) để quét radar nghiêng theo sườn dốc thay vì nhìn lên trời
            // EN: Use transform.up (Mech's local Y axis) to scan radar tilted along the slope instead of looking at the sky
            rawTargetPos += transform.right * _currentVisualOffset.x + transform.up * _currentVisualOffset.y;
        }

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

            // Lấy Offset nội bộ, chưa áp dụng ngay để tránh lỗi tọa độ
            _targetVisualOffset = new Vector3(Random.Range(-currentMaxX, currentMaxX), Random.Range(-currentMaxY, currentMaxY), 0f);
        }
        _currentVisualOffset = Vector3.Lerp(_currentVisualOffset, _targetVisualOffset, Time.deltaTime * lookTransitionSpeed);
    }

    private Vector3 GetPointOfInterest()
    {
        // 1. COMBAT: Nhìn thẳng vào ngực/đầu Player (Đã bù Y chuẩn)
        if (_agent.Context.HasState(AIWorldState.HasTarget) && _agent.Context.Target != null)
            return _agent.Context.Target.position + Vector3.up * 1.2f;

        // 2. OVERRIDE: Điểm ép buộc nhìn (Đã được tính toán chuẩn từ trước)
        if (_agent.Context.LookFocusOverride.HasValue)
            return _agent.Context.LookFocusOverride.Value;

        // 3. INVESTIGATE: Vị trí nghi ngờ
        if (_agent.Context.HasState(AIWorldState.HasSuspectedLocation))
            return _agent.Context.SuspectedLocation + Vector3.up * 1.2f;

        // 4. PATROL (ĐANG ĐI): Phóng tầm nhìn thẳng về phía trước theo hướng đi, GIỮ NGUYÊN độ cao của tháp pháo!
        if (_agent.NavAgent.hasPath && _agent.NavAgent.remainingDistance > 0.5f)
        {
            Vector3 dirToSteering = (_agent.NavAgent.steeringTarget - turretBase.position);
            dirToSteering.y = 0; // Chặn đứng việc nhìn xuống đất
            if (dirToSteering != Vector3.zero)
            {
                return turretBase.position + dirToSteering.normalized * virtualLookDistance;
            }
        }

        // 5. PATROL (ĐỨNG YÊN): Nhìn thẳng phía trước thân hình
        return turretBase.position + transform.forward * virtualLookDistance;
    }
}