using System.Collections;
using UnityEngine;
using TMPro;
using Yarn.Unity;

public class DialogueExecutor : MonoBehaviour
{
    [Header("--- YARN SPINNER ---")]
    public DialogueRunner dialogueRunner;

    [Header("--- RADAR 3D DIALOGUE (CUSTOM) ---")]
    [Tooltip("Kéo Canvas của UI Radar vào đây / Drag the Radar Canvas here")]
    public Canvas radarCanvas;

    [Tooltip("Kéo cục TextMeshPro của Radar vào đây / Drag the Radar TextMeshPro here")]
    public TextMeshProUGUI radarText;

    [Tooltip("Kéo cục Pivot để UI xoay theo Camera vào đây / Drag the Pivot here")]
    public Transform radarPivot;

    private Camera _mainCamera;
    private bool _isRadarPlaying = false;
    private Player _cachedPlayer;

    private void Start()
    {
        _mainCamera = Camera.main; // Tự động tìm Camera chính

        // Tìm Player 1 lần lúc vào game để tối ưu hiệu năng (dùng cho việc khóa di chuyển)
        _cachedPlayer = FindAnyObjectByType<Player>();

        if (radarCanvas != null) radarCanvas.enabled = false;
    }

    // [VI] Giữ cho UI Radar luôn xoay mặt về phía Camera (Hiệu ứng Billboard)
    private void LateUpdate()
    {
        if (radarCanvas != null && radarCanvas.enabled && _mainCamera != null && radarPivot != null)
        {
            radarPivot.LookAt(
                radarPivot.position + _mainCamera.transform.rotation * Vector3.forward,
                _mainCamera.transform.rotation * Vector3.up
            );
        }
    }

    // ==========================================
    // 1. LỆNH DÀNH CHO YARN SPINNER (CUTSCENE)
    // ==========================================
    public void PlayYarnNode(string nodeName)
    {
        if (dialogueRunner != null && !dialogueRunner.IsDialogueRunning)
        {
            Debug.Log($"<color=magenta>[EXECUTOR]</color> Đang chạy thoại Yarn: {nodeName}");
            dialogueRunner.StartDialogue(nodeName);
        }
    }

    // ==========================================
    // 2. LỆNH DÀNH CHO RADAR 3D (LẨM NHẨM / BỘ ĐÀM)
    // ==========================================
    public void PlayRadarDialogue(DialogueData3D dialogueData)
    {
        if (_isRadarPlaying || dialogueData == null) return;
        StartCoroutine(PlayRadarRoutine(dialogueData));
    }

    private IEnumerator PlayRadarRoutine(DialogueData3D data)
    {
        _isRadarPlaying = true;
        if (radarCanvas != null) radarCanvas.enabled = true;

        // 1. Khóa Player nếu Data (ScriptableObject) yêu cầu
        if (data.isLock && _cachedPlayer != null)
        {
            _cachedPlayer.SetInputLock(true);
            Debug.Log("<color=red>[RADAR]</color> Đã khóa di chuyển của Player!");
        }

        // 2. Hiệu ứng gõ chữ Typewriter
        foreach (string line in data.lines)
        {
            if (radarText != null) radarText.text = "";

            foreach (char letter in line.ToCharArray())
            {
                if (radarText != null) radarText.text += letter;
                yield return new WaitForSeconds(data.typingSpeed);
            }

            yield return new WaitForSeconds(data.delayBetweenLines);
        }

        // 3. Tắt UI sau khi nói xong
        if (radarCanvas != null) radarCanvas.enabled = false;
        _isRadarPlaying = false;

        // 4. Mở khóa Player
        if (data.isLock && _cachedPlayer != null)
        {
            _cachedPlayer.SetInputLock(false);
            Debug.Log("<color=green>[RADAR]</color> Đã mở khóa Player!");
        }
    }
}