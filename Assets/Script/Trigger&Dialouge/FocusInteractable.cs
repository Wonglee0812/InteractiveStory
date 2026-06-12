using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class FocusInteractable : MonoBehaviour
{
    [Header("--- FOCUS SETTINGS ---")]
    [Tooltip("VI: Thời gian nhìn liên tục (giây) để kích hoạt.\nEN: Continuous look time (seconds) to trigger.")]
    public float requiredLookTime = 1.5f;

    public bool playOnce = true;


    [Header("--- OUTPUT EVENTS ---")]
    public UnityEvent onLookAtTriggered;

    private float _currentLookTimer = 0f;
    private bool _hasTriggered = false;

    // ==========================================
    // CÁC HÀM ĐƯỢC GỌI BỞI PLAYER VISION
    // FUNCTIONS CALLED BY PLAYER VISION
    // ==========================================

    public void IncreaseTimer(float deltaTime)
    {
        if (_hasTriggered && playOnce) return;


        _currentLookTimer += deltaTime;

        if (_currentLookTimer >= requiredLookTime)
        {
            _hasTriggered = true;
            Debug.Log($"<color=cyan>[INTERACT]</color> Đã kích hoạt điểm khám phá: {gameObject.name}");

            if (onLookAtTriggered != null)
            {
                onLookAtTriggered.Invoke();
            }
        }
    }

    public void ResetTimer()
    {
        // VI: Chỉ reset nếu chưa kích hoạt xong
        // EN: Only reset if not fully triggered yet
        if (!_hasTriggered)
        {
            _currentLookTimer = 0f;
        }
    }
}