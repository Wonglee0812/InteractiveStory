using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DynamicCrosshair : MonoBehaviour
{
    public static DynamicCrosshair Instance { get; private set; }

    [Header("--- UI REFERENCES ---")]
    public RectTransform topPart;
    public RectTransform bottomPart;
    public RectTransform leftPart;
    public RectTransform rightPart;

    private CanvasGroup _canvasGroup;

    [Header("--- SPREAD & FADE SETTINGS ---")]
    public float baseAimSpread = 15f;
    public float fireSpreadKick = 15f;
    public float maxSpread = 100f;
    public float spreadReturnSpeed = 8f;
    public float fadeSpeed = 15f;

    private float _currentSpread;
    private bool _isAiming;

    // [VI] BIẾN MỚI: Cờ kiểm tra xem có đang giữ cò súng không
    // [EN] NEW VARIABLE: Flag to check if trigger is being held
    private bool _isShooting;

    private void Awake()
    {
        Instance = this;
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _currentSpread = baseAimSpread;
    }

    private void Update()
    {
        // 1. FADE ALPHA
        float targetAlpha = _isAiming ? 1f : 0f;
        _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

        // 2. SPREAD LOGIC (CHỐNG GIẬT / ANTI-JITTER)
        // [VI] Nếu KHÔNG bóp cò thì mới cho phép tâm ngắm co lại
        // [EN] Only allow the crosshair to shrink back if NOT pulling the trigger
        if (!_isShooting)
        {
            _currentSpread = Mathf.Lerp(_currentSpread, baseAimSpread, spreadReturnSpeed * Time.deltaTime);
        }

        // 3. UPDATE UI
        if (topPart != null) topPart.anchoredPosition = new Vector2(0, _currentSpread);
        if (bottomPart != null) bottomPart.anchoredPosition = new Vector2(0, -_currentSpread);
        if (leftPart != null) leftPart.anchoredPosition = new Vector2(-_currentSpread, 0);
        if (rightPart != null) rightPart.anchoredPosition = new Vector2(_currentSpread, 0);
    }

    public void SetAimingState(bool isAiming) => _isAiming = isAiming;

    // [VI] Hàm mới tiếp nhận tín hiệu giữ chuột
    // [EN] New function to receive mouse hold signal
    public void SetShootingState(bool isShooting) => _isShooting = isShooting;

    public void ApplyFireKick()
    {
        if (_isAiming)
        {
            _currentSpread = Mathf.Min(_currentSpread + fireSpreadKick, maxSpread);
        }
    }
}