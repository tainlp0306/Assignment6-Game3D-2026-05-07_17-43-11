using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.FPS.AI;

namespace Unity.FPS.UI
{
    public class BossPhaseUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Text hiển thị trạng thái boss")]
        public TextMeshProUGUI PhaseStatusText;

        [Tooltip("Text hiển thị phase hiện tại")]
        public TextMeshProUGUI PhaseNumberText;

        [Tooltip("Panel chứa toàn bộ boss UI (để ẩn/hiện)")]
        public GameObject BossUIPanel;

        [Tooltip("Image thanh máu boss")]
        public Image BossHealthBar;

        [Header("Colors")]
        [Tooltip("Màu text khi boss bình thường")]
        public Color NormalColor = Color.white;

        [Tooltip("Màu text khi boss bất tử")]
        public Color InvincibleColor = Color.red;

        [Tooltip("Màu thanh máu bình thường")]
        public Color HealthBarNormalColor = Color.red;

        [Tooltip("Màu thanh máu khi boss bất tử")]
        public Color HealthBarInvincibleColor = Color.gray;

        // Components
        BossController m_BossController;
        Unity.FPS.Game.Health m_BossHealth;

        void Start()
        {
            m_BossController = FindAnyObjectByType<BossController>();

            if (m_BossController == null)
            {
                Debug.LogWarning("[BossPhaseUI] Không tìm thấy BossController trong scene!");
                if (BossUIPanel != null) BossUIPanel.SetActive(false);
                return;
            }

            m_BossHealth = m_BossController.GetComponent<Unity.FPS.Game.Health>();

            // Đăng ký events
            m_BossController.OnPhaseChanged += OnPhaseChanged;
            m_BossController.OnBossInvincible += OnBossInvincible;
            m_BossController.OnBossVulnerable += OnBossVulnerable;

            // Hiển thị UI ban đầu
            if (BossUIPanel != null) BossUIPanel.SetActive(true);
            UpdatePhaseText(0);
            UpdateStatusText(false);
        }

        void Update()
        {
            // Cập nhật thanh máu boss mỗi frame
            if (m_BossHealth != null && BossHealthBar != null)
            {
                BossHealthBar.fillAmount = m_BossHealth.GetRatio();
            }
        }

        void OnPhaseChanged(int newPhase)
        {
            UpdatePhaseText(newPhase);
        }

        void OnBossInvincible()
        {
            UpdateStatusText(true);

            if (BossHealthBar != null)
                BossHealthBar.color = HealthBarInvincibleColor;
        }

        void OnBossVulnerable()
        {
            UpdateStatusText(false);

            if (BossHealthBar != null)
                BossHealthBar.color = HealthBarNormalColor;
        }

        void UpdatePhaseText(int phase)
        {
            if (PhaseNumberText == null) return;

            PhaseNumberText.text = phase == 0
                ? "BOSS"
                : $"BOSS — PHASE {phase}";
        }

        void UpdateStatusText(bool isInvincible)
        {
            if (PhaseStatusText == null) return;

            if (isInvincible)
            {
                PhaseStatusText.text = "⚠ BẤT TỬ — Tiêu diệt minion!";
                PhaseStatusText.color = InvincibleColor;
            }
            else
            {
                PhaseStatusText.text = "Tấn công boss!";
                PhaseStatusText.color = NormalColor;
            }
        }

        void OnDestroy()
        {
            if (m_BossController == null) return;

            m_BossController.OnPhaseChanged -= OnPhaseChanged;
            m_BossController.OnBossInvincible -= OnBossInvincible;
            m_BossController.OnBossVulnerable -= OnBossVulnerable;
        }
    }
}