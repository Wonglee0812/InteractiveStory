using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class LookAtSensor : MonoBehaviour
{
    [Header("--- SENSOR SETTINGS ---")]
    [Tooltip("VI: Khoảng cách tối đa để có thể nhìn thấy.\nEN: Max distance to trigger.")]
    public float maxDistance = 5f;

    [Tooltip("VI: Góc nhìn tối đa (độ) để tính là đang nhìn vào.\nEN: Max angle threshold.")]
    public float viewAngleThreshold = 15f;

    [Tooltip("VI: Thời gian (giây) phải nhìn liên tục để kích hoạt.\nEN: Continuous look duration (seconds) to trigger.")]
    public float requiredLookTime = 1.5f;

    public bool playOnce = true;

    [Header("--- LINE OF SIGHT (CHỐNG NHÌN XUYÊN TƯỜNG) ---")]
    [Tooltip("VI: Chọn Layer của tường/vật cản để cảm biến không nhìn xuyên qua.\nEN: Select the Layer for walls/obstacles so the sensor doesn't see through them.")]
    public LayerMask obstacleMask;

    [Header("--- OUTPUT EVENTS ---")]
    public UnityEvent onLookAtTriggered;

    private bool _hasTriggered = false;
    private Camera _mainCam;
    private Collider _collider;
    private float _currentLookTimer = 0f;

    private void Start()
    {
        _mainCam = Camera.main;

        // VI: Lấy Collider để tìm ra tâm thực sự của vật thể
        // EN: Get the Collider to find the true center of the object
        _collider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (_hasTriggered && playOnce) return;


        // VI: Sử dụng tâm của Collider thay vì transform.position
        // EN: Use the Collider's center instead of transform.position
        Vector3 targetCenter = _collider.bounds.center;
        float distance = Vector3.Distance(_mainCam.transform.position, targetCenter);

        if (distance <= maxDistance)
        {
            Vector3 dirToObject = (targetCenter - _mainCam.transform.position).normalized;
            float angle = Vector3.Angle(_mainCam.transform.forward, dirToObject);

            if (angle <= viewAngleThreshold)
            {
                // VI: Phóng tia Raycast kiểm tra xem có bức tường nào chặn giữa Camera và Vật thể không
                // EN: Shoot a Raycast to check if any wall is blocking between the Camera and the Object
                if (!Physics.Linecast(_mainCam.transform.position, targetCenter, obstacleMask))
                {
                    _currentLookTimer += Time.deltaTime;

                    if (_currentLookTimer >= requiredLookTime)
                    {
                        _hasTriggered = true;
                        Debug.Log($"<color=cyan>[SENSOR]</color> Người chơi đã tập trung nhìn vào {gameObject.name}");

                        if (onLookAtTriggered != null) onLookAtTriggered.Invoke();
                    }
                    return; // VI: Thoát vòng lặp để không bị reset / EN: Exit loop to prevent reset
                }
            }
        }

        // VI: Reset bộ đếm nếu quay mặt đi, đi ra xa, hoặc bị tường che
        // EN: Reset timer if looking away, moving too far, or occluded by a wall
        _currentLookTimer = 0f;
    }

    // ==========================================
    // HỆ THỐNG VẼ ĐƯỜNG DÒ TRONG EDITOR (GIZMOS)
    // ==========================================
    private void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;

        Collider col = GetComponent<Collider>();
        Vector3 center = col != null ? col.bounds.center : transform.position;
        Vector3 camPos = Camera.main.transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.2f); // VI: Vẽ tâm vật thể / EN: Draw object center

        // VI: Đổi màu tia nối thành Đỏ nếu ngoài tầm, Xanh lá nếu trong tầm nhìn
        // EN: Change line color to Red if out of range, Green if in visual range
        float distance = Vector3.Distance(camPos, center);
        if (distance > maxDistance) Gizmos.color = Color.red;
        else Gizmos.color = Color.green;

        Gizmos.DrawLine(camPos, center);
    }
}