using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using DG.Tweening;
using KinematicCharacterController.Examples;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    private Transform cameraTarget;
    [SerializeField] private CinemachineCamera vCam;
    [Space]
    [Header("Post Processing")]
    [SerializeField] private float blurFadeDuration = 0.25f; // [VI] Thời gian mờ dần / [EN] Fade duration
    [SerializeField] private Volume aimVolume;
    [Header("Look Settings")]
    public float lookSensitivity = 0.5f;
    public float minPitch = -85f;
    public float maxPitch = 85f;
    [Space]
    [Header("FOV Polish")]
    [SerializeField] private float normalFOV = 90f;
    [SerializeField] private float slideFOV = 110f; // [VI] Góc rộng hơn khi trượt / [EN] Wider angle when sliding
    [SerializeField] private float fovChangeSpeed = 10f; // [VI] Tốc độ chuyển đổi / [EN] Transition speed
    [SerializeField] private float aimFOV = 40f;
    [Header("Tilt Polish (Nghiêng)")]
    [SerializeField] private float tiltAngle = 4f; // [VI] Góc nghiêng tối đa / [EN] Max tilt angle
    [SerializeField] private float tiltSpeed = 6f;
    [Header("Damping Polish")]
    [SerializeField] private Vector3 normalDamping = new Vector3(0.1f, 0.6f, 0.1f);
    [SerializeField] private Vector3 lockDamping = new Vector3(0.02f, 0.02f, 0.02f); // [VI] Khóa cứng cực mượt / [EN] Extremely tight lock
    [SerializeField] private float dampingChangeSpeed = 15f;
    [Header("Framing Polish (Khoảng cách & Góc vai)")]
    [SerializeField] private float normalDistance = 5f;
    [SerializeField] private float aimDistance = 2f; // [VI] Sát vào vai / [EN] Close to shoulder
    [SerializeField] private Vector3 normalShoulder = new Vector3(1.5f, 0.5f, 0f);
    [SerializeField] private Vector3 aimShoulder = new Vector3(1f, 0.5f, 0f); // [VI] Đưa tâm về giữa / [EN] Center the crosshair
    [SerializeField] private Vector3 slideShoulder = new Vector3(1.5f, 1.2f, 0f); // [VI] Nhấc cao lên để khỏi đụng đất / [EN] Lift high to avoid ground

    private CinemachineThirdPersonFollow _tpFollow;
    private float currentPitch;
    private float currentYaw;
    private void Awake()
    {
        if (vCam != null)
        {
            _tpFollow = vCam.GetComponent<CinemachineThirdPersonFollow>();
        }
    }
    public void Initialize(Transform target)
    {
        cameraTarget = target;

        // [VI] Đồng bộ ban đầu cẩn thận hơn để tránh lỗi 360
        // [EN] More careful initial sync to avoid 360 bugs
        Vector3 euler = cameraTarget.eulerAngles;
        currentYaw = euler.y;

        // [VI] Xử lý chuẩn hóa Pitch về khoảng [-180, 180]
        // [EN] Normalize Pitch to [-180, 180] range
        currentPitch = euler.x;
        if (currentPitch > 180f) currentPitch -= 360f;
    }

    public void UpdateCameraLogic(ref CharacterInputs inputs, float deltaTime)
    {
        if (cameraTarget == null) return;

        // 1. Cộng dồn
        currentYaw += inputs.LookInput.x * lookSensitivity;
        currentPitch -= inputs.LookInput.y * lookSensitivity;

        // 2. Khóa Pitch
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // 3. Chuẩn hóa Yaw để nó luôn nằm trong khoảng [0, 360) (Triệt tiêu lỗi quá 360 độ)
        currentYaw = Mathf.Repeat(currentYaw, 360f);

        // 4. ÁP DỤNG XOAY WORLD SPACE CHUẨN
        // [VI] Xoay Yaw theo trục Y của World (trái đất), sau đó xoay Pitch theo trục X cục bộ của chính nó
        // [EN] Rotate Yaw along World Y axis, then Pitch along its own local X axis
        Quaternion yawRotation = Quaternion.AngleAxis(currentYaw, Vector3.up);
        Quaternion pitchRotation = Quaternion.AngleAxis(currentPitch, Vector3.right);

        cameraTarget.rotation = yawRotation * pitchRotation;
    }

    public void UpdateCameraPolish(Stance playerStance, float horizontalInput, bool isAiming, float deltaTime)
    {
        if (vCam == null) return;

        // 1. XỬ LÝ FOV (ZOOM IN/OUT)
        // [VI] Ưu tiên Aim > Slide > Normal
        // [EN] Priority: Aim > Slide > Normal
        float targetFOV = normalFOV;
        if (isAiming) targetFOV = aimFOV;
        else if (playerStance == Stance.Slide) targetFOV = slideFOV;

        vCam.Lens.FieldOfView = Mathf.Lerp(vCam.Lens.FieldOfView, targetFOV, fovChangeSpeed * deltaTime);
        // 2. XỬ LÝ NGHIÊNG (TILT / DUTCH)
        // [VI] Tắt nghiêng khi đang ngắm để bắn cho chuẩn
        // [EN] Disable tilt while aiming for precise shooting
        float targetTilt = isAiming ? 0f : (-horizontalInput * tiltAngle);
        vCam.Lens.Dutch = Mathf.Lerp(vCam.Lens.Dutch, targetTilt, tiltSpeed * deltaTime);
        if (_tpFollow != null)
        {
            // 3. DAMPING (Đã có / Already there)
            Vector3 targetDamping = (isAiming || playerStance == Stance.Slide) ? lockDamping : normalDamping;
            _tpFollow.Damping = Vector3.Lerp(_tpFollow.Damping, targetDamping, dampingChangeSpeed * deltaTime);

            // 4. FRAMING (DISTANCE & SHOULDER OFFSET)
            float targetDistance = normalDistance;
            Vector3 targetShoulder = normalShoulder;

            if (isAiming)
            {
                targetDistance = aimDistance;
                targetShoulder = aimShoulder;
            }
            else if (playerStance == Stance.Slide)
            {
                targetShoulder = slideShoulder;
            }

            _tpFollow.CameraDistance = Mathf.Lerp(_tpFollow.CameraDistance, targetDistance, fovChangeSpeed * deltaTime);
            _tpFollow.ShoulderOffset = Vector3.Lerp(_tpFollow.ShoulderOffset, targetShoulder, fovChangeSpeed * deltaTime);
        }

        if (aimVolume != null)
        {
            // [VI] Nếu đang ngắm -> Đẩy Weight lên 1. Ngược lại -> Trả về 0
            // [EN] If aiming -> Push Weight to 1. Otherwise -> Return to 0
            float targetAimWeight = isAiming ? 1f : 0f;
            aimVolume.weight = Mathf.Lerp(aimVolume.weight, targetAimWeight, fovChangeSpeed * deltaTime);
        }
    }
}

