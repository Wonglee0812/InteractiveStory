using UnityEngine;
using System.Collections.Generic;

public class PlayerTargetScanner : MonoBehaviour
{
    [Header("--- SCAN SETTINGS ---")]
    public float scanRadius = 15f;
    public LayerMask enemyLayer;

    // [VI] Lưu trữ mục tiêu của frame trước để so sánh 
    // [EN] Store previous frame's targets for comparison
    private HashSet<TargetIcon> _previousIcons = new HashSet<TargetIcon>();

    // [VI] Mảng chứa kết quả vật lý siêu nhẹ (Giới hạn 30 quái một lúc) 
    // [EN] Ultra-light array for physics results (Limit to 30 monsters at once)
    // [VI] Tăng lên 150 để quét không trượt phát nào!
    // [EN] Increase to 150 to never miss a scan!
    private Collider[] _hitResults = new Collider[150];

    private void Update()
    {
        ScanTargets();
    }

    private void ScanTargets()
    {
        // 1. [VI] Quét vật lý không sinh rác / [EN] Garbage-free physics scan
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, _hitResults, enemyLayer);

        // [VI] Tập hợp các Icon trong tầm ngắm / [EN] Collection of Icons currently in range
        HashSet<TargetIcon> currentIcons = new HashSet<TargetIcon>();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hitResults[i];

            // [VI] BỎ QUÉT TAG! Chỉ cần tìm xem nó có chứa TargetIcon không là đủ.
            // [EN] REMOVED TAG SCAN! Just checking if it contains TargetIcon is enough.
            TargetIcon icon = hit.GetComponentInChildren<TargetIcon>(true);

            if (icon != null)
            {
                currentIcons.Add(icon);

                // [VI] Mới bước vào tầm -> Bật lên
                // [EN] Just entered range -> Turn it on
                if (!_previousIcons.Contains(icon))
                {
                    icon.ShowIcon();
                }
            }
        }

        // 2. [VI] SO SÁNH VÀ DỌN DẸP / [EN] COMPARE AND CLEAN UP
        // [VI] Nếu Icon nằm ở frame trước mà KHÔNG có ở frame này -> Ra khỏi tầm
        // [EN] If Icon was in the previous frame but NOT this one -> Out of range
        foreach (TargetIcon oldIcon in _previousIcons)
        {
            if (oldIcon != null && !currentIcons.Contains(oldIcon))
            {
                oldIcon.HideIcon();
            }
        }

        // 3. [VI] Lưu lại cho frame tiếp theo / [EN] Save for the next frame
        _previousIcons = currentIcons;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }
}