using UnityEngine;
using FluidHTN;

[RequireComponent(typeof(AIAgent))]
public class VisionSensor : MonoBehaviour
{
    [Header("--- EYE SOURCE (AAA) ---")]
    public Transform eyeSource;
    [Space]

    [Header("Vision Settings")]
    public float viewRadius = 30f;
    [Range(0, 360)] public float viewAngle = 90f;
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;

    [Header("--- AWARENESS METER (STEALTH) ---")]
    public float timeToDetect = 1.5f;
    public float timeToLose = 4.0f;
    private float _detectionLevel = 0f;

    private AIAgent _agent;
    private Collider[] _hitColliders = new Collider[8];
    private float _scanInterval = 0.1f; // Tăng tốc độ quét lên 10 lần/s để làm mượt thanh nhận thức
    private float _timer = 0f;

    void Awake()
    {
        _agent = GetComponent<AIAgent>();
        if (eyeSource == null) eyeSource = transform;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _scanInterval)
        {
            _timer = 0f;
            ProcessVisionEscalation();
        }
    }

    private void ProcessVisionEscalation()
    {
        bool isSeeingPlayer = CheckLineOfSight(out Transform playerTarget);

        if (isSeeingPlayer)
        {
            _detectionLevel += _scanInterval / timeToDetect;
            if (_detectionLevel > 1f) _detectionLevel = 1f;

            // MỨC 1 (MẮT VÀNG): Thấy lờ mờ -> Đẩy lên Group xin vé điều tra tập trung
            if (_detectionLevel > 0f && _detectionLevel < 1f && !_agent.Context.HasState(AIWorldState.HasTarget))
            {
                if (_agent.Context.Group != null)
                {
                    _agent.Context.Group.ReportDisturbance(playerTarget.position, _agent);
                }
            }

            // MỨC 2 (MẮT ĐỎ): Xác nhận 100% -> Vào Combat -> Hét gọi bầy
            if (_detectionLevel >= 1f && !_agent.Context.HasState(AIWorldState.HasTarget))
            {
                _agent.Context.SetTarget(playerTarget);

                if (_agent.Context.Group != null)
                {
                    _agent.Context.Group.BroadcastPlayerLocation(playerTarget.position);
                }
            }
        }
        else
        {
            // MẤT DẤU (Cool-down)
            if (_detectionLevel > 0f)
            {
                _detectionLevel -= _scanInterval / timeToLose;
                if (_detectionLevel <= 0f)
                {
                    _detectionLevel = 0f;
                    // Nếu đang trong giao tranh mà thanh nhận diện về 0 -> Kích hoạt Sục sạo
                    if (_agent.Context.HasState(AIWorldState.HasTarget))
                    {
                        // [THÊM MỚI DÒNG NÀY CHÍNH XÁC Ở ĐÂY]: 
                        // Bơm vị trí cuối cùng của Player vào bộ nhớ trước khi xóa Target!
                        if (_agent.Context.Target != null)
                            _agent.Context.SuspectedLocation = _agent.Context.Target.position;

                        _agent.Context.SetTarget(null);
                        _agent.Context.SetState(AIWorldState.IsSearching, true, EffectType.Permanent);
                    }
                }
            }
        }
    }

    private bool CheckLineOfSight(out Transform foundPlayer)
    {
        foundPlayer = null;
        int hits = Physics.OverlapSphereNonAlloc(eyeSource.position, viewRadius, _hitColliders, playerLayerMask);

        for (int i = 0; i < hits; i++)
        {
            Transform target = _hitColliders[i].transform;
            Vector3 dirToTarget = (target.position - eyeSource.position).normalized;

            if (Vector3.Dot(eyeSource.forward, dirToTarget) > Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad))
            {
                Vector3 eyePos = eyeSource.position;
                Vector3 targetPos = target.position + Vector3.up * 1.0f;

                if (!Physics.Linecast(eyePos, targetPos, obstacleLayerMask))
                {
                    PlayerStealth stealth = target.GetComponent<PlayerStealth>();
                    bool isPlayerInGrass = stealth != null && stealth.IsHidingInGrass;

                    if (isPlayerInGrass && _detectionLevel < 1f)
                    {
                        continue;
                    }

                    foundPlayer = target;
                    return true;
                }
            }
        }
        return false;
    }

    public void ForceDetectPlayer(Transform playerTarget)
    {
        if (playerTarget == null) return;

        _detectionLevel = 1f;
        _agent.Context.SetTarget(playerTarget);

        if (_agent.Context.Group != null)
        {
            _agent.Context.Group.BroadcastPlayerLocation(playerTarget.position);
        }
    }

    // [VI] Kiểm tra xem mục tiêu có đang nằm trong tầm mắt không (Dùng cho Combat)
    // [EN] Check if target is currently in line of sight (Used for Combat)
    public bool IsTargetVisible(Transform target)
    {
        if (target == null) return false;

        Vector3 dirToTarget = (target.position - eyeSource.position).normalized;

        // 1. Kiểm tra góc FOV / Check FOV Angle
        if (Vector3.Dot(eyeSource.forward, dirToTarget) < Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad))
            return false;

        // 2. Kiểm tra vật cản (Tường) / Check for obstacles (Walls)
        if (Physics.Linecast(eyeSource.position, target.position + Vector3.up * 1.0f, obstacleLayerMask))
            return false;

        return true;
    }
}