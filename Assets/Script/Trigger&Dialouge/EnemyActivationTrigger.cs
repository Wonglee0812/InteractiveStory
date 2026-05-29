using System.Collections.Generic;
using UnityEngine;

public class EnemyActivationTrigger : MonoBehaviour
{
    [Header("--- AMBUSH SETTINGS ---")]
    public List<GameObject> objectsToActivate;
    public bool playOnce = true;
    private bool _hasActivated = false;

    // =========================================================
    // [VI] BIẾN MỚI: Chỉ kích hoạt nếu GameManager cho phép
    // [EN] NEW VARIABLE: Only activate if GameManager allows
    // =========================================================
    [Header("--- STORY CONDITION ---")]
    public bool waitForSignal = false;
    private bool _signalReceived = false;

    private void Start()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(false);
        }

        // [VI] Nếu không cần chờ lệnh, coi như đã có tín hiệu sẵn
        // [EN] If no signal needed, consider it already received
        if (!waitForSignal) _signalReceived = true;
    }

    // [VI] GameManager sẽ gọi hàm này từ Yarn Spinner
    // [EN] GameManager will call this function from Yarn Spinner
    public void ReceiveActivationSignal()
    {
        _signalReceived = true;
        Debug.Log($"<color=cyan>[TRIGGER]</color> {gameObject.name} đã nhận lệnh chiến đấu!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // [VI] CHẶN: Nếu chưa có tín hiệu từ GameManager thì không cho quái hiện ra
            // [EN] BLOCK: If no signal from GameManager, don't spawn monsters
            if (!_signalReceived) return;

            if (playOnce && _hasActivated) return;

            ExecuteActivation();
        }
    }

    private void ExecuteActivation()
    {
        _hasActivated = true;
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(true);
        }

        if (playOnce) Destroy(gameObject, 1f);
    }
}