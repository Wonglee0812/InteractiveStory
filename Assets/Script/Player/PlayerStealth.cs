using UnityEngine;

// Tự động yêu cầu GameObject phải có PlayerCharater
[RequireComponent(typeof(PlayerCharater))]
public class PlayerStealth : MonoBehaviour
{
    private PlayerCharater _player;
    private int _grassCount = 0;

    // CHỈ TÀNG HÌNH KHI: Đứng trong cỏ VÀ Stance đang là Crouch (Ngồi)
    public bool IsHidingInGrass => _grassCount > 0 && _player != null && _player.CurrentStance == Stance.Crouch;

    private void Awake()
    {
        // Tự động lấy tham chiếu không cần kéo thả
        _player = GetComponent<PlayerCharater>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StealthGrass"))
        {
            _grassCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("StealthGrass"))
        {
            _grassCount--;
            if (_grassCount < 0) _grassCount = 0;
        }
    }
}