using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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

    [Header("--- HIT MARKER SETTINGS ---")]
    [Tooltip("VI: Kéo object CrosshairHit_Parent vào đây\nEN: Drag the CrosshairHit_Parent object here")]
    public CanvasGroup hitMarkerGroup;
    public Color normalHitColor = Color.white;
    public Color headshotColor = Color.red;
    //public AudioClip hitSound; // [VI] Tiếng trúng thân / [EN] Body hit sound
    //public AudioClip headshotSound; // [VI] Tiếng Ping headshot / [EN] Headshot Ping sound

    //private AudioSource _audioSource;
    private Image[] _hitMarkerLines; // [VI] Bộ nhớ chứa 4 line con / [EN] Cache for the 4 child lines

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

        if (hitMarkerGroup != null)
        {
            hitMarkerGroup.alpha = 0f;
            _hitMarkerLines = hitMarkerGroup.GetComponentsInChildren<Image>();
        }
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
    public void ShowHitMarker(bool isHeadshot)
    {
        if (hitMarkerGroup == null) return;

        // 1. Cấu hình màu sắc cho toàn bộ 4 vạch Line bên trong
        // 1. Configure colors for all 4 internal Lines
        Color targetColor = isHeadshot ? headshotColor : normalHitColor;
        if (_hitMarkerLines != null)
        {
            foreach (var line in _hitMarkerLines)
            {
                line.color = targetColor;
            }
        }

        // 2. Phát âm thanh
        // 2. Play Audio
        //if (_audioSource != null)
        //{
        //    AudioClip clipToPlay = isHeadshot ? headshotSound : hitSound;
        //    if (clipToPlay != null) _audioSource.PlayOneShot(clipToPlay);
        //}

        // 3. Chặn các hiệu ứng cũ đang chạy dở
        // 3. Kill ongoing effects
        DOTween.Kill(hitMarkerGroup.transform);
        DOTween.Kill(hitMarkerGroup);

        // 4. Phóng đại kích thước Parent lên nếu là Headshot
        // 4. Scale up the Parent if it's a Headshot
        hitMarkerGroup.transform.localScale = Vector3.one * (isHeadshot ? 1.5f : 1.0f);

        // 5. Chuỗi Tween: Hiện tức thì -> Giật nảy Parent -> Mờ dần về 0
        // 5. Tween Sequence: Instant fade-in -> Punch Parent -> Fade out to 0
        hitMarkerGroup.DOFade(1f, 0f);
        hitMarkerGroup.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 10, 1f);
        hitMarkerGroup.DOFade(0f, 0.3f).SetDelay(0.1f);
    }
}