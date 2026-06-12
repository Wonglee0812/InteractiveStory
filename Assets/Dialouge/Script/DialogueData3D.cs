using UnityEngine;
using UnityEngine.Localization; // [VI] Thêm thư viện Localization / [EN] Add Localization library

[CreateAssetMenu(fileName = "New3DDialogue", menuName = "Dialogue/3D Dialogue Data")]
public class DialogueData3D : ScriptableObject
{
    [Header("--- LOCALIZATION DATA ---")]
    // [VI] Thay thế string[] cũ bằng LocalizedString[]
    // [EN] Replace the old string[] with LocalizedString[]
    public LocalizedString[] localizedLines;

    public float typingSpeed = 0.05f;
    public float delayBetweenLines = 2f;

    [Header("--- LOCK SETTINGS ---")]
    [Tooltip("Khóa input của người chơi khi đang chạy thoại này? / Lock player input while playing this dialogue?")]
    public bool isLock = false;
}