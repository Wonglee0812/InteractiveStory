using UnityEngine;

[CreateAssetMenu(fileName = "New Surface Effect", menuName = "Impact/Surface Effect")]
public class SurfaceEffectData : ScriptableObject
{
    [Header("--- VISUAL EFFECTS ---")]
    [Tooltip("Hiệu ứng tóe lửa, máu, bụi đất...")]
    public GameObject impactVFX;

    [Tooltip("Vết đạn dán lên tường")]
    public GameObject decalPrefab;
}