using UnityEngine;

public class TargetIcon : MonoBehaviour
{
    private Camera _mainCamera;

    private void Awake()
    {
        gameObject.SetActive(false); // [VI] Vẫn ẩn lúc đầu / [EN] Still hidden initially
        _mainCamera = Camera.main;
    }

    public void ShowIcon()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true); // [VI] Chỉ bật lên, KHÔNG chỉnh scale / [EN] Just turn on, NO scale adjustments
        }
    }

    public void HideIcon()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false); // [VI] Tắt ngay lập tức / [EN] Turn off immediately
        }
    }

    private void LateUpdate()
    {
        // [VI] Logic Biển quảng cáo giữ nguyên / [EN] Billboard logic remains the same
        if (_mainCamera != null && gameObject.activeSelf)
        {
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                             _mainCamera.transform.rotation * Vector3.up);
        }
    }
}