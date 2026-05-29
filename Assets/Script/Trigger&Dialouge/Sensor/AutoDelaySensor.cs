using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AutoDelaySensor : MonoBehaviour
{
    [Header("--- DELAY SETTINGS ---")]
    public float delayTime = 2f;

    [Header("--- OUTPUT EVENTS ---")]
    public UnityEvent onDelayFinished;

    // Kéo thả hàm này vào sự kiện OnRoomCleared của EncounterManager
    public void StartDelayTimer()
    {
        StartCoroutine(DelayRoutine());
    }

    private IEnumerator DelayRoutine()
    {
        Debug.Log($"<color=cyan>[SENSOR]</color> Đang đếm ngược {delayTime} giây...");
        yield return new WaitForSeconds(delayTime);

        if (onDelayFinished != null)
        {
            onDelayFinished.Invoke();
        }
    }
}