using System;
using UnityEngine;

public static class AIAuditorySystem
{
    public static event Action<Vector3, float> OnSoundReported;

    public static void ReportSound(Vector3 position, float radius)
    {
        OnSoundReported?.Invoke(position, radius);
    }
}