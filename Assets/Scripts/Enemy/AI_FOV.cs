using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AI_FOV : MonoBehaviour {
    public event UnityAction<ulong> OnFOVEntered;
    public event UnityAction OnFOVExit;

    private List<ulong> m_playersOnFOV;
    private Vector3 m_actualTarget;

    private void Awake() {
        
    }

    private void OnDestroy() {
        
    }

    private void SubscribePlayerOnFOV(ulong playerId) {

    }

    private void UnsubscribePlayerOnFOV(ulong playerId) {

    }

    private void Update() {
        
    }
}
