using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class NPCMoveToPoint : MonoBehaviour
{
    [Header("--- MOVEMENT SETTINGS ---")]
    [Tooltip("VI: Điểm đến của NPC.\nEN: The NPC's destination.")]
    public Transform targetPoint;

    public float moveSpeed = 2.5f;
    public float turnSpeed = 5f;

    [Header("--- OUTPUT EVENTS ---")]
    [Tooltip("VI: Sự kiện kích hoạt khi NPC đến đích (Vd: Bật Quest, Bật AreaSensor).\nEN: Events triggered when NPC arrives (e.g: Enable Quest, Enable AreaSensor).")]
    public UnityEvent onDestinationReached;

    // VI: OnEnable tự động chạy khi GameObject chứa script này được bật lên
    // EN: OnEnable runs automatically when the GameObject holding this script is turned on
    private void OnEnable()
    {
        if (targetPoint != null)
        {
            StartCoroutine(MoveRoutine());
        }
    }

    private IEnumerator MoveRoutine()
    {
        // VI: Vòng lặp di chuyển cho đến khi khoảng cách nhỏ hơn 0.1 mét
        // EN: Movement loop until the distance is less than 0.1 meters
        while (Vector3.Distance(transform.position, targetPoint.position) > 0.1f)
        {
            // 1. TÍNH GÓC XOAY (CALCULATE ROTATION)
            Vector3 dir = (targetPoint.position - transform.position).normalized;
            dir.y = 0;

            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            // 2. DI CHUYỂN (MOVE)
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);

            yield return null;
        }

        // VI: Khi đã đến đích, kích hoạt luồng sự kiện
        // EN: Once arrived, trigger the event flow
        if (onDestinationReached != null)
        {
            onDestinationReached.Invoke();
        }
    }
}