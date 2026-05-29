using UnityEngine;

public interface IDamageable
{
    // [VI] Nhận sát thương trực tiếp bằng số float / [EN] Take direct float damage
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default);
}