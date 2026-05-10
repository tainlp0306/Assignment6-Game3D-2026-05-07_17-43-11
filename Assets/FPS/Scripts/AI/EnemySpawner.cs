using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    public class EnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct Wave
        {
            [Tooltip("Số lượng enemy spawn trong wave này")]
            public int EnemyCount;

            [Tooltip("Thời gian delay giữa mỗi lần spawn (giây)")]
            public float SpawnInterval;

            [Tooltip("Thời gian chờ trước khi bắt đầu wave này (giây)")]
            public float DelayBeforeWave;
        }

        [Header("Spawn Settings")]
        [Tooltip("Prefab enemy sẽ được spawn")]
        public GameObject EnemyPrefab;

        [Tooltip("Danh sách các điểm spawn (sẽ random)")]
        public Transform[] SpawnPoints;

        [Tooltip("Danh sách các wave")]
        public Wave[] Waves;

        [Tooltip("Tự động bắt đầu wave tiếp theo khi hết enemy")]
        public bool AutoStartNextWave = true;

        [Tooltip("Thời gian chờ giữa các wave khi AutoStart")]
        public float DelayBetweenWaves = 5f;

        [Header("Debug")]
        public bool ShowSpawnPointGizmos = true;

        // State
        int m_CurrentWaveIndex = 0;
        bool m_IsSpawning = false;
        List<GameObject> m_SpawnedEnemies = new List<GameObject>();

        // Event để BossController hoặc script khác lắng nghe
        public System.Action<int> OnWaveStarted;   // wave index
        public System.Action<int> OnWaveCompleted; // wave index

        void Start()
        {
            if (SpawnPoints.Length == 0)
            {
                Debug.LogWarning("EnemySpawner: Chưa có SpawnPoint nào!");
                return;
            }

            if (Waves.Length == 0)
            {
                Debug.LogWarning("EnemySpawner: Chưa có Wave nào!");
                return;
            }

            StartWave(0);
        }

        void Update()
        {
            if (!AutoStartNextWave || m_IsSpawning) return;

            // Kiểm tra nếu hết enemy thì sang wave tiếp
            CleanDeadEnemies();

            if (m_SpawnedEnemies.Count == 0 && m_CurrentWaveIndex < Waves.Length)
            {
                StartWave(m_CurrentWaveIndex);
            }
        }

        // Xóa những enemy đã bị destroy khỏi list
        void CleanDeadEnemies()
        {
            m_SpawnedEnemies.RemoveAll(e => e == null);
        }

        public void StartWave(int waveIndex)
        {
            if (waveIndex >= Waves.Length) return;
            if (m_IsSpawning) return;

            m_CurrentWaveIndex = waveIndex;
            StartCoroutine(SpawnWaveCoroutine(Waves[waveIndex]));
        }

        IEnumerator SpawnWaveCoroutine(Wave wave)
        {
            m_IsSpawning = true;

            // Chờ trước khi bắt đầu wave
            yield return new WaitForSeconds(wave.DelayBeforeWave);

            OnWaveStarted?.Invoke(m_CurrentWaveIndex);

            for (int i = 0; i < wave.EnemyCount; i++)
            {
                SpawnOneEnemy();
                yield return new WaitForSeconds(wave.SpawnInterval);
            }

            m_IsSpawning = false;
            OnWaveCompleted?.Invoke(m_CurrentWaveIndex);
            m_CurrentWaveIndex++;

            // Nếu còn wave và không AutoStart thì dừng
            // AutoStart sẽ tự trigger trong Update()
        }

        void SpawnOneEnemy()
        {
            if (EnemyPrefab == null || SpawnPoints.Length == 0) return;

            // Chọn random spawn point
            Transform spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];

            GameObject enemy = Instantiate(EnemyPrefab, spawnPoint.position, spawnPoint.rotation);
            m_SpawnedEnemies.Add(enemy);
        }

        // Hàm này để BossController gọi khi cần spawn minion
        public List<GameObject> SpawnEnemiesAt(GameObject prefab, Transform[] points, int count)
        {
            List<GameObject> spawned = new List<GameObject>();

            for (int i = 0; i < count; i++)
            {
                Transform point = points[Random.Range(0, points.Length)];
                GameObject enemy = Instantiate(prefab, point.position, point.rotation);
                spawned.Add(enemy);
            }

            return spawned;
        }

        void OnDrawGizmos()
        {
            if (!ShowSpawnPointGizmos || SpawnPoints == null) return;

            Gizmos.color = Color.cyan;
            foreach (var point in SpawnPoints)
            {
                if (point == null) continue;
                Gizmos.DrawWireSphere(point.position, 0.5f);
                Gizmos.DrawLine(point.position, point.position + Vector3.up * 2f);
            }
        }
    }
}