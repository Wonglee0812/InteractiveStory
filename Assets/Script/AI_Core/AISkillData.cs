using UnityEngine;

public enum SkillArchetype
{
    MeleeLunge,     // Vồ tới cắn
    RangedShoot,    // Bắn đạn/Phun độc
    AoEStomp        // Dậm đất
}

[CreateAssetMenu(fileName = "NewSkillData", menuName = "AI/Skill Data V2")]
public class AISkillData : ScriptableObject
{
    [Header("--- SKILL IDENTITY ---")]
    public string skillName;
    public SkillArchetype skillType = SkillArchetype.MeleeLunge;

    [Header("--- UTILITY SCORING ---")]
    [Tooltip("Khoảng cách xa nhất mà chiêu này có thể với tới")]
    public float maxRange = 20f;

    [Tooltip("Trục X: Khoảng cách chuẩn hóa (0 = 0m, 1 = maxRange). Trục Y: Điểm (0-1)")]
    public AnimationCurve distanceScoreCurve;

    public float cooldown = 5f;

    [Header("--- COMBAT TIMING (GIÂY) ---")]
    public float windUpTime = 1.0f;
    public float executeTime = 0.5f;
    public float recoveryTime = 1.5f;
    [Tooltip("Khoảng cách lướt lố ra sau lưng Player (Ví dụ: 2m-3m)")]
    public float overshootDistance = 2.5f; // <--- THÊM DÒNG NÀY

    [Header("--- MOVEMENT & FOV ---")]
    public float weaponFOV = 60f;

    [Header("--- LUNGE SETTINGS (Dành riêng cho Vồ mồi) ---")]
    public float windUpDip = 0.5f;
    public float jumpArcHeight = 1.5f;
    public float damage = 30f;
    public float knockbackForce = 15f; // <--- THÊM DÒNG NÀY (Lực hất văng)

}