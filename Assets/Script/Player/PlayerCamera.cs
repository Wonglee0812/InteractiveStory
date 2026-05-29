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
    [SerializeField] private Volume inventoryBlurVolume; // [VI] Kéo cái Inventory Volume có Priority=1 vào đây / [EN] Drag the Inventory Volume with Priority=1 here
    [SerializeField] private float blurFadeDuration = 0.25f; // [VI] Thời gian mờ dần / [EN] Fade duration
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
    [Header("Crosshair UI")]
    [SerializeField] private CanvasGroup crosshairCanvasGroup; // [VI] Dùng CanvasGroup để làm mờ mượt mà / [EN] Use CanvasGroup for smooth fading
    [SerializeField] private float crosshairFadeDuration = 0.15f;
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
    }
    public void ToggleFocusBlur(bool isInventoryOpen)
    {
        if (inventoryBlurVolume == null) return;

        // [VI] Hủy các animation đang chạy lở dở để tránh lỗi giật hình
        // [EN] Kill unfinished animations to avoid jittering
        DOTween.Kill(inventoryBlurVolume);

        // [VI] Nếu mở: Đích đến là 1 (Mờ tịt). Nếu đóng: Đích đến là 0 (Rõ nét)
        // [EN] If open: Target is 1 (Heavy blur). If closed: Target is 0 (Sharp)
        float targetWeight = isInventoryOpen ? 1f : 0f;

        // [VI] Dùng DOTween để tween thuộc tính 'weight'
        // [EN] Use DOTween to tween the 'weight' property
        DOTween.To(() => inventoryBlurVolume.weight, x => inventoryBlurVolume.weight = x, targetWeight, blurFadeDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true); // [VI] LƯU Ý QUAN TRỌNG: Phải có SetUpdate(true) vì lúc này game đang bị Slow Motion
                              // [EN] CRITICAL NOTE: Must have SetUpdate(true) because the game is in Slow Motion right now
    }
    public void UpdateCrosshair(bool isAiming)
    {
        if (crosshairCanvasGroup == null) return;

        // [VI] Hủy các animation cũ để tránh xung đột
        // [EN] Kill old animations to avoid conflicts
        crosshairCanvasGroup.DOKill();

        float targetAlpha = isAiming ? 1f : 0f;
        Vector3 targetScale = isAiming ? Vector3.one : Vector3.one * 0.5f;

        // [VI] Làm mờ dần (Fade)
        // [EN] Fade in/out
        crosshairCanvasGroup.DOFade(targetAlpha, crosshairFadeDuration).SetUpdate(true);

        // [VI] Hiệu ứng nảy nhẹ khi hiện ra (Scale)
        // [EN] Slight bounce effect when appearing (Scale)
        crosshairCanvasGroup.transform.DOScale(targetScale, crosshairFadeDuration).SetUpdate(true);
    }
}

