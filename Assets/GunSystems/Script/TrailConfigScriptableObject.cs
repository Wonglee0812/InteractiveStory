using UnityEngine;

[CreateAssetMenu(fileName = "Trail Config", menuName = "Weapons/Gun Trail Config", order = 4)]
public class TrailConfigScriptableObject : ScriptableObject
{
    public Material Materail;
    public AnimationCurve WidthCurve;
    public float Duration = 0.5f;
    public float MinVertexDistance = 0.1f;
    public Gradient Color;

    public float MissDisstance = 100;
    public float StimlationSpeed = 100;
}
