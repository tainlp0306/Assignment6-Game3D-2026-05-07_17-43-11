using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(EnemyController), typeof(Health))]
    public class BossController : MonoBehaviour
    {
        [Header("Minion Settings")]
        [Tooltip("Prefab HoverBot sẽ được triệu hồi")]
        public GameObject MinionPrefab;

        [Tooltip("Các điểm spawn minion xung quanh boss")]
        public Transform[] MinionSpawnPoints;

        [Tooltip("Số lượng minion triệu hồi mỗi phase (nên để 3)")]
        public int MinionsPerPhase = 3;

        [Header("VFX / SFX")]
        [Tooltip("VFX khi boss chuyển phase (optional)")]
        public GameObject PhaseTransitionVfx;

        [Tooltip("SFX khi boss chuyển phase (optional)")]
        public AudioClip PhaseTransitionSfx;

        [Header("Debug")]
        public bool ShowDebugLog = true;

        // Components
        Health m_Health;

        // Phase state
        int m_CurrentPhase = 0;
        bool m_IsInPhaseTransition = false;

        // Ngưỡng máu trigger phase: 2/3 và 1/3
        float m_Phase1Threshold; // 66% hp
        float m_Phase2Threshold; // 33% hp

        // Minion đang sống
        List<GameObject> m_ActiveMinions = new List<GameObject>();

        // Events cho UI
        public System.Action<int> OnPhaseChanged;
        public System.Action OnBossVulnerable;
        public System.Action OnBossInvincible;

        void Start()
        {
            m_Health = GetComponent<Health>();

            m_Phase1Threshold = m_Health.MaxHealth * 2f / 3f;
            m_Phase2Threshold = m_Health.MaxHealth * 1f / 3f;

            m_Health.OnDamaged += OnBossDamaged;

            if (ShowDebugLog)
                Debug.Log($"[Boss] Start. MaxHP={m_Health.MaxHealth} | " +
                          $"Phase1 tại {m_Phase1Threshold:F0}hp | Phase2 tại {m_Phase2Threshold:F0}hp");
        }

        void OnBossDamaged(float damage, GameObject source)
        {
            if (m_IsInPhaseTransition) return;

            float hp = m_Health.CurrentHealth;

            if (m_CurrentPhase == 0 && hp <= m_Phase1Threshold)
            {
                StartCoroutine(PhaseTransitionCoroutine(1));
            }
            else if (m_CurrentPhase == 1 && hp <= m_Phase2Threshold)
            {
                StartCoroutine(PhaseTransitionCoroutine(2));
            }
        }

        IEnumerator PhaseTransitionCoroutine(int newPhase)
        {
            m_IsInPhaseTransition = true;
            m_CurrentPhase = newPhase;

            if (ShowDebugLog)
                Debug.Log($"[Boss] === Phase {newPhase} bắt đầu! Boss bất tử ===");

            // Bước 1: Bật bất tử
            m_Health.Invincible = true;
            OnBossInvincible?.Invoke();
            OnPhaseChanged?.Invoke(newPhase);

            // Bước 2: Spawn VFX/SFX
            if (PhaseTransitionVfx != null)
            {
                var vfx = Instantiate(PhaseTransitionVfx, transform.position, Quaternion.identity);
                Destroy(vfx, 5f);
            }

            if (PhaseTransitionSfx != null)
                AudioUtility.CreateSFX(PhaseTransitionSfx, transform.position,
                    AudioUtility.AudioGroups.EnemyDetection, 1f);

            // Bước 3: Triệu hồi minion
            yield return new WaitForSeconds(0.5f);
            SpawnMinions();

            if (ShowDebugLog)
                Debug.Log($"[Boss] Đã triệu hồi {m_ActiveMinions.Count} minion. Hãy tiêu diệt chúng!");

            // Bước 4: Chờ cho đến khi tất cả minion chết
            yield return StartCoroutine(WaitForAllMinionsDead());

            // Bước 5: Tắt bất tử
            m_Health.Invincible = false;
            m_IsInPhaseTransition = false;
            OnBossVulnerable?.Invoke();

            if (ShowDebugLog)
                Debug.Log($"[Boss] Tất cả minion đã chết! Boss có thể nhận damage.");
        }

        void SpawnMinions()
        {
            m_ActiveMinions.Clear();

            if (MinionPrefab == null)
            {
                Debug.LogWarning("[Boss] Chưa gán MinionPrefab!");
                return;
            }

            if (MinionSpawnPoints == null || MinionSpawnPoints.Length == 0)
            {
                Debug.LogWarning("[Boss] Chưa có MinionSpawnPoints!");
                return;
            }

            for (int i = 0; i < MinionsPerPhase; i++)
            {
                // Chọn spawn point tuần tự để không chồng nhau
                Transform spawnPoint = MinionSpawnPoints[i % MinionSpawnPoints.Length];
                GameObject minion = Instantiate(MinionPrefab, spawnPoint.position, spawnPoint.rotation);
                m_ActiveMinions.Add(minion);

                // Lắng nghe khi minion chết
                Health minionHealth = minion.GetComponent<Health>();
                if (minionHealth != null)
                    minionHealth.OnDie += () => OnMinionDied(minion);
            }
        }

        void OnMinionDied(GameObject minion)
        {
            m_ActiveMinions.Remove(minion);

            if (ShowDebugLog)
                Debug.Log($"[Boss] Minion chết. Còn lại: {m_ActiveMinions.Count}");
        }

        IEnumerator WaitForAllMinionsDead()
        {
            // Chờ cho đến khi list minion rỗng
            // (OnMinionDied sẽ remove dần)
            while (m_ActiveMinions.Count > 0)
            {
                // Dọn null phòng trường hợp bị Destroy bất thường
                m_ActiveMinions.RemoveAll(m => m == null);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}