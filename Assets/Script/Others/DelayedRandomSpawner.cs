using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedRandomSpawner : MonoBehaviour
{
    [Header("--- SPAWNER SETTINGS ---")]
    [Tooltip("Prefab quái/vật phẩm cần tạo / Prefab of monster/item to spawn")]
    public GameObject prefabToSpawn;

    [Tooltip("Danh sách các điểm xuất hiện / List of spawn points")]
    public List<Transform> spawnPoints;

    [Tooltip("Khoảng thời gian chờ giữa 2 lần đẻ / Delay between 2 spawns")]
    public float spawnInterval = 0.5f;

    [Header("--- DEFAULT BEHAVIOR ---")]
    [Tooltip("Số lượng quái sinh ra khi Spawner được bật / Number of monsters spawned when Spawner is enabled")]
    public int defaultSpawnCount = 5;

    // ==========================================
    // [VI] TỰ ĐỘNG CHẠY KHI ĐƯỢC BẬT LÊN
    // [EN] AUTO RUN WHEN ENABLED
    // ==========================================
    private void OnEnable()
    {
        // [VI] Bắt đầu gọi quái ngay lập tức / [EN] Start spawning immediately
        StartCoroutine(SpawnRoutine(defaultSpawnCount));
    }

    private IEnumerator SpawnRoutine(int amountToSpawn)
    {
        for (int i = 0; i < amountToSpawn; i++)
        {
            if (spawnPoints.Count > 0 && prefabToSpawn != null)
            {
                // [VI] Rút thăm ngẫu nhiên 1 vị trí / [EN] Randomly pick 1 position
                Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
                Instantiate(prefabToSpawn, randomPoint.position, randomPoint.rotation);
            }

            // [VI] Nghỉ một nhịp trước khi đẻ con tiếp theo / [EN] Pause a beat before the next spawn
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}