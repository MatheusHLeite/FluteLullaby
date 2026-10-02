using Unity.Netcode;
using UnityEngine;

namespace DelightStudio.Player {
    public class Player_FlaskManager : NetworkBehaviour {
        [Header("Visual")]
        [SerializeField] private GameObject m_healFlask;
        [SerializeField] private ParticleSystem m_healFX;

        private Player_InputHandler input;
        private Player_HealthSystem healthSystem;
        private Player_AnimationSystem animator;

        private float flaskHealthAmount = 40;
        private int flaskAmount;

        public bool IsDrinking { get; private set;}

        private void Awake() {
            input = GetComponent<Player_InputHandler>();
            healthSystem = GetComponent<Player_HealthSystem>();
            animator = GetComponent<Player_AnimationSystem>();

            m_healFlask.SetActive(false);
        }

        public void SetInitialFlaskAmount(bool isOwner) {
            if (!isOwner) return;

            PlayerSaveData data = Singleton.Instance.SaveManager.PlayerData;
            OnFlaskAdded(5); //[TODO] Add to saving system
        }

        private void DrinkFlask() {
            if (flaskAmount <= 0 || IsDrinking || !healthSystem.CanDrinkFlask()) 
                return;

            IsDrinking = true;

            m_healFlask.SetActive(true);
            animator.OnFlaskDrink();
        }

        public void OnFlashDrankEvent() {
            flaskAmount--;
            healthSystem.Heal(flaskHealthAmount);

            UpdateUI();

            if (IsServer)
                PlayHealParticleRpc();            
            else
                RequestPlayHealParticleRpc();            
        }

        [Rpc(SendTo.Server)]
        private void RequestPlayHealParticleRpc() => PlayHealParticleRpc();

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayHealParticleRpc() {
            m_healFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_healFX.Play();
        }

        public void OnEndFlaskDrinkAnimation() {
            IsDrinking = false;

            m_healFlask.SetActive(false); 
        }

        public void OnFlaskAdded(int amount) {
            flaskAmount += amount;
            UpdateUI();
        }

        private void UpdateUI() {
            Singleton.Instance.GameEvents.OnFlaskAmountUpdated?.Invoke(flaskAmount);
        }

        public void Tick(bool isOwner) {
            if (!isOwner || healthSystem.IsDead || GameManager.GetGameState() == GameState.Paused) return;

            if (input.DrinkFlask)
                DrinkFlask();
        }
    }
}
