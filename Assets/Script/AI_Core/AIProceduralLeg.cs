using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

public class AIProceduralLeg : MonoBehaviour
{
    public bool isLocked = false;

    [Header("--- CORE REFERENCES ---")]
    public Transform homeTransform;
    public NavMeshAgent navAgent;
    public LayerMask groundLayer;

    [Header("--- STEPPING SETTINGS (PREDICTIVE) ---")]
    public float stepDistance = 1.0f;
    public float stepHeight = 0.4f;
    public float stepDurationBase = 0.3f;
    public float overshootMultiplier = 0.5f;

    public bool IsStepping { get; private set; }
    public Vector3 IdealPosition { get; private set; }
    private Vector3 _currentFootprint;

    // [FIX]: Biến quản lý DOTween an toàn
    private Sequence _stepSeq;

    void Start()
    {
        _currentFootprint = transform.position;
        IdealPosition = transform.position;
    }

    void Update()
    {
        if (isLocked)
        {
            if (homeTransform != null)
            {
                _currentFootprint = homeTransform.position + (Vector3.down * 0.5f);
                transform.position = _currentFootprint;
            }
            return;
        }

        if (!IsStepping) transform.position = _currentFootprint;
    }

    public bool WantsToStep()
    {
        if (IsStepping) return false;
        Vector3 velocityOffset = Vector3.zero;
        if (navAgent != null && navAgent.isActiveAndEnabled)
            velocityOffset = navAgent.velocity * overshootMultiplier;

        Vector3 predictedPos = homeTransform.position + velocityOffset;
        if (Physics.Raycast(predictedPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, groundLayer))
            IdealPosition = hit.point;

        return Vector3.Distance(_currentFootprint, IdealPosition) > stepDistance;
    }

    public void Step()
    {
        IsStepping = true;
        float currentSpeed = navAgent != null ? Mathf.Max(navAgent.velocity.magnitude, 1f) : 1f;
        float actualDuration = Mathf.Clamp(stepDurationBase / (currentSpeed * 0.5f), 0.1f, 0.4f);

        _currentFootprint = IdealPosition;

        // [FIX]: Chặt đứt mọi DOTween đang dở dang trước khi bước nhịp mới
        if (_stepSeq != null && _stepSeq.IsActive()) _stepSeq.Kill();

        _stepSeq = DOTween.Sequence();
        _stepSeq.Join(transform.DOMoveY(IdealPosition.y + stepHeight, actualDuration / 2f).SetEase(Ease.OutSine));
        _stepSeq.Append(transform.DOMoveY(IdealPosition.y, actualDuration / 2f).SetEase(Ease.InSine));
        _stepSeq.Insert(0, transform.DOMove(IdealPosition, actualDuration).SetEase(Ease.Linear));

        _stepSeq.OnComplete(() => { IsStepping = false; });
    }

    // Hàm gọi khi đang bay
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (locked)
        {
            if (_stepSeq != null && _stepSeq.IsActive()) _stepSeq.Kill();
            IsStepping = false;
        }
    }

    // Hàm gọi khi vừa hạ cánh
    public void SnapToIdealPosition()
    {
        if (_stepSeq != null && _stepSeq.IsActive()) _stepSeq.Kill();
        IsStepping = false;

        if (Physics.Raycast(homeTransform.position + Vector3.up * 4f, Vector3.down, out RaycastHit hit, 5f, groundLayer))
            IdealPosition = hit.point;
        else
            IdealPosition = homeTransform.position - Vector3.up * 1f;

        _currentFootprint = IdealPosition;
        transform.position = IdealPosition;
    }

    private void OnDrawGizmos()
    {
        if (homeTransform != null && navAgent != null)
        {
            Gizmos.color = Color.red;
            Vector3 predictedPos = homeTransform.position + (navAgent.velocity * overshootMultiplier);
            Gizmos.DrawLine(predictedPos + Vector3.up * 2f, predictedPos + Vector3.down * 3f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(IdealPosition, 0.1f);
        }
    }
}