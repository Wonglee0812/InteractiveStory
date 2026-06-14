using UnityEngine;
using DG.Tweening;

public class TargetBracket : MonoBehaviour
{
    [Header("--- UI SETTINGS ---")]
    [Tooltip("VI: Kéo Prefab UI Khung Vuông vào đây\nEN: Drag the Square Frame UI Prefab here")]
    public RectTransform bracketPrefab;

    [Tooltip("VI: Đẩy UI lên cao một chút (ngay giữa ngực/đầu quái)\nEN: Push UI up slightly (middle of chest/head)")]
    public Vector3 targetOffset = new Vector3(0, 1.2f, 0);

    [Header("--- TRACKING SETTINGS ---")]
    public float maxTrackingDistance = 40f;
    public float lockRadius = 300f;

    private RectTransform _bracketInstance;
    private CanvasGroup _canvasGroup;
    private Camera _mainCam;
    private bool _isLocked = false;

    // [VI] Cờ đánh dấu kẻ địch đã chết / [EN] Flag marking the enemy as dead
    private bool _isDead = false;

    void Start()
    {
        _mainCam = Camera.main;
        GameObject canvasObj = GameObject.Find("HUD_Canvas");

        if (canvasObj != null && bracketPrefab != null)
        {
            _bracketInstance = Instantiate(bracketPrefab, canvasObj.transform);
            _canvasGroup = _bracketInstance.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = _bracketInstance.gameObject.AddComponent<CanvasGroup>();

            _canvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        // [VI] Thêm điều kiện: Nếu quái ĐÃ CHẾT thì ngừng chạy logic UI ngay lập tức
        // [EN] Added condition: If the monster is DEAD, stop running UI logic immediately
        if (_bracketInstance == null || _mainCam == null || _isDead) return;

        Vector3 screenPos = _mainCam.WorldToScreenPoint(transform.position + targetOffset);
        float distanceToPlayer = Vector3.Distance(_mainCam.transform.position, transform.position);

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        float distanceFromCenter = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), screenCenter);

        bool isTargetable = screenPos.z > 0 && distanceToPlayer <= maxTrackingDistance && distanceFromCenter <= lockRadius;

        if (isTargetable)
        {
            _bracketInstance.position = screenPos;

            if (!_isLocked)
            {
                _isLocked = true;
                _canvasGroup.DOKill();
                _canvasGroup.DOFade(1f, 0.2f);

                _bracketInstance.DOKill();
                _bracketInstance.localRotation = Quaternion.Euler(0, 0, 45f);
                _bracketInstance.localScale = Vector3.one * 2f;

                _bracketInstance.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                _bracketInstance.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
            }
        }
        else
        {
            if (_isLocked)
            {
                _isLocked = false;
                _canvasGroup.DOKill();
                _canvasGroup.DOFade(0f, 0.2f);
            }
        }
    }

    // ==========================================
    // [VI] HÀM MỚI: TẮT UI KHI QUÁI CHẾT
    // [EN] NEW METHOD: TURN OFF UI ON MONSTER DEATH
    // ==========================================
    public void OnTargetDeath()
    {
        _isDead = true; // [VI] Khóa Update / [EN] Lock Update

        if (_bracketInstance != null && _canvasGroup != null)
        {
            // [VI] Làm mờ đi mượt mà trong 0.2s rồi tự hủy xóa rác
            // [EN] Fade out smoothly over 0.2s then self-destruct to clean up
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(0f, 0.2f).OnComplete(() => {
                Destroy(_bracketInstance.gameObject);
            });
        }
    }

    // [VI] Đề phòng trường hợp quái bị nhốt vào Object Pool thay vì Destroy
    // [EN] In case the monster is thrown into an Object Pool instead of Destroyed
    void OnDisable()
    {
        if (_bracketInstance != null && _canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _isLocked = false;
        }
    }

    void OnDestroy()
    {
        if (_bracketInstance != null)
        {
            _bracketInstance.DOKill();
            Destroy(_bracketInstance.gameObject);
        }
    }
}