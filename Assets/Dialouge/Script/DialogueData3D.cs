using UnityEngine;

[CreateAssetMenu(fileName = "New3DDialogue", menuName = "Dialogue/3D Dialogue Data")]
public class DialogueData3D : ScriptableObject
{
    [TextArea(3, 5)]
    public string[] lines; // [VI] Các câu thoại sẽ chạy lần lượt / [EN] The lines of dialogue that will play in sequence[cite: 7]

    public float typingSpeed = 0.05f; // [VI] Tốc độ gõ chữ / [EN] Typing speed[cite: 7]
    public float delayBetweenLines = 2f; // [VI] Thời gian chờ trước khi sang câu mới hoặc biến mất / [EN] Wait time before new line or hiding[cite: 7]

    [Header("--- LOCK SETTINGS ---")]
    [Tooltip("Khóa input của người chơi khi đang chạy thoại này? / Lock player input while playing this dialogue?")]
    public bool isLock = false;
}