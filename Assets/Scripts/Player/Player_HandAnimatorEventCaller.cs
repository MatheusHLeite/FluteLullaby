using UnityEngine;

namespace DelightStudio.Player {
    public class Player_HandAnimatorEventCaller : MonoBehaviour {
        private Player_CombatSystem _combatSystem;

        private void Awake() {
            _combatSystem = transform.root.GetComponent<Player_CombatSystem>();
        }

        
    }
}