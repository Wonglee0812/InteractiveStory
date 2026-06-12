using System.Collections;
using UnityEngine;
using TMPro;
using Yarn.Unity;
// [VI] Thêm thư viện nếu cần / [EN] Add library if needed

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
        _mainCamera = Camera.main;
        _cachedPlayer = FindAnyObjectByType<Player>();
        if (radarCanvas != null) radarCanvas.enabled = false;
    }

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

    public void PlayYarnNode(string nodeName)
    {
        if (dialogueRunner != null && !dialogueRunner.IsDialogueRunning)
        {
            Debug.Log($"<color=magenta>[EXECUTOR]</color> Đang chạy thoại Yarn: {nodeName}");
            dialogueRunner.StartDialogue(nodeName);
        }
    }

    public void PlayRadarDialogue(DialogueData3D dialogueData)
    {
        if (_isRadarPlaying || dialogueData == null) return;
        StartCoroutine(PlayRadarRoutine(dialogueData));
    }

    private IEnumerator PlayRadarRoutine(DialogueData3D data)
    {
        _isRadarPlaying = true;
        if (radarCanvas != null) radarCanvas.enabled = true;

        if (data.isLock && _cachedPlayer != null)
        {
            _cachedPlayer.SetInputLock(true);
            Debug.Log("<color=red>[RADAR]</color> Đã khóa di chuyển của Player!");
        }

        // ==========================================
        // [VI] XỬ LÝ LẤY DỮ LIỆU ĐA NGÔN NGỮ
        // [EN] MULTI-LANGUAGE DATA FETCHING
        // ==========================================
        foreach (var localizedLine in data.localizedLines)
        {
            if (radarText != null) radarText.text = "";

            // [VI] Tải câu thoại đã dịch bất đồng bộ dựa trên ngôn ngữ hiện tại của game
            // [EN] Load the translated line asynchronously based on the game's current language
            var handle = localizedLine.GetLocalizedStringAsync();
            yield return handle; // [VI] Chờ tải xong / [EN] Wait until loaded

            // [VI] Lấy chuỗi chữ sau khi đã dịch thành công
            // [EN] Get the text string after successful translation
            string translatedText = handle.Result;

            // [VI] Hiệu ứng gõ chữ Typewriter
            // [EN] Typewriter effect
            foreach (char letter in translatedText.ToCharArray())
            {
                if (radarText != null) radarText.text += letter;
                yield return new WaitForSeconds(data.typingSpeed);
            }

            yield return new WaitForSeconds(data.delayBetweenLines);
        }

        if (radarCanvas != null) radarCanvas.enabled = false;
        _isRadarPlaying = false;

        if (data.isLock && _cachedPlayer != null)
        {
            _cachedPlayer.SetInputLock(false);
            Debug.Log("<color=green>[RADAR]</color> Đã mở khóa Player!");
        }
    }
}