using UnityEngine;

public class AIRagdollController : MonoBehaviour
{
    private Animator _animator;
    private Rigidbody[] _boneRigidbodies;
    private Collider[] _boneColliders;

    [Header("--- RAGDOLL RECOVERY ---")]
    [Tooltip("Kéo cục xương chậu (Hips/Pelvis) vào đây để làm mốc dời xác")]
    public Transform hipsBone;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _boneRigidbodies = GetComponentsInChildren<Rigidbody>();
        _boneColliders = GetComponentsInChildren<Collider>();

        SetRagdollState(false);
    }

    // Dùng cho cái chết (Health = 0)
    public void EnableRagdoll()
    {
        SetRagdollState(true);
    }

    // [DÙNG CHO HARD STAGGER]: Ngã ngửa văng ra sau
    public void EnableRagdollWithForce(Vector3 force)
    {
        SetRagdollState(true);
        if (hipsBone != null)
        {
            Rigidbody hipsRb = hipsBone.GetComponent<Rigidbody>();
            if (hipsRb != null) hipsRb.AddForce(force, ForceMode.Impulse);
        }
    }

    // [DÙNG KHI TỈNH DẬY]: Dời cái vỏ Root đến chỗ cục xương đang nằm
    public void DisableRagdollAndAlignRoot()
    {
        if (hipsBone != null)
        {
            Vector3 newRootPos = hipsBone.position;
            // Bắn tia Raycast xuống đất để Root không bị lơ lửng hoặc chui xuống gầm
            if (Physics.Raycast(newRootPos + Vector3.up, Vector3.down, out RaycastHit hit, 3f))
            {
                newRootPos.y = hit.point.y;
            }
            transform.position = newRootPos;
        }

        SetRagdollState(false);
    }

    public void SetRagdollState(bool isRagdollActive)
    {
        foreach (Collider col in _boneColliders)
        {
            if (col == null) continue;

            // [VI] SỬA LỖI Ở ĐÂY: KHÔNG tắt Collider nữa! Luôn bật để nhận tia Raycast.
            // [EN] FIX HERE: DO NOT turn off the Collider anymore! Always enable to receive Raycasts.
            col.enabled = true;
        }

        foreach (Rigidbody rb in _boneRigidbodies)
        {
            if (rb == null) continue;

            // [VI] Rigidbody sẽ quyết định: Bật = Rớt tự do (Ragdoll), Tắt = Chạy theo Animation.
            // [EN] Rigidbody decides: On = Free fall (Ragdoll), Off = Follow Animation.
            rb.isKinematic = !isRagdollActive;
            rb.useGravity = isRagdollActive;
        }
    }
}