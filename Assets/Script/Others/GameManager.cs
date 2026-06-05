using UnityEngine;
using TMPro;
using Yarn.Unity;

public class GameManager : MonoBehaviour
{
    // VI: Tạo Singleton để các thành phần khác dễ dàng truy cập
    // EN: Create Singleton for easy access by other components
    public static GameManager Instance { get; private set; }

    [Header("--- YARN SPINNER ---")]
    public DialogueRunner dialogueRunner;

    [Header("--- STORY STATES (Sổ tay cốt truyện) ---")]
    public bool isArmoryCleared = false;

    [Header("--- QUEST UI ---")]
    public GameObject questPanel;
    public TextMeshProUGUI questTextUI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // VI: Ẩn bảng nhiệm vụ lúc bắt đầu game
        // EN: Hide quest panel at the start of the game
        ClearQuest();
    }

    private void OnEnable()
    {
        if (dialogueRunner == null) return;

        // VI: Đăng ký lệnh nhận diện hội thoại từ Yarn Spinner
        // EN: Register dialogue recognition commands from Yarn Spinner
        dialogueRunner.AddCommandHandler<string>("set_quest", SetQuest);
        dialogueRunner.AddCommandHandler("complete_quest", CompleteQuest);
        dialogueRunner.AddCommandHandler("clear_quest", ClearQuest);
    }

    public void SetQuest(string questDescription)
    {
        if (questPanel != null) questPanel.SetActive(true);
        if (questTextUI != null)
        {
            questTextUI.text = "Objective: " + questDescription;
            questTextUI.color = Color.white;
        }
    }

    public void CompleteQuest()
    {
        if (questTextUI != null)
        {
            questTextUI.text = "<s>" + questTextUI.text + "</s>";
            questTextUI.color = Color.green;
        }
    }

    public void ClearQuest()
    {
        if (questPanel != null) questPanel.SetActive(false);
        if (questTextUI != null) questTextUI.text = "";
    }
}