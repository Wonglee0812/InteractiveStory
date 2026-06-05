using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class PlayerWaypointSensor : MonoBehaviour
{
    [Header("--- SENSOR SETTINGS ---")]
    public bool playOnce = true;

    [Header("--- WAYPOINT MOVEMENT ---")]
    [Tooltip("VI: Tốc độ bước đi của Player.\nEN: Player's walking speed.")]
    public float moveSpeed = 3f;

    [Tooltip("VI: Tốc độ xoay người hướng về Waypoint.\nEN: Speed of rotating towards the Waypoint.")]
    public float turnSpeed = 5f;

    [Tooltip("VI: Danh sách các điểm đến theo thứ tự.\nEN: List of destination points in order.")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("--- OUTPUT EVENTS ---")]
    [Tooltip("VI: Gọi sự kiện (như mở cửa, phát thoại) khi Player tới điểm cuối cùng.\nEN: Trigger events (like opening doors, playing dialogue) when Player reaches the final point.")]
    public UnityEvent onSequenceFinished;

    private bool _hasTriggered = false;
    private Player _playerObj;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && (!_hasTriggered || !playOnce))
        {
            _hasTriggered = true;
            _playerObj = other.GetComponentInParent<Player>();

            if (_playerObj != null)
            {
                Debug.Log($"<color=cyan>[WAYPOINT SENSOR]</color> Kích hoạt! Đang ép Player đi theo lộ trình...");

                // VI: Khóa điều khiển di chuyển của Player (Camera vẫn quay được nếu script Player cho phép)
                // EN: Lock Player movement control (Camera can still rotate if Player script allows it)
                _playerObj.SetInputLock(true);

                // VI: Khởi chạy tiến trình dẫn đường
                // EN: Start the pathfinding routine
                StartCoroutine(FollowWaypointsRoutine(_playerObj.transform));
            }

            if (playOnce)
            {
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    private IEnumerator FollowWaypointsRoutine(Transform playerTransform)
    {
        // VI: Tắt CharacterController (hoặc Collider) tạm thời để tránh bị hệ thống Vật lý cản bước
        // EN: Temporarily disable CharacterController (or Collider) to prevent Physics system obstruction
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // VI: Lặp qua từng điểm Waypoint trong danh sách
        // EN: Iterate through each Waypoint in the list
        foreach (Transform wp in waypoints)
        {
            if (wp == null) continue;

            // VI: Tiếp tục di chuyển cho đến khi khoảng cách tới Waypoint nhỏ hơn 0.1 mét
            // EN: Keep moving until the distance to the Waypoint is less than 0.1 meters
            while (Vector3.Distance(playerTransform.position, wp.position) > 0.1f)
            {
                // 1. XOAY NGƯỜI (ROTATION)
                Vector3 dir = (wp.position - playerTransform.position).normalized;
                dir.y = 0; // VI: Ép trục Y về 0 để Player không bị ngửa mặt lên trời / EN: Force Y axis to 0 so Player doesn't tilt upward

                if (dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }

                // 2. DI CHUYỂN (MOVEMENT)
                playerTransform.position = Vector3.MoveTowards(playerTransform.position, wp.position, moveSpeed * Time.deltaTime);

                // VI: Chờ khung hình tiếp theo
                // EN: Wait for the next frame
                yield return null;
            }
        }

        Debug.Log($"<color=cyan>[WAYPOINT SENSOR]</color> Đã đến đích! Trả lại quyền điều khiển.");

        // VI: Bật lại Vật lý cho Player
        // EN: Re-enable Physics for the Player
        if (cc != null) cc.enabled = true;

        // VI: Mở khóa điều khiển
        // EN: Unlock input control
        if (_playerObj != null)
        {
            _playerObj.SetInputLock(false);
        }

        // VI: Kích hoạt các sự kiện sau khi đi xong (vd: Mở cửa)
        // EN: Trigger events after walking finishes (e.g.: Open door)
        if (onSequenceFinished != null)
        {
            onSequenceFinished.Invoke();
        }
    }
}