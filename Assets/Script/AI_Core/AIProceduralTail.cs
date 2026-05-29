using UnityEngine;
using UnityEngine.AI;

public class AIProceduralTail : MonoBehaviour
{
    [Header("--- CORE REFERENCES ---")]
    [Tooltip("Kéo cục NavMeshAgent của con quái vào đây")]
    public NavMeshAgent navAgent;

    [Header("--- HÌNH DÁNG ĐUÔI (TAIL SHAPE) ---")]
    [Tooltip("Ép đuôi cong tự nhiên. (Thử Z âm để kéo giật chóp đuôi lại, Y dương để hếch lên)")]
    public Vector3 restCurveOffset = new Vector3(0f, 0.5f, -0.5f);

    [Header("--- IDLE SWAY (LÚC ĐỨNG YÊN) ---")]
    public float idleSwaySpeed = 1.5f;
    public float idleHorizontalSway = 0.3f;
    public float idleVerticalSway = 0.15f;

    [Header("--- MOVING SWAY (LÚC DI CHUYỂN) ---")]
    public float moveSpeedMultiplier = 0.5f;
    public float moveHorizontalMultiplier = 2.5f;
    public float moveVerticalOffset = 0.4f;

    private Vector3 _startLocalPosition;
    private float _timeCounter = 0f;

    void Start()
    {
        // Lưu lại vị trí gốc ban đầu
        _startLocalPosition = transform.localPosition;
    }

    void Update()
    {
        float currentSpeed = 0f;
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            currentSpeed = navAgent.velocity.magnitude;
        }

        bool isMoving = currentSpeed > 0.1f;

        // 1. VẬN TỐC VẪY
        float actualSwaySpeed = idleSwaySpeed + (isMoving ? (currentSpeed * moveSpeedMultiplier) : 0f);
        _timeCounter += Time.deltaTime * actualSwaySpeed;

        // 2. BIÊN ĐỘ VẪY
        float actualHorizSway = idleHorizontalSway * (isMoving ? moveHorizontalMultiplier : 1f);
        float offsetX = Mathf.Sin(_timeCounter) * actualHorizSway;
        float offsetY = Mathf.Sin(_timeCounter * 2f) * idleVerticalSway;

        if (isMoving) offsetY += moveVerticalOffset;

        // 3. TÍCH HỢP ĐỘ CONG TỰ NHIÊN (REST CURVE OFFSET) CỘNG VỚI SÓNG SIN
        Vector3 targetPos = _startLocalPosition + restCurveOffset + new Vector3(offsetX, offsetY, 0f);

        // 4. LÀM MƯỢT
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * 8f);
    }
}