using UnityEngine;

namespace DelightStudio.Player {
    public class Player_HandAnimatorEventCaller : MonoBehaviour {
        private Player_CombatSystem _combatSystem;
        private Player_InventorySystem _inventorySystem;
        private Player_PauseHandler _pauseHandler;

        private void Awake() {
            _combatSystem = transform.root.GetComponent<Player_CombatSystem>();
            _inventorySystem = transform.root.GetComponent<Player_InventorySystem>();
            _pauseHandler = transform.root.GetComponent<Player_PauseHandler>();
        }

        public void OnDrawAnimationStarted() {
            _inventorySystem.OnDrawAnimationStarted();
        }

        public void OnDrawAnimationEnded() {
            _combatSystem.SetCanSwitch(true);
        }

        public void SetDiaryVisibility() {
            _pauseHandler.SetDiaryVisibility(false);
        }
    }
}