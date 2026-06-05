using UnityEngine;

public class PlayerVisionRaycast : MonoBehaviour
{
    [Header("--- RAYCAST SETTINGS ---")]
    [Tooltip("VI: Camera chính để bắn tia từ giữa màn hình.\nEN: Main camera to shoot the ray from center screen.")]
    public Camera mainCamera;

    [Tooltip("VI: Khoảng cách nhìn tối đa.\nEN: Maximum looking distance.")]
    public float interactDistance = 5f;

    [Tooltip("VI: CHỈ bắn tia vào các vật thể thuộc Layer này (Vd: tạo layer 'Interactable').\nEN: ONLY hit objects in this Layer (e.g., create an 'Interactable' layer).")]
    public LayerMask interactableLayer;

    // VI: Lưu trữ vật thể đang bị nhìn trúng / EN: Store the currently looked-at object
    private FocusInteractable _currentTarget;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        // VI: Bắn 1 tia từ giữa màn hình thẳng tới trước
        // EN: Shoot 1 ray from the center of the screen straight forward
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            // VI: Nếu tia chạm trúng vật có script FocusInteractable
            // EN: If the ray hits an object with the FocusInteractable script
            FocusInteractable interactable = hit.collider.GetComponent<FocusInteractable>();

            if (interactable != null)
            {
                // VI: Nếu vừa nhìn sang một vật MỚI, reset vật CŨ
                // EN: If just looked at a NEW object, reset the OLD object
                if (_currentTarget != interactable)
                {
                    ClearTarget();
                    _currentTarget = interactable;
                }

                // VI: Truyền thời gian cho vật thể để nó tự đếm
                // EN: Pass time to the object so it counts itself
                _currentTarget.IncreaseTimer(Time.deltaTime);
            }
            else
            {
                ClearTarget(); // Bắn trúng tường / Hit a wall
            }
        }
        else
        {
            ClearTarget(); // Bắn vào không khí / Hit thin air
        }
    }

    private void ClearTarget()
    {
        if (_currentTarget != null)
        {
            _currentTarget.ResetTimer();
            _currentTarget = null;
        }
    }
}