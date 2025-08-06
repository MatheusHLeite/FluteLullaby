using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player_Manager : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            StartCoroutine(LoadPlayer());
        }
    }

    private IEnumerator LoadPlayer() {
        yield return new WaitForEndOfFrame();
        Singleton.Instance.GameEvents.OnPlayerLoaded?.Invoke();
    }
}
