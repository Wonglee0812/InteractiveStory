using UnityEngine;

public class SoundTester : MonoBehaviour
{
    [Header("--- THÔNG SỐ TIẾNG ĐỘNG TỰ ĐỘNG ---")]
    [Tooltip("Bán kính lan truyền của tiếng động (mét)")]
    public float soundRadius = 12f;

    [Tooltip("Cứ sau bao nhiêu giây thì tự phát ra tiếng động 1 lần")]
    public float intervalTime = 5f;

    [Header("--- VÙNG PHÁT TIẾNG ĐỘNG NGẪU NHIÊN ---")]
    [Tooltip("Bán kính vùng ngẫu nhiên xung quanh vị trí của Object này để tạo tiếng động")]
    public float spawnRange = 10f;

    private float _nextSoundTime;

    void Start()
    {
        // Khởi tạo nhịp thời gian phát đầu tiên
        _nextSoundTime = Time.time + intervalTime;
    }

    void Update()
    {
        // TUYỆT ĐỐI KHÔNG DÙNG INPUT GET MOUSE / GET KEY CHỐNG LỖI XUNG ĐỘT INPUT SYSTEM
        if (Time.time >= _nextSoundTime)
        {
            _nextSoundTime = Time.time + intervalTime;
            TriggerAutomatedSound();
        }
    }

    private void TriggerAutomatedSound()
    {
        // Tính toán một tọa độ ngẫu nhiên trên mặt phẳng XZ xung quanh Object này
        Vector2 randomCircle = Random.insideUnitCircle * spawnRange;
        Vector3 soundPosition = new Vector3(
            transform.position.x + randomCircle.x,
            transform.position.y,
            transform.position.z + randomCircle.y
        );

        // Bắn sự kiện âm thanh tĩnh ngầm vào bộ não AI của toàn bầy đàn
        AIAuditorySystem.ReportSound(soundPosition, soundRadius);

        // Vẽ vòng tròn giả lập tia màu đỏ trong Scene view để Designer quan sát cự ly âm thanh
        DrawVisualSoundCircle(soundPosition, soundRadius);

        Debug.Log($"<color=yellow>[AUTO SOUND]</color> Đã tự động kích hoạt tiếng ồn {soundRadius}m tại tọa độ: {soundPosition}");
    }

    // Hàm phụ trợ vẽ vòng tròn bằng Debug.DrawLine để sếp dễ căn cự ly trên Scene
    private void DrawVisualSoundCircle(Vector3 center, float radius)
    {
        int segments = 20;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            // Vẽ đường nối trong 3 giây rồi biến mất
            Debug.DrawLine(prevPoint, nextPoint, Color.red, 3f);
            prevPoint = nextPoint;
        }
    }
}