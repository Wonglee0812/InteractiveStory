using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using Unity.Cinemachine; // [VI] Bắt buộc dùng namespace này cho Cinemachine 3 / [EN] Mandatory namespace for CM3

public class CutsceneCameraSwitcher : MonoBehaviour
{
    [Header("--- YARN SPINNER ---")]
    public DialogueRunner dialogueRunner;

    [Header("--- CAMERAS ---")]
    public CinemachineVirtualCameraBase playerFollowCamera;

    [Tooltip("Kéo thả tất cả Camera cận mặt của bạn vào đây / Drag all your close-up Cameras here")]
    public List<CinemachineVirtualCameraBase> cutsceneCameras;

    // Từ điển lưu trữ Camera / Dictionary to store Cameras
    private Dictionary<string, CinemachineVirtualCameraBase> _cameraDict;

    private void Awake()
    {
        // [VI] Nạp toàn bộ Camera từ List vào Dictionary khi game bắt đầu
        // [EN] Load all Cameras from the List into the Dictionary when game starts
        _cameraDict = new Dictionary<string, CinemachineVirtualCameraBase>();
        foreach (var cam in cutsceneCameras)
        {
            if (cam != null && !_cameraDict.ContainsKey(cam.gameObject.name))
            {
                _cameraDict.Add(cam.gameObject.name, cam); // Dùng tên Object làm Key / Use Object name as Key
            }
        }
    }

    private void OnEnable()
    {
        // [VI] Đăng ký lệnh cho Yarn Spinner
        // [EN] Register commands for Yarn Spinner
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler<string>("switch_camera", SwitchToCamera);
            dialogueRunner.AddCommandHandler("reset_camera", ResetToPlayerCamera);
        }
    }

    private void OnDisable()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.RemoveCommandHandler("switch_camera");
            dialogueRunner.RemoveCommandHandler("reset_camera");
        }
    }

    // [VI] Hàm hỗ trợ đổi Priority an toàn cho Cinemachine 3
    // [EN] Helper function to safely change Priority for Cinemachine 3
    private void SetCameraPriority(CinemachineVirtualCameraBase cam, int priorityValue)
    {
        if (cam == null) return;
        cam.Priority = priorityValue;

        // Cú pháp đặc thù của Cinemachine 3 / Specific CM3 syntax
        if (cam is CinemachineCamera cm3Cam)
        {
            cm3Cam.Priority.Enabled = (priorityValue > 0);
        }
    }

    // ==========================================
    // CÁC HÀM ĐƯỢC GỌI TỪ YARN SPINNER (COMMANDS)
    // ==========================================

    public void SwitchToCamera(string targetCameraName)
    {
        Debug.Log($"<color=cyan>[CAMERA]</color> Chuyển góc quay sang: {targetCameraName}");

        // 1. Hạ Priority của gameplay camera và các camera khác xuống
        // 1. Lower Priority of gameplay camera and other cameras
        SetCameraPriority(playerFollowCamera, 10);
        foreach (var cam in cutsceneCameras)
        {
            SetCameraPriority(cam, 0);
        }

        // 2. Tìm camera đích trong Dictionary và đẩy Priority lên cao nhất
        // 2. Find target camera in Dictionary and push Priority to max
        if (_cameraDict.TryGetValue(targetCameraName, out CinemachineVirtualCameraBase targetCam))
        {
            SetCameraPriority(targetCam, 50);
            targetCam.Prioritize(); // [VI] Lệnh mồi bắt buộc của CM3 / [EN] Mandatory prime command for CM3
        }
        else
        {
            Debug.LogError($"<color=red>[CAMERA ERROR]</color> Không tìm thấy camera nào tên là: {targetCameraName}");
        }
    }

    public void ResetToPlayerCamera()
    {
        Debug.Log("<color=green>[CAMERA]</color> Trả lại góc nhìn người chơi.");

        foreach (var cam in cutsceneCameras)
        {
            SetCameraPriority(cam, 0);
        }

        if (playerFollowCamera != null)
        {
            SetCameraPriority(playerFollowCamera, 100);
            playerFollowCamera.Prioritize();
        }
    }
}