using System.Collections.Generic;
using UnityEngine;

public static class AIUtilityEvaluator
{
    // Hàm này chỉ tính toán và trả về chiêu tốt nhất, không can thiệp vào game
    public static ActiveSkill EvaluateBestSkill(
        List<ActiveSkill> arsenal,
        Transform agentTransform,
        Transform targetTransform,
        ActiveSkill lastSelected,
        float momentumBonus)
    {
        if (arsenal.Count == 0 || targetTransform == null) return null;

        ActiveSkill bestSkill = null;
        float highestScore = -1f;

        // Tính khoảng cách thực tế từ quái đến Player
        float distanceToTarget = Vector3.Distance(agentTransform.position, targetTransform.position);

        foreach (var skill in arsenal)
        {
            if (!skill.IsCooldownReady()) continue; // Cổng lọc Cooldown

            // ==========================================
            // [FIX]: CHUẨN HÓA TRỤC X CỦA ĐỒ THỊ (0.0 ĐẾN 1.0)
            // ==========================================
            // Lấy khoảng cách thực tế chia cho tầm đánh tối đa (maxRange) của chiêu đó.
            // Ví dụ: Đứng cách 5m, maxRange của chiêu Lunge là 10m -> normalizedDistance = 0.5 (Tức là 50% trục X)
            // Mathf.Clamp01 đảm bảo giá trị không bao giờ vượt quá 1 (đề phòng quái đứng quá xa)
            // Mathf.Max(0.1f, ...) dùng để chống lỗi chia cho 0 nếu sếp quên setup maxRange
            float normalizedDistance = Mathf.Clamp01(distanceToTarget / Mathf.Max(0.1f, skill.data.maxRange));

            // Đọc điểm số từ đồ thị tại vị trí % đã tính
            float score = skill.data.distanceScoreCurve.Evaluate(normalizedDistance);

            // Quán tính quyết định (Ưu tiên dùng tiếp chiêu đang dùng nếu điểm bằng nhau)
            if (skill == lastSelected) score += momentumBonus;

            if (score > highestScore)
            {
                highestScore = score;
                bestSkill = skill;
            }
        }

        return bestSkill;
    }
}