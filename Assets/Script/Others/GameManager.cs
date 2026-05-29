using UnityEngine;
using TMPro;
using Yarn.Unity;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Tạo Singleton để các thành phần khác dễ dàng truy cập
    public static GameManager Instance { get; private set; }

    [Header("--- YARN SPINNER ---")]
    public DialogueRunner dialogueRunner;

    [Header("--- STORY STATES (Sổ tay cốt truyện) ---")]
    public bool isArmoryCleared = false; // Biến nhớ trạng thái phòng Armory

    [Header("--- QUEST UI ---")]
    public GameObject questPanel;
    public TextMeshProUGUI questTextUI;

    [Header("--- BONUS SUPPORT ---")]
    public List<EnemyActivationTrigger> zenithSupportTriggers = new List<EnemyActivationTrigger>();

    [Header("--- EXECUTION SETUP ---")]
    public Animator playerAnimator;

    [Tooltip("Kéo cấu trúc AIHealth của con quái vật đóng vai trò tướng/boss vào đây")]
    public AIHealth generalBoss; // Chuẩn hóa từ FortressBoss cũ sang AIHealth của hệ thống mới

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ClearQuest(); // Ẩn bảng nhiệm vụ lúc bắt đầu game
    }

    private void OnEnable()
    {
        if (dialogueRunner == null) return;

        // Đăng ký lệnh nhận diện hội thoại từ Yarn Spinner
        dialogueRunner.AddCommandHandler<string>("set_quest", SetQuest);
        dialogueRunner.AddCommandHandler("complete_quest", CompleteQuest);
        dialogueRunner.AddCommandHandler("clear_quest", ClearQuest);
        dialogueRunner.AddCommandHandler("execute_general", ExecuteGeneralDecision);
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

    // ===============================================
    // QUYẾT ĐỊNH CỐT TRUYỆN (Gọi từ Yarn)
    // ===============================================
    private void ExecuteGeneralDecision()
    {
        if (playerAnimator != null) playerAnimator.SetTrigger("ShootExecution");

        // Cập nhật hàm gây sát thương kết liễu theo cấu trúc số thực float mới của hệ thống súng
        if (generalBoss != null)
        {
            generalBoss.TakeDamage(9999f, generalBoss.transform.position);
        }
    }
}