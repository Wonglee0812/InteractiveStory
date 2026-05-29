using UnityEngine;

[CreateAssetMenu(fileName = "NewTacticalSettings", menuName = "AI/Tactical Settings")]
public class AITacticalSettings : ScriptableObject
{
    [Header("--- THỜI GIAN (GIÂY) ---")]
    public float windUpTime = 1.5f;       // Thời gian khựng lại giật mình
    public float searchDuration = 10f;    // Thời gian hùng hục sục sạo khi mất dấu
    public float scanDuration = 4f;       // Thời gian rà quét bụi cỏ
    public float overwatchTime = 5f;      // Thời gian đứng lườm yểm trợ
    public float tauntDuration = 2f;      // Thời gian gầm gừ chờ đợi
    public float patrolRestTime = 3f; // Thời gian nghỉ và nhìn quanh tại mỗi điểm Waypoint


    [Header("--- DI CHUYỂN & VẬN ĐỘNG ---")]
    public float circleRadius = 8f;       // Bán kính đi vòng cung vây ráp
    public float turnSpeed = 4f;          // Tốc độ vặn người
    public float prowlSpeed = 2.5f;       // Tốc độ đi bộ dạo quanh gầm gừ
    public float approachDistance = 3f;   // Khoảng cách tiếp cận an toàn

    [Header("--- CHIẾN ĐẤU (COMBAT) ---")]
    public float combatTurnSpeed = 8f;    // Tốc độ vặn người lúc vung đòn
    public float meleeDuration = 2f;      // Thời gian thực thi đòn cận chiến
    public float rangedDuration = 2.5f;   // Thời gian thực thi đòn đánh xa
}