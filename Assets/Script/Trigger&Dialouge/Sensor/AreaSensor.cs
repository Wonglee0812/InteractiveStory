using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class AreaSensor : MonoBehaviour
{
    [Header("--- SENSOR SETTINGS ---")]
    public bool playOnce = true;

    [Tooltip("VI: Nếu tick, Player sẽ đứng im khi đạp trúng Trigger này.\nEN: If ticked, locks Player movement on enter.")]
    public bool lockPlayerControl = false;

    [Tooltip("VI: Thời gian tự động mở khóa (Để 0 nếu muốn tự mở bằng Code/Yarn).\nEN: Auto unlock delay (Leave 0 to unlock manually via Code/Yarn).")]
    public float autoUnlockDelay = 0f;

    [Header("--- OUTPUT EVENTS ---")]
    [Tooltip("VI: Kéo thả các sự kiện muốn kích hoạt vào đây.\nEN: Drag and drop events to trigger here.")]
    public UnityEvent onTriggered;

    private bool _hasTriggered = false;
    private Player _playerObj;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && (!_hasTriggered || !playOnce))
        {
            _hasTriggered = true;
            _playerObj = other.GetComponentInParent<Player>();

            Debug.Log($"<color=cyan>[SENSOR]</color> {gameObject.name} đã đạp trúng! Đang phát tín hiệu...");

            if (lockPlayerControl && _playerObj != null)
            {
                _playerObj.SetInputLock(true);

                // VI: Kích hoạt bộ đếm tự động mở khóa nếu có cài đặt
                // EN: Activate auto-unlock timer if set
                if (autoUnlockDelay > 0f)
                {
                    StartCoroutine(AutoUnlockRoutine());
                }
            }

            if (onTriggered != null)
            {
                onTriggered.Invoke();
            }

            if (playOnce)
            {
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    private IEnumerator AutoUnlockRoutine()
    {
        yield return new WaitForSeconds(autoUnlockDelay);
        UnlockPlayer();
    }

    public void UnlockPlayer()
    {
        if (_playerObj != null)
        {
            _playerObj.SetInputLock(false);
            Debug.Log($"<color=cyan>[SENSOR]</color> {gameObject.name} đã mở khóa Player.");
        }
    }
}