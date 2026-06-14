using UnityEngine;
using DG.Tweening; // [VI] Bắt buộc phải có để dùng hiệu ứng mượt / [EN] Mandatory for smooth effects

[RequireComponent(typeof(SphereCollider))] // [VI] Tự động thêm vùng Trigger / [EN] Auto-add Trigger zone
public class GuidanceUI : MonoBehaviour
{
    [Header("--- UI REFERENCES ---")]
    [Tooltip("VI: Kéo Canvas hoặc object chứa CanvasGroup vào đây\nEN: Drag the Canvas or object with CanvasGroup here")]
    public CanvasGroup uiGroup;

    [Tooltip("VI: Kéo Transform của UI cần phóng to/thu nhỏ vào đây\nEN: Drag the Transform of the UI to scale here")]
    public Transform uiTransform;

    [Header("--- TWEEN SETTINGS ---")]
    public float animDuration = 0.35f;
    public Ease easeInType = Ease.OutBack; // [VI] Hiệu ứng nảy lố ra ngoài một chút rồi co lại / [EN] Bounces out a bit then settles
    public Ease easeOutType = Ease.InBack; // [VI] Hiệu ứng co thụt vào trong / [EN] Shrinks inward

    private Vector3 _originalScale;
    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;

        // 1. [VI] Lưu lại kích thước ban đầu để biết điểm đích khi phóng to
        // 1. [EN] Save the original scale to know the target when scaling up
        if (uiTransform != null)
        {
            _originalScale = uiTransform.localScale;
            uiTransform.localScale = Vector3.zero; // [VI] Giấu đi lúc mới vào game / [EN] Hide at game start
        }

        // 2. [VI] Đặt độ mờ về 0
        // 2. [EN] Set alpha to 0
        if (uiGroup != null)
        {
            uiGroup.alpha = 0f;
        }

        // 3. [VI] Tự động ép Collider thành Trigger để chống kẹt nhân vật
        // 3. [EN] Auto-force Collider to Trigger to prevent character stuck
        GetComponent<SphereCollider>().isTrigger = true;
    }

    private void Update()
    {
        // ==========================================
        // [VI] BILLBOARDING: Ép UI luôn luôn nhìn thẳng vào mặt Camera
        // [EN] BILLBOARDING: Force UI to always look directly at the Camera
        // ==========================================
        if (uiTransform != null && _mainCam != null)
        {
            uiTransform.rotation = _mainCam.transform.rotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // [VI] Nếu người chơi đi vào vùng kích hoạt
        // [EN] If the player enters the trigger zone
        if (other.CompareTag("Player") && uiGroup != null && uiTransform != null)
        {
            // [VI] Hủy các lệnh animation cũ đang chạy dở
            // [EN] Kill unfinished old animation commands
            DOTween.Kill(uiGroup);
            DOTween.Kill(uiTransform);

            // [VI] Hiện dần lên (Alpha 0 -> 1) và Nảy to ra (Scale 0 -> Original)
            // [EN] Fade in (Alpha 0 -> 1) and Bounce scale up (Scale 0 -> Original)
            uiGroup.DOFade(1f, animDuration);
            uiTransform.DOScale(_originalScale, animDuration).SetEase(easeInType);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // [VI] Nếu người chơi bỏ đi khỏi vùng kích hoạt
        // [EN] If the player leaves the trigger zone
        if (other.CompareTag("Player") && uiGroup != null && uiTransform != null)
        {
            DOTween.Kill(uiGroup);
            DOTween.Kill(uiTransform);

            // [VI] Mờ dần đi (Alpha 1 -> 0) và Thu nhỏ bẹp lại (Scale Original -> 0)
            // [EN] Fade out (Alpha 1 -> 0) and Shrink down (Scale Original -> 0)
            uiGroup.DOFade(0f, animDuration);
            uiTransform.DOScale(Vector3.zero, animDuration).SetEase(easeOutType);
        }
    }
}