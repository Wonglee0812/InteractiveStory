using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIProceduralBody : MonoBehaviour
{
    [System.Serializable]
    public class ProceduralLeg
    {
        [Tooltip("VI: Điểm neo IK dưới bàn chân (Target của Chain IK Constraint).\nEN: IK anchor under the foot (Target of Chain IK Constraint).")]
        public Transform tipTarget;

        [Tooltip("VI: Điểm mốc lơ lửng gắn chết với thân mech để xác định vị trí gốc của chân.\nEN: Floating reference point parented to the mech body to determine the leg's home position.")]
        public Transform homeTransform;

        [HideInInspector] public Vector3 currentFootprint;
        [HideInInspector] public Vector3 idealPosition;
        [HideInInspector] public bool isStepping;
        [HideInInspector] public float stepTimer;
        [HideInInspector] public Vector3 stepStartPos;
    }

    [Header("--- LEGS CONFIGURATION ---")]
    [Tooltip("VI: Mảng chứa toàn bộ chân. Nên xếp chéo góc (0=TrướcTrái, 1=TrướcPhải, 2=SauPhải, 3=SauTrái) để nhện bước mượt nhất.\nEN: Array of all legs. Should be ordered diagonally (0=FrontLeft, 1=FrontRight, 2=BackRight, 3=BackLeft) for smoothest gait.")]
    public ProceduralLeg[] legs;

    [Header("--- GLOBAL STEP SETTINGS (PURE MATH) ---")]
    [Tooltip("VI: Khoảng cách chân bị kéo giãn tối đa trước khi buộc phải nhấc lên bước tiếp.\nEN: Maximum distance the leg stretches before it is forced to lift and take a step.")]
    public float stepDistance = 0.8f;

    [Tooltip("VI: Chiều cao nhấc chân (tính bằng mét) khi bước.\nEN: Leg lift height (in meters) when stepping.")]
    public float stepHeight = 0.3f;

    [Tooltip("VI: Thời gian (giây) để hoàn thành một bước chân. Càng nhỏ bước càng nhanh.\nEN: Time (in seconds) to complete one footstep. Smaller value means faster step.")]
    public float stepDuration = 0.25f;

    [Tooltip("VI: Hệ số nhân với vận tốc để đoán trước điểm đáp chân (0 = rớt đâu dẫm đó, 0.4 = đón đầu hướng đi).\nEN: Multiplier with velocity to predict landing spot (0 = step exactly below, 0.4 = step into movement direction).")]
    public float overshootMultiplier = 0.4f;

    [Tooltip("VI: Độ dài tia dò đường xuống mặt đất. Tăng lên nếu mech cao kều, giảm đi nếu mech lùn.\nEN: Downward raycast length to the ground. Increase if mech is tall, decrease if short.")]
    public float raycastLength = 8f;

    [Tooltip("VI: Đồ thị tạo vòm cong bước chân. Vẽ Parabol từ (0,0) lên đỉnh (0.5, 1) và hạ về (1,0).\nEN: Curve to generate step arc. Draw a Parabola from (0,0) peaking at (0.5, 1) and ending at (1,0).")]
    public AnimationCurve stepHeightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("VI: Layer của mặt đất. Chân sẽ chỉ bước lên vật thể có Layer này.\nEN: Ground Layer. Legs will only step on objects with this Layer.")]
    public LayerMask groundLayer;

    [Header("--- BODY BOBBING ---")]
    [Tooltip("VI: Khối Mesh của thân Mech để tạo hiệu ứng nhấp nhô theo nhịp thở/di chuyển.\nEN: The Mech's body Mesh object to create breathing/movement bobbing effects.")]
    public Transform bodyMesh;

    [Tooltip("VI: Tốc độ nhấp nhô thân hình khi đang đi dạo.\nEN: Body bobbing speed while walking.")]
    public float moveBobSpeed = 14f;

    [Tooltip("VI: Biên độ (độ nảy cao thấp) của thân hình khi đi dạo.\nEN: Amplitude (up/down bounce) of the body while walking.")]
    public float moveBobAmount = 0.08f;

    private NavMeshAgent _navAgent;
    private float _bobTimer;
    private Vector3 _startBodyLocalPos;

    void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (bodyMesh != null) _startBodyLocalPos = bodyMesh.localPosition;

        foreach (var leg in legs)
        {
            if (leg != null && leg.tipTarget != null)
            {
                leg.currentFootprint = leg.tipTarget.position;
                leg.idealPosition = leg.tipTarget.position;
            }
        }
    }

    void Update()
    {
        if (legs == null || legs.Length == 0) return;

        ManageLegStepping();
        ApplyBodyBobbing();
    }

    private void ManageLegStepping()
    {
        bool isEvenGroupStepping = false;
        bool isOddGroupStepping = false;

        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i] != null && legs[i].isStepping)
            {
                if (i % 2 == 0) isEvenGroupStepping = true;
                else isOddGroupStepping = true;
            }
        }

        Vector3 velocityOffset = _navAgent != null ? _navAgent.velocity * overshootMultiplier : Vector3.zero;

        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i] == null || legs[i].tipTarget == null || legs[i].homeTransform == null) continue;

            // Sử dụng raycastLength có thể tùy chỉnh
            Vector3 predictedPos = legs[i].homeTransform.position + velocityOffset;
            if (Physics.Raycast(predictedPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, raycastLength, groundLayer))
            {
                legs[i].idealPosition = hit.point;
            }

            if (!legs[i].isStepping)
            {
                float dist = Vector3.Distance(legs[i].currentFootprint, legs[i].idealPosition);
                if (dist > stepDistance)
                {
                    if ((i % 2 == 0 && !isOddGroupStepping) || (i % 2 != 0 && !isEvenGroupStepping))
                    {
                        legs[i].isStepping = true;
                        legs[i].stepTimer = 0f;
                        legs[i].stepStartPos = legs[i].currentFootprint;

                        if (i % 2 == 0) isEvenGroupStepping = true;
                        else isOddGroupStepping = true;
                    }
                }

                legs[i].tipTarget.position = legs[i].currentFootprint;
            }
            else
            {
                legs[i].stepTimer += Time.deltaTime / stepDuration;
                if (legs[i].stepTimer >= 1f)
                {
                    legs[i].stepTimer = 1f;
                    legs[i].isStepping = false;
                    legs[i].currentFootprint = legs[i].idealPosition;
                }

                Vector3 lerpedPos = Vector3.Lerp(legs[i].stepStartPos, legs[i].idealPosition, legs[i].stepTimer);
                float heightOffset = stepHeightCurve != null ? stepHeightCurve.Evaluate(legs[i].stepTimer) * stepHeight : 0f;
                lerpedPos.y += heightOffset;

                legs[i].tipTarget.position = lerpedPos;
            }
        }
    }

    private void ApplyBodyBobbing()
    {
        if (bodyMesh == null) return;

        float currentSpeed = _navAgent.velocity.magnitude;
        if (currentSpeed > 0.1f)
        {
            _bobTimer += Time.deltaTime * moveBobSpeed;
            float offsetY = Mathf.Sin(_bobTimer) * moveBobAmount;
            Vector3 targetBodyPos = _startBodyLocalPos + new Vector3(0f, offsetY, 0f);
            bodyMesh.localPosition = Vector3.Lerp(bodyMesh.localPosition, targetBodyPos, Time.deltaTime * 10f);
        }
        else
        {
            bodyMesh.localPosition = Vector3.Lerp(bodyMesh.localPosition, _startBodyLocalPos, Time.deltaTime * 5f);
        }
    }
}