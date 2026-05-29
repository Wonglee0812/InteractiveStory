using UnityEngine;
using UnityEngine.Events;

public class LookAtSensor : MonoBehaviour
{
    [Header("--- SENSOR SETTINGS ---")]
    [Tooltip("Khoảng cách tối đa để có thể nhìn thấy / Max distance to trigger")]
    public float maxDistance = 5f;

    [Tooltip("Góc nhìn tối đa (độ) để tính là đang nhìn vào / Max angle threshold")]
    public float viewAngleThreshold = 15f;

    public bool playOnce = true;

    [Header("--- CONDITIONS (TùY CHỌN) ---")]
    [Tooltip("Nếu tick, phải đánh xong quái (GameManager cho phép) mới được nhìn / Requires Room Cleared")]
    public bool requireArmoryCleared = false;

    [Header("--- OUTPUT EVENTS ---")]
    public UnityEvent onLookAtTriggered;

    private bool _hasTriggered = false;
    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main; // Tự động tìm Camera chính
    }

    private void Update()
    {
        if (_hasTriggered && playOnce) return;

        // Nếu yêu cầu phải dọn sạch phòng mới được khám phá, mà phòng chưa sạch thì bỏ qua
        if (requireArmoryCleared && GameManager.Instance != null && !GameManager.Instance.isArmoryCleared)
            return;

        // Tính khoảng cách
        float distance = Vector3.Distance(_mainCam.transform.position, transform.position);
        if (distance <= maxDistance)
        {
            // Tính góc nhìn giữa hướng Camera và vị trí vật thể
            Vector3 dirToObject = (transform.position - _mainCam.transform.position).normalized;
            float angle = Vector3.Angle(_mainCam.transform.forward, dirToObject);

            if (angle <= viewAngleThreshold)
            {
                _hasTriggered = true;
                Debug.Log($"<color=cyan>[SENSOR]</color> Người chơi đang nhìn vào {gameObject.name}");

                if (onLookAtTriggered != null) onLookAtTriggered.Invoke();
            }
        }
    }
}