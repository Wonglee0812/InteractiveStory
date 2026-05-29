using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class AreaSensor : MonoBehaviour
{
    [Header("--- SENSOR SETTINGS ---")]
    public bool playOnce = true;

    [Tooltip("Nếu tick, Player sẽ đứng im khi đạp trúng Trigger này / If ticked, locks Player movement on enter")]
    public bool lockPlayerControl = false;

    [Header("--- OUTPUT EVENTS ---")]
    [Tooltip("Kéo thả các sự kiện sếp muốn kích hoạt vào đây / Drag and drop events to trigger here")]
    public UnityEvent onTriggered;

    private bool _hasTriggered = false;
    private Player _playerObj; // Lưu trữ Player để lát nữa mở khóa

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && (!_hasTriggered || !playOnce))
        {
            _hasTriggered = true;
            _playerObj = other.GetComponentInParent<Player>();

            Debug.Log($"<color=cyan>[SENSOR]</color> {gameObject.name} đã đạp trúng! Đang phát tín hiệu...");

            // 1. [VI] Khóa Player nếu sếp tick vào ô LockPlayer
            if (lockPlayerControl && _playerObj != null)
            {
                _playerObj.SetInputLock(true);
            }

            // 2. [VI] Bắn luồng điện (Kích hoạt toàn bộ danh sách ở Inspector)
            if (onTriggered != null)
            {
                onTriggered.Invoke();
            }

            // 3. [VI] Tắt Collider ngay lập tức nếu chỉ chạy 1 lần để tối ưu Game
            if (playOnce)
            {
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    // ==========================================
    // HÀM HỖ TRỢ ĐỂ MỞ KHÓA PLAYER
    // ==========================================
    /// <summary>
    /// Hàm này dùng để gọi khi thoại Yarn chạy xong, trả lại quyền điều khiển cho Player.
    /// </summary>
    public void UnlockPlayer()
    {
        if (_playerObj != null)
        {
            _playerObj.SetInputLock(false);
            Debug.Log($"<color=cyan>[SENSOR]</color> {gameObject.name} đã mở khóa Player.");
        }
    }
}