using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EncounterManager : MonoBehaviour
{
    [Header("--- ENEMY LIST (UPDATED FOR SPRINT MVP) ---")]
    [Tooltip("Kéo tất cả cấu trúc AIHealth của quái trong phòng vào đây")]
    public List<AIHealth> enemiesInRoom;

    [Header("--- SETTINGS ---")]
    public float delayAfterClear = 2f;

    [Header("--- EVENTS ---")]
    public UnityEvent onRoomCleared;

    private bool _isCleared = false;

    private void Update()
    {
        if (_isCleared) return;

        bool allDead = true;
        foreach (AIHealth enemy in enemiesInRoom)
        {
            // Kiểm tra theo biến máu mới của hệ thống súng
            if (enemy != null && enemy.currentHealth > 0)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            _isCleared = true;
            StartCoroutine(RoomClearedRoutine());
        }
    }

    private IEnumerator RoomClearedRoutine()
    {
        Debug.Log("<color=green>[ENCOUNTER]</color> Khu vực an toàn! Kích hoạt mở cửa/chạy thoại...");
        yield return new WaitForSeconds(delayAfterClear);

        if (onRoomCleared != null) onRoomCleared.Invoke();
    }
}